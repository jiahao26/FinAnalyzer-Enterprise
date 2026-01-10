using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
using FinAnalyzer.Core.Models;
using FinAnalyzer.Engine.Services;
using Microsoft.SemanticKernel;
using NSubstitute;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace FinAnalyzer.Test
{
    public class OrchestrationTests
    {
        [Fact]
        public async Task QueryAsync_ShouldInvokePipelineCorrectly()
        {
            // Arrange
            var mockVectorDb = Substitute.For<IVectorDbService>();
            var mockReranker = Substitute.For<IRerankerService>();
            var builder = Kernel.CreateBuilder();
            var kernel = builder.Build();
            
            var service = new SemanticKernelService(mockVectorDb, mockReranker, kernel);
            
            var question = "What is the revenue?";
            var dummyResults = new List<SearchResult> { 
                new SearchResult { Text = "Revenue was 1B", Score = 0.8f }, 
                new SearchResult { Text = "Cost was 0.5B", Score = 0.7f } 
            };
            var rerankedResults = new List<SearchResult> { dummyResults[0] };

            mockVectorDb.SearchAsync(Arg.Any<string>(), question, Arg.Any<int>()).Returns(dummyResults);
            mockReranker.RerankAsync(question, dummyResults, Arg.Any<int>()).Returns(rerankedResults);

            // Act
            try {
                await service.QueryAsync(question);
            } catch (KernelException) {
                
            }

            // Assert
            await mockVectorDb.Received(1).SearchAsync(Arg.Any<string>(), question, Arg.Is<int>(x => x >= 5));
            await mockReranker.Received(1).RerankAsync(question, dummyResults, Arg.Is<int>(x => x == 5));
        }
    }
}
