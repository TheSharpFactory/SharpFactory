#region Usings
using CommunityToolkit.Mvvm.ComponentModel;
using SF.Engine;
using SF.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// One layer's project name and namespace, with the full names they produce.
/// </summary>
internal sealed class LayerRowViewModel : ViewModelBase
{
    private readonly CommonSettings _settings;

    public LayerRowViewModel(LayerEntry entry, CommonSettings settings)
    {
        Title = entry.Title;
        Description = entry.Description;
        Layer = entry.Layer;
        _settings = settings;
        Layer.PropertyChanged += OnChanged;
        _settings.PropertyChanged += OnChanged;
    }

    public string Title { get; }
    public string Description { get; }
    public LayerInfo Layer { get; }

    public string FullProjectName
    {
        get
        {
            return Layer.FullProjectName(_settings);
        }
    }

    public string FullNamespace
    {
        get
        {
            return Layer.FullNamespace(_settings);
        }
    }

    /// <summary>
    /// Stops listening to the layer and settings (when the row is no longer shown).
    /// </summary>
    public void Detach()
    {
        Layer.PropertyChanged -= OnChanged;
        _settings.PropertyChanged -= OnChanged;
    }

    private void OnChanged(object sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(FullProjectName));
        OnPropertyChanged(nameof(FullNamespace));
    }
}

/// <summary>
/// The project name and namespace of every layer: the selected model's layers and the common layers.
/// </summary>
internal sealed class LayersPageViewModel : PageViewModel
{
    public LayersPageViewModel(MainWindowViewModel shell) : base(shell)
    {
    }

    public override string Title
    {
        get
        {
            return "Layers";
        }
    }

    public ObservableCollection<LayerRowViewModel> ModelLayers { get; } = new ObservableCollection<LayerRowViewModel>();
    public ObservableCollection<LayerRowViewModel> CommonLayers { get; } = new ObservableCollection<LayerRowViewModel>();

    public string ModelName
    {
        get
        {
            return Shell.SelectedModel?.Name;
        }
    }

    public bool HasModel
    {
        get
        {
            return Shell.SelectedModel != null;
        }
    }

    public override void OnShown()
    {
        Rebuild();
    }

    public override void OnDocumentChanged()
    {
        Rebuild();
    }

    private void Rebuild()
    {
        Clear(ModelLayers);
        Clear(CommonLayers);

        var document = Shell.Document;
        if(document != null)
        {
            var settings = document.Container.CommonSettings;
            if(Shell.SelectedModel != null)
            {
                foreach(var entry in LayerCatalog.GetModelLayers(Shell.SelectedModel.Layers))
                    ModelLayers.Add(new LayerRowViewModel(entry, settings));
            }

            foreach(var entry in LayerCatalog.GetCommonLayers(document.Container.CommonLayers))
                CommonLayers.Add(new LayerRowViewModel(entry, settings));
        }

        OnPropertyChanged(nameof(ModelName));
        OnPropertyChanged(nameof(HasModel));
    }

    private static void Clear(ObservableCollection<LayerRowViewModel> rows)
    {
        foreach(var row in rows)
            row.Detach();

        rows.Clear();
    }
}

/// <summary>
/// The settings every model shares.
/// </summary>
internal sealed partial class SettingsPageViewModel : PageViewModel
{
    [ObservableProperty]
    private string _indentSizeText;

    [ObservableProperty]
    private string _indentSizeError;

    public SettingsPageViewModel(MainWindowViewModel shell) : base(shell)
    {
    }

    public override string Title
    {
        get
        {
            return "Settings";
        }
    }

    public CommonSettings Settings
    {
        get
        {
            return Shell.Document?.Container.CommonSettings;
        }
    }

    public ObservableCollection<LayerRowViewModel> Preview { get; } = new ObservableCollection<LayerRowViewModel>();

    public bool UseRootNamespaceInProjectName
    {
        get
        {
            return Settings != null && Settings.UseRootNSInProjectName == YesOrNo.Yes;
        }
        set
        {
            if(Settings == null)
                return;

            Settings.UseRootNSInProjectName = value ? YesOrNo.Yes : YesOrNo.No;
            OnPropertyChanged();
        }
    }

    public override void OnShown()
    {
        Rebuild();
    }

    public override void OnDocumentChanged()
    {
        Rebuild();
    }

    private void Rebuild()
    {
        foreach(var row in Preview)
            row.Detach();

        Preview.Clear();
        if(Settings != null)
        {
            IndentSizeText = Settings.IndentSize.ToString();
            foreach(var entry in LayerCatalog.GetCommonLayers(Shell.Document.Container.CommonLayers))
                Preview.Add(new LayerRowViewModel(entry, Settings));

            if(Shell.SelectedModel != null)
            {
                foreach(var entry in LayerCatalog.GetModelLayers(Shell.SelectedModel.Layers))
                    Preview.Add(new LayerRowViewModel(entry, Settings));
            }
        }

        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(UseRootNamespaceInProjectName));
    }

    partial void OnIndentSizeTextChanged(string value)
    {
        if(Settings == null)
            return;

        IndentSizeError = ModelValidation.ValidateIndentSize(value, out var indent);
        if(IndentSizeError == null && Settings.IndentSize != indent)
            Settings.IndentSize = indent;
    }
}
