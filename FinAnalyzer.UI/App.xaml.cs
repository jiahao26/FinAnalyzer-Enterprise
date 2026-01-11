using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using FinAnalyzer.Core.Configuration;
using FinAnalyzer.Core.Interfaces;
using FinAnalyzer.Engine.Services;

namespace FinAnalyzer.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider? Services { get; private set; }
        public IConfiguration? Configuration { get; private set; }

        public App()
        {
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Load Configuration
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            
            Configuration = configBuilder.Build();

            // 2. Configure Services
            var services = new ServiceCollection();
            
            services.AddSingleton<IConfiguration>(Configuration);
            services.Configure<AISettings>(Configuration.GetSection("AIServices"));
            services.Configure<QdrantSettings>(Configuration.GetSection("Qdrant"));
            services.Configure<TeiSettings>(Configuration.GetSection("Tei"));

            // 3. Register Core Services
            services.AddHttpClient<IEmbeddingService, GenericEmbeddingService>();
            
            // 4. Semantic Kernel & AI Service Switcher
            var kernelBuilder = services.AddKernel();

            // Bind settings manually for switcher logic
            var aiSettings = Configuration.GetSection("AIServices").Get<AISettings>() ?? new AISettings();

            if (aiSettings.BackendType.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                // Scenario A: Standard Ollama or BYOM (Custom Local Model).
                // Use OpenAI Compatible Interface for Ollama.
                // Ensure endpoint points to /v1
                var endpoint = aiSettings.ChatEndpoint;
                if (!endpoint.EndsWith("/v1"))
                {
                    endpoint = endpoint.TrimEnd('/') + "/v1";
                }

                kernelBuilder.AddOpenAIChatCompletion(
                    modelId: aiSettings.ChatModelId,
                    apiKey: "ollama", // Set dummy key (required)
                    endpoint: new Uri(endpoint)
                );
            }
            else if (aiSettings.BackendType.Equals("OpenAI_Compatible", StringComparison.OrdinalIgnoreCase))
            {
                // Scenario B: Custom Backend (BYOB).
                kernelBuilder.AddOpenAIChatCompletion(
                    modelId: aiSettings.ChatModelId,
                    apiKey: aiSettings.ApiKey ?? "no-key-required",
                    endpoint: new Uri(aiSettings.ChatEndpoint) 
                );
            }
            else
            {
                // Set default fallback
                 kernelBuilder.AddOpenAIChatCompletion(
                    modelId: "llama3",
                    apiKey: "ollama",
                    endpoint: new Uri("http://localhost:11434/v1")
                );
            }
            
            // 5. Build Service Provider
            Services = services.BuildServiceProvider();
            
            // Warm up if needed or resolve main window
            // WPF starts MainWindow by StartupUri (Xaml); resolve ViewModels via DI if MVVM locator is implemented.
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
}
