using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FinAnalyzer.Engine.Services;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using Xunit;

namespace FinAnalyzer.Test.Services
{
    public class PdfPigLoaderTests : IDisposable
    {
        private readonly string _testPdfPath;

        public PdfPigLoaderTests()
        {
            _testPdfPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.pdf");
        }

        [Fact]
        public async Task LoadAsync_ShouldExtractText_FromValidPdf()
        {
            // Arrange
            CreateTestPdf(_testPdfPath, "This is test content for page 1.", "Second page content here.");
            var loader = new PdfPigLoader();

            // Act
            var pages = new System.Collections.Generic.List<FinAnalyzer.Core.Models.PageContent>();
            await foreach (var page in loader.LoadAsync(_testPdfPath))
            {
                pages.Add(page);
            }

            // Assert
            Assert.Equal(2, pages.Count);
            Assert.Contains("test", pages[0].Text.ToLowerInvariant());
            Assert.Contains("second", pages[1].Text.ToLowerInvariant());
            Assert.Equal(1, pages[0].PageNumber);
            Assert.Equal(2, pages[1].PageNumber);
        }


        [Fact]
        public async Task LoadAsync_ShouldExtractMultiplePages()
        {
            // Arrange
            var pageContents = Enumerable.Range(1, 5)
                .Select(i => $"Page{i} content data")
                .ToArray();
            
            CreateTestPdf(_testPdfPath, pageContents);
            var loader = new PdfPigLoader();

            // Act
            var pages = new System.Collections.Generic.List<FinAnalyzer.Core.Models.PageContent>();
            await foreach (var page in loader.LoadAsync(_testPdfPath))
            {
                pages.Add(page);
            }

            // Assert
            Assert.Equal(5, pages.Count);
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(i + 1, pages[i].PageNumber);
                // Just verify text is not empty
                Assert.NotEmpty(pages[i].Text);
            }
        }

        [Fact]
        public async Task LoadAsync_ShouldPreserveTextOrder()
        {
            // Arrange
            CreateTestPdf(_testPdfPath, "First word. Second word. Third word.");
            var loader = new PdfPigLoader();

            // Act
            var pages = new System.Collections.Generic.List<FinAnalyzer.Core.Models.PageContent>();
            await foreach (var page in loader.LoadAsync(_testPdfPath))
            {
                pages.Add(page);
            }

            // Assert
            var text = pages[0].Text.ToLowerInvariant();
            var firstIndex = text.IndexOf("first");
            var secondIndex = text.IndexOf("second");
            var thirdIndex = text.IndexOf("third");
            
            Assert.True(firstIndex >= 0, "Should contain 'first'");
            Assert.True(secondIndex >= 0, "Should contain 'second'");
            Assert.True(thirdIndex >= 0, "Should contain 'third'");
            Assert.True(firstIndex < secondIndex, "First should come before Second");
            Assert.True(secondIndex < thirdIndex, "Second should come before Third");
        }

        private void CreateTestPdf(string path, params string[] pageTexts)
        {
            var builder = new PdfDocumentBuilder();
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);

            if (pageTexts.Length == 0)
            {
                // Create empty page
                builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
            }
            else
            {
                foreach (var text in pageTexts)
                {
                    var page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
                    page.AddText(text, 12, new UglyToad.PdfPig.Core.PdfPoint(50, 700), font);
                }
            }

            File.WriteAllBytes(path, builder.Build());
        }

        public void Dispose()
        {
            if (File.Exists(_testPdfPath))
            {
                File.Delete(_testPdfPath);
            }
        }
    }
}
