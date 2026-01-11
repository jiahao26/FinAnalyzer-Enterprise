using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FinAnalyzer.Engine.Services;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using Xunit;
using Qdrant.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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
        [Trait("Category", "Integration")]
         public async Task FullIngestionPipeline_ShouldWork()
        {
            _output.WriteLine("Starting Ingestion Test...");
            // Arrange
            var pdfPath = "test_ingestion.pdf";
            var collectionName = "test_collection_" + Guid.NewGuid().ToString("N");
            
            // Step 1: Create dummy PDF.
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
                // Step 2: Load PDF using PdfPigLoader.
                _output.WriteLine("Loading PDF...");
                var loader = new PdfPigLoader();
                var pages = new System.Collections.Generic.List<FinAnalyzer.Core.Models.PageContent>();
                await foreach (var p in loader.LoadAsync(pdfPath))
                {
                    pages.Add(p);
                }
                
                Assert.NotEmpty(pages);
                Assert.Contains("FinAnalyzer", pages.First().Text);

                // Step 3: Chunk text into smaller segments.
                _output.WriteLine("Chunking...");
                var chunker = new TextChunker(windowSize: 500, overlap: 50); 
                var chunks = pages.SelectMany(p => chunker.Chunk(p, pdfPath)).ToList();
                Assert.NotEmpty(chunks);

                // Step 4: Generate Embeddings using Ollama.
                _output.WriteLine("Generating Embeddings...");
                
                // Build Configuration using Engine's Loader
                var config = FinAnalyzer.Engine.Configuration.ConfigurationLoader.Load("appsettings.json");

                using var httpClient = new System.Net.Http.HttpClient();
                
                // Bind Ollama Settings
                var ollamaSection = config.GetSection("Ollama");
                var ollamaConfigValue = ollamaSection.Get<FinAnalyzer.Core.Configuration.OllamaSettings>() 
                                        ?? throw new InvalidOperationException("Ollama settings missing");
                var ollamaSettings = Microsoft.Extensions.Options.Options.Create(ollamaConfigValue);
                
                var embeddingService = new OllamaEmbeddingService(httpClient, ollamaSettings);
                
                foreach (var chunk in chunks)
                {
                   chunk.Vector = await embeddingService.GenerateEmbeddingAsync(chunk.Text);
                }

                // Step 5: Upsert chunks to Qdrant.
                _output.WriteLine("Upserting to Qdrant...");
                
                // Bind Qdrant Settings
                var qdrantSection = config.GetSection("Qdrant");
                var qdrantConfigValue = qdrantSection.Get<FinAnalyzer.Core.Configuration.QdrantSettings>()
                                        ?? throw new InvalidOperationException("Qdrant settings missing");
                var qdrantSettings = Microsoft.Extensions.Options.Options.Create(qdrantConfigValue);
                
                // Explicitly set host/port for client creation (since client doesn't take Options, only service does)
                // Actually, the service takes Options, but we also create a raw client below for verification.
                var qHost = qdrantSettings.Value.Host;
                var qPort = qdrantSettings.Value.Port;

                var vectorService = new QdrantVectorService(embeddingService, qdrantSettings); 
                await vectorService.UpsertAsync(collectionName, chunks);

                var client = new QdrantClient(qHost, qPort);
                
                // Delay to allow indexing propagation.
                await Task.Delay(2000);

                var collectionInfo = await client.GetCollectionInfoAsync(collectionName);
                _output.WriteLine($"Detailed Verification: Expected {chunks.Count}, Found {collectionInfo.PointsCount}");
                Assert.Equal((ulong)chunks.Count, collectionInfo.PointsCount);
            }
            finally
            {
                if (File.Exists(pdfPath)) File.Delete(pdfPath);
                try {
                    var client = new QdrantClient("localhost", 6334);
                    await client.DeleteCollectionAsync(collectionName);
                } catch { /* ignore cleanup errors */ }
            }
        }
    }
}
