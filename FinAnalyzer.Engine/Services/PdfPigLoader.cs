using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
using FinAnalyzer.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace FinAnalyzer.Engine.Services
{
    public class PdfPigLoader : IFileLoader
    {
        public async IAsyncEnumerable<PageContent> LoadAsync(string filePath)
        {
            // Stream pages one by one to avoid loading entire document into memory.
            // Open file and iterate (cold loading).
            using var document = PdfDocument.Open(filePath);
            
            foreach (var page in document.GetPages())
            {
                // Yield to prevent thread blocking during tight loop;
                // allow caller to process yielded item.
                await Task.Yield(); 

                var wordExtractor = NearestNeighbourWordExtractor.Instance;
                // Execute parsing on .Letters access.
                var words = wordExtractor.GetWords(page.Letters);
                
                var text = string.Join(" ", words.Select(w => w.Text));

                if (!string.IsNullOrWhiteSpace(text))
                {
                    yield return new PageContent
                    {
                        Text = text,
                        PageNumber = page.Number
                    };
                }
            }
        }
    }
}
