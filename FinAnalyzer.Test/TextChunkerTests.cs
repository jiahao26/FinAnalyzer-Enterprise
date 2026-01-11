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
            // Create a text larger than window size (50)
            var duplicateWord = "TestWord "; // 9 chars
            var largeText = string.Concat(Enumerable.Repeat(duplicateWord, 10)); // 90 chars
            
            var chunker = new TextChunker(windowSize: 50, overlap: 10);
            var page = new PageContent { Text = largeText, PageNumber = 1 };

            // Act
            var chunks = chunker.Chunk(page, "test.pdf").ToList();

            // Assert
            Assert.True(chunks.Count > 1, "Should split into multiple chunks");
            Assert.All(chunks, c => Assert.True(c.Text.Length <= 50, $"Chunk length {c.Text.Length} exceeds window size 50"));
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
            // Ideally, if it fits, it keeps together. But let's force a split by making window small?
            // Actually, the current recursive logic splits by separator IF acceptable? 
            // Wait, the current logic calls SplitTextRecursive.
            
            // Let's test that it DOESN'T split if it fits?
            // The logic: if text fits window, return it.
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

            // Window 80. Can't fit both. Should split on \n\n.
            var chunker = new TextChunker(windowSize: 80, overlap: 0);
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
    }
}
