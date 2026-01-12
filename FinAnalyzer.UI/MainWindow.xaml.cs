using System.Windows;
using FinAnalyzer.UI.ViewModels;

namespace FinAnalyzer.UI;

/// <summary>
/// Main application window with sidebar navigation.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}