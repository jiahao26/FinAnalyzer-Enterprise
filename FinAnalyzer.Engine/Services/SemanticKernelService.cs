using System;
using System.Text;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
using System.IO;
using Microsoft.SemanticKernel;

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
        
        private const string CollectionName = "finance_docs";

        public SemanticKernelService(IVectorDbService vectorDb, IRerankerService reranker, Kernel kernel)
        {
            _vectorDb = vectorDb;
            _reranker = reranker;
            _kernel = kernel;
        }

        public Task IngestDocumentAsync(string filePath)
        {
            throw new NotImplementedException("Use the Ingestion Pipeline components directly for now.");
        }

        /// <summary>
        /// Execute full RAG query pipeline.
        /// </summary>
        /// <param name="question">User's question.</param>
        /// <returns>The LLM's generated answer based on retrieved context (streamed).</returns>
        public async IAsyncEnumerable<string> QueryAsync(string question)
        {
            // Step 1: Retrieval (Hybrid/Vector Search). Execute search.
            // Fetch top 25 candidates to provide sufficient data for reranker.
            var searchResults = await _vectorDb.SearchAsync(CollectionName, question, limit: 25);

            // Step 2: Reranking (Precision Filtering). Apply precision filtering.
            // Sort candidates by semantic relevance. Retain top 5.
            var topResults = await _reranker.RerankAsync(question, searchResults, topN: 5);


            // Step 3: Context Construction. Build context.
            // Construct prompt context string
            var contextBuilder = new StringBuilder();
            
            // Apply token budget and window safety using character approximation (~4 chars per token)
            // Reserve 4k for context and 4k for prompt+response (Llama 3 8k context).
            const int MaxChars = 12000; 
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
