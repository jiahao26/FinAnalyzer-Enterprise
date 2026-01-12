using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
using FinAnalyzer.Core.Models;
using FinAnalyzer.Core.Configuration;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.Options;

namespace FinAnalyzer.Engine.Services
{
    /// <summary>
    /// Service for interacting with Qdrant vector database.
    /// Handle storage and retrieval of document chunks and embeddings.
    /// </summary>
    public class QdrantVectorService : IVectorDbService
    {
        private readonly QdrantClient _client;
        private readonly int _vectorSize;
        private readonly IEmbeddingService _embeddingService;

        public QdrantVectorService(IEmbeddingService embeddingService, IOptions<QdrantSettings> options, QdrantClient? client = null)
        {
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            
            var settings = options?.Value ?? new QdrantSettings();
            
            // Apply defensive defaults for any missing/invalid values
            var host = !string.IsNullOrWhiteSpace(settings.Host) ? settings.Host : "localhost";
            var port = settings.Port > 0 ? settings.Port : 6334;
            _vectorSize = settings.VectorSize > 0 ? settings.VectorSize : 768;
            
            _client = client ?? new QdrantClient(host, port);
        }

        /// <summary>
        /// Upsert batch of document chunks into specific collection.
        /// Create collection if non-existent.
        /// </summary>
        public async Task UpsertAsync(string collectionName, IEnumerable<DocumentChunk> chunks)
        {
            var collections = await _client.ListCollectionsAsync();
            if (!collections.Contains(collectionName))
            {
                await _client.CreateCollectionAsync(collectionName, new VectorParams { Size = (ulong)_vectorSize, Distance = Distance.Cosine });
            }

            var points = new List<PointStruct>();
            foreach (var chunk in chunks)
            {
                if (chunk.Vector.IsEmpty)
                {
                    throw new ArgumentException($"Chunk {chunk.Id} has no embedding vector.");
                }

                var point = new PointStruct
                {
                    Id = Guid.Parse(chunk.Id),
                    Vectors = chunk.Vector.ToArray(),
                    Payload = {
                        ["text"] = chunk.Text,
                        ["source"] = chunk.SourceFileName,
                        ["page"] = (int)chunk.PageNumber
                    }
                };
                
                foreach(var kvp in chunk.Metadata)
                {
                   point.Payload[kvp.Key] = new Qdrant.Client.Grpc.Value { StringValue = kvp.Value.ToString() };
                }

                points.Add(point);
            }

            if (points.Any())
            {
                await _client.UpsertAsync(collectionName, points);
            }
        }

        /// <summary>
        /// Perform semantic search using cosine similarity.
        /// </summary>
        public async Task<IEnumerable<SearchResult>> SearchAsync(string collectionName, string query, int limit = 10)
        {
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(query);

            var results = await _client.SearchAsync(
                collectionName: collectionName,
                vector: queryVector.ToArray(),
                limit: (ulong)limit
            );

            var searchResults = new List<SearchResult>();
            foreach(var r in results)
            {
                var dict = new Dictionary<string, object>();
                foreach(var payloadItem in r.Payload)
                {
                    dict[payloadItem.Key] = payloadItem.Value.ToString();
                }

                searchResults.Add(new SearchResult
                {
                    Id = r.Id.Uuid.ToString(),
                    Text = r.Payload["text"].StringValue,
                    SourceFileName = r.Payload["source"].StringValue,
                    PageNumber = (int)r.Payload["page"].IntegerValue,
                    Score = r.Score,
                    Metadata = dict
                });
            }

            return searchResults;
        }


        /// <summary>
        /// Delete an entire collection.
        /// </summary>
        public async Task DeleteCollectionAsync(string collectionName)
        {
            var collections = await _client.ListCollectionsAsync();
            if (collections.Contains(collectionName))
            {
                await _client.DeleteCollectionAsync(collectionName);
            }
        }
    }
}
