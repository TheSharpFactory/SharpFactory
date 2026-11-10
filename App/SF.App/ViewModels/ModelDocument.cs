#region Usings
using CommunityToolkit.Mvvm.ComponentModel;
using SF.Engine;
using SF.Models;
using SF.Utils;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// The open .edmj: its model container, where it is saved, and whether it has unsaved changes.
/// </summary>
internal sealed partial class ModelDocument : ViewModelBase
{
    [ObservableProperty]
    private string _filePath;

    [ObservableProperty]
    private bool _isDirty;

    public ModelContainer Container { get; }

    public ModelDocument(ModelContainer container, string filePath, bool isDirty)
    {
        Container = container;
        _filePath = filePath;
        _isDirty = isDirty;
        Attach();
    }

    /// <summary>
    /// The file name, or "Untitled" for a model that was never saved.
    /// </summary>
    public string DisplayName
    {
        get
        {
            return string.IsNullOrWhiteSpace(FilePath) ? "Untitled" : Path.GetFileName(FilePath);
        }
    }

    /// <summary>
    /// The model file as a document, or null with the reason it cannot be opened.
    /// </summary>
    public static ModelDocument Load(string filePath, out string problem)
    {
        var container = ModelFileCheck.Load(filePath, out problem);
        if(container == null)
            return null;

        return new ModelDocument(container, Path.GetFullPath(filePath), false);
    }

    public void Save(string filePath)
    {
        ModelUtils.Save(Container, filePath);
        FilePath = Path.GetFullPath(filePath);
        IsDirty = false;
    }

    /// <summary>
    /// Goes up with every change, saved or not: a page can tell whether the model changed since it last looked.
    /// </summary>
    public int ChangeCount { get; private set; }

    public void MarkDirty()
    {
        ChangeCount++;
        IsDirty = true;
    }

    partial void OnFilePathChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayName));
    }

    private void Attach()
    {
        Container.CommonSettings.PropertyChanged += OnSomethingChanged;
        foreach(var layer in LayerCatalog.GetCommonLayers(Container.CommonLayers))
            layer.Layer.PropertyChanged += OnSomethingChanged;

        foreach(var model in Container.Models)
            AttachModel(model);

        Container.Models.CollectionChanged += OnModelsChanged;
    }

    private void AttachModel(Model model)
    {
        foreach(var layer in LayerCatalog.GetModelLayers(model.Layers))
            layer.Layer.PropertyChanged += OnSomethingChanged;
    }

    private void OnModelsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if(e.NewItems != null)
        {
            foreach(var item in e.NewItems)
            {
                if(item is Model model)
                    AttachModel(model);
            }
        }

        MarkDirty();
    }

    private void OnSomethingChanged(object sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
    }
}

/// <summary>
/// A layer with the title the screens show for it.
/// </summary>
internal sealed class LayerEntry
{
    public string Title { get; set; }
    public string Description { get; set; }
    public LayerInfo Layer { get; set; }
}

/// <summary>
/// Every layer of a model and of the container, in the order the old app showed them.
/// </summary>
internal static class LayerCatalog
{
    public static List<LayerEntry> GetModelLayers(ModelLayers layers)
    {
        return new List<LayerEntry>
        {
            new LayerEntry { Title = "Entities", Description = "One class per table, view, procedure result and function result.", Layer = layers.Entity },
            new LayerEntry { Title = "Nav Property Enumerations", Description = "The navigation properties of each entity.", Layer = layers.NavPropEnum },
            new LayerEntry { Title = "Property Enumerations", Description = "The properties of each entity.", Layer = layers.PropEnum },
            new LayerEntry { Title = "Entity Utilities", Description = "Change tracking, merging and comparing entities.", Layer = layers.EntityUtil },
            new LayerEntry { Title = "Repositories", Description = "The data access for each table, view, procedure, function and sequence.", Layer = layers.Repository },
            new LayerEntry { Title = "Repository Interfaces", Description = "The interfaces of the repositories.", Layer = layers.IRepository },
        };
    }

    public static List<LayerEntry> GetCommonLayers(CommonLayers layers)
    {
        return new List<LayerEntry>
        {
            new LayerEntry { Title = "Sorting, Filtering and Pagination", Description = "The query composition classes (Query).", Layer = layers.Query },
            new LayerEntry { Title = "Exceptions", Description = "The exceptions the generated code throws.", Layer = layers.Exceptions },
            new LayerEntry { Title = "Repository Common", Description = "The base classes every repository uses.", Layer = layers.Repository },
            new LayerEntry { Title = "Repository Container", Description = "One place to get every repository.", Layer = layers.RepoContainer },
            new LayerEntry { Title = "Repository Container Interface", Description = "The interface of the repository container.", Layer = layers.IRepoContainer },
        };
    }
}
