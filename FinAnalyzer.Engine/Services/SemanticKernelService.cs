using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
using System.IO;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace FinAnalyzer.Engine.Services
{
    /// <summary>
    /// Orchestrate RAG flow: Retrieval -> Reranking -> Generation.
    /// </summary>
    public class SemanticKernelService : IRagService
    {
        private readonly IVectorDbService _vectorDb;
        private readonly IRerankerService _reranker;
        private readonly Kernel _kernel;
        private readonly IngestionService? _ingestionService;
        
        private const string CollectionName = "finance_docs";

        public SemanticKernelService(
            IVectorDbService vectorDb,
            IRerankerService reranker,
            Kernel kernel,
            IngestionService? ingestionService = null)
        {
            _vectorDb = vectorDb;
            _reranker = reranker;
            _kernel = kernel;
            _ingestionService = ingestionService;
            
            CentralLogger.Info("SemanticKernelService initialized");
        }

        public async Task IngestDocumentAsync(
            string filePath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (_ingestionService == null)
            {
                CentralLogger.Error("IngestionService not configured");
                throw new InvalidOperationException("IngestionService not configured.");
            }

            CentralLogger.Step("DOCUMENT INGESTION START", filePath);
            await _ingestionService.IngestAsync(filePath, progress, cancellationToken);
        }

        private async Task WarmupModelAsync()
        {
            try
            {
                CentralLogger.Info("Warming up LLM model...");
                // Send a tiny prompt to trigger model loading early if needed
                await foreach (var _ in _kernel.InvokePromptStreamingAsync("Hi", new KernelArguments()))
                {
                    break; // Just need to trigger the start
                }
                CentralLogger.Info("LLM Warmup successful");
            }
            catch (Exception ex)
            {
                CentralLogger.Warn($"LLM Warmup failed (expected if model is still pulling/loading): {ex.Message}");
            }
        }

        /// <summary>
        /// Execute full RAG query pipeline.
        /// </summary>
        /// <param name="question">User's question.</param>
        /// <returns>The LLM's generated answer based on retrieved context (streamed).</returns>
        public async IAsyncEnumerable<string> QueryAsync(string question)
        {
            CentralLogger.Step("RAG QUERY START", $"Question: {question}");
            var startTime = DateTime.Now;

            // Optional: Trigger warmup but don't block if it's the very first time
            // In a real app, this might be called on app startup instead.
            // await WarmupModelAsync(); 

            // Step 1: Retrieval (Hybrid/Vector Search). Execute search.
            CentralLogger.Step("Step 1: Vector Retrieval", $"Collection: {CollectionName}");
            // Reduced limit from 25 to 10 to prevent HTTP 413 (Payload Too Large) in Reranker
            var searchResults = await _vectorDb.SearchAsync(CollectionName, question, limit: 10);
            CentralLogger.Info($"Retrieved {searchResults.Count()} results from vector search");

            // Step 2: Reranking (Precision Filtering). Apply precision filtering.
            // Sort candidates by semantic relevance. Retain top 5.
            CentralLogger.Step("Step 2: Reranking", $"Filtering {searchResults.Count()} results to top 5");
            IEnumerable<FinAnalyzer.Core.Models.SearchResult> topResults;
            try 
            {
                topResults = await _reranker.RerankAsync(question, searchResults, topN: 5);
                CentralLogger.Info($"Reranking complete - {topResults.Count()} results retained");
            }
            catch (Exception ex)
            {
                 // Graceful degradation: If reranker fails (e.g., 413 or timeout), use raw vector results
                 CentralLogger.Warn($"Reranker failed, falling back to top 5 vector results: {ex.Message}");
                 topResults = searchResults.Take(5);
            }


            // Step 3: Context Construction. Build context.
            CentralLogger.Step("Step 3: Context Construction", "Building prompt context");
            // Construct prompt context string
            var contextBuilder = new StringBuilder();
            
            // Apply token budget and window safety using character approximation (~4 chars per token)
            // Reduced to 6000 chars (~1500 tokens) to better support CPU-only Ollama inference 
            // and prevent HTTP timeouts during Prompt Processing.
            const int MaxChars = 6000; 
            int currentLength = 0;

            foreach (var item in topResults)
            {
                var entry = $"Source: {item.SourceFileName} (Page {item.PageNumber})\nContent: {item.Text}\n---\n";
                
                if (currentLength + entry.Length > MaxChars)
                {
                    if (currentLength > 0) break;
                }

                contextBuilder.Append(entry);
                currentLength += entry.Length;
            }

            CentralLogger.Debug($"Context built: {currentLength} chars from {topResults.Count()} sources");

            // Load prompt from file (Prototype: use direct file read; Phase 5: use Dependency Injection via IPromptProvider)
            string promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "FinancialAnalysis.txt");
            string promptTemplate = "Answer based on context: {{$Context}} \n Question: {{$Question}}"; // Set fallback template

            if (File.Exists(promptPath))
            {
                promptTemplate = await File.ReadAllTextAsync(promptPath);
                CentralLogger.Debug($"Loaded prompt template from {promptPath}");
            }
            else
            {
                CentralLogger.Warn($"Prompt template not found at {promptPath}, using fallback");
            }

            var arguments = new KernelArguments()
            {
                ["Context"] = contextBuilder.ToString(),
                ["Question"] = question
            };

            // Step 4: Generation (Streaming). Generate response.
            CentralLogger.Step("Step 4: LLM Generation", "Streaming response from Semantic Kernel");
            var skResult = _kernel.InvokePromptStreamingAsync(promptTemplate, arguments);

            int tokenCount = 0;
            await foreach (var message in skResult)
            {
                tokenCount++;
                yield return message.ToString();
            }

            var elapsed = DateTime.Now - startTime;
            CentralLogger.Step("RAG QUERY COMPLETE", $"Generated {tokenCount} tokens in {elapsed.TotalSeconds:F1}s");
        }
    }
}
