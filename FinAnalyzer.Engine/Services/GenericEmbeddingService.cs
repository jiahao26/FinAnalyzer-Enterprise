using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
using Microsoft.Extensions.Options;
using FinAnalyzer.Core.Configuration;

namespace FinAnalyzer.Engine.Services
{
    /// <summary>
    /// Provide embedding generation using configurable backend (defaulting to Ollama).
    /// </summary>
    public class GenericEmbeddingService : IEmbeddingService, IModelLifecycle
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelName;
        private readonly string _baseUrl;

        public GenericEmbeddingService(HttpClient httpClient, IOptions<AISettings> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            var settings = options?.Value ?? new AISettings();
            _baseUrl = !string.IsNullOrWhiteSpace(settings.EmbeddingEndpoint) 
                ? settings.EmbeddingEndpoint 
                : "http://localhost:11434";
            _modelName = !string.IsNullOrWhiteSpace(settings.EmbeddingModelId)
                ? settings.EmbeddingModelId
                : "nomic-embed-text";
        }

        /// <summary>
        /// Generate vector embedding for input text.
        /// </summary>
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

        /// <summary>
        /// Send dummy request to ensure model is loaded into memory (Warm-Up).
        /// </summary>
        public async Task WarmUpAsync()
        {
            try 
            {
                await GenerateEmbeddingAsync("warmup");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Embedding Service WarmUp failed: {ex.Message}");
            }
        }

        private class OllamaEmbeddingResponse
        {
            public float[]? embedding { get; set; }
        }
    }
}
