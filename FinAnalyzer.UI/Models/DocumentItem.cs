using CommunityToolkit.Mvvm.ComponentModel;

namespace FinAnalyzer.UI.Models;

/// <summary>
/// Represents a document in the repository.
/// Uses ObservableObject to support status updates.
/// </summary>
public sealed partial class DocumentItem : ObservableObject
{
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _fileType = string.Empty;

    [ObservableProperty]
    private string _fileSize = string.Empty;

    [ObservableProperty]
    private DocumentStatus _status;

    [ObservableProperty]
    private DateTime _lastModified;

    [ObservableProperty]
    private double _progress = 100;
}

/// <summary>
/// Processing status of a document.
/// </summary>
public enum DocumentStatus
{
    Ingested,
    Processing,
    Pending,
    Archived,
    Error
}
