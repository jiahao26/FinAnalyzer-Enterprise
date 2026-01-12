using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinAnalyzer.Core.Interfaces;
using FinAnalyzer.UI.Models;
using FinAnalyzer.UI.Services;

namespace FinAnalyzer.UI.ViewModels;

/// <summary>
/// ViewModel for the Chat view providing RAG analysis interface.
/// </summary>
public sealed partial class ChatViewModel : ViewModelBase
{
    private readonly IRagService? _ragService;
    private readonly DocumentStore _documentStore;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = [];

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _selectedModel = "Local Ollama";

    [ObservableProperty]
    private DocumentItem? _selectedDocument;

    [ObservableProperty]
    private bool _isDocumentPickerOpen;

    /// <summary>
    /// Available documents for mention in chat.
    /// </summary>
    public IEnumerable<DocumentItem> AvailableDocuments => _documentStore.GetIngestedDocuments();

    /// <summary>
    /// Whether any documents are available.
    /// </summary>
    public bool HasDocuments => _documentStore.Documents.Any(d => d.Status == DocumentStatus.Ingested);

    public ChatViewModel(DocumentStore documentStore)
    {
        _documentStore = documentStore;
        _documentStore.Documents.CollectionChanged += (_, _) => 
        {
            OnPropertyChanged(nameof(AvailableDocuments));
            OnPropertyChanged(nameof(HasDocuments));
        };
    }

    public ChatViewModel(DocumentStore documentStore, IRagService ragService)
    {
        _documentStore = documentStore;
        _ragService = ragService;
        _documentStore.Documents.CollectionChanged += (_, _) => 
        {
            OnPropertyChanged(nameof(AvailableDocuments));
            OnPropertyChanged(nameof(HasDocuments));
        };
    }

    [RelayCommand]
    private void ToggleDocumentPicker()
    {
        IsDocumentPickerOpen = !IsDocumentPickerOpen;
    }

    [RelayCommand]
    private void SelectDocument(DocumentItem document)
    {
        SelectedDocument = document;
        IsDocumentPickerOpen = false;
        
        // Add document mention to input
        if (!string.IsNullOrEmpty(InputText) && !InputText.EndsWith(" "))
        {
            InputText += " ";
        }
        InputText += $"@{document.FileName} ";
    }

    [RelayCommand]
    private void ClearSelectedDocument()
    {
        SelectedDocument = null;
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
            return;

        var userMessage = new ChatMessage();
        userMessage.Content = InputText;
        userMessage.IsUser = true;
        userMessage.Timestamp = DateTime.Now;
        
        // Include document context if selected
        if (SelectedDocument != null)
        {
            userMessage.Content = $"[Re: {SelectedDocument.FileName}]\n{InputText}";
        }
        
        Messages.Add(userMessage);
        
        var question = InputText;
        InputText = string.Empty;
        IsProcessing = true;

        try
        {
            if (_ragService != null)
            {
                var responseBuilder = new StringBuilder();
                var aiMessage = new ChatMessage();
                aiMessage.Content = "";
                aiMessage.IsUser = false;
                aiMessage.Timestamp = DateTime.Now;
                aiMessage.Citations = [];
                Messages.Add(aiMessage);

                await foreach (var chunk in _ragService.QueryAsync(question))
                {
                    responseBuilder.Append(chunk);
                    aiMessage.Content = responseBuilder.ToString();
                }
            }
            else
            {
                // Demo response when no service
                var aiMessage = new ChatMessage
                {
                    IsUser = false,
                    Timestamp = DateTime.Now,
                    Citations = []
                };
                
                if (SelectedDocument != null)
                {
                    aiMessage.Content = $"I'm analyzing **{SelectedDocument.FileName}** based on your query.\n\n" +
                        "This is a demo response. Connect to Ollama for real AI analysis.";
                }
                else
                {
                    aiMessage.Content = "No document selected. Please select a document using the 📄 button, " +
                        "or upload one in the Documents tab.";
                }
                
                Messages.Add(aiMessage);
            }
        }
        catch (Exception ex)
        {
            // If we have an existing AI message (partial stream or empty), update it with error
            // to avoid leaving a blank bubble in the chat.
            var lastMessage = Messages.LastOrDefault();
            if (lastMessage != null && !lastMessage.IsUser)
            {
                lastMessage.Content += $"\n\n❌ Error: {ex.Message}";
            }
            else
            {
                // Fallback if no message exists yet
                var errorMessage = new ChatMessage
                {
                    Content = $"❌ Error: {ex.Message}",
                    IsUser = false,
                    Timestamp = DateTime.Now,
                    Citations = []
                };
                Messages.Add(errorMessage);
            }
        }
        finally
        {
            IsProcessing = false;
            SelectedDocument = null; // Clear after sending
        }
    }
}
