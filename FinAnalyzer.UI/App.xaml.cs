using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using FinAnalyzer.Core.Configuration;
using FinAnalyzer.Core.Interfaces;
using FinAnalyzer.Engine.Services;
using FinAnalyzer.UI.ViewModels;

namespace FinAnalyzer.UI;

/// <summary>
/// Application entry point with dependency injection configuration.
/// </summary>
public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }
    public IConfiguration? Configuration { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Dynamic Model Discovery: Ensure we use a valid model that actually exists
            var aiSettings = Configuration.GetSection("AIServices").Get<AISettings>() ?? new AISettings();
            if (aiSettings.BackendType.Equals("Ollama", StringComparison.OrdinalIgnoreCase)) 
            {
                var discoveredModel = await TryDiscoverOllamaModelAsync(aiSettings.ChatEndpoint, aiSettings.ChatModelId);
                if (!string.Equals(discoveredModel, aiSettings.ChatModelId, StringComparison.OrdinalIgnoreCase))
                {
                    // Update in-memory configuration if we switched models
                    Configuration["AIServices:ChatModelId"] = discoveredModel;
                }
            }

            var services = new ServiceCollection();

            ConfigureServices(services);

            Services = services.BuildServiceProvider();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error: {ex.Message}\n\n{ex.StackTrace}", "FinAnalyzer Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private async Task<string> TryDiscoverOllamaModelAsync(string endpoint, string requestedModel)
    {
        try
        {
            // Normalize endpoint
            var baseUri = endpoint.TrimEnd('/');
            if (baseUri.EndsWith("/v1")) baseUri = baseUri.Substring(0, baseUri.Length - 3);

            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2); // Fast fail

            var response = await client.GetAsync($"{baseUri}/api/tags");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            
            var models = doc.RootElement.GetProperty("models").EnumerateArray()
                .Select(m => m.GetProperty("name").GetString())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            if (!models.Any()) return requestedModel; // No models found, stick to default

            // 1. Check if requested model exists (exact match)
            if (models.Contains(requestedModel)) return requestedModel;

            // 2. Check for loose match (e.g. "llama3" matching "llama3:latest")
            var looseMatch = models.FirstOrDefault(m => m!.StartsWith(requestedModel, StringComparison.OrdinalIgnoreCase));
            if (looseMatch != null) return looseMatch;

            // 3. Fallback: Pick the first available chat model (heuristic: prefer "instruct" or "chat")
            var fallback = models.FirstOrDefault(m => m!.Contains("instruct", StringComparison.OrdinalIgnoreCase)) 
                           ?? models.First();

            return fallback!;
        }
        catch
        {
            // If discovery fails (e.g. Ollama down), fail safe to the requested/default model
            return requestedModel;
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfiguration>(Configuration!);
        
        // Configure settings with default fallback values
        services.Configure<AISettings>(Configuration!.GetSection("AIServices"));
        services.Configure<QdrantSettings>(Configuration!.GetSection("Qdrant"));
        services.Configure<TeiSettings>(Configuration!.GetSection("Tei"));

        // HTTP Clients
        services.AddHttpClient<IEmbeddingService, GenericEmbeddingService>();
        services.AddHttpClient<IRerankerService, TeiRerankerService>();

        // Core Services - use lazy factory to defer initialization
        services.AddSingleton<IVectorDbService>(sp => 
        {
            var embedding = sp.GetRequiredService<IEmbeddingService>();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<QdrantSettings>>();
            return new QdrantVectorService(embedding, options);
        });

        // File loader and chunker
        services.AddSingleton<IFileLoader, PdfPigLoader>();
        services.AddSingleton<TextChunker>();

        // Ingestion pipeline
        services.AddSingleton<IngestionService>(sp =>
        {
            var fileLoader = sp.GetRequiredService<IFileLoader>();
            var chunker = sp.GetRequiredService<TextChunker>();
            var embeddingService = sp.GetRequiredService<IEmbeddingService>();
            var vectorDb = sp.GetRequiredService<IVectorDbService>();
            return new IngestionService(fileLoader, chunker, embeddingService, vectorDb);
        });

        // Configure Semantic Kernel before registering IRagService
        ConfigureSemanticKernel(services);

        services.AddSingleton<IRagService>(sp =>
        {
            var vectorDb = sp.GetRequiredService<IVectorDbService>();
            var reranker = sp.GetRequiredService<IRerankerService>();
            var kernel = sp.GetRequiredService<Kernel>();
            var ingestionService = sp.GetRequiredService<IngestionService>();
            return new SemanticKernelService(vectorDb, reranker, kernel, ingestionService);
        });

        // UI Services
        services.AddSingleton<FinAnalyzer.UI.Services.DocumentStore>();
        services.AddSingleton<FinAnalyzer.UI.Services.ServiceHealthChecker>();

        // ViewModels - inject services for real functionality
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<DocumentsViewModel>(sp => 
            new DocumentsViewModel(
                sp.GetRequiredService<FinAnalyzer.UI.Services.DocumentStore>(),
                sp.GetRequiredService<IRagService>()));
        services.AddTransient<ChatViewModel>(sp => 
            new ChatViewModel(
                sp.GetRequiredService<FinAnalyzer.UI.Services.DocumentStore>(),
                sp.GetRequiredService<IRagService>()));
        services.AddTransient<SettingsViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();
    }

    private void ConfigureSemanticKernel(IServiceCollection services)
    {
        var kernelBuilder = services.AddKernel();

        var aiSettings = Configuration!.GetSection("AIServices").Get<AISettings>() ?? new AISettings();

        // Use defaults if endpoint is empty/null
        var endpoint = !string.IsNullOrWhiteSpace(aiSettings.ChatEndpoint) 
            ? aiSettings.ChatEndpoint 
            : "http://localhost:11434";
        
        var modelId = !string.IsNullOrWhiteSpace(aiSettings.ChatModelId)
            ? aiSettings.ChatModelId
            : "llama3:8b-instruct-q8_0";

        // Default to Ollama backend
        if (string.IsNullOrWhiteSpace(aiSettings.BackendType) || 
            aiSettings.BackendType.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            if (!endpoint.EndsWith("/v1"))
            {
                endpoint = endpoint.TrimEnd('/') + "/v1";
            }

            // Custom robust HTTP client for local AI (prevents 100s timeouts)
            var ollamaClient = new System.Net.Http.HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10),
                BaseAddress = new Uri(endpoint)
            };

            kernelBuilder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: "ollama",
                httpClient: ollamaClient
            );
        }
        else if (aiSettings.BackendType.Equals("OpenAI_Compatible", StringComparison.OrdinalIgnoreCase))
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: aiSettings.ApiKey ?? "no-key-required",
                endpoint: new Uri(endpoint)
            );
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}
