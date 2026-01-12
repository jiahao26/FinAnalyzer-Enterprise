using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
using FinAnalyzer.Core.Models;

namespace FinAnalyzer.Engine.Services
{
    /// <summary>
    /// Orchestrates document ingestion: PDF → Chunk → Embed → Store.
    /// </summary>
    public class IngestionService
    {
        private readonly IFileLoader _fileLoader;
        private readonly TextChunker _chunker;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDbService _vectorDbService;

        private const string CollectionName = "finance_docs";

        public IngestionService(
            IFileLoader fileLoader,
            TextChunker chunker,
            IEmbeddingService embeddingService,
            IVectorDbService vectorDbService)
        {
            _fileLoader = fileLoader ?? throw new ArgumentNullException(nameof(fileLoader));
            _chunker = chunker ?? throw new ArgumentNullException(nameof(chunker));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _vectorDbService = vectorDbService ?? throw new ArgumentNullException(nameof(vectorDbService));
        }

        /// <summary>
        /// Ingest a document through the full pipeline.
        /// </summary>
        /// <param name="filePath">Path to the PDF file.</param>
        /// <param name="progress">Optional progress reporter (0-100).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task IngestAsync(
            string filePath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            var fileName = System.IO.Path.GetFileName(filePath);
            var allChunks = new List<DocumentChunk>();

            progress?.Report(5);

            // Phase 1: Load PDF pages (0-20%)
            var pages = new List<PageContent>();
            await foreach (var page in _fileLoader.LoadAsync(filePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                pages.Add(page);
            }

            if (pages.Count == 0)
                throw new InvalidOperationException("No text content extracted from PDF.");

            progress?.Report(20);

            // Phase 2: Chunk pages (20-40%)
            int processedPages = 0;
            foreach (var page in pages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chunks = _chunker.Chunk(page, fileName);
                allChunks.AddRange(chunks);

                processedPages++;
                int chunkProgress = 20 + (int)(20.0 * processedPages / pages.Count);
                progress?.Report(chunkProgress);
            }

            if (allChunks.Count == 0)
                throw new InvalidOperationException("No chunks generated from document.");

            progress?.Report(40);

            // Phase 3: Generate embeddings (40-80%)
            int processedChunks = 0;
            foreach (var chunk in allChunks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Text);
                chunk.Vector = embedding;

                processedChunks++;
                int embedProgress = 40 + (int)(40.0 * processedChunks / allChunks.Count);
                progress?.Report(embedProgress);
            }

            progress?.Report(80);

            // Phase 4: Store in Qdrant (80-100%)
            await _vectorDbService.UpsertAsync(CollectionName, allChunks);

            progress?.Report(100);
        }
    }
}
