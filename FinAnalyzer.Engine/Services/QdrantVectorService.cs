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
            CentralLogger.Info("Initializing QdrantVectorService...");
            
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            
            var settings = options?.Value ?? new QdrantSettings();
            
            // Log configuration values for debugging
            CentralLogger.Debug($"Raw settings - Host: '{settings.Host}', Port: {settings.Port}, VectorSize: {settings.VectorSize}");
            
            // Apply defensive defaults for any missing/invalid values
            var host = !string.IsNullOrWhiteSpace(settings.Host) ? settings.Host.Trim() : "localhost";
            var port = settings.Port > 0 ? settings.Port : 6334;
            _vectorSize = settings.VectorSize > 0 ? settings.VectorSize : 768;
            
            CentralLogger.Info($"Using Qdrant config - Host: '{host}', Port: {port}, VectorSize: {_vectorSize}");
            
            try
            {
                _client = client ?? new QdrantClient(host, port);
                CentralLogger.Info("QdrantClient created successfully");
            }
            catch (Exception ex)
            {
                CentralLogger.Error($"Failed to create QdrantClient with host='{host}', port={port}", ex);
                throw;
            }
        }

        /// <summary>
        /// Upsert batch of document chunks into specific collection.
        /// Create collection if non-existent.
        /// </summary>
        public async Task UpsertAsync(string collectionName, IEnumerable<DocumentChunk> chunks)
        {
            CentralLogger.Info($"UpsertAsync called for collection '{collectionName}'");
            
            var collections = await _client.ListCollectionsAsync();
            CentralLogger.Debug($"Existing collections: [{string.Join(", ", collections)}]");
            
            if (!collections.Contains(collectionName))
            {
                CentralLogger.Info($"Creating new collection '{collectionName}' with vector size {_vectorSize}");
                await _client.CreateCollectionAsync(collectionName, new VectorParams { Size = (ulong)_vectorSize, Distance = Distance.Cosine });
            }

            var points = new List<PointStruct>();
            foreach (var chunk in chunks)
            {
                if (chunk.Vector.IsEmpty)
                {
                    CentralLogger.Error($"Chunk {chunk.Id} has no embedding vector");
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

            CentralLogger.Info($"Upserting {points.Count} points to '{collectionName}'");
            
            if (points.Any())
            {
                await _client.UpsertAsync(collectionName, points);
                CentralLogger.Info($"Successfully upserted {points.Count} points");
            }
        }

        /// <summary>
        /// Perform semantic search using cosine similarity.
        /// </summary>
        public async Task<IEnumerable<SearchResult>> SearchAsync(string collectionName, string query, int limit = 10)
        {
            CentralLogger.Info($"SearchAsync called - collection: '{collectionName}', query: '{query.Substring(0, Math.Min(50, query.Length))}...', limit: {limit}");
            
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(query);
            CentralLogger.Debug($"Generated query embedding with {queryVector.Length} dimensions");

            var results = await _client.SearchAsync(
                collectionName: collectionName,
                vector: queryVector.ToArray(),
                limit: (ulong)limit
            );

            CentralLogger.Info($"Search returned {results.Count()} results");

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
            CentralLogger.Info($"DeleteCollectionAsync called for '{collectionName}'");
            
            var collections = await _client.ListCollectionsAsync();
            if (collections.Contains(collectionName))
            {
                await _client.DeleteCollectionAsync(collectionName);
                CentralLogger.Info($"Collection '{collectionName}' deleted");
            }
            else
            {
                CentralLogger.Warn($"Collection '{collectionName}' not found, nothing to delete");
            }
        }
    }
}
