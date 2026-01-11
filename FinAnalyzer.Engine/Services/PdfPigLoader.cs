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
            // Stream pages one by one to avoid loading entire document into memory
            // This is "cold" loading - we open file and iterate.
            using var document = PdfDocument.Open(filePath);
            
            foreach (var page in document.GetPages())
            {
                // Yield to ensure we don't block the thread entirely during tight loop, 
                // allowing caller to process the yielded item.
                await Task.Yield(); 

                var wordExtractor = NearestNeighbourWordExtractor.Instance;
                // Parsing happens here on .Letters access usually
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
