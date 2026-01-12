using System.Collections.ObjectModel;
using FinAnalyzer.UI.Models;

namespace FinAnalyzer.UI.Services;

/// <summary>
/// Shared store for documents accessible across views.
/// Singleton service to share document state between DocumentsViewModel and ChatViewModel.
/// </summary>
public class DocumentStore
{
    /// <summary>
    /// All documents in the repository.
    /// </summary>
    public ObservableCollection<DocumentItem> Documents { get; } = [];

    /// <summary>
    /// Number of successfully ingested documents.
    /// </summary>
    public int IngestedCount => Documents.Count(d => d.Status == DocumentStatus.Ingested);

    /// <summary>
    /// Number of documents currently processing.
    /// </summary>
    public int PendingCount => Documents.Count(d => d.Status == DocumentStatus.Processing);

    /// <summary>
    /// Add a new document to the store.
    /// </summary>
    public void AddDocument(DocumentItem document)
    {
        Documents.Add(document);
    }

    /// <summary>
    /// Get all ingested documents (ready for chat).
    /// </summary>
    public IEnumerable<DocumentItem> GetIngestedDocuments()
    {
        return Documents.Where(d => d.Status == DocumentStatus.Ingested);
    }

    /// <summary>
    /// Get document by filename.
    /// </summary>
    public DocumentItem? GetDocumentByName(string fileName)
    {
        return Documents.FirstOrDefault(d => 
            d.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }
}
