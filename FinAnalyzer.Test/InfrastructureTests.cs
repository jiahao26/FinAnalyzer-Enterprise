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
            // /api/tags is a lightweight endpoint to check if Ollama is up
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

            // Act
            var response = await _client.PostAsync(url, content);

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Reranker at {url} is not reachable. Status: {response.StatusCode}");
        }
    }
}
