using System;
using System.Collections.Generic;
using System.Threading;

namespace FinAnalyzer.Core.Interfaces
{
    /// <summary>
    /// Contract for RAG operations: ingestion and query.
    /// </summary>
    public interface IRagService
    {
        /// <summary>
        /// Ingest a document into the vector database.
        /// </summary>
        /// <param name="filePath">Path to the document file.</param>
        /// <param name="progress">Progress reporter (0-100).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task IngestDocumentAsync(
            string filePath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Query the RAG system with a question.
        /// </summary>
        /// <param name="question">User's question.</param>
        /// <returns>Streaming answer tokens.</returns>
        IAsyncEnumerable<string> QueryAsync(string question);
    }
}

