using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FinAnalyzer.Core.Configuration;
using FinAnalyzer.Engine;
using FinAnalyzer.Engine.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Xunit;
using Xunit.Abstractions;

namespace FinAnalyzer.Test
{
    /// <summary>
    /// Comprehensive debug test for tracing the complete RAG pipeline.
    /// Traces: PDF Loading → Chunking → Embedding → Vector Storage → Query → Response
    /// All steps are logged to debug.log for analysis.
    /// </summary>
    public class BackendPipelineDebugTest
    {
        private readonly ITestOutputHelper _output;
        private readonly IConfiguration _config;

        public BackendPipelineDebugTest(ITestOutputHelper output)
        {
            _output = output;
            _config = FinAnalyzer.Engine.Configuration.ConfigurationLoader.Load("appsettings.json");
            
            // Initialize centralized logger
            CentralLogger.Initialize();
        }

        [Fact]
        [Trait("Category", "Debug")]
        public async Task FullPipeline_Ingestion_And_Query_Test()
        {
            CentralLogger.Step("=== BACKEND PIPELINE DEBUG TEST START ===");
            _output.WriteLine($"Debug log location: {CentralLogger.GetLogPath()}");
            
            // Locate the SEC filing PDF
            var pdfPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "0001957238-26-000001.pdf");
            if (!File.Exists(pdfPath))
            {
                // Try alternate path (running from solution root)
                pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "0001957238-26-000001.pdf");
            }
            
            if (!File.Exists(pdfPath))
            {
                // Search for the PDF
                var searchDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..");
                var found = Directory.GetFiles(searchDir, "*.pdf", SearchOption.AllDirectories).FirstOrDefault();
                if (found != null) pdfPath = found;
            }

            CentralLogger.Info($"PDF path resolved to: {pdfPath}");
            Assert.True(File.Exists(pdfPath), $"SEC filing PDF not found at {pdfPath}");
            
            var collectionName = "debug_test_" + Guid.NewGuid().ToString("N")[..8];
            CentralLogger.Info($"Using test collection: {collectionName}");

