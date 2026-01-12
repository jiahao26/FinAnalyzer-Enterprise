using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FinAnalyzer.UI.ViewModels;

namespace FinAnalyzer.UI.Views;

/// <summary>
/// Documents view displaying document repository.
/// </summary>
public partial class DocumentsView : UserControl
{
    public DocumentsView()
    {
        InitializeComponent();
    }

    private void UserControl_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var pdfFiles = files.Where(f => f.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase)).ToArray();
            
            if (pdfFiles.Length > 0 && DataContext is DocumentsViewModel vm)
            {
                vm.HandleDropCommand.Execute(pdfFiles);
            }
        }
    }
}

