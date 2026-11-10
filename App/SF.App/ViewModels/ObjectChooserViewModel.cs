#region Usings
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// The database objects to put in the model: category → schema → object, with check boxes, search and counts.
/// Checking a category or schema checks everything below it; the object check boxes are what the model uses.
/// </summary>
internal sealed partial class ObjectChooserViewModel : ViewModelBase
{
    private readonly List<ChooserNode> _allRoots = new List<ChooserNode>();
    private readonly List<ChooserNode> _objectNodes = new List<ChooserNode>();

    [ObservableProperty]
    private string _searchText;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private string _changeText;

    public ObjectChooserViewModel(DBObjectCategoryCollection categories) : this(categories, false)
    {
    }

    /// <param name="showChanges">Update: mark what the rebuild adds to or removes from the model.</param>
    public ObjectChooserViewModel(DBObjectCategoryCollection categories, bool showChanges)
    {
        Categories = categories;
        ShowChanges = showChanges;
        foreach(var category in categories)
        {
            var categoryIcon = ChooserIcon(category.Name);
            var categoryNode = new ChooserNode(this, null) { Title = category.Name, IconKey = categoryIcon, Detail = $"{ModelTreeBuilder.Plural(category.Schemas.Count, "schema")}, {ModelTreeBuilder.Plural(category.Count, "object")}", IsExpanded = true };
            foreach(var schema in category.Schemas)
            {
                var schemaNode = new ChooserNode(this, categoryNode) { Title = schema.Name, IconKey = "IconFolder", Detail = $"{schema.CategoryType} · {schema.Count}", Schema = schema };
                foreach(var dbObject in schema.Objects)
                {
                    var objectNode = new ChooserNode(this, schemaNode) { Title = dbObject.Name, DbObject = dbObject, IconKey = categoryIcon };
                    objectNode.Initialize(dbObject.IsSelected.HasValue && dbObject.IsSelected.Value);
                    schemaNode.AllChildren.Add(objectNode);
                    _objectNodes.Add(objectNode);
                }

                schemaNode.RecomputeFromChildren();
                categoryNode.AllChildren.Add(schemaNode);
            }

            categoryNode.RecomputeFromChildren();
            _allRoots.Add(categoryNode);
        }

        ApplyFilter();
        UpdateCounts();
    }

    public DBObjectCategoryCollection Categories { get; }
    public bool ShowChanges { get; }

    public bool HasChangeText
    {
        get
        {
            return ChangeText != null;
        }
    }
    public ObservableCollection<ChooserNode> Roots { get; } = new ObservableCollection<ChooserNode>();

    public int TotalCount
    {
        get
        {
            return _objectNodes.Count;
        }
    }

    public string CountText
    {
        get
        {
            return $"{SelectedCount} of {TotalCount} selected";
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedCountChanged(int value)
    {
        OnPropertyChanged(nameof(CountText));
    }

    partial void OnChangeTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasChangeText));
    }

    [RelayCommand]
    private void SelectAll()
    {
        SetAll(true);
    }

    [RelayCommand]
    private void SelectNone()
    {
        SetAll(false);
    }

    /// <summary>
    /// Select all and None act on what the search shows: everything when there is no search.
    /// </summary>
    private void SetAll(bool value)
    {
        foreach(var root in Roots)
            root.Check(value);
    }

    private void ApplyFilter()
    {
        var shown = new List<ChooserNode>();
        foreach(var root in _allRoots)
        {
            if(root.ApplyFilter(SearchText))
                shown.Add(root);
        }

        ShownItems.Update(Roots, shown);
    }

    public void UpdateCounts()
    {
        var count = 0;
        var added = 0;
        var removed = 0;
        foreach(var node in _objectNodes)
        {
            if(node.IsChecked == true)
                count++;

            if(node.IsChecked == true && !node.WasInModel)
                added++;
            else if(node.IsChecked != true && node.WasInModel)
                removed++;
        }

        SelectedCount = count;
        if(ShowChanges)
            ChangeText = Describe(added, removed);
    }

    /// <summary>
    /// The chooser's category names are the database's: Tables, Views, Stored Procedures, Functions, Sequences.
    /// </summary>
    private static string ChooserIcon(string categoryName)
    {
        var name = categoryName ?? "";
        if(name.Contains("View", StringComparison.OrdinalIgnoreCase))
            return "IconEye";

        if(name.Contains("Proc", StringComparison.OrdinalIgnoreCase))
            return "IconProcedure";

        if(name.Contains("Func", StringComparison.OrdinalIgnoreCase))
            return "IconFunction";

        if(name.Contains("Seq", StringComparison.OrdinalIgnoreCase))
            return "IconSequence";

        return "IconTable";
    }

    /// <summary>
    /// "Adds 2 objects and removes 1", or that the objects stay the same.
    /// </summary>
    public static string Describe(int added, int removed)
    {
        if(added == 0 && removed == 0)
            return "The same objects as now: the update reads their columns and parameters again.";

        var parts = new List<string>();
        if(added > 0)
            parts.Add("adds " + ModelTreeBuilder.Plural(added, "object"));

        if(removed > 0)
            parts.Add("removes " + ModelTreeBuilder.Plural(removed, "object"));

        var text = string.Join(" and ", parts);
        return "The update " + text + ", and reads the others again.";
    }
}

