using System.Windows.Controls;
using System.Windows.Input;
using FinAnalyzer.UI.ViewModels;

namespace FinAnalyzer.UI.Views;

/// <summary>
/// Chat view for RAG analysis interface.
/// </summary>
public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            
            if (DataContext is ChatViewModel viewModel && viewModel.SendMessageCommand.CanExecute(null))
            {
                viewModel.SendMessageCommand.Execute(null);
            }
        }
    }
}
