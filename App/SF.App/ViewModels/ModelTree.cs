#region Usings
using CommunityToolkit.Mvvm.ComponentModel;
using SF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace SF.App.ViewModels;

internal enum ModelNodeKind
{
    Category = 0,
    Schema = 1,
    Table = 2,
    View = 3,
    Procedure = 4,
    Function = 5,
    Sequence = 6,
    Column = 7,
    Parameter = 8,
}

/// <summary>
/// One node of the model tree: category → schema → table, view, procedure, function or sequence → columns or parameters.
/// </summary>
internal sealed partial class ModelTreeNode : ViewModelBase
{
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Where the search text is in the title (-1 when the node is not a match); the tree highlights that part.
    /// </summary>
    [ObservableProperty]
    private int _matchStart = -1;

    private int _matchLength;
    private bool _expandedBySearch;

    public string Title { get; set; }

    public string TitleBefore
    {
        get
        {
            return MatchStart < 0 ? Title : Title.Substring(0, MatchStart);
        }
    }

    public string TitleMatch
    {
        get
        {
            return MatchStart < 0 ? "" : Title.Substring(MatchStart, _matchLength);
        }
    }

    public string TitleAfter
    {
        get
        {
            return MatchStart < 0 ? "" : Title.Substring(MatchStart + _matchLength);
        }
    }
    public string Detail { get; set; }

    /// <summary>
    /// The schema of a category that has only one (the schema level is then left out of the tree).
    /// </summary>
    public string Note { get; set; }

    private string _iconKey;

    /// <summary>
    /// The icon in front of the title: one per kind of object.
    /// </summary>
    public string IconKey
    {
        get
        {
            if(_iconKey != null)
                return _iconKey;

            switch(Kind)
            {
                case ModelNodeKind.Schema:
                    return "IconFolder";
                case ModelNodeKind.Table:
                    return "IconTable";
                case ModelNodeKind.View:
                    return "IconEye";
                case ModelNodeKind.Procedure:
                    return "IconProcedure";
                case ModelNodeKind.Function:
                    return "IconFunction";
                case ModelNodeKind.Sequence:
                    return "IconSequence";
                case ModelNodeKind.Parameter:
                    return "IconParameter";
                default:
                    return "IconColumn";
            }
        }
        set
        {
            _iconKey = value;
        }
    }
    public ModelNodeKind Kind { get; set; }
    public ModelTreeNode Parent { get; set; }
    public object Item { get; set; }
    public Schema Schema { get; set; }

    /// <summary>
    /// Every child, whatever the filter.
    /// </summary>
    public List<ModelTreeNode> AllChildren { get; } = new List<ModelTreeNode>();

    /// <summary>
    /// The children that match the filter (all of them when there is none).
    /// </summary>
    public ObservableCollection<ModelTreeNode> Children { get; } = new ObservableCollection<ModelTreeNode>();

    /// <summary>
    /// Shows the children that match <paramref name="filter"/> (by title, at any depth). Returns true when
    /// this node or one of its descendants matches.
    /// </summary>
    public bool ApplyFilter(string filter)
    {
        var start = string.IsNullOrWhiteSpace(filter) ? -1 : Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase);
        var selfMatches = string.IsNullOrWhiteSpace(filter) || start >= 0;
        var length = start < 0 ? 0 : filter.Length;
        if(start != MatchStart || length != _matchLength)
        {
            _matchLength = length;
            MatchStart = start;
            OnPropertyChanged(nameof(TitleBefore));
            OnPropertyChanged(nameof(TitleMatch));
            OnPropertyChanged(nameof(TitleAfter));
        }

        var shown = new List<ModelTreeNode>();
        foreach(var child in AllChildren)
        {
            //once a node matches, its whole subtree shows
            var childVisible = selfMatches ? child.ApplyFilter(null) : child.ApplyFilter(filter);
            if(childVisible)
            {
                child.Parent = this;
                shown.Add(child);
            }
        }

        ShownItems.Update(Children, shown);
        var anyChild = shown.Count > 0;

        //the search opens the nodes on the way to a match; clearing it folds back the ones it opened
        if(!string.IsNullOrWhiteSpace(filter) && anyChild && !selfMatches && !IsExpanded)
        {
            IsExpanded = true;
            _expandedBySearch = true;
        }
        else if(string.IsNullOrWhiteSpace(filter) && _expandedBySearch)
        {
            IsExpanded = false;
            _expandedBySearch = false;
        }

        //a category or schema says how many of its objects the search left: "2 of 14"
        var isGroup = Kind == ModelNodeKind.Category || Kind == ModelNodeKind.Schema;
        ShownDetail = isGroup && !string.IsNullOrWhiteSpace(filter) && !selfMatches ? $"{CountShownObjects()} of {Detail}" : Detail;

        return selfMatches || anyChild;
    }

    /// <summary>
    /// The detail as the tree shows it: <see cref="Detail"/>, or "2 of 14" on a group while searching.
    /// </summary>
    [ObservableProperty]
    private string _shownDetail;

    private int CountShownObjects()
    {
        var count = 0;
        foreach(var child in Children)
        {
            if(child.Kind == ModelNodeKind.Category || child.Kind == ModelNodeKind.Schema)
                count += child.CountShownObjects();
            else
                count++;
        }

        return count;
    }

    /// <summary>
    /// The nodes at or below this one whose own title matched the search.
    /// </summary>
    public int CountMatches()
    {
        var count = MatchStart >= 0 ? 1 : 0;
        foreach(var child in Children)
            count += child.CountMatches();

        return count;
    }
}