            try
            {
                // ===== STEP 1: Initialize Services =====
                CentralLogger.Step("STEP 1: SERVICE INITIALIZATION");
                _output.WriteLine("Initializing services...");

                // Load settings
                var aiSettings = Options.Create(_config.GetSection("AIServices").Get<AISettings>() ?? new AISettings());
                var qdrantSettings = Options.Create(_config.GetSection("Qdrant").Get<QdrantSettings>() ?? new QdrantSettings());
                var teiSettings = Options.Create(_config.GetSection("Tei").Get<TeiSettings>() ?? new TeiSettings());

                CentralLogger.Debug($"AI Settings - Endpoint: {aiSettings.Value.EmbeddingEndpoint}, Model: {aiSettings.Value.EmbeddingModelId}");
                CentralLogger.Debug($"Qdrant Settings - Host: {qdrantSettings.Value.Host}, Port: {qdrantSettings.Value.Port}");

                // Create services
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                
                var embeddingService = new GenericEmbeddingService(httpClient, aiSettings);
                CentralLogger.Info("EmbeddingService created");

                var vectorService = new QdrantVectorService(embeddingService, qdrantSettings);
                CentralLogger.Info("QdrantVectorService created");

                var fileLoader = new PdfPigLoader();
                var chunker = new TextChunker(windowSize: 500, overlap: 50);
                var ingestionService = new IngestionService(fileLoader, chunker, embeddingService, vectorService);
                CentralLogger.Info("IngestionService created");

                // ===== STEP 2: PDF LOADING TEST =====
                CentralLogger.Step("STEP 2: PDF LOADING TEST");
                _output.WriteLine("Testing PDF loading...");
                
                var pages = new System.Collections.Generic.List<FinAnalyzer.Core.Models.PageContent>();
                await foreach (var page in fileLoader.LoadAsync(pdfPath))
                {
                    pages.Add(page);
                }
                
                CentralLogger.Info($"PDF loaded: {pages.Count} pages extracted");
                Assert.NotEmpty(pages);
                _output.WriteLine($"  ✓ Loaded {pages.Count} pages from PDF");
                _output.WriteLine($"\n--- PDF CONTENT PREVIEW ---\n{pages[0].Text.Substring(0, Math.Min(2000, pages[0].Text.Length))}\n--- END PREVIEW ---\n");

                // ===== STEP 3: CHUNKING TEST =====
                CentralLogger.Step("STEP 3: CHUNKING TEST");
                _output.WriteLine("Testing text chunking...");
                
                var allChunks = pages.SelectMany(p => chunker.Chunk(p, Path.GetFileName(pdfPath))).ToList();
                CentralLogger.Info($"Chunking complete: {allChunks.Count} chunks generated");
                Assert.NotEmpty(allChunks);
                _output.WriteLine($"  ✓ Generated {allChunks.Count} chunks");

                // ===== STEP 4: EMBEDDING TEST =====
                CentralLogger.Step("STEP 4: EMBEDDING TEST");
                _output.WriteLine("Testing embedding generation...");

                // Test single embedding first
                var testEmbed = await embeddingService.GenerateEmbeddingAsync("test document embedding");
                CentralLogger.Info($"Test embedding dimensions: {testEmbed.Length}");
                Assert.True(testEmbed.Length > 0, "Embedding should have dimensions");
                _output.WriteLine($"  ✓ Generated test embedding with {testEmbed.Length} dimensions");

                // ===== STEP 5: FULL INGESTION TEST =====
                CentralLogger.Step("STEP 5: FULL INGESTION PIPELINE");
                _output.WriteLine("Running full ingestion pipeline...");

                var progress = new Progress<int>(p => 
                {
                    if (p % 20 == 0) _output.WriteLine($"  Ingestion progress: {p}%");
                });

                // Delete collection if exists (cleanup from previous runs)
                await vectorService.DeleteCollectionAsync(collectionName);

                // Run ingestion manually with our custom collection
                foreach (var chunk in allChunks)
                {
                    chunk.Vector = await embeddingService.GenerateEmbeddingAsync(chunk.Text);
                }
                await vectorService.UpsertAsync(collectionName, allChunks);

                CentralLogger.Info("Full ingestion complete");
                _output.WriteLine($"  ✓ Ingested {allChunks.Count} chunks to Qdrant collection '{collectionName}'");

                // ===== STEP 6: VECTOR SEARCH TEST =====
                CentralLogger.Step("STEP 6: VECTOR SEARCH TEST");
                _output.WriteLine("Testing vector search...");

                var searchResults = await vectorService.SearchAsync(collectionName, "Brief me about this document", limit: 5);
                var resultList = searchResults.ToList();
                
                CentralLogger.Info($"Search returned {resultList.Count} results");
                Assert.NotEmpty(resultList);
                _output.WriteLine($"  ✓ Vector search returned {resultList.Count} results");
                
                foreach (var r in resultList.Take(3))
                {
                    _output.WriteLine($"    - Score: {r.Score:F4}, Source: {r.SourceFileName}, Page: {r.PageNumber}");
                    CentralLogger.Debug($"Result: Score={r.Score:F4}, Source={r.SourceFileName}, Page={r.PageNumber}");
                }

                // ===== STEP 7: FULL RAG QUERY TEST =====
                CentralLogger.Step("STEP 7: FULL RAG QUERY TEST");
                _output.WriteLine("Testing full RAG query with LLM...");

                // Setup Ollama-compatible HTTP client
                var ollamaEndpoint = aiSettings.Value.ChatEndpoint ?? "http://localhost:11434";
                if (!ollamaEndpoint.EndsWith("/v1")) ollamaEndpoint = ollamaEndpoint.TrimEnd('/') + "/v1";
                
                using var ollamaClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                ollamaClient.BaseAddress = new Uri(ollamaEndpoint);

                var builder = Kernel.CreateBuilder();
                builder.AddOpenAIChatCompletion(
                        modelId: aiSettings.Value.ChatModelId ?? "llama3:8b-instruct-q8_0",
                        apiKey: "ollama",
                        httpClient: ollamaClient
                    );
                var kernel = builder.Build();

                var reranker = new TeiRerankerService(httpClient, teiSettings);
                var semanticKernelService = new SemanticKernelService(vectorService, reranker, kernel, ingestionService);

                var query = "Brief me about the document and mention the PDF filename.";
                var responseBuilder = new StringBuilder();
                
                await foreach (var token in semanticKernelService.QueryAsync(query))
                {
                    responseBuilder.Append(token);
                }

                var fullResponse = responseBuilder.ToString();
                CentralLogger.Info($"LLM Response: {fullResponse}");
                _output.WriteLine($"\n  LLM Reply:\n{fullResponse}");
                
                Assert.NotEmpty(fullResponse);

                CentralLogger.Step("=== BACKEND PIPELINE DEBUG TEST COMPLETE ===");
                _output.WriteLine("\n✓✓✓ All pipeline steps completed successfully! ✓✓✓");
                _output.WriteLine($"\nReview full debug log at: {CentralLogger.GetLogPath()}");
            }
            catch (Exception ex)
            {
                CentralLogger.Error($"Pipeline test failed at: {ex.Message}", ex);
                _output.WriteLine($"\n✗ TEST FAILED: {ex.Message}");
                _output.WriteLine($"\nFull stack trace:\n{ex.StackTrace}");
                _output.WriteLine($"\nReview debug log for details: {CentralLogger.GetLogPath()}");
                throw;
            }
            finally
            {
                // Cleanup
                try
                {
                    var qdrantSettings = Options.Create(_config.GetSection("Qdrant").Get<QdrantSettings>() ?? new QdrantSettings());
                    using var httpClient = new HttpClient();
                    var aiSettings = Options.Create(_config.GetSection("AIServices").Get<AISettings>() ?? new AISettings());
                    var embeddingService = new GenericEmbeddingService(httpClient, aiSettings);
                    var vectorService = new QdrantVectorService(embeddingService, qdrantSettings);
                    await vectorService.DeleteCollectionAsync(collectionName);
                    CentralLogger.Info($"Cleanup: Deleted test collection '{collectionName}'");
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        [Fact]
        [Trait("Category", "Debug")]
        public async Task InfrastructureConnectivity_Test()
        {
            CentralLogger.Step("=== INFRASTRUCTURE CONNECTIVITY TEST ===");
            _output.WriteLine("Testing connectivity to all backend services...\n");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var allPassed = true;

            // Test 1: Qdrant
            CentralLogger.Step("Testing Qdrant connectivity");
            var qdrantSettings = _config.GetSection("Qdrant").Get<QdrantSettings>() ?? new QdrantSettings();
            var qdrantUrl = $"http://{qdrantSettings.Host}:{qdrantSettings.HttpPort}/collections";
            try
            {
                var response = await client.GetAsync(qdrantUrl);
                if (response.IsSuccessStatusCode)
                {
                    CentralLogger.Info($"Qdrant OK at {qdrantUrl}");
                    _output.WriteLine($"✓ Qdrant: OK ({qdrantUrl})");
                }
                else
                {
                    CentralLogger.Warn($"Qdrant returned {response.StatusCode}");
                    _output.WriteLine($"✗ Qdrant: {response.StatusCode} ({qdrantUrl})");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                CentralLogger.Error($"Qdrant connection failed", ex);
                _output.WriteLine($"✗ Qdrant: FAILED - {ex.Message}");
                allPassed = false;
            }

            // Test 2: Ollama
            CentralLogger.Step("Testing Ollama connectivity");
            var aiSettings = _config.GetSection("AIServices").Get<AISettings>() ?? new AISettings();
            var ollamaUrl = $"{aiSettings.EmbeddingEndpoint}/api/tags";
            try
            {
                var response = await client.GetAsync(ollamaUrl);
                if (response.IsSuccessStatusCode)
                {
                    CentralLogger.Info($"Ollama OK at {ollamaUrl}");
                    _output.WriteLine($"✓ Ollama: OK ({ollamaUrl})");
                }
                else
                {
                    CentralLogger.Warn($"Ollama returned {response.StatusCode}");
                    _output.WriteLine($"✗ Ollama: {response.StatusCode} ({ollamaUrl})");
                    allPassed = false;
                }
            }
            catch (Exception ex)
            {
                CentralLogger.Error($"Ollama connection failed", ex);
                _output.WriteLine($"✗ Ollama: FAILED - {ex.Message}");
                allPassed = false;
            }

            // Test 3: Reranker (TEI)
            CentralLogger.Step("Testing Reranker connectivity");
            var teiSettings = _config.GetSection("Tei").Get<TeiSettings>() ?? new TeiSettings();
            var teiUrl = $"{teiSettings.BaseUrl}/health";
            try
            {
                var response = await client.GetAsync(teiUrl);
                if (response.IsSuccessStatusCode)
                {
                    CentralLogger.Info($"Reranker OK at {teiUrl}");
                    _output.WriteLine($"✓ Reranker: OK ({teiUrl})");
                }
                else
                {
                    // Try alternate health endpoint
                    response = await client.GetAsync($"{teiSettings.BaseUrl}/");
                    if (response.IsSuccessStatusCode)
                    {
                        CentralLogger.Info($"Reranker OK at {teiSettings.BaseUrl}");
                        _output.WriteLine($"✓ Reranker: OK ({teiSettings.BaseUrl})");
                    }
                    else
                    {
                        CentralLogger.Warn($"Reranker returned {response.StatusCode}");
                        _output.WriteLine($"⚠ Reranker: {response.StatusCode} (may still be warming up)");
                    }
                }
            }
            catch (Exception ex)
            {
                CentralLogger.Warn($"Reranker connection issue: {ex.Message}");
                _output.WriteLine($"⚠ Reranker: TIMEOUT - May still be loading model ({teiUrl})");
            }

            CentralLogger.Step("=== INFRASTRUCTURE TEST COMPLETE ===");
            _output.WriteLine($"\nDebug log: {CentralLogger.GetLogPath()}");
            
            Assert.True(allPassed, "One or more infrastructure services are not reachable");
        }
    }
}
