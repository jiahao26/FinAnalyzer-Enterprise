using System;
using System.Collections.Generic;
using System.Linq;
using FinAnalyzer.Core.Models;

namespace FinAnalyzer.Engine.Services
{
    public class TextChunker
    {
        private readonly int _windowSize;
        private readonly int _overlap;

        public TextChunker(int windowSize = 500, int overlap = 100)
        {
            _windowSize = windowSize;
            _overlap = overlap;
        }

        public IEnumerable<DocumentChunk> Chunk(PageContent page, string sourceFileName)
        {
            var words = page.Text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                yield break;
            }

            for (int i = 0; i < words.Length; i += (_windowSize - _overlap))
            {
                var chunkWords = words.Skip(i).Take(_windowSize);
                var chunkText = string.Join(" ", chunkWords);

                // Ensure unique ID generation strategy (e.g., hash of text or GUID)
                // For now using GUID for simplicity
                yield return new DocumentChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = chunkText,
                    SourceFileName = sourceFileName,
                    PageNumber = page.PageNumber,
                    Metadata = new Dictionary<string, object>
                    {
                        { "StartIndex", i },
                        { "Length", chunkWords.Count() }
                    }
                };

                // Prevent infinite loop if overlap >= windowSize (though construct should guard, loop logic handles if we increment > 0)
                // Logic check: i increments by (window - overlap). If window <= overlap, this stalls.
                if (_windowSize <= _overlap) throw new InvalidOperationException("Overlap must be smaller than Window Size.");
            }
        }
    }
}
