#region Usings
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SF.App.Services;
using SF.Common.Logging;
using SF.Engine;
using SF.Engine.Settings;
using SF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// The main window: the open model file, navigation, the wizards and dialogs shown over the pages.
/// </summary>
internal sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFileDialogs _fileDialogs;

    [ObservableProperty]
    private ModelDocument _document;

    [ObservableProperty]
    private PageViewModel _currentPage;

    [ObservableProperty]
    private NavItemViewModel _selectedNavItem;

    [ObservableProperty]
    private Model _selectedModel;

    [ObservableProperty]
    private ViewModelBase _overlay;

    [ObservableProperty]
    private DialogViewModel _dialog;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText;

    //true while a new document is being shown, so the selected-model change it causes notifies the pages only once
    private bool _swappingDocument;

    public MainWindowViewModel(IFileDialogs fileDialogs, AppSettings settings, string version)
    {
        _fileDialogs = fileDialogs;
        Settings = settings;
        Version = version;

        Welcome = new WelcomePageViewModel(this);
        Models = new ModelsPageViewModel(this);
        Layers = new LayersPageViewModel(this);
        SettingsPage = new SettingsPageViewModel(this);
        Sync = new SyncPageViewModel(this);
        Documentation = new DocumentationPageViewModel(this);
        About = new AboutPageViewModel(this);

        NavItems.Add(new NavItemViewModel { Title = "Home", IconKey = "IconHome", Page = Welcome });
        NavItems.Add(new NavItemViewModel { Title = "Models", IconKey = "IconDatabase", Page = Models, RequiresDocument = true });
        NavItems.Add(new NavItemViewModel { Title = "Layers", IconKey = "IconLayers", Page = Layers, RequiresDocument = true });
        NavItems.Add(new NavItemViewModel { Title = "Settings", IconKey = "IconSettings", Page = SettingsPage, RequiresDocument = true });
        NavItems.Add(new NavItemViewModel { Title = "Sync", IconKey = "IconSync", Page = Sync, RequiresDocument = true });
        NavItems.Add(new NavItemViewModel { Title = "Documentation", IconKey = "IconBook", Page = Documentation });
        NavItems.Add(new NavItemViewModel { Title = "About", IconKey = "IconInfo", Page = About });

        UpdateNavItems();
        Navigate(Welcome);
    }

    public AppSettings Settings { get; }
    public string Version { get; }
    public ObservableCollection<NavItemViewModel> NavItems { get; } = new ObservableCollection<NavItemViewModel>();

    public WelcomePageViewModel Welcome { get; }
    public ModelsPageViewModel Models { get; }
    public LayersPageViewModel Layers { get; }
    public SettingsPageViewModel SettingsPage { get; }
    public SyncPageViewModel Sync { get; }
    public DocumentationPageViewModel Documentation { get; }
    public AboutPageViewModel About { get; }

    public IFileDialogs FileDialogs
    {
        get
        {
            return _fileDialogs;
        }
    }

    /// <summary>
    /// The top bar, the navigation and the page take the mouse and the keyboard only when no wizard or dialog is up.
    /// </summary>
    public bool IsShellEnabled
    {
        get
        {
            return Overlay == null && Dialog == null;
        }
    }

    partial void OnOverlayChanged(ViewModelBase value)
    {
        OnPropertyChanged(nameof(IsShellEnabled));
    }

    partial void OnDialogChanged(DialogViewModel value)
    {
        OnPropertyChanged(nameof(IsShellEnabled));
    }

    public bool HasDocument
    {
        get
        {
            return Document != null;
        }
    }

    public string WindowTitle
    {
        get
        {
            if(Document == null)
                return "The Sharp Factory";

            return $"{Document.DisplayName}{(Document.IsDirty ? " •" : "")} — The Sharp Factory";
        }
    }

    public ObservableCollection<Model> ModelList
    {
        get
        {
            return Document?.Container.Models;
        }
    }

    #region Navigation
    public void Navigate(PageViewModel page)
    {
        foreach(var item in NavItems)
        {
            if(item.Page == page)
            {
                SelectedNavItem = item;
                return;
            }
        }
    }

    partial void OnSelectedNavItemChanged(NavItemViewModel value)
    {
        foreach(var item in NavItems)
            item.IsSelected = item == value;

        if(value == null)
            return;

        if(value.RequiresDocument && Document == null)
        {
            SelectedNavItem = NavItems[0];
            return;
        }

        CurrentPage = value.Page;
        value.Page.OnShown();
    }

    private void UpdateNavItems()
    {
        foreach(var item in NavItems)
            item.IsEnabled = !item.RequiresDocument || Document != null;
    }
    #endregion

    #region Document
    partial void OnDocumentChanged(ModelDocument oldValue, ModelDocument newValue)
    {
        if(oldValue != null)
            oldValue.PropertyChanged -= OnDocumentPropertyChanged;

        if(newValue != null)
            newValue.PropertyChanged += OnDocumentPropertyChanged;

        _swappingDocument = true;
        try
        {
            //the model picker gets the new file's list first: a model that is not in the list it has is no selection
            OnPropertyChanged(nameof(ModelList));
            SelectedModel = newValue != null && newValue.Container.Models.Count > 0 ? newValue.Container.Models[0] : null;
        }
        finally
        {
            _swappingDocument = false;
        }

        UpdateNavItems();
        OnPropertyChanged(nameof(HasDocument));
        OnPropertyChanged(nameof(WindowTitle));
        NotifyPagesOfDocument();
    }

    partial void OnSelectedModelChanged(Model value)
    {
        if(!_swappingDocument)
            NotifyPagesOfSelectedModel();
    }

    private void OnDocumentPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(WindowTitle));

        //"Saved" or "Opened" is no longer true once the model has unsaved changes. Every message is set after the
        //change it reports, so a message about that change stays
        if(string.Equals(e.PropertyName, nameof(ModelDocument.IsDirty), StringComparison.Ordinal) && Document != null && Document.IsDirty)
            StatusText = null;
    }

    /// <summary>
    /// A model file was opened, created or closed: every page starts over.
    /// </summary>
    private void NotifyPagesOfDocument()
    {
        Welcome.OnDocumentChanged();
        Models.OnDocumentChanged();
        Layers.OnDocumentChanged();
        SettingsPage.OnDocumentChanged();
        Sync.OnDocumentChanged();
    }

    /// <summary>
    /// Another model was picked: the pages that show one model refresh. Sync works on the whole file, so its target
    /// and report stay.
    /// </summary>
    private void NotifyPagesOfSelectedModel()
    {
        Models.OnSelectedModelChanged();
        Layers.OnSelectedModelChanged();
        SettingsPage.OnSelectedModelChanged();
        Sync.OnSelectedModelChanged();
    }

    /// <summary>
    /// Opens a model file. Returns false (after telling the user) when it cannot be read.
    /// </summary>
    public bool OpenFile(string filePath)
    {
        var document = (ModelDocument)null;
        var problem = (string)null;
        try
        {
            document = ModelDocument.Load(filePath, out problem);
        }
        catch(Exception ex)
        {
            LogManager.WriteErrorLog(ex, "Unable to open the model file", filePath);
            problem = ex.Message;
        }

        if(document == null)
        {
            //a file that is there but cannot be read now (locked, a share that is offline) stays on the list
            if(!File.Exists(filePath))
            {
                AppSettingsStore.RemoveRecentFile(Settings, filePath);
                AppSettingsStore.Save(Settings);
            }

            Welcome.OnDocumentChanged();
            _ = ShowMessageAsync("Unable to open the model", $"{filePath} could not be opened. {problem}", true);
            return false;
        }

        Document = document;
        AppSettingsStore.AddRecentFile(Settings, filePath);
        AppSettingsStore.Save(Settings);
        StatusText = $"Opened {document.DisplayName}";
        Navigate(Models);
        return true;
    }

    /// <summary>
    /// Shows a model built by the Add wizard; a new container becomes an unsaved document.
    /// </summary>
    public void AcceptNewModel(ModelContainer container, Model model, bool isNewContainer)
    {
        //a new document, or another selected model: either one refreshes the pages
        if(isNewContainer)
            Document = new ModelDocument(container, null, true);

        SelectedModel = model;
        Navigate(Models);
    }

    /// <summary>
    /// Puts an updated model in the place of the current one; the pages refresh once, for the updated model.
    /// </summary>
    public void ReplaceModel(Model current, Model updated)
    {
        //taking the model out of the list empties the model picker for a moment: no page refreshes for that
        _swappingDocument = true;
        try
        {
            ModelBuilder.ReplaceModel(Document.Container, current, updated);
        }
        finally
        {
            _swappingDocument = false;
        }

        if(SelectedModel == updated)
            NotifyPagesOfSelectedModel();
        else
            SelectedModel = updated;
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if(!await ConfirmDiscardChangesAsync())
            return;

        var path = await _fileDialogs.OpenModelFileAsync();
        if(path != null)
            OpenFile(path);
    }

    [RelayCommand]
    private async Task OpenRecentAsync(string filePath)
    {
        await OpenWithConfirmationAsync(filePath);
    }

    /// <summary>
    /// Opens a file chosen outside the open dialog (recent files, a file the OS hands over), asking about unsaved
    /// changes first. Does nothing while a model build or Sync is running, or a wizard is open (its model belongs
    /// to the file that is open now).
    /// </summary>
    public async Task<bool> OpenWithConfirmationAsync(string filePath)
    {
        if(IsBusy || Overlay != null || !await ConfirmDiscardChangesAsync())
            return false;

        return OpenFile(filePath);
    }

    [RelayCommand]
    private async Task NewAsync()
    {
        if(!await ConfirmDiscardChangesAsync())
            return;

        Overlay = new AddModelWizardViewModel(this, null);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await SaveDocumentAsync();
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        await SaveDocumentAsAsync();
    }

    /// <summary>
    /// Saves to the current file (asks for one when the model was never saved). Returns false when cancelled.
    /// </summary>
    public async Task<bool> SaveDocumentAsync()
    {
        if(Document == null)
            return false;

        if(string.IsNullOrWhiteSpace(Document.FilePath))
            return await SaveDocumentAsAsync();

        return SaveTo(Document.FilePath);
    }

    public async Task<bool> SaveDocumentAsAsync()
    {
        if(Document == null)
            return false;

        var suggested = string.IsNullOrWhiteSpace(Document.FilePath) ? "ModelContainer.edmj" : Path.GetFileName(Document.FilePath);
        var path = await _fileDialogs.SaveModelFileAsync(suggested);
        if(path == null)
            return false;

        return SaveTo(path);
    }

    private bool SaveTo(string path)
    {
        try
        {
            Document.Save(path);
            AppSettingsStore.AddRecentFile(Settings, path);
            AppSettingsStore.Save(Settings);
            StatusText = $"Saved {Document.DisplayName}";
            OnPropertyChanged(nameof(WindowTitle));
            return true;
        }
        catch(Exception ex)
        {
            LogManager.WriteErrorLog(ex, "Unable to save the model file", path);
            _ = ShowMessageAsync("Unable to save", ex.Message, true);
            return false;
        }
    }

    /// <summary>
    /// When there are unsaved changes, asks whether to save them. Returns false when the user cancels.
    /// </summary>
    public async Task<bool> ConfirmDiscardChangesAsync()
    {
        if(Document == null || !Document.IsDirty)
            return true;

        var dialog = new DialogViewModel
        {
            Title = "Unsaved changes",
            Message = $"Save the changes to {Document.DisplayName}?",
            Buttons = new List<DialogButton>
            {
                new DialogButton { Text = "Don't save", Result = DialogResults.Discard },
                new DialogButton { Text = "Cancel", Result = DialogResults.Cancel },
                new DialogButton { Text = "Save", Result = DialogResults.Save, IsPrimary = true },
            },
        };

        var result = await ShowDialogAsync(dialog);
        if(string.Equals(result, DialogResults.Save, StringComparison.Ordinal))
            return await SaveDocumentAsync();

        return string.Equals(result, DialogResults.Discard, StringComparison.Ordinal);
    }
    #endregion

    #region Models
    public void StartAddModel()
    {
        if(Document == null)
            return;

        Overlay = new AddModelWizardViewModel(this, Document.Container);
    }

    public void StartUpdateModel()
    {
        if(SelectedModel == null)
        {
            _ = ShowMessageAsync("Update cancelled", "You need to select a model first.", false);
            return;
        }

        Overlay = new UpdateModelWizardViewModel(this, Document.Container, SelectedModel);
    }

    public async Task DeleteSelectedModelAsync()
    {
        var model = SelectedModel;
        if(Document == null || model == null)
        {
            await ShowMessageAsync("Delete cancelled", "There is no selected model to delete.", false);
            return;
        }

        var dialog = new DialogViewModel
        {
            Title = "Delete model",
            IsWarning = true,
            Message = $"Delete model {model.Name}? It is removed from {Document.DisplayName} when you save; the database is not changed.",
            Buttons = new List<DialogButton>
            {
                new DialogButton { Text = "Cancel", Result = DialogResults.No },
                new DialogButton { Text = "Delete", Result = DialogResults.Yes, IsDanger = true },
            },
        };

        var result = await ShowDialogAsync(dialog);
        if(!string.Equals(result, DialogResults.Yes, StringComparison.Ordinal))
            return;

        Document.Container.Models.Remove(model);
        SelectedModel = Document.Container.Models.Count > 0 ? Document.Container.Models[0] : null;
        StatusText = $"Deleted model {model.Name}";
    }

    /// <summary>
    /// Called by the wizards when they finish or are cancelled.
    /// </summary>
    public void CloseOverlay()
    {
        //a wizard that built a model already refreshed the pages when it was shown
        Overlay = null;
    }
    #endregion

    #region Dialogs
    public Task<string> ShowDialogAsync(DialogViewModel dialog)
    {
        //one dialog at a time: a dialog still up is answered as cancelled, so what waits for it goes on
        var previous = Dialog;
        Dialog = dialog;
        previous?.Cancel();
        return WaitForDialogAsync(dialog);
    }

    private async Task<string> WaitForDialogAsync(DialogViewModel dialog)
    {
        var result = await dialog.Result;
        if(Dialog == dialog)
            Dialog = null;

        return result;
    }

    public Task<string> ShowMessageAsync(string title, string message, bool isError)
    {
        var dialog = new DialogViewModel
        {
            Title = title,
            Message = message,
            IsError = isError,
            Buttons = new List<DialogButton> { new DialogButton { Text = "OK", Result = DialogResults.Ok, IsPrimary = true } },
        };

        return ShowDialogAsync(dialog);
    }
    #endregion
}
