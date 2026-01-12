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
    /// Service for reranking search results via Text Embeddings Inference (TEI) server.
    /// </summary>
    public class TeiRerankerService : IRerankerService, IModelLifecycle
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public TeiRerankerService(HttpClient httpClient, IOptions<TeiSettings> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            var settings = options?.Value ?? new TeiSettings();
            _baseUrl = !string.IsNullOrWhiteSpace(settings.BaseUrl) 
                ? settings.BaseUrl 
                : "http://localhost:8080";
        }

        /// <summary>
        /// Rerank list of initial search results based on query relevance.
        /// </summary>
        public async Task<IEnumerable<SearchResult>> RerankAsync(string query, IEnumerable<SearchResult> results, int topN = 5)
        {
            var resultsList = results.ToList();
            if (!resultsList.Any())
            {
                return Enumerable.Empty<SearchResult>();
            }

            var payload = new
            {
                query = query.Length > 500 ? query.Substring(0, 500) : query,
                texts = resultsList.Select(r => 
                {
                    // Aggressive truncation to 1000 chars (approx 250 tokens)
                    // The ms-marco-MiniLM-L-6-v2 model has a 512 token HARD limit.
                    // 250 (text) + 125 (query) + special tokens < 512.
                    return r.Text.Length > 1000 ? r.Text.Substring(0, 1000) : r.Text;
                }).ToArray()
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/rerank", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var rerankResponses = JsonSerializer.Deserialize<List<TeiRerankResponse>>(responseString);

            if (rerankResponses == null)
            {
                 throw new InvalidOperationException("Failed to rerank: null response.");
            }

            var rerankedResults = new List<SearchResult>();
            foreach (var rr in rerankResponses)
            {
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
