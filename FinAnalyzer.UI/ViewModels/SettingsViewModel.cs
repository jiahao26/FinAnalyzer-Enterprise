using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinAnalyzer.UI.Services;
using Microsoft.Extensions.Options;
using FinAnalyzer.Core.Configuration;

namespace FinAnalyzer.UI.ViewModels;

/// <summary>
/// ViewModel for the Settings view to configure Docker service endpoints.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ServiceHealthChecker _healthChecker = new();
    private readonly AISettings _aiSettings;
    private readonly FinAnalyzer.Core.Interfaces.IVectorDbService _vectorDbService;

    public SettingsViewModel(IOptions<AISettings> options, FinAnalyzer.Core.Interfaces.IVectorDbService vectorDbService)
    {
        _aiSettings = options.Value;
        _vectorDbService = vectorDbService;
        _ollamaEndpoint = _aiSettings.ChatEndpoint ?? "http://localhost:11434";
        _ollamaModelName = _aiSettings.ChatModelId ?? "llama3:8b-instruct-q8_0";
        _embeddingModelName = _aiSettings.EmbeddingModelId ?? "nomic-embed-text";
    }

    [ObservableProperty]
    private string _qdrantUrl = "http://localhost:6333";

    [ObservableProperty]
    private string _ollamaEndpoint;

    [ObservableProperty]
    private string _ollamaModelName;

    [ObservableProperty]
    private string _teiRerankerEndpoint = "http://localhost:8080";

    [ObservableProperty]
    private string _embeddingModelName;

    [ObservableProperty]
    private bool _isTestingConnection;

    [ObservableProperty]
    private string _connectionTestResult = string.Empty;

    [ObservableProperty]
    private bool _qdrantOnline;

    [ObservableProperty]
    private bool _ollamaOnline;

    [ObservableProperty]
    private bool _teiOnline;

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTestingConnection = true;
        ConnectionTestResult = "Checking services...";

        var results = await _healthChecker.CheckAllAsync(QdrantUrl, OllamaEndpoint, TeiRerankerEndpoint);

        var sb = new StringBuilder();
        foreach (var result in results)
        {
            var icon = result.IsOnline ? "✓" : "✗";
            sb.AppendLine($"{icon} {result.ServiceName}: {result.Message}");

            // Update individual status properties
            switch (result.ServiceName)
            {
                case "Qdrant": QdrantOnline = result.IsOnline; break;
                case "Ollama": OllamaOnline = result.IsOnline; break;
                case "TEI Reranker": TeiOnline = result.IsOnline; break;
            }
        }

        var allOnline = results.All(r => r.IsOnline);
        if (allOnline)
        {
            sb.AppendLine("\n✓ All services ready!");
        }
        else
        {
            sb.AppendLine("\n⚠ Some services are offline. Run docker-compose up -d");
        }

        ConnectionTestResult = sb.ToString();
        IsTestingConnection = false;
    }

    [RelayCommand]
    private async Task StartDockerServicesAsync()
    {
        IsTestingConnection = true;
        ConnectionTestResult = "Starting Docker services...";

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = "up -d",
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                if (process.ExitCode == 0)
                {
                    ConnectionTestResult = "Docker services started. Testing connections...";
                    await Task.Delay(3000); // Wait for services to initialize
                    await TestConnectionAsync();
                }
                else
                {
                    ConnectionTestResult = $"Failed to start Docker:\n{error}";
                }
            }
        }
        catch (Exception ex)
        {
            ConnectionTestResult = $"Error: {ex.Message}\n\nMake sure Docker Desktop is running.";
        }

        IsTestingConnection = false;
    }

    [RelayCommand]
    private async Task ResetDatabaseAsync()
    {
        if (System.Windows.MessageBox.Show(
            "Are you sure you want to clear the Vector Database? You will need to re-ingest all documents.", 
            "Confirm Reset", 
            System.Windows.MessageBoxButton.YesNo, 
            System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
        {
            IsTestingConnection = true;
            ConnectionTestResult = "Resetting database...";
            
            try
            {
                await _vectorDbService.DeleteCollectionAsync("finance_docs");
                ConnectionTestResult = "✅ Database cleared successfully.";
            }
            catch (Exception ex)
            {
                ConnectionTestResult = $"❌ Failed to reset DB: {ex.Message}";
            }
            
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        // TODO: Save settings to appsettings.json
        ConnectionTestResult = "Settings saved (in-memory only for now)";
    }
}
