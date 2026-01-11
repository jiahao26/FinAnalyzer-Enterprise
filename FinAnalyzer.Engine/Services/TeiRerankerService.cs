using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
using FinAnalyzer.Core.Models;
using Microsoft.Extensions.Options;
using FinAnalyzer.Core.Configuration;

namespace FinAnalyzer.Engine.Services
{
    /// <summary>
    /// Service for reranking search results using a Text Embeddings Inference (TEI) server.
    /// Improves result relevance using a cross-encoder model.
    /// </summary>
    public class TeiRerankerService : IRerankerService, IModelLifecycle
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public TeiRerankerService(HttpClient httpClient, IOptions<TeiSettings> options)
        {
            _httpClient = httpClient;
            _baseUrl = options.Value.BaseUrl;
        }

        /// <summary>
        /// Reranks a list of initial search results based on their relevance to the query.
        /// </summary>
        /// <param name="query">The original search query.</param>
        /// <param name="results">The initial candidate results from vector search.</param>
        /// <param name="topN">Number of top results to return after reranking.</param>
        /// <returns>Top N results sorted by relevance score.</returns>
        public async Task<IEnumerable<SearchResult>> RerankAsync(string query, IEnumerable<SearchResult> results, int topN = 5)
        {
            var resultsList = results.ToList();
            if (!resultsList.Any())
            {
                return Enumerable.Empty<SearchResult>();
            }

            var payload = new
            {
                query = query,
                texts = resultsList.Select(r => r.Text).ToArray()
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/rerank", content);
            response.EnsureSuccessStatusCode();

            // TEI server returns JSON list of scores and indices.
            var responseString = await response.Content.ReadAsStringAsync();
            var rerankResponses = JsonSerializer.Deserialize<List<TeiRerankResponse>>(responseString);

            if (rerankResponses == null)
            {
                 throw new InvalidOperationException("Failed to rerank: null response.");
            }

            var rerankedResults = new List<SearchResult>();
            foreach (var rr in rerankResponses)
            {
                // Map TEI response back to original DocumentChunks using index.
                if (rr.Index >= 0 && rr.Index < resultsList.Count)
                {
                    var original = resultsList[rr.Index];
                    rerankedResults.Add(new SearchResult
                    {
                        Id = original.Id,
                        Text = original.Text,
                        SourceFileName = original.SourceFileName,
                        PageNumber = original.PageNumber,
                        Score = (float)rr.Score,
                        Metadata = original.Metadata
                    });
                }
            }

            return rerankedResults.OrderByDescending(r => r.Score).Take(topN);
        }

        public async Task WarmUpAsync()
        {
             try
             {
                 var payload = new { query = "warmup", texts = new[] { "warmup" } };
                 var jsonPayload = JsonSerializer.Serialize(payload);
                 var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                 await _httpClient.PostAsync($"{_baseUrl}/rerank", content);
             }
             catch(Exception ex)
             {
                 Console.WriteLine($"[Warning] TEI WarmUp failed: {ex.Message}");
             }
        }

        private class TeiRerankResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("index")]
            public int Index { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("score")]
            public double Score { get; set; }
        }
    }
}
