#region Usings
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.TextMate;
using SF.App.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using TextMateSharp.Grammars;
#endregion

namespace SF.App.Views;

internal sealed partial class SyncPage : UserControl
{
    private readonly DiffLineBackground _background = new DiffLineBackground();
    private readonly DiffMargin _margin = new DiffMargin();
    private SyncPageViewModel _viewModel;
    private TextMate.Installation _textMate;
    private RegistryOptions _registry;
    private bool _editorReady;
    private int _scrollToLine;

    public SyncPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;
    }

    private void OnAttached(object sender, VisualTreeAttachmentEventArgs e)
    {
        var editor = this.FindControl<TextEditor>("DiffEditor");
        if(editor == null)
            return;

        if(!_editorReady)
        {
            editor.TextArea.TextView.BackgroundRenderers.Add(_background);
            editor.TextArea.LeftMargins.Insert(0, _margin);
            _editorReady = true;
        }

        if(_textMate == null)
        {
            try
            {
                _registry = new RegistryOptions(ThemeName.DarkPlus);
                _textMate = editor.InstallTextMate(_registry);
                _textMate.SetGrammar(_registry.GetScopeByLanguageId(_registry.GetLanguageByExtension(".cs").Id));
            }
            catch(Exception)
            {
                //no highlighting is fine: the diff still shows
                _textMate = null;
            }
        }

        ShowDiff();
    }

    private void OnDetached(object sender, VisualTreeAttachmentEventArgs e)
    {
        _textMate?.Dispose();
        _textMate = null;
    }

    private void OnSplitterDragCompleted(object sender, VectorEventArgs e)
    {
        PaneWidths.Remember(this.FindControl<Grid>("ChangesGrid"), PaneWidths.SyncFiles, _viewModel?.Shell.Settings);
    }

    private async void OnCopyLogClick(object sender, RoutedEventArgs e)
    {
        await LogCopier.CopyAsync(this, this.FindControl<TextBlock>("CopySyncLogLabel"), _viewModel?.Progress.CopyText);
    }

    private void ScrollToFirstChange()
    {
        var editor = this.FindControl<TextEditor>("DiffEditor");
        if(editor == null || _scrollToLine <= 0 || _scrollToLine > editor.Document.LineCount)
            return;

        var textView = editor.TextArea.TextView;
        textView.EnsureVisualLines();
        var top = textView.GetVisualTopByDocumentLine(_scrollToLine);
        editor.ScrollToVerticalOffset(Math.Max(0, top - editor.Bounds.Height / 3));
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if(_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as SyncPageViewModel;
        if(_viewModel != null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        PaneWidths.Restore(this.FindControl<Grid>("ChangesGrid"), PaneWidths.SyncFiles, _viewModel?.Shell.Settings);
        ShowDiff();
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if(string.Equals(e.PropertyName, nameof(SyncPageViewModel.Diff), StringComparison.Ordinal))
            ShowDiff();
    }

    /// <summary>
    /// The editor's text is not bindable: set it, and the line kinds for the tints and the gutter, when the diff changes.
    /// </summary>
    private void ShowDiff()
    {
        var editor = this.FindControl<TextEditor>("DiffEditor");
        if(editor == null)
            return;

        var diff = _viewModel?.Diff;
        var hasText = diff != null && !diff.HasMessage;
        _background.Lines = hasText ? diff.Lines : new List<DiffLineViewModel>();
        _background.IsTinted = hasText && diff.IsTinted;
        _margin.Lines = _background.Lines;
        _margin.IsTinted = _background.IsTinted;

        editor.IsVisible = hasText;
        editor.Text = hasText ? diff.Text : "";
        editor.ScrollToHome();

        //a changed file opens at its first change, a third of the way down
        _scrollToLine = hasText && diff.IsTinted ? diff.FirstChangedLine : 0;
        if(_scrollToLine > 0)
        {
            editor.ScrollTo(_scrollToLine, 0);
            Dispatcher.UIThread.Post(ScrollToFirstChange, DispatcherPriority.Background);
        }

        editor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        _margin.InvalidateVisual();
    }
}
