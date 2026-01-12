using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FinAnalyzer.UI.Models;
using FinAnalyzer.UI.Services;
using FinAnalyzer.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace FinAnalyzer.UI.ViewModels;

/// <summary>
/// ViewModel for the Dashboard view displaying system health and pipeline status.
/// </summary>
public sealed partial class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly ServiceHealthChecker _healthChecker;
    private readonly AISettings _aiSettings;
    private readonly QdrantSettings _qdrantSettings;
    private readonly TeiSettings _teiSettings;
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private ObservableCollection<ServiceHealthInfo> _serviceHealth = [];

    [ObservableProperty]
    private ObservableCollection<PipelineStep> _pipelineSteps = [];

    [ObservableProperty]
    private ObservableCollection<DocumentItem> _recentReports = [];

    [ObservableProperty]
    private string _lastPipelineRun = "Never";

    public DashboardViewModel(
        ServiceHealthChecker healthChecker,
        IOptions<AISettings> aiSettings,
        IOptions<QdrantSettings> qdrantSettings,
        IOptions<TeiSettings> teiSettings)
    {
        _healthChecker = healthChecker;
        _aiSettings = aiSettings.Value;
        _qdrantSettings = qdrantSettings.Value;
        _teiSettings = teiSettings.Value;

        // Initialize health indicators
        ServiceHealth.Add(new ServiceHealthInfo 
        { 
            ServiceName = "Vector DB", 
            DisplayName = "Qdrant Vector DB",
            Status = ServiceStatus.Unknown 
        });
        ServiceHealth.Add(new ServiceHealthInfo 
        { 
            ServiceName = "Ollama", 
            DisplayName = "Ollama LLM",
            Status = ServiceStatus.Unknown 
        });
        ServiceHealth.Add(new ServiceHealthInfo 
        { 
            ServiceName = "Reranker", 
            DisplayName = "TEI Reranker",
            Status = ServiceStatus.Unknown 
        });

        // Setup timer for every 30 seconds
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _timer.Tick += async (s, e) => await CheckHealthAsync();
        _timer.Start();

        // Initial check
        Task.Run(CheckHealthAsync);
    }

    private async Task CheckHealthAsync()
    {
        var aiEndpoint = _aiSettings.ChatEndpoint ?? "http://localhost:11434";
        var qdrantUrl = $"http://{_qdrantSettings.Host}:{_qdrantSettings.HttpPort}";
        var teiUrl = _teiSettings.BaseUrl ?? "http://localhost:8080";

        var results = await _healthChecker.CheckAllAsync(qdrantUrl, aiEndpoint, teiUrl);

        // Update UI on main thread
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ServiceHealth.Clear();
            foreach (var result in results)
            {
                ServiceHealth.Add(new ServiceHealthInfo 
                { 
                    ServiceName = result.ServiceName,
                    DisplayName = result.ServiceName, // Use service name as display name for now
                    Status = result.IsOnline ? ServiceStatus.Online : ServiceStatus.Offline
                });
            }
        });
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}
