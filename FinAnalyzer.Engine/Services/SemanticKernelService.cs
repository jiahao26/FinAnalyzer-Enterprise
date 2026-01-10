using System;
using System.Text;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
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
            // Build prompt including retrieved text chunks for LLM to answer from.
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("You are a specialized financial analyst helper.");
            contextBuilder.AppendLine("Answer the question using ONLY the provided context below.");
            contextBuilder.AppendLine("If the answer isn't in the context, say 'I don't have enough information'.");
            contextBuilder.AppendLine("Cite your sources by referring to the filename and page number provided.");
            contextBuilder.AppendLine("\n--- CONTEXT START ---");

            foreach (var item in topResults)
            {
                contextBuilder.AppendLine($"Source: {item.SourceFileName} (Page {item.PageNumber})");
                contextBuilder.AppendLine($"Content: {item.Text}");
                contextBuilder.AppendLine("---");
            }
            contextBuilder.AppendLine("--- CONTEXT END ---\n");
            
            contextBuilder.AppendLine($"Question: {question}");
            contextBuilder.AppendLine("Answer:");

            // Step 4: Generation
            // Send constructed prompt to LLM (via Semantic Kernel) to get final answer.
            var skResult = await _kernel.InvokePromptAsync(contextBuilder.ToString());

            return skResult.GetValue<string>() ?? string.Empty;
        }
    }
}
