using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FinAnalyzer.Core.Models;
using Microsoft.ML.Tokenizers;

namespace FinAnalyzer.Engine.Services
{
    public class TextChunker
    {
        private readonly int _windowSize; // Set target tokens per chunk
        private readonly int _overlap;    // Define overlap tokens (kept for API consistency)
        private readonly Tokenizer _tokenizer;
        // Define recursive separators: Paragraphs -> Lines -> Sentences -> Words
        private readonly string[] _separators = new[] { "\r\n\r\n", "\n\n", "\r\n", "\n", " " };

        public TextChunker(int windowSize = 500, int overlap = 100)
        {
            _windowSize = windowSize;
            _overlap = overlap;
            
            // Initialize Tokenizer to approximate Llama-3/GPT-4 token counting
            try 
            {
                // Use Microsoft.ML.Tokenizers library
                // Use TiktokenTokenizer for efficient and accurate token counting
                _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
            }
            catch
            {
                // Fallback to null if model loading fails
                _tokenizer = null;
            }
        }

        public IEnumerable<DocumentChunk> Chunk(PageContent page, string sourceFileName)
        {
            if (string.IsNullOrWhiteSpace(page.Text))
            {
                yield break;
            }

            var chunks = new List<string>();
            SplitTextRecursive(page.Text, _separators, chunks);

            int chunkIndex = 0;
            foreach (var chunkText in chunks)
            {
                if (string.IsNullOrWhiteSpace(chunkText)) continue;
                
                // Generate deterministic ID for idempotency
                var chunkId = GenerateId(sourceFileName, page.PageNumber, chunkIndex);
                chunkIndex++;

                yield return new DocumentChunk
                {
                    Id = chunkId,
                    Text = chunkText.Trim(),
                    SourceFileName = sourceFileName,
                    PageNumber = page.PageNumber,
                    Metadata = new Dictionary<string, object>
                    {
                        { "TokenCount", CountTokens(chunkText) },
                        { "Method", "RecursiveTokenSplit" }
                    }
                };
            }
        }

        private string GenerateId(string fileName, int page, int chunkIndex)
        {
            // Create unique, deterministic hash for chunk position
            var input = $"{fileName}_{page}_{chunkIndex}";
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray()).ToString();
        }

        private int CountTokens(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            // Use tokenizer if available; otherwise approximate 4 chars per token
            return _tokenizer?.CountTokens(text) ?? (text.Length / 4);
        }

        private void SplitTextRecursive(string text, string[] separators, List<string> chunks)
        {
            int tokenCount = CountTokens(text);
            if (tokenCount <= _windowSize)
            {
                chunks.Add(text);
                return;
            }

            // Find optimal separator
            string separator = null;
            int separatorIndex = -1;
            
            for (int i = 0; i < separators.Length; i++)
            {
                if (text.Contains(separators[i]))
                {
                    separator = separators[i];
                    separatorIndex = i;
                    break;
                }
            }

            if (separator != null)
            {
                var splits = text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                var currentDoc = new StringBuilder();
                
                foreach (var split in splits)
                {
                    string segment = split; 
                    
                    // Construct potential next segment
                    string nextText = currentDoc.Length == 0 ? segment : currentDoc.ToString() + separator + segment;
                    
                    if (CountTokens(nextText) > _windowSize)
                    {
                        if (currentDoc.Length > 0)
                        {
                            chunks.Add(currentDoc.ToString());
                            currentDoc.Clear();
                        }
                        
                        // Recurse if segment exceeds window size
                        if (CountTokens(segment) > _windowSize)
                        {
                            if (separatorIndex < separators.Length - 1)
                            {
                                var nextSeparators = separators.Skip(separatorIndex + 1).ToArray();
                                SplitTextRecursive(segment, nextSeparators, chunks);
                            }
                            else
                            {
                                // Force split when no separators remain
                                // Implement hard split strategy
                                int charLimit = _windowSize * 4;
                                if (segment.Length > charLimit) 
                                {
                                     // Apply basic hard split
                                     chunks.Add(segment.Substring(0, charLimit)); 
                                     // Truncate for safety in edge cases
                                }
                                else
                                {
                                    chunks.Add(segment);
                                }
                            }
                        }
                        else
                        {
                            currentDoc.Append(segment);
                        }
                    }
                    else
                    {
                        if (currentDoc.Length > 0) currentDoc.Append(separator);
                        currentDoc.Append(segment);
                    }
                }
                
                if (currentDoc.Length > 0)
                {
                    chunks.Add(currentDoc.ToString());
                }
            }
            else
            {
                // Execute hard character split fallback
                int charLimit = _windowSize * 4;
                for (int i = 0; i < text.Length; i += charLimit)
                {
                     int len = Math.Min(charLimit, text.Length - i);
                     chunks.Add(text.Substring(i, len));
                }
            }
        }
    }
}