/// <summary>
/// One check box of the object chooser.
/// </summary>
internal sealed partial class ChooserNode : ViewModelBase
{
    private readonly ObjectChooserViewModel _owner;
    private readonly ChooserNode _parent;
    private bool? _isChecked = false;

    [ObservableProperty]
    private bool _isExpanded;

    public ChooserNode(ObjectChooserViewModel owner, ChooserNode parent)
    {
        _owner = owner;
        _parent = parent;
    }

    public string Title { get; set; }
    public string Detail { get; set; }

    /// <summary>
    /// The icon in front of the title, as in the model tree.
    /// </summary>
    public string IconKey { get; set; }

    public DbObjectSchema Schema { get; set; }
    public DbObject DbObject { get; set; }
    public List<ChooserNode> AllChildren { get; } = new List<ChooserNode>();
    public ObservableCollection<ChooserNode> Children { get; } = new ObservableCollection<ChooserNode>();

    /// <summary>
    /// True, false, or null when only some of the objects below are checked.
    /// </summary>
    public bool? IsChecked
    {
        get
        {
            return _isChecked;
        }
        set
        {
            //an object takes the box's value. A category or schema ignores it: the check box turns a partly checked
            //box into unchecked, but a click on one checks every object it shows, and unchecks them when all are checked
            if(AllChildren.Count == 0)
            {
                var newValue = value ?? false;
                if(_isChecked == newValue)
                    return;

                Check(newValue);
                return;
            }

            Check(!AreShownObjectsChecked());
        }
    }

    /// <summary>
    /// Checks or unchecks this node and the objects below it that the search shows.
    /// </summary>
    public void Check(bool value)
    {
        SetCheckedDown(value);
        _parent?.RecomputeFromChildren();
        _owner.UpdateCounts();

        //the check box set its own value during the click: raised again after it, so it shows the real state
        Dispatcher.UIThread.Post(RaiseIsChecked);
    }

    private void RaiseIsChecked()
    {
        OnPropertyChanged(nameof(IsChecked));
    }

    /// <summary>
    /// True when every object below this node that the search shows is checked.
    /// </summary>
    private bool AreShownObjectsChecked()
    {
        if(AllChildren.Count == 0)
            return _isChecked == true;

        foreach(var child in Children)
        {
            if(!child.AreShownObjectsChecked())
                return false;
        }

        return true;
    }

    public void Initialize(bool value)
    {
        _isChecked = value;
        WasInModel = value;
    }

    /// <summary>
    /// Checked when the chooser opened: for an update, the object is in the model now.
    /// </summary>
    public bool WasInModel { get; private set; }

    /// <summary>
    /// For an update: "new" when the object will be added, "removed" when it will be taken out.
    /// </summary>
    public string Mark
    {
        get
        {
            if(DbObject == null || !_owner.ShowChanges)
                return null;

            if(_isChecked == true && !WasInModel)
                return "new";

            if(_isChecked != true && WasInModel)
                return "removed";

            return null;
        }
    }

    private void SetCheckedDown(bool value)
    {
        if(AllChildren.Count == 0)
        {
            _isChecked = value;
            if(DbObject != null)
                DbObject.IsSelected = value;

            OnPropertyChanged(nameof(IsChecked));
            OnPropertyChanged(nameof(Mark));
            return;
        }

        //only what the search shows: the children it hides keep their check boxes
        foreach(var child in Children)
            child.SetCheckedDown(value);

        RecomputeOwnState();
    }

    public void RecomputeFromChildren()
    {
        RecomputeOwnState();
        _parent?.RecomputeFromChildren();
    }

    /// <summary>
    /// A category or schema is checked when all its objects are, unchecked when none are, partly checked otherwise.
    /// </summary>
    private void RecomputeOwnState()
    {
        if(AllChildren.Count == 0)
            return;

        var anyTrue = false;
        var anyFalse = false;
        foreach(var child in AllChildren)
        {
            if(child._isChecked == true)
                anyTrue = true;
            else if(child._isChecked == false)
                anyFalse = true;
            else
            {
                anyTrue = true;
                anyFalse = true;
            }
        }

        _isChecked = anyTrue && anyFalse ? null : (bool?)anyTrue;
        if(Schema != null)
            Schema.IsSelected = _isChecked;

        OnPropertyChanged(nameof(IsChecked));
    }

    public bool ApplyFilter(string filter)
    {
        var selfMatches = string.IsNullOrWhiteSpace(filter) || Title.Contains(filter, StringComparison.OrdinalIgnoreCase);
        var shown = new List<ChooserNode>();
        foreach(var child in AllChildren)
        {
            if(selfMatches ? child.ApplyFilter(null) : child.ApplyFilter(filter))
                shown.Add(child);
        }

        ShownItems.Update(Children, shown);
        var anyChild = shown.Count > 0;

        if(!string.IsNullOrWhiteSpace(filter) && anyChild)
            IsExpanded = true;

        return selfMatches || anyChild;
    }
}
