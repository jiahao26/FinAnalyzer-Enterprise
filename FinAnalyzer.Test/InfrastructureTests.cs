using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;
using System.Text;

namespace FinAnalyzer.Test
{
    public class InfrastructureTests
    {
        private readonly HttpClient _client;

        public InfrastructureTests()
        {
            _client = new HttpClient();
        }

        [Fact]
        public async Task Qdrant_ShouldBeReachable()
        {
            // Arrange
            var url = "http://localhost:6333/collections";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Qdrant at {url} is not reachable. Status: {response.StatusCode}");
        }

        [Fact]
        public async Task Ollama_ShouldBeReachable()
        {
            // Arrange
            // Use /api/tags to check health.
            var url = "http://localhost:11434/api/tags";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Ollama at {url} is not reachable. Status: {response.StatusCode}");
        }

        [Fact]
        public async Task Reranker_ShouldBeReachable()
        {
            // Arrange
            var url = "http://localhost:8080/rerank";
            var payload = new { query = "test", texts = new[] { "test" } };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
 
            // Act & Assert: Retry logic.
            // Reranker takes time to warm up. Poll until ready.
            var isConnected = await WaitForServiceAsync(async () => 
            {
                try
                {
                    // Re-create content for each attempt as it might be disposed.
                    var currentContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var response = await _client.PostAsync(url, currentContent);
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }, TimeSpan.FromSeconds(300)); // Wait up to 5 minutes for slow model loading.

            Assert.True(isConnected, $"Reranker at {url} did not become reachable within the timeout period.");
        }

        // Helper for polling services
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
