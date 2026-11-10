#region Usings
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SF.App.Services;
using SF.Common.Logging;
using SF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// The models of the open file: the model list, the searchable tree, read-only details and a code preview.
/// </summary>
internal sealed partial class ModelsPageViewModel : PageViewModel
{
    private List<ModelTreeNode> _allRoots = new List<ModelTreeNode>();
    private Model _shownModel;

    [ObservableProperty]
    private string _searchText;

    [ObservableProperty]
    private ModelTreeNode _selectedNode;

    [ObservableProperty]
    private DetailsViewModel _details;

    [ObservableProperty]
    private CodeFileViewModel _selectedCodeFile;

    public ModelsPageViewModel(MainWindowViewModel shell) : base(shell)
    {
    }

    public override string Title
    {
        get
        {
            return "Models";
        }
    }

    public ObservableCollection<ModelTreeNode> Roots { get; } = new ObservableCollection<ModelTreeNode>();
    public ObservableCollection<CodeFileViewModel> CodeFiles { get; } = new ObservableCollection<CodeFileViewModel>();

    public bool HasModels
    {
        get
        {
            return Shell.Document != null && Shell.Document.Container.Models.Count > 0;
        }
    }

    public bool HasCode
    {
        get
        {
            return CodeFiles.Count > 0;
        }
    }

    /// <summary>
    /// "185 tables · 2 views · 22 procedures · 4 functions · 2 sequences" for the selected model.
    /// </summary>
    public string Summary
    {
        get
        {
            return Describe(Shell.SelectedModel);
        }
    }

    /// <summary>
    /// "14 tables · 2 views · 5 procedures · 4 functions · 2 sequences"; null without a model.
    /// </summary>
    public static string Describe(Model model)
    {
        if(model?.Categories == null)
            return null;

        return $"{ModelTreeBuilder.Plural(Count(model, CategoryNames.Entities), "table")} · {ModelTreeBuilder.Plural(Count(model, CategoryNames.Views), "view")} · " +
            $"{ModelTreeBuilder.Plural(Count(model, CategoryNames.Procs), "procedure")} · {ModelTreeBuilder.Plural(Count(model, CategoryNames.Funcs), "function")} · " +
            $"{ModelTreeBuilder.Plural(Count(model, CategoryNames.Seqs), "sequence")}";
    }

    public string ConnectionText
    {
        get
        {
            var conInfo = Shell.SelectedModel?.ConInfo;
            if(conInfo == null)
                return null;

            return $"{conInfo.Server} / {conInfo.Database}";
        }
    }

    public override void OnShown()
    {
        Refresh(false);
    }

    public override void OnDocumentChanged()
    {
        Refresh(true);
    }

    private void Refresh(bool force)
    {
        var model = Shell.SelectedModel;
        if(!force && model == _shownModel && _allRoots.Count > 0)
        {
            //the same model: the Layers and Settings pages may have changed the names the code uses
            RefreshCodePreview();
            return;
        }

        _shownModel = model;
        _allRoots = ModelTreeBuilder.Build(model);
        SelectedNode = null;
        ApplyFilter();

        OnPropertyChanged(nameof(HasModels));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(ConnectionText));
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var shown = new List<ModelTreeNode>();
        var matches = 0;
        foreach(var root in _allRoots)
        {
            if(root.ApplyFilter(SearchText))
            {
                shown.Add(root);
                matches += root.CountMatches();
            }
        }

        ShownItems.Update(Roots, shown);
        MatchText = DescribeMatches(SearchText, matches);
    }

    /// <summary>
    /// "12 matches" under the search box while searching; null otherwise.
    /// </summary>
    [ObservableProperty]
    private string _matchText;

    public static string DescribeMatches(string search, int matches)
    {
        if(string.IsNullOrWhiteSpace(search))
            return null;

        if(matches == 0)
            return "No matches";

        return matches == 1 ? "1 match" : $"{matches} matches";
    }

    partial void OnSelectedNodeChanged(ModelTreeNode value)
    {
        //show the selected node: open every node above it
        var parent = value?.Parent;
        while(parent != null)
        {
            parent.IsExpanded = true;
            parent = parent.Parent;
        }

        Details = DetailsBuilder.Build(value);
        BuildCodePreview(value);
    }

    /// <summary>
    /// Builds the selected object's code again, keeping the file that was shown.
    /// </summary>
    private void RefreshCodePreview()
    {
        var fileName = SelectedCodeFile?.FileName;
        BuildCodePreview(SelectedNode);
        foreach(var file in CodeFiles)
        {
            if(string.Equals(file.FileName, fileName, StringComparison.Ordinal))
            {
                SelectedCodeFile = file;
                return;
            }
        }
    }

    private void BuildCodePreview(ModelTreeNode node)
    {
        CodeFiles.Clear();
        SelectedCodeFile = null;

        if(node == null || Shell.IsBusy || Shell.Document == null || Shell.SelectedModel == null)
        {
            OnPropertyChanged(nameof(HasCode));
            return;
        }

        if(node.Kind != ModelNodeKind.Table && node.Kind != ModelNodeKind.View && node.Kind != ModelNodeKind.Procedure && node.Kind != ModelNodeKind.Function)
        {
            OnPropertyChanged(nameof(HasCode));
            return;
        }

        try
        {
            foreach(var file in CodePreview.ForObject(Shell.Document.Container, Shell.SelectedModel, node.Schema, node.Item))
                CodeFiles.Add(file);
        }
        catch(Exception ex)
        {
            LogManager.WriteErrorLog(ex, "Code preview failed", node.Title);
        }

        if(CodeFiles.Count > 0)
            SelectedCodeFile = CodeFiles[0];

        OnPropertyChanged(nameof(HasCode));
    }

    [RelayCommand]
    private void AddModel()
    {
        Shell.StartAddModel();
    }

    [RelayCommand]
    private void UpdateModel()
    {
        Shell.StartUpdateModel();
    }

    [RelayCommand]
    private async Task DeleteModelAsync()
    {
        await Shell.DeleteSelectedModelAsync();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = null;
    }

    private static int Count(Model model, string category)
    {
        var found = model.Categories.GetByName(category);
        return found == null ? 0 : found.Count;
    }
}