/// <summary>
/// Updates the rows a search shows by taking out and putting in only the ones that change, so the rows that stay
/// keep their state (expanded, selected) and the tree does not rebuild them on every key.
/// </summary>
internal static class ShownItems
{
    /// <param name="shown">What the tree shows now: some of the items, in their order.</param>
    /// <param name="wanted">What it should show: some of the same items, in the same order.</param>
    public static void Update<T>(ObservableCollection<T> shown, List<T> wanted) where T : class
    {
        var keep = new HashSet<T>(wanted);
        for(var i = shown.Count - 1; i >= 0; i--)
        {
            if(!keep.Contains(shown[i]))
                shown.RemoveAt(i);
        }

        //what is left is in the wanted order: put the missing ones in their places
        for(var i = 0; i < wanted.Count; i++)
        {
            if(i >= shown.Count || shown[i] != wanted[i])
                shown.Insert(i, wanted[i]);
        }
    }
}

/// <summary>
/// Builds the tree of one model.
/// </summary>
internal static class ModelTreeBuilder
{
    public static List<ModelTreeNode> Build(Model model)
    {
        var result = new List<ModelTreeNode>();
        if(model?.Categories == null)
            return result;

        foreach(var category in model.Categories)
        {
            var categoryNode = new ModelTreeNode
            {
                Title = GetCategoryTitle(category.Name),
                Detail = category.Count.ToString(),
                Kind = ModelNodeKind.Category,
                Item = category,
                IsExpanded = true,
                IconKey = GetCategoryIcon(category.Name),
            };

            //one schema: its objects sit right under the category, and the schema name is a note
            if(category.Schemas.Count == 1)
            {
                categoryNode.Note = category.Schemas[0].Name;
                AddObjects(categoryNode, category.Schemas[0]);
            }
            else
            {
                foreach(var schema in category.Schemas)
                {
                    var schemaNode = new ModelTreeNode { Title = schema.Name, Detail = schema.Count.ToString(), Kind = ModelNodeKind.Schema, Item = schema, Schema = schema };
                    AddObjects(schemaNode, schema);
                    categoryNode.AllChildren.Add(schemaNode);
                }
            }

            result.Add(categoryNode);
        }

        return result;
    }

    private static void AddObjects(ModelTreeNode schemaNode, Schema schema)
    {
        switch(schema.Type)
        {
            case ModelObjectType.Entities:
                AddEntities(schemaNode, schema, schema.Entities, ModelNodeKind.Table);
                break;
            case ModelObjectType.Views:
                AddEntities(schemaNode, schema, schema.Views, ModelNodeKind.View);
                break;
            case ModelObjectType.StoredProcs:
                if(schema.StoredProcs != null)
                {
                    foreach(var proc in schema.StoredProcs)
                    {
                        var node = new ModelTreeNode { Title = proc.N, Detail = Plural(proc.Count, "parameter"), Kind = ModelNodeKind.Procedure, Item = proc, Schema = schema };
                        AddProperties(node, proc.Par, ModelNodeKind.Parameter, schema);
                        schemaNode.AllChildren.Add(node);
                    }
                }

                break;
            case ModelObjectType.Functions:
                if(schema.Functions != null)
                {
                    foreach(var func in schema.Functions)
                    {
                        var node = new ModelTreeNode { Title = func.N, Detail = Plural(func.Count, "parameter"), Kind = ModelNodeKind.Function, Item = func, Schema = schema };
                        AddProperties(node, func.Par, ModelNodeKind.Parameter, schema);
                        schemaNode.AllChildren.Add(node);
                    }
                }

                break;
            case ModelObjectType.Sequences:
                if(schema.Sequences != null)
                {
                    foreach(var seq in schema.Sequences)
                        schemaNode.AllChildren.Add(new ModelTreeNode { Title = seq.N, Detail = "sequence", Kind = ModelNodeKind.Sequence, Item = seq, Schema = schema });
                }

                break;
        }
    }

    private static void AddEntities(ModelTreeNode schemaNode, Schema schema, EntityCollection entities, ModelNodeKind kind)
    {
        if(entities == null)
            return;

        foreach(var entity in entities)
        {
            var node = new ModelTreeNode { Title = entity.N ?? entity.SN, Detail = Plural(entity.Count, "column"), Kind = kind, Item = entity, Schema = schema };
            AddProperties(node, entity.Ps, ModelNodeKind.Column, schema);
            schemaNode.AllChildren.Add(node);
        }
    }

    private static void AddProperties(ModelTreeNode parent, PropertyCollection properties, ModelNodeKind kind, Schema schema)
    {
        if(properties == null)
            return;

        foreach(var property in properties)
            parent.AllChildren.Add(new ModelTreeNode { Title = property.N ?? property.SN, Detail = property.T, Kind = kind, Item = property, Schema = schema });
    }

    /// <summary>
    /// The category names the model file uses, as the screens show them.
    /// </summary>
    public static string GetCategoryTitle(string categoryName)
    {
        if(string.Equals(categoryName, CategoryNames.Entities, StringComparison.Ordinal))
            return "Tables";

        if(string.Equals(categoryName, CategoryNames.Procs, StringComparison.Ordinal))
            return "Stored procedures";

        return categoryName;
    }

    private static string GetCategoryIcon(string categoryName)
    {
        if(string.Equals(categoryName, CategoryNames.Entities, StringComparison.Ordinal))
            return "IconTable";

        if(string.Equals(categoryName, CategoryNames.Views, StringComparison.Ordinal))
            return "IconEye";

        if(string.Equals(categoryName, CategoryNames.Procs, StringComparison.Ordinal))
            return "IconProcedure";

        if(string.Equals(categoryName, CategoryNames.Funcs, StringComparison.Ordinal))
            return "IconFunction";

        return "IconSequence";
    }

    public static string Plural(int count, string word)
    {
        return count == 1 ? $"1 {word}" : $"{count} {word}s";
    }
}
