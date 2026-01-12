using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinAnalyzer.UI.ViewModels;

/// <summary>
/// Main ViewModel managing navigation between views.
/// </summary>
public sealed partial class MainViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly DocumentsViewModel _documentsViewModel;
    private readonly ChatViewModel _chatViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private int _selectedNavIndex;

    public MainViewModel(
        DashboardViewModel dashboardViewModel,
        DocumentsViewModel documentsViewModel,
        ChatViewModel chatViewModel,
        SettingsViewModel settingsViewModel)
    {
        _dashboardViewModel = dashboardViewModel;
        _documentsViewModel = documentsViewModel;
        _chatViewModel = chatViewModel;
        _settingsViewModel = settingsViewModel;

        _currentView = _dashboardViewModel;
        _selectedNavIndex = 0;
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentView = _dashboardViewModel;
        SelectedNavIndex = 0;
    }

    [RelayCommand]
    private void NavigateToDocuments()
    {
        CurrentView = _documentsViewModel;
        SelectedNavIndex = 1;
    }

    [RelayCommand]
    private void NavigateToChat()
    {
        CurrentView = _chatViewModel;
        SelectedNavIndex = 2;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentView = _settingsViewModel;
        SelectedNavIndex = 3;
    }
}
