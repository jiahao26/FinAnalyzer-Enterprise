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
        }

        public async Task IngestDocumentAsync(
            string filePath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (_ingestionService == null)
                throw new InvalidOperationException("IngestionService not configured.");

            await _ingestionService.IngestAsync(filePath, progress, cancellationToken);
        }

        /// <summary>
        /// Execute full RAG query pipeline.
        /// </summary>
        /// <param name="question">User's question.</param>
        /// <returns>The LLM's generated answer based on retrieved context (streamed).</returns>
        public async IAsyncEnumerable<string> QueryAsync(string question)
        {
            // Step 1: Retrieval (Hybrid/Vector Search). Execute search.
            // Reduced limit from 25 to 10 to prevent HTTP 413 (Payload Too Large) in Reranker
            var searchResults = await _vectorDb.SearchAsync(CollectionName, question, limit: 10);

            // Step 2: Reranking (Precision Filtering). Apply precision filtering.
            // Sort candidates by semantic relevance. Retain top 5.
            IEnumerable<FinAnalyzer.Core.Models.SearchResult> topResults;
            try 
            {
                topResults = await _reranker.RerankAsync(question, searchResults, topN: 5);
            }
            catch (Exception ex)
            {
                 // Graceful degradation: If reranker fails (e.g., 413 or timeout), use raw vector results
                 // Log error if possible, but for now just fallback
                 topResults = searchResults.Take(5);
            }


            // Step 3: Context Construction. Build context.
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

            // Load prompt from file (Prototype: use direct file read; Phase 5: use Dependency Injection via IPromptProvider)
            string promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "FinancialAnalysis.txt");
            string promptTemplate = "Answer based on context: {{$Context}} \n Question: {{$Question}}"; // Set fallback template

            if (File.Exists(promptPath))
            {
                promptTemplate = await File.ReadAllTextAsync(promptPath);
            }

            var arguments = new KernelArguments()
            {
                ["Context"] = contextBuilder.ToString(),
                ["Question"] = question
            };

            // Step 4: Generation (Streaming). Generate response.
            var skResult = _kernel.InvokePromptStreamingAsync(promptTemplate, arguments);

            await foreach (var message in skResult)
            {
                yield return message.ToString();
            }
        }
    }
}
