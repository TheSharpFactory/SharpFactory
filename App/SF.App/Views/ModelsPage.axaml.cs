#region Usings
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using SF.App.ViewModels;
using System;
using System.ComponentModel;
using TextMateSharp.Grammars;
#endregion

namespace SF.App.Views;

internal sealed partial class ModelsPage : UserControl
{
    //the code takes at most this share of the width for the tree while the Generated code tab is open
    private const double _treeShareWithCode = 0.38;

    private ModelsPageViewModel _viewModel;
    private TextMate.Installation _textMate;
    private RegistryOptions _registry;

    //the tree's width before the Generated code tab narrowed it; null when it did not
    private double? _treeWidthBeforeCode;

    public ModelsPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;

        var tabs = this.FindControl<ListBox>("CodeFileTabs");
        if(tabs != null)
        {
            tabs.AddHandler(ScrollViewer.ScrollChangedEvent, OnTabsScrollChanged);
            tabs.AddHandler(PointerWheelChangedEvent, OnTabsWheel, RoutingStrategies.Tunnel);
        }
    }

    private void OnAttached(object sender, VisualTreeAttachmentEventArgs e)
    {
        PaneWidths.Restore(this.FindControl<Grid>("ModelsGrid"), PaneWidths.ModelsTree, _viewModel?.Shell.Settings);

        var editor = this.FindControl<TextEditor>("CodeEditor");
        if(editor == null || _textMate != null)
            return;

        try
        {
            _registry = new RegistryOptions(ThemeName.DarkPlus);
            _textMate = editor.InstallTextMate(_registry);
            _textMate.SetGrammar(_registry.GetScopeByLanguageId(_registry.GetLanguageByExtension(".cs").Id));
        }
        catch(Exception)
        {
            //no highlighting is fine: the code still shows
            _textMate = null;
        }

        ShowCode();
    }

    private void OnDetached(object sender, VisualTreeAttachmentEventArgs e)
    {
        _textMate?.Dispose();
        _textMate = null;
    }

    private void OnSplitterDragCompleted(object sender, VectorEventArgs e)
    {
        //a width the user chose stays, on either tab
        _treeWidthBeforeCode = null;
        PaneWidths.Remember(this.FindControl<Grid>("ModelsGrid"), PaneWidths.ModelsTree, _viewModel?.Shell.Settings);
    }

    /// <summary>
    /// Code needs the width: while the Generated code tab is open the tree gives up its width past a share of the page,
    /// and gets it back on Details. A drag of the splitter keeps whatever the user chose.
    /// </summary>
    private void OnDetailsTabChanged(object sender, SelectionChangedEventArgs e)
    {
        //the tab control picks its first tab while the page loads, before its names can be looked up
        if(VisualRoot == null)
            return;

        var tabs = sender as TabControl;
        var grid = this.FindControl<Grid>("ModelsGrid");
        if(tabs == null || grid == null || !ReferenceEquals(e.Source, tabs) || grid.ColumnDefinitions.Count == 0)
            return;

        var column = grid.ColumnDefinitions[0];
        if(tabs.SelectedIndex == 1)
        {
            var limit = Math.Max(column.MinWidth, grid.Bounds.Width * _treeShareWithCode);
            if(grid.Bounds.Width > 0 && column.ActualWidth > limit)
            {
                _treeWidthBeforeCode = column.ActualWidth;
                column.Width = new GridLength(limit);
            }

            return;
        }

        if(_treeWidthBeforeCode.HasValue)
        {
            column.Width = new GridLength(_treeWidthBeforeCode.Value);
            _treeWidthBeforeCode = null;
        }
    }

    /// <summary>
    /// The code file tabs fade at an edge that has more tabs past it.
    /// </summary>
    private void OnTabsScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if(e.Source is ScrollViewer scroller)
            ShowTabCues(scroller);
    }

    private void ShowTabCues(ScrollViewer scroller)
    {
        var left = this.FindControl<Border>("TabsMoreLeft");
        var right = this.FindControl<Border>("TabsMoreRight");
        if(left == null || right == null)
            return;

        left.IsVisible = scroller.Offset.X > 1;
        right.IsVisible = scroller.Offset.X + scroller.Viewport.Width < scroller.Extent.Width - 1;
    }

    /// <summary>
    /// The mouse wheel scrolls the row of tabs sideways.
    /// </summary>
    private void OnTabsWheel(object sender, PointerWheelEventArgs e)
    {
        var scroller = FindScroller(this.FindControl<ListBox>("CodeFileTabs"));
        if(scroller == null || scroller.Extent.Width <= scroller.Viewport.Width)
            return;

        var delta = Math.Abs(e.Delta.X) > Math.Abs(e.Delta.Y) ? e.Delta.X : e.Delta.Y;
        var x = Math.Clamp(scroller.Offset.X - delta * 60, 0, scroller.Extent.Width - scroller.Viewport.Width);
        scroller.Offset = new Vector(x, 0);
        e.Handled = true;
    }

    private static ScrollViewer FindScroller(Control control)
    {
        if(control == null)
            return null;

        foreach(var visual in control.GetVisualDescendants())
        {
            if(visual is ScrollViewer scroller)
                return scroller;
        }

        return null;
    }

    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if(_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as ModelsPageViewModel;
        if(_viewModel != null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        PaneWidths.Restore(this.FindControl<Grid>("ModelsGrid"), PaneWidths.ModelsTree, _viewModel?.Shell.Settings);
        ShowCode();
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if(string.Equals(e.PropertyName, nameof(ModelsPageViewModel.SelectedCodeFile), StringComparison.Ordinal))
            ShowCode();

        //a column, schema or sequence has no code: the hidden code tab would stay selected, empty, so Details shows
        if(string.Equals(e.PropertyName, nameof(ModelsPageViewModel.HasCode), StringComparison.Ordinal) && _viewModel != null && !_viewModel.HasCode)
        {
            var tabs = this.FindControl<TabControl>("DetailsTabs");
            if(tabs != null)
                tabs.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// The editor's text is not bindable: set it when the selected file changes.
    /// </summary>
    private void ShowCode()
    {
        var editor = this.FindControl<TextEditor>("CodeEditor");
        if(editor == null)
            return;

        editor.Text = _viewModel?.SelectedCodeFile?.Content ?? "";
        editor.ScrollToHome();

        //the selected file's tab is always in view, even when it sits past the edge of the row
        var selected = _viewModel?.SelectedCodeFile;
        if(selected != null)
            this.FindControl<ListBox>("CodeFileTabs")?.ScrollIntoView(selected);
    }
}
