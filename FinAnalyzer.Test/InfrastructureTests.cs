using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using FinAnalyzer.Core.Configuration;

namespace FinAnalyzer.Test
{
    public class InfrastructureTests
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _config;

        public InfrastructureTests()
        {
            _client = new HttpClient();
            _config = FinAnalyzer.Engine.Configuration.ConfigurationLoader.Load("appsettings.json");
        }

        [Fact]
        public async Task Qdrant_ShouldBeReachable()
        {
            // Arrange
            var settings = _config.GetSection("Qdrant").Get<QdrantSettings>();
            var url = $"http://{settings.Host}:{settings.HttpPort}/collections";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Qdrant at {url} is not reachable. Status: {response.StatusCode}");
        }

        [Fact]
        public async Task Ollama_ShouldBeReachable()
        {
            // Arrange: Check health via /api/tags.
            var settings = _config.GetSection("AIServices").Get<AISettings>();
            var url = $"{settings.EmbeddingEndpoint}/api/tags";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Ollama at {url} is not reachable. Status: {response.StatusCode}");
        }

        [Fact]
        public async Task Reranker_ShouldBeReachable()
        {
            // Arrange
            var settings = _config.GetSection("Tei").Get<TeiSettings>();
            var url = $"{settings.BaseUrl}/rerank";
            var payload = new { query = "test", texts = new[] { "test" } };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
 
            // Act & Assert: Retry logic.
            // Poll until ready (Reranker warmup).
            var isConnected = await WaitForServiceAsync(async () => 
            {
                try
                {
                    // Re-create content for each attempt to avoid disposal issues.
                    var currentContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var response = await _client.PostAsync(url, currentContent);
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }, TimeSpan.FromSeconds(300)); // Wait up to 5 minutes.

            Assert.True(isConnected, $"Reranker at {url} did not become reachable within the timeout period.");
        }

        // Helper for polling services.
        private async Task<bool> WaitForServiceAsync(Func<Task<bool>> healthCheck, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < timeout)
            {
                if (await healthCheck())
                {
                    return true;
                }
                await Task.Delay(2000); // Wait 2s before retry.
            }
            return false;
        }
    }
}
