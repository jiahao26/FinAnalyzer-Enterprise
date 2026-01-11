using System.Net;
using System.Text;
using System.Text.Json;
using FinAnalyzer.Core.Configuration;
using FinAnalyzer.Core.Models;
using FinAnalyzer.Engine.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinAnalyzer.Test.Services
{
    public class TeiRerankerTests
    {
        private readonly IOptions<TeiSettings> _options;
        private const string BaseUrl = "http://localhost:8080";

        public TeiRerankerTests()
        {
            _options = Options.Create(new TeiSettings { BaseUrl = BaseUrl });
        }

        [Fact]
        public async Task RerankAsync_ShouldReturnEmpty_WhenInputIsEmpty()
        {
            var handler = new MockHttpMessageHandler("", HttpStatusCode.OK);
            var client = new HttpClient(handler);
            var service = new TeiRerankerService(client, _options);

            var result = await service.RerankAsync("query", Enumerable.Empty<SearchResult>());

            Assert.Empty(result);
        }

        [Fact]
        public async Task RerankAsync_ShouldReturnRerankedResults_WhenApiSucceeds()
        {
            // Arrange
            var searchResults = new List<SearchResult>
            {
                new SearchResult { Id = "1", Text = "Doc 1" },
                new SearchResult { Id = "2", Text = "Doc 2" }
            };

            // Mock response: Doc 2 (index 1) has higher score than Doc 1 (index 0)
            var responsePayload = new List<object> 
            {
                new { index = 1, score = 0.99 },
                new { index = 0, score = 0.5 }
            };
            var jsonResponse = JsonSerializer.Serialize(responsePayload);

            var handler = new MockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
            var client = new HttpClient(handler);
            var service = new TeiRerankerService(client, _options);

            // Act
            var result = (await service.RerankAsync("query", searchResults)).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("2", result[0].Id); // Higher score first
            Assert.Equal(0.99f, result[0].Score, 0.01);
            Assert.Equal("1", result[1].Id);
        }

        [Fact]
        public async Task RerankAsync_ShouldHandleMissingIndices_Gracefully()
        {
             // Arrange
            var searchResults = new List<SearchResult>
            {
                new SearchResult { Id = "1", Text = "Doc 1" }
            };

            // Mock response with invalid index
            var responsePayload = new List<object> 
            {
                new { index = 99, score = 0.99 }, // Invalid index
                new { index = 0, score = 0.5 }    // Valid
            };
            var jsonResponse = JsonSerializer.Serialize(responsePayload);

            var handler = new MockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
            var client = new HttpClient(handler);
            var service = new TeiRerankerService(client, _options);

            // Act
            var result = (await service.RerankAsync("query", searchResults)).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].Id);
        }

         public class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _response;
            private readonly HttpStatusCode _statusCode;

            public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
            {
                _response = response;
                _statusCode = statusCode;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return new HttpResponseMessage
                {
                    StatusCode = _statusCode,
                    Content = new StringContent(_response, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
