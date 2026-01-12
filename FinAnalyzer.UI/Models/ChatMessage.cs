using CommunityToolkit.Mvvm.ComponentModel;

namespace FinAnalyzer.UI.Models;

/// <summary>
/// Represents a chat message in the RAG analysis interface.
/// Uses ObservableObject to support streaming content updates.
/// </summary>
public sealed partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private bool _isUser;

    [ObservableProperty]
    private DateTime _timestamp;

    [ObservableProperty]
    private List<Citation>? _citations;
}

/// <summary>
/// Represents a source citation from a document.
/// </summary>
public sealed class Citation
{
    public required string FileName { get; init; }
    public required string Section { get; init; }
    public required int PageNumber { get; init; }
}
