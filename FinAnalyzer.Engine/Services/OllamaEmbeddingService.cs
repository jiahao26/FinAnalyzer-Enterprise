using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;

namespace FinAnalyzer.Engine.Services
{
    public class OllamaEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelName;
        private readonly string _baseUrl;

        public OllamaEmbeddingService(HttpClient httpClient, string baseUrl = "http://localhost:11434", string modelName = "nomic-embed-text")
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl;
            _modelName = modelName;
        }

        public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
        {
            var payload = new
            {
                model = _modelName,
                prompt = text
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/embeddings", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseString);
            
            if (result?.embedding == null)
            {
                 throw new InvalidOperationException("Failed to generate embedding: null response.");
            }

            return new ReadOnlyMemory<float>(result.embedding);
        }

        // Internal class for deserialization
        private class OllamaEmbeddingResponse
        {
            public float[]? embedding { get; set; }
        }
    }
}
