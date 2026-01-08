using System;
using System.Collections.Generic;
using System.Text;

namespace FinAnalyzer.Core.Interfaces
{
    public interface IRagService
    {
        // TASK 1: Ingestion
        Task IngestDocumentAsync(string docId, string textContent);

        // TASK 2: Search (Rough Filter)
        Task<List<string>> SearchAsync(string query);

        // TASK 3: The 3-Stage Pipeline (Replacing 'AnalyzeAsync')
        // Retiever -> Reranker -> Generator
        Task<string> ExecutePipelineAsync(string query);
    }
}
