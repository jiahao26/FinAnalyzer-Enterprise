using System;
using System.Text;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
using System.IO;
using Microsoft.SemanticKernel;

namespace FinAnalyzer.Engine.Services
{
    /// <summary>
    /// Orchestrates the RAG flow: Retrieval -> Reranking -> Generation.
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
        /// Executes a full RAG query pipeline.
        /// </summary>
        /// <param name="question">User's question.</param>
        /// <returns>The LLM's generated answer based on retrieved context.</returns>
        public async Task<string> QueryAsync(string question)
        {
            // Step 1: Retrieval (Hybrid/Vector Search)
            // Fetch more candidates (top 25) to give reranker enough data to work with.
            var searchResults = await _vectorDb.SearchAsync(CollectionName, question, limit: 25);

            // Step 2: Reranking (Precision Filtering)
            // Reranker sorts candidates by semantic relevance. Keep top 5.
            var topResults = await _reranker.RerankAsync(question, searchResults, topN: 5);


            // Step 3: Context Construction
            // Build prompt context string
            var contextBuilder = new StringBuilder();
            foreach (var item in topResults)
            {
                contextBuilder.AppendLine($"Source: {item.SourceFileName} (Page {item.PageNumber})");
                contextBuilder.AppendLine($"Content: {item.Text}");
                contextBuilder.AppendLine("---");
            }

            // Load prompt from file (Prototype: direct file read, Phase 5: Dependency Injection via IPromptProvider)
            string promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "FinancialAnalysis.txt");
            string promptTemplate = "Answer based on context: {{$Context}} \n Question: {{$Question}}"; // Fallback

            if (File.Exists(promptPath))
            {
                promptTemplate = await File.ReadAllTextAsync(promptPath);
            }

            var arguments = new KernelArguments()
            {
                ["Context"] = contextBuilder.ToString(),
                ["Question"] = question
            };

            // Step 4: Generation
            var skResult = await _kernel.InvokePromptAsync(promptTemplate, arguments);

            return skResult.GetValue<string>() ?? string.Empty;
        }
    }
}
