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
    public class QdrantVectorService : IVectorDbService
    {
        private readonly QdrantClient _client;
        private const int VectorSize = 768; // nomic-embed-text-v1.5 uses 768 dimensions

        public QdrantVectorService(string host = "localhost", int port = 6334)
        {
            _client = new QdrantClient(host, port);
        }

        public async Task UpsertAsync(string collectionName, IEnumerable<DocumentChunk> chunks)
        {
            // Ensure collection exists
            var collections = await _client.ListCollectionsAsync();
            if (!collections.Contains(collectionName))
            {
                await _client.CreateCollectionAsync(collectionName, new VectorParams { Size = VectorSize, Distance = Distance.Cosine });
            }

            var points = new List<PointStruct>();
            foreach (var chunk in chunks)
            {
                // Ensure chunk has a vector
                if (chunk.Vector.IsEmpty)
                {
                    // In a production scenario, we might generate it here on demand,
                    // but for this pipeline, we expect the vector to be pre-populated.
                    throw new ArgumentException($"Chunk {chunk.Id} has no embedding vector.");
                }
                
                if (chunk.Vector.Length != VectorSize)
                {
                    // This could be a guard clause, or we assume caller ensures size. 
                    // To be safe, let's complain if it's empty.
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
                
                // Add explicit metadata
                foreach(var kvp in chunk.Metadata)
                {
                   // simplistic handling of basic types
                   point.Payload[kvp.Key] = kvp.Value.ToString();
                }

                points.Add(point);
            }

            if (points.Any())
            {
                await _client.UpsertAsync(collectionName, points);
            }
        }

        public async Task<IEnumerable<SearchResult>> SearchAsync(string collectionName, string query, int limit = 10)
        {
            // TODO: Phase 3 - Implement Retrieval Logic (Convert query to vector -> Search)
            return Enumerable.Empty<SearchResult>();
        }
    }
}
