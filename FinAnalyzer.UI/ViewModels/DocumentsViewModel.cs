using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinAnalyzer.Core.Interfaces;
using FinAnalyzer.UI.Models;
using FinAnalyzer.UI.Services;
using Microsoft.Win32;

namespace FinAnalyzer.UI.ViewModels;

/// <summary>
/// ViewModel for the Documents view displaying document repository.
/// </summary>
public sealed partial class DocumentsViewModel : ViewModelBase
{
    private readonly IRagService? _ragService;
    private readonly DocumentStore _documentStore;
    private CancellationTokenSource? _currentIngestionCts;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isIngesting;

    /// <summary>
    /// Documents from shared store.
    /// </summary>
    public ObservableCollection<DocumentItem> Documents => _documentStore.Documents;

    /// <summary>
    /// Count of ingested documents.
    /// </summary>
    public int IngestedCount => _documentStore.IngestedCount;

    /// <summary>
    /// Count of pending documents.
    /// </summary>
    public int PendingCount => _documentStore.PendingCount;

    public DocumentsViewModel(DocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    public DocumentsViewModel(DocumentStore documentStore, IRagService ragService)
    {
        _documentStore = documentStore;
        _ragService = ragService;
    }

    partial void OnSearchTextChanged(string value)
    {
        // TODO: Implement document filtering
    }

    [RelayCommand]
    private async Task AddDocumentAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
            Title = "Select Document to Ingest",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() != true)
            return;

        await IngestFileAsync(openFileDialog.FileName);
    }

    [RelayCommand]
    private void CancelIngestion()
    {
        _currentIngestionCts?.Cancel();
    }

    [RelayCommand]
    private async Task HandleDropAsync(string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                await IngestFileAsync(filePath);
            }
        }
    }

    private async Task IngestFileAsync(string filePath)
    {
        var fileName = System.IO.Path.GetFileName(filePath);

        var newDocument = new DocumentItem();
        newDocument.FileName = fileName;
        newDocument.FileType = "PDF";
        newDocument.FileSize = GetFileSize(filePath);
        newDocument.Status = DocumentStatus.Processing;
        newDocument.LastModified = DateTime.Now;
        newDocument.Progress = 0;

        _documentStore.AddDocument(newDocument);
        OnPropertyChanged(nameof(PendingCount));

        IsIngesting = true;
        _currentIngestionCts = new CancellationTokenSource();

        var progress = new Progress<int>(percent =>
        {
            newDocument.Progress = percent;
        });

        try
        {
            if (_ragService != null)
            {
                await _ragService.IngestDocumentAsync(filePath, progress, _currentIngestionCts.Token);
            }

            newDocument.Status = DocumentStatus.Ingested;
            newDocument.Progress = 100;
            OnPropertyChanged(nameof(IngestedCount));
            OnPropertyChanged(nameof(PendingCount));
        }
        catch (OperationCanceledException)
        {
            newDocument.Status = DocumentStatus.Error;
            _documentStore.Documents.Remove(newDocument);
        }
        catch (Exception ex)
        {
            FinAnalyzer.Engine.CentralLogger.Error($"Ingestion Error for {fileName}: {ex.Message}", ex);
            newDocument.Status = DocumentStatus.Error;
            System.Diagnostics.Debug.WriteLine($"Ingestion failed: {ex.Message}");
        }
        finally
        {
            IsIngesting = false;
            _currentIngestionCts?.Dispose();
            _currentIngestionCts = null;
        }
    }

    private static string GetFileSize(string filePath)
    {
        var fileInfo = new System.IO.FileInfo(filePath);
        var bytes = fileInfo.Length;
        
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
        };
    }
}
