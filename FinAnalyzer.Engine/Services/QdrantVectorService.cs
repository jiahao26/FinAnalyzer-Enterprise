using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
using FinAnalyzer.Core.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace FinAnalyzer.Engine.Services
{
    /// <summary>
    /// Service for interacting with the Qdrant vector database.
    /// Handles storage and retrieval of document chunks and their embeddings.
    /// </summary>
    public class QdrantVectorService : IVectorDbService
    {
        private readonly QdrantClient _client;
        
        // Note: nomic-embed-text-v1.5 model always outputs 768 dimensions.
        // If switching models (e.g. to mxbai-embed-large), MUST update this value.
        private const int VectorSize = 768; 

        private readonly IEmbeddingService _embeddingService;

        public QdrantVectorService(IEmbeddingService embeddingService, string host = "localhost", int port = 6334)
        {
            _client = new QdrantClient(host, port);
            _embeddingService = embeddingService;
        }

        /// <summary>
        /// UPSERTs a batch of document chunks into a specific collection.
        /// Creates the collection if it does not exist.
        /// </summary>
        /// <param name="collectionName">Target Qdrant collection.</param>
        /// <param name="chunks">List of chunks to insert.</param>
        public async Task UpsertAsync(string collectionName, IEnumerable<DocumentChunk> chunks)
        {
            var collections = await _client.ListCollectionsAsync();
            if (!collections.Contains(collectionName))
            {
                await _client.CreateCollectionAsync(collectionName, new VectorParams { Size = VectorSize, Distance = Distance.Cosine });
            }

            var points = new List<PointStruct>();
            foreach (var chunk in chunks)
            {
                // Validation: Chunks must have embeddings before saving.
                if (chunk.Vector.IsEmpty)
                {
                    throw new ArgumentException($"Chunk {chunk.Id} has no embedding vector.");
                }
                
                if (chunk.Vector.Length != VectorSize)
                {
                     if (chunk.Vector.IsEmpty) throw new ArgumentException($"Chunk {chunk.Id} has no embedding vector.");
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
                
                // Tip: Storing metadata in payload allows filtering search results later
                // (e.g. "only show results from 2024").
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
        /// Performs a semantic search using cosine similarity.
        /// </summary>
        /// <param name="collectionName">Collection to search.</param>
        /// <param name="query">User's natural language query.</param>
        /// <param name="limit">Max number of results to return.</param>
        /// <returns>A list of search results sorted by relevance score.</returns>
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
    }
}
