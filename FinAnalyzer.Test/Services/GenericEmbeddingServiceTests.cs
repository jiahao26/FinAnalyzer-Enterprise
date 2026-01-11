using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FinAnalyzer.Core.Configuration;
using FinAnalyzer.Engine.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinAnalyzer.Test.Services
{
    public class GenericEmbeddingServiceTests
    {
        private readonly IOptions<AISettings> _options;

        public GenericEmbeddingServiceTests()
        {
            _options = Options.Create(new AISettings 
            { 
                BackendType = "ollama",
                EmbeddingEndpoint = "http://localhost:11434",
                EmbeddingModelId = "nomic-embed-text"
            });
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_ShouldReturnVector_WhenApiSucceeds()
        {
            // Arrange
            var mockVector = Enumerable.Range(0, 384).Select(i => (float)i / 384).ToArray();
            var response = new { embedding = mockVector };
            var jsonResponse = JsonSerializer.Serialize(response);

            var handler = new MockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
            var httpClient = new HttpClient(handler);
            var service = new GenericEmbeddingService(httpClient, _options);

            // Act
            var result = await service.GenerateEmbeddingAsync("test text");

            // Assert
            Assert.Equal(384, result.Length);
            Assert.Equal(mockVector, result.ToArray());
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_ShouldThrowException_WhenApiFails()
        {
            // Arrange
            var handler = new MockHttpMessageHandler("Error", HttpStatusCode.InternalServerError);
            var httpClient = new HttpClient(handler);
            var service = new GenericEmbeddingService(httpClient, _options);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(
                async () => await service.GenerateEmbeddingAsync("test")
            );
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_ShouldSendCorrectPayload()
        {
            // Arrange
            MockHttpMessageHandler handler = null;
            handler = new MockHttpMessageHandler("{\"embedding\": []}", HttpStatusCode.OK, 
                request =>
                {
                    // Verify the request
                    Assert.Equal(HttpMethod.Post, request.Method);
                    Assert.Contains("embed", request.RequestUri.ToString());
                    return true;
                });
            
            var httpClient = new HttpClient(handler);
            var service = new GenericEmbeddingService(httpClient, _options);

            // Act
            await service.GenerateEmbeddingAsync("financial report analysis");

            // Assert - handled in callback
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_ShouldHandleEmptyResponse()
        {
            // Arrange
            var response = new { embedding = Array.Empty<float>() };
            var jsonResponse = JsonSerializer.Serialize(response);

            var handler = new MockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
            var httpClient = new HttpClient(handler);
            var service = new GenericEmbeddingService(httpClient, _options);

            // Act
            var result = await service.GenerateEmbeddingAsync("test");

            // Assert
            Assert.Empty(result.ToArray());
        }

        public class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _response;
            private readonly HttpStatusCode _statusCode;
            private readonly Func<HttpRequestMessage, bool> _requestValidator;

            public MockHttpMessageHandler(string response, HttpStatusCode statusCode, Func<HttpRequestMessage, bool> requestValidator = null)
            {
                _response = response;
                _statusCode = statusCode;
                _requestValidator = requestValidator;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _requestValidator?.Invoke(request);

                if (_statusCode != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(_statusCode)
                    {
                        Content = new StringContent(_response)
                    };
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(_response, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
