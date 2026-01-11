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
    /// Provides embedding generation using a local Ollama instance.
    /// </summary>
    public class OllamaEmbeddingService : IEmbeddingService, IModelLifecycle
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelName;
        private readonly string _baseUrl;

        public OllamaEmbeddingService(HttpClient httpClient, IOptions<OllamaSettings> options)
        {
            _httpClient = httpClient;
            var settings = options.Value;
            _baseUrl = settings.BaseUrl;
            _modelName = settings.ModelName;
        }

        /// <summary>
        /// Generates a vector embedding for the provided text.
        /// </summary>
        /// <param name="text">Input text to embed.</param>
        /// <returns>A read-only memory block containing the floating-point vector.</returns>
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
        /// Sends a dummy request to ensure the model is loaded into memory (Warm-Up).
        /// </summary>
        public async Task WarmUpAsync()
        {
            // Tip: Send dummy request ("warmup") to force Ollama to load model into RAM.
            // Prevents first user query from being slow.
            try 
            {
                await GenerateEmbeddingAsync("warmup");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Ollama WarmUp failed: {ex.Message}");
            }
        }

        private class OllamaEmbeddingResponse
        {
            public float[]? embedding { get; set; }
        }
    }
}
