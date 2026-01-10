using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FinAnalyzer.Engine.Services;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using Xunit;
using Qdrant.Client;

namespace FinAnalyzer.Test
{
    public class IngestionTests
    {
        private readonly Xunit.Abstractions.ITestOutputHelper _output;

        public IngestionTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
         public async Task FullIngestionPipeline_ShouldWork()
        {
            _output.WriteLine("Starting Ingestion Test...");
            // Arrange
            var pdfPath = "test_ingestion.pdf";
            var collectionName = "test_collection_" + Guid.NewGuid().ToString("N");
            
            // 1. Create dummy PDF
            _output.WriteLine("Creating PDF...");
            var builder = new PdfDocumentBuilder();
            var page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);
            page.AddText("This is a test document for FinAnalyzer.", 12, new UglyToad.PdfPig.Core.PdfPoint(25, 700), font);
            page.AddText("It has multiple lines to test chunking.", 12, new UglyToad.PdfPig.Core.PdfPoint(25, 680), font);
            page.AddText("We expect this to be processed correctly.", 12, new UglyToad.PdfPig.Core.PdfPoint(25, 660), font);

            File.WriteAllBytes(pdfPath, builder.Build());

            try
            {
                // 2. Load
                _output.WriteLine("Loading PDF...");
                var loader = new PdfPigLoader();
                var pages = await loader.LoadAsync(pdfPath);
                Assert.NotEmpty(pages);
                Assert.Contains("FinAnalyzer", pages.First().Text);

                // 3. Chunk
                _output.WriteLine("Chunking...");
                var chunker = new TextChunker(windowSize: 500, overlap: 50); 
                var chunks = pages.SelectMany(p => chunker.Chunk(p, pdfPath)).ToList();
                Assert.NotEmpty(chunks);

                // 3.5 Embeddings
                _output.WriteLine("Generating Embeddings...");
                using var httpClient = new System.Net.Http.HttpClient();
                var embeddingService = new OllamaEmbeddingService(httpClient);
                
                foreach (var chunk in chunks)
                {
                   // Generate embedding for the chunk before upserting
                   chunk.Vector = await embeddingService.GenerateEmbeddingAsync(chunk.Text);
                }

                // 4. Upsert
                _output.WriteLine("Upserting to Qdrant...");
                var vectorService = new QdrantVectorService("localhost", 6334); 
                await vectorService.UpsertAsync(collectionName, chunks);

                // 5. Verify (Count via separate client instance to be sure)
                var client = new QdrantClient("localhost", 6334);
                
                // Allow a moment for propagation
                await Task.Delay(2000);

                var collectionInfo = await client.GetCollectionInfoAsync(collectionName);
                _output.WriteLine($"Detailed Verification: Expected {chunks.Count}, Found {collectionInfo.PointsCount}");
                Assert.Equal((ulong)chunks.Count, collectionInfo.PointsCount);
            }
            finally
            {
                // Cleanup
                if (File.Exists(pdfPath)) File.Delete(pdfPath);
                try {
                    var client = new QdrantClient("localhost", 6334);
                    await client.DeleteCollectionAsync(collectionName);
                } catch { /* ignore cleanup errors */ }
            }
        }
    }
}
