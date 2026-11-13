#region Usings
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SF.App.ViewModels;
using System.Threading.Tasks;
#endregion

namespace SF.App.Views;

internal sealed partial class MainWindow : Window
{
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Escape closes a message or question as its Cancel would.
    /// </summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if(e.Key != Key.Escape || DataContext is not MainWindowViewModel viewModel || viewModel.Dialog == null)
            return;

        viewModel.Dialog.Cancel();
        e.Handled = true;
    }

    /// <summary>
    /// With unsaved changes, asks first; the window closes only when the user saves or discards them.
    /// </summary>
    private void OnClosing(object sender, WindowClosingEventArgs e)
    {
        if(_closeConfirmed || DataContext is not MainWindowViewModel viewModel)
            return;

        //closing now would stop a Sync halfway (files written, the solution not saved)
        if(viewModel.IsBusy)
        {
            e.Cancel = true;
            _ = viewModel.ShowMessageAsync("Please wait", "A model build, Sync or update is running. Close the app when it has finished.", false);
            return;
        }

        if(viewModel.Document == null || !viewModel.Document.IsDirty)
            return;

        e.Cancel = true;
        _ = ConfirmAndCloseAsync(viewModel);
    }

    /// <summary>
    /// Closes the app without the unsaved-changes question (the update asked it before it started).
    /// </summary>
    public void CloseWithoutAsking()
    {
        _closeConfirmed = true;
        Close();
    }

    private async Task ConfirmAndCloseAsync(MainWindowViewModel viewModel)
    {
        if(!await viewModel.ConfirmDiscardChangesAsync())
            return;

        _closeConfirmed = true;
        Close();
    }
}
