using System.Collections.Generic;
using System.Threading.Tasks;
using FinAnalyzer.Core.Interfaces;
using FinAnalyzer.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FinAnalyzer.Engine.Services
{
    public class PdfPigLoader : IFileLoader
    {
        public Task<IEnumerable<PageContent>> LoadAsync(string filePath)
        {
            return Task.Run(() =>
            {
                var pages = new List<PageContent>();

                using (var document = PdfDocument.Open(filePath))
                {
                    foreach (var page in document.GetPages())
                    {
                        var text = page.Text;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            pages.Add(new PageContent
                            {
                                Text = text,
                                PageNumber = page.Number
                            });
                        }
                    }
                }

                return (IEnumerable<PageContent>)pages;
            });
        }
    }
}
