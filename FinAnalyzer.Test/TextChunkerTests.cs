using System.Collections.Generic;
using System.Linq;
using FinAnalyzer.Core.Models;
using FinAnalyzer.Engine.Services;
using Xunit;

namespace FinAnalyzer.Test
{
    public class TextChunkerTests
    {
        [Fact]

        public void Chunk_ShouldSplitLargeParagraphs()
        {
            // Arrange
            // Create text exceeding window size (50 tokens).
            // Approximation: 4 chars per token -> 50 tokens ~= 200 chars.
            // Use 50 repetitions of "TestWord " (9 chars) -> 450 chars.
            var duplicateWord = "TestWord "; 
            var largeText = string.Concat(Enumerable.Repeat(duplicateWord, 50)); 
            
            var chunker = new TextChunker(windowSize: 50, overlap: 10);
            var page = new PageContent { Text = largeText, PageNumber = 1 };

            // Act
            var chunks = chunker.Chunk(page, "test.pdf").ToList();

            // Assert
            Assert.True(chunks.Count > 1, "Should split into multiple chunks because input exceeds window token limit");
            
            // Verify each chunk respects token limit.
            foreach(var chunk in chunks)
            {
               if(chunk.Metadata.TryGetValue("TokenCount", out object tokenCountObj) && tokenCountObj is int tokenCount)
               {
                   Assert.True(tokenCount <= 50, $"Chunk token count {tokenCount} exceeds window size 50");
               }
            }
        }

        [Fact]
        public void Chunk_ShouldRespectSeparators()
        {
            // Arrange
            // Paragraphs separated by double newline
            var p1 = "This is paragraph one.";
            var p2 = "This is paragraph two.";
            var text = $"{p1}\n\n{p2}";

            var chunker = new TextChunker(windowSize: 100, overlap: 0);
            var page = new PageContent { Text = text, PageNumber = 1 };

            // Act
            var chunks = chunker.Chunk(page, "test.pdf").ToList();

            // Assert
            // Verify content remains intact if it fits window.
            Assert.Single(chunks);
            Assert.Equal(text, chunks[0].Text);
        }

        [Fact]
        public void Chunk_ShouldSplitOnSeparator_WhenTooLarge()
        {
            // Arrange
            var p1 = new string('A', 60); // 60 chars
            var p2 = new string('B', 60); // 60 chars
            var text = $"{p1}\n\n{p2}"; // 122 chars total

            // Window 20 tokens. Wraps one paragraph (15 tokens) but not two. Should split on \n\n.
            var chunker = new TextChunker(windowSize: 20, overlap: 0);
            var page = new PageContent { Text = text, PageNumber = 1 };

            // Act
            var chunks = chunker.Chunk(page, "test.pdf").ToList();

            // Assert
            Assert.Equal(2, chunks.Count);
            Assert.Contains(p1, chunks.Select(c => c.Text));
            Assert.Contains(p2, chunks.Select(c => c.Text));
        }
        
        [Fact]
        public void Chunk_ShouldHandleEmptyText()
        {
            var chunker = new TextChunker();
            var page = new PageContent { Text = "", PageNumber = 1 };
            var chunks = chunker.Chunk(page, "test.pdf").ToList();
            Assert.Empty(chunks);
        }

        [Fact]
        public void Chunk_ShouldHandleWhitespaceOnlyText()
        {
            // Arrange
            var chunker = new TextChunker();
            var page = new PageContent { Text = "   \n\n  \t  ", PageNumber = 1 };

            // Act
            var chunks = chunker.Chunk(page, "test.pdf").ToList();

            // Assert
            Assert.Empty(chunks);
        }

        [Fact]
        public void Chunk_ShouldGenerateDeterministicIds()
        {
            // Arrange
            var chunker = new TextChunker(windowSize: 500);
            var page = new PageContent { Text = "Test content", PageNumber = 1 };

            // Act - Call twice with same input
            var chunks1 = chunker.Chunk(page, "test.pdf").ToList();
            var chunks2 = chunker.Chunk(page, "test.pdf").ToList();

            // Assert - IDs should be identical for same input
            Assert.Equal(chunks1[0].Id, chunks2[0].Id);
        }

        [Fact]
        public void Chunk_ShouldIncludeMetadata()
        {
            // Arrange
            var chunker = new TextChunker(windowSize: 100);
            var page = new PageContent { Text = "Financial analysis report", PageNumber = 5 };

            // Act
            var chunks = chunker.Chunk(page, "annual_report.pdf").ToList();

            // Assert
            Assert.Single(chunks);
            Assert.NotNull(chunks[0].Metadata);
            Assert.True(chunks[0].Metadata.ContainsKey("TokenCount"));
            Assert.Equal("RecursiveTokenSplit", chunks[0].Metadata["Method"]);
            Assert.Equal("annual_report.pdf", chunks[0].SourceFileName);
            Assert.Equal(5, chunks[0].PageNumber);
        }

        [Fact]
        public void Chunk_ShouldHandleSingleWord()
        {
            // Arrange
            var chunker = new TextChunker(windowSize: 10);
            var page = new PageContent { Text = "Revenue", PageNumber = 1 };

            // Act
            var chunks = chunker.Chunk(page, "test.pdf").ToList();

            // Assert
            Assert.Single(chunks);
            Assert.Equal("Revenue", chunks[0].Text);
        }

        [Fact]
        public void Chunk_ShouldSplitOnMultipleSeparatorLevels()
        {
            // Arrange: Text with paragraphs, sentences, and words
            var text = "First paragraph has multiple sentences. Another sentence here.\n\nSecond paragraph content. More text.";
            var chunker = new TextChunker(windowSize: 10, overlap: 0);
            var page = new PageContent { Text = text, PageNumber = 1 };

            // Act
            var chunks = chunker.Chunk(page, "test.pdf").ToList();

            // Assert - Should split into multiple chunks due to small window
            Assert.True(chunks.Count > 1);
            Assert.All(chunks, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk.Text)));
        }
    }
}
