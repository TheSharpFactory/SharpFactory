#region Usings
using Avalonia.Controls;
using Avalonia.Interactivity;
using SF.App.ViewModels;
using System;
using System.Collections.Specialized;
#endregion

namespace SF.App.Views;

internal sealed partial class ProgressView : UserControl
{
    private ProgressViewModel _viewModel;

    public ProgressView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if(_viewModel != null)
            _viewModel.Entries.CollectionChanged -= OnEntriesChanged;

        _viewModel = DataContext as ProgressViewModel;
        if(_viewModel != null)
            _viewModel.Entries.CollectionChanged += OnEntriesChanged;
    }

    private async void OnCopyLogClick(object sender, RoutedEventArgs e)
    {
        await LogCopier.CopyAsync(this, this.FindControl<TextBlock>("CopyProgressLogLabel"), _viewModel?.CopyText);
    }

    /// <summary>
    /// Keeps the newest line in view while the log grows.
    /// </summary>
    private void OnEntriesChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if(e.Action != NotifyCollectionChangedAction.Add || _viewModel == null || _viewModel.Entries.Count == 0)
            return;

        var log = this.FindControl<ListBox>("ProgressLog");
        log?.ScrollIntoView(_viewModel.Entries.Count - 1);
    }
}
