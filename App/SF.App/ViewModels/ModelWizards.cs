#region Usings
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SF.App.Services;
using SF.Common.Logging;
using SF.Engine;
using SF.Models;
using SF.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
#endregion

namespace SF.App.ViewModels;

internal enum WizardStep
{
    Settings = 0,
    Connection = 1,
    Summary = 2,
    Objects = 3,
    Progress = 4,
}

/// <summary>
/// One step in a wizard's step indicator.
/// </summary>
internal sealed partial class StepItem : ViewModelBase
{
    [ObservableProperty]
    private bool _isCurrent;

    [ObservableProperty]
    private bool _isDone;

    public int Number { get; set; }
    public string Title { get; set; }
    public WizardStep Step { get; set; }
}

/// <summary>
/// What the Add and Update wizards share: steps, the object chooser and the progress log.
/// </summary>
internal abstract partial class ModelWizardViewModel : ViewModelBase
{
    [ObservableProperty]
    private WizardStep _step;

    [ObservableProperty]
    private bool _isWorking;

    [ObservableProperty]
    private string _error;

    [ObservableProperty]
    private ObjectChooserViewModel _chooser;

    [ObservableProperty]
    private bool _isFinished;

    [ObservableProperty]
    private bool _succeeded;

    protected ModelWizardViewModel(MainWindowViewModel shell)
    {
        Shell = shell;
    }

    public MainWindowViewModel Shell { get; }
    public ProgressViewModel Progress { get; } = new ProgressViewModel();
    public ObservableCollection<StepItem> Steps { get; } = new ObservableCollection<StepItem>();
    public abstract string Title { get; }

    public bool IsSettingsStep
    {
        get
        {
            return Step == WizardStep.Settings;
        }
    }

    public bool IsConnectionStep
    {
        get
        {
            return Step == WizardStep.Connection;
        }
    }

    public bool IsSummaryStep
    {
        get
        {
            return Step == WizardStep.Summary;
        }
    }

    public bool IsObjectsStep
    {
        get
        {
            return Step == WizardStep.Objects;
        }
    }

    public bool IsProgressStep
    {
        get
        {
            return Step == WizardStep.Progress;
        }
    }

    public bool CanGoBack
    {
        get
        {
            return !IsWorking && Steps.Count > 0 && Step != Steps[0].Step && (Step != WizardStep.Progress || CanRetry);
        }
    }

    /// <summary>
    /// The build failed: Back returns to the objects, with the connection and the choices kept, to try again.
    /// </summary>
    public bool CanRetry
    {
        get
        {
            return Step == WizardStep.Progress && IsFinished && !Succeeded;
        }
    }

    /// <summary>
    /// Back shows on every step but the build, and on the build once it failed.
    /// </summary>
    public bool ShowBack
    {
        get
        {
            return !IsProgressStep || CanRetry;
        }
    }

    public bool CanGoNext
    {
        get
        {
            return !IsWorking && Step != WizardStep.Progress;
        }
    }

    public string NextText
    {
        get
        {
            return Step == WizardStep.Objects ? FinishText : "Next";
        }
    }

    public abstract string FinishText { get; }

    public void AddStep(WizardStep step, string title)
    {
        Steps.Add(new StepItem { Number = Steps.Count + 1, Title = title, Step = step });
    }

    partial void OnStepChanged(WizardStep value)
    {
        //back from a failed build: the wizard is not finished any more
        if(value != WizardStep.Progress)
        {
            IsFinished = false;
            Succeeded = false;
        }

        RefreshSteps();
    }

    partial void OnIsFinishedChanged(bool value)
    {
        RaiseNavigation();
    }

    partial void OnSucceededChanged(bool value)
    {
        RaiseNavigation();
    }

    /// <summary>
    /// Updates the step indicator and everything that depends on the current step.
    /// </summary>
    public void RefreshSteps()
    {
        var value = Step;
        var reached = false;
        foreach(var item in Steps)
        {
            item.IsCurrent = item.Step == value;
            if(item.IsCurrent)
                reached = true;

            item.IsDone = !reached;
        }

        Error = null;
        OnPropertyChanged(nameof(IsSettingsStep));
        OnPropertyChanged(nameof(IsConnectionStep));
        OnPropertyChanged(nameof(IsSummaryStep));
        OnPropertyChanged(nameof(IsObjectsStep));
        OnPropertyChanged(nameof(IsProgressStep));
        RaiseNavigation();
    }

    partial void OnIsWorkingChanged(bool value)
    {
        Shell.IsBusy = value;
        RaiseNavigation();
    }

    private void RaiseNavigation()
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanRetry));
        OnPropertyChanged(nameof(ShowBack));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(NextText));
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if(IsWorking)
            return;

        Error = null;
        await GoNextAsync();
    }

    [RelayCommand]
    private void Back()
    {
        if(!CanGoBack)
            return;

        for(var i = 1; i < Steps.Count; i++)
        {
            if(Steps[i].Step == Step)
            {
                Step = Steps[i - 1].Step;
                return;
            }
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if(IsWorking)
            return;

        Shell.CloseOverlay();
    }

    [RelayCommand]
    private void Done()
    {
        Shell.CloseOverlay();
    }

    public abstract Task GoNextAsync();

    /// <summary>
    /// Builds the model from the chosen objects on a background thread, showing the progress log.
    /// </summary>
    public async Task<Model> BuildAsync(BuildJob job)
    {
        Step = WizardStep.Progress;
        Progress.Clear();
        Progress.IsRunning = true;
        IsWorking = true;
        try
        {
            job.Log = new UiProgressLog(Progress);
            await Task.Run(job.Run);
        }
        finally
        {
            Progress.IsRunning = false;
            IsWorking = false;
        }

        if(job.Exception != null)
        {
            LogManager.WriteErrorLog(job.Exception, "Sharp Factory Model Exception");
            Progress.Add($"Failed: {job.Exception.Message}", "The model could not be built. The details are in the error log.");
            IsFinished = true;
            Succeeded = false;
            return null;
        }

        IsFinished = true;
        Succeeded = true;
        foreach(var item in Steps)
        {
            item.IsCurrent = false;
            item.IsDone = true;
        }

        return job.Result;
    }

    /// <summary>
    /// Reads a database's objects on a background thread. Returns null (with Error set) when it fails.
    /// </summary>
    public async Task<DBObjectCategoryCollection> ReadObjectsAsync(ReadObjectsJob job)
    {
        IsWorking = true;
        try
        {
            await Task.Run(job.Run);
        }
        finally
        {
            IsWorking = false;
        }

        if(job.Error != null)
        {
            Error = job.Error;
            return null;
        }

        return job.Result;
    }
}

/// <summary>
/// Add a model: [common settings, for a new model file] → connection → objects → progress.
/// </summary>
internal sealed partial class AddModelWizardViewModel : ModelWizardViewModel
{
    private readonly ModelContainer _container;
    private ConnectionInfo _databasesLoadedFor;

    [ObservableProperty]
    private string _rootNamespace = "";

    [ObservableProperty]
    private string _dependenciesFolder = "Dependencies";

    [ObservableProperty]
    private string _indentSize = "4";

    [ObservableProperty]
    private bool _useRootNamespaceInProjectName = true;

    [ObservableProperty]
    private string _modelName = "";

    [ObservableProperty]
    private string _server = Environment.MachineName;

    [ObservableProperty]
    private bool _integratedSecurity = true;

    [ObservableProperty]
    private string _user = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _selectedDatabase;

    [ObservableProperty]
    private string _indentSizeError;

    [ObservableProperty]
    private string _modelNameError;

    [ObservableProperty]
    private string _serverError;

    [ObservableProperty]
    private string _databaseError;

    [ObservableProperty]
    private string _userError;

    [ObservableProperty]
    private string _passwordError;

    /// <param name="container">The open model container; null to start a new model file.</param>
    public AddModelWizardViewModel(MainWindowViewModel shell, ModelContainer container) : base(shell)
    {
        _container = container;
        if(IsNewContainer)
            AddStep(WizardStep.Settings, "Common settings");

        AddStep(WizardStep.Connection, "Connect");
        AddStep(WizardStep.Objects, "Choose objects");
        AddStep(WizardStep.Progress, "Build");
        Step = Steps[0].Step;
        RefreshSteps();
    }

    public bool IsNewContainer
    {
        get
        {
            return _container == null;
        }
    }

    public override string Title
    {
        get
        {
            return IsNewContainer ? "New model file" : "Add a model";
        }
    }

    public override string FinishText
    {
        get
        {
            return "Create model";
        }
    }

    public ObservableCollection<string> Databases { get; } = new ObservableCollection<string>();

    /// <summary>
    /// On macOS and Linux, integrated security needs Kerberos to be set up.
    /// </summary>
    public bool ShowIntegratedSecurityNote
    {
        get
        {
            return IntegratedSecurity && !OperatingSystem.IsWindows();
        }
    }

    partial void OnIntegratedSecurityChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowIntegratedSecurityNote));
        UserError = null;
        PasswordError = null;
        ConnectedText = null;
    }

    /// <summary>
    /// After Load: "Connected to SQL01 as sa · 12 databases"; cleared when the server or the sign-in changes.
    /// </summary>
    [ObservableProperty]
    private string _connectedText;

    partial void OnIndentSizeChanged(string value)
    {
        IndentSizeError = null;
    }

    partial void OnModelNameChanged(string value)
    {
        ModelNameError = null;
    }

    partial void OnServerChanged(string value)
    {
        ServerError = null;
        ConnectedText = null;
    }

    partial void OnSelectedDatabaseChanged(string value)
    {
        DatabaseError = null;
    }

    partial void OnUserChanged(string value)
    {
        UserError = null;
        ConnectedText = null;
    }

    partial void OnPasswordChanged(string value)
    {
        PasswordError = null;
        ConnectedText = null;
    }

    /// <summary>
    /// Shows each problem under its field; the ones that belong to no field go to the footer.
    /// </summary>
    private void ShowErrors(List<string> problems)
    {
        ModelNameError = null;
        ServerError = null;
        DatabaseError = null;
        UserError = null;
        PasswordError = null;

        var general = new List<string>();
        foreach(var problem in problems)
        {
            if(!ShowFieldError(problem))
                general.Add(problem);
        }

        Error = general.Count > 0 ? ModelValidation.Join(general) : null;
    }

    private bool ShowFieldError(string problem)
    {
        switch(problem)
        {
            case ModelValidation.ModelNameRequired:
            case ModelValidation.ModelNameExists:
            case ModelValidation.ModelNameIsRootNamespace:
                if(ModelNameError == null)
                    ModelNameError = problem;

                return true;
            case ModelValidation.ServerRequired:
                ServerError = problem;
                return true;
            case ModelValidation.DatabaseRequired:
                DatabaseError = problem;
                return true;
            case ModelValidation.UserRequired:
                UserError = problem;
                return true;
            case ModelValidation.PasswordRequired:
                PasswordError = problem;
                return true;
            default:
                return false;
        }
    }

    public ConnectionInfo GetConnectionInfo()
    {
        return new ConnectionInfo
        {
            Server = ModelValidation.Trim(Server),
            Database = SelectedDatabase,
            User = EmptyToNull(ModelValidation.Trim(User)),
            //a password is used as typed: spaces at either end can be part of it
            Password = EmptyToNull(Password),
            DisableIntegratedSecurity = !IntegratedSecurity,
        };
    }

    private static string EmptyToNull(string value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }

    [RelayCommand]
    private async Task LoadDatabasesAsync()
    {
        Error = null;
        var conInfo = GetConnectionInfo();
        conInfo.Database = null;

        var problem = ModelValidation.ValidateServerInfo(conInfo);
        if(problem != null)
        {
            ShowErrors(new List<string> { problem });
            Databases.Clear();
            return;
        }

        ShowErrors(new List<string>());

        if(_databasesLoadedFor != null && ModelValidation.IsSameServerLogin(_databasesLoadedFor, conInfo) && Databases.Count > 0)
            return;

        var job = new LoadDatabasesJob(conInfo);
        IsWorking = true;
        try
        {
            await Task.Run(job.Run);
        }
        finally
        {
            IsWorking = false;
        }

        Databases.Clear();
        if(job.Error != null)
        {
            Error = job.Error;
            _databasesLoadedFor = null;
            return;
        }

        foreach(var database in job.Result)
            Databases.Add(database);

        _databasesLoadedFor = conInfo;
        var who = IntegratedSecurity ? " (Windows)" : " as " + conInfo.User;
        ConnectedText = $"Connected to {conInfo.Server}{who} · {ModelTreeBuilder.Plural(Databases.Count, "database")}";
    }

    public override async Task GoNextAsync()
    {
        switch(Step)
        {
            case WizardStep.Settings:
                GoFromSettings();
                break;
            case WizardStep.Connection:
                await GoFromConnectionAsync();
                break;
            case WizardStep.Objects:
                await BuildModelAsync();
                break;
        }
    }

    private void GoFromSettings()
    {
        var problem = ModelValidation.ValidateIndentSize(IndentSize, out _);
        if(problem != null)
        {
            IndentSizeError = problem;
            return;
        }

        RootNamespace = ModelValidation.CleanName(RootNamespace);
        DependenciesFolder = ModelValidation.CleanName(DependenciesFolder);
        Step = WizardStep.Connection;
    }

    private async Task GoFromConnectionAsync()
    {
        ModelName = ModelValidation.Trim(ModelName);
        Server = ModelValidation.Trim(Server);

        var conInfo = GetConnectionInfo();
        var errors = ModelValidation.ValidateDbInfo(_container, ModelName, conInfo, true);
        var rootNamespace = IsNewContainer ? RootNamespace : _container.CommonSettings.RootNamespace;
        if(!string.IsNullOrWhiteSpace(rootNamespace) && string.CompareOrdinal(ModelName, rootNamespace) == 0)
            errors.Insert(0, ModelValidation.ModelNameIsRootNamespace);

        ShowErrors(errors);
        if(errors.Count > 0)
            return;

        var categories = await ReadObjectsAsync(new ReadObjectsJob(conInfo, null));
        if(categories == null)
            return;

        Chooser = new ObjectChooserViewModel(categories);
        Step = WizardStep.Objects;
    }

    private async Task BuildModelAsync()
    {
        var job = new BuildJob { ModelName = ModelName, ConInfo = GetConnectionInfo(), Layers = Defaults.ModelLayers(), Categories = Chooser.Categories };
        var model = await BuildAsync(job);
        if(model == null)
            return;

        var container = _container;
        if(IsNewContainer)
        {
            ModelValidation.ValidateIndentSize(IndentSize, out var indent);
            container = ModelBuilder.CreateContainer(RootNamespace, DependenciesFolder, indent, UseRootNamespaceInProjectName);
        }

        container.Models.Add(model);
        Progress.Succeed($"Model {model.Name} created: {ModelsPageViewModel.Describe(model)}");
        Shell.AcceptNewModel(container, model, IsNewContainer);
        Shell.StatusText = $"Added model {model.Name}";
    }
}

/// <summary>
/// Update a model: connection summary → objects (with the model's current selection) → progress.
/// </summary>
internal sealed class UpdateModelWizardViewModel : ModelWizardViewModel
{
    private readonly ModelContainer _container;

    public UpdateModelWizardViewModel(MainWindowViewModel shell, ModelContainer container, Model model) : base(shell)
    {
        _container = container;
        Model = model;
        AddStep(WizardStep.Summary, "Connect");
        AddStep(WizardStep.Objects, "Choose objects");
        AddStep(WizardStep.Progress, "Build");
        Step = WizardStep.Summary;
        RefreshSteps();
    }

    public Model Model { get; }

    public override string Title
    {
        get
        {
            return $"Update model {Model.Name}";
        }
    }

    public override string FinishText
    {
        get
        {
            return "Update model";
        }
    }

    /// <summary>
    /// How the model signs in: Windows authentication, or the SQL Server user.
    /// </summary>
    public string SignInText
    {
        get
        {
            if(Model.ConInfo.DisableIntegratedSecurity.HasValue && Model.ConInfo.DisableIntegratedSecurity.Value)
                return $"SQL Server user {Model.ConInfo.User}";

            return "Windows authentication";
        }
    }

    public override async Task GoNextAsync()
    {
        switch(Step)
        {
            case WizardStep.Summary:
                await GoFromSummaryAsync();
                break;
            case WizardStep.Objects:
                await UpdateModelAsync();
                break;
        }
    }

    private async Task GoFromSummaryAsync()
    {
        var errors = ModelValidation.ValidateDbInfo(_container, Model.Name, Model.ConInfo, false);
        if(errors.Count > 0)
        {
            Error = ModelValidation.Join(errors);
            return;
        }

        var categories = await ReadObjectsAsync(new ReadObjectsJob(Model.ConInfo, Model));
        if(categories == null)
            return;

        Chooser = new ObjectChooserViewModel(categories, true);
        Step = WizardStep.Objects;
    }

    private async Task UpdateModelAsync()
    {
        var job = new BuildJob { CurrentModel = Model, Categories = Chooser.Categories };
        var updated = await BuildAsync(job);
        if(updated == null)
            return;

        Shell.ReplaceModel(Model, updated);
        Progress.Succeed($"Model {updated.Name} updated: {ModelsPageViewModel.Describe(updated)}. {Chooser.ChangeText}");
        Shell.StatusText = $"Updated model {updated.Name}";
    }
}

/// <summary>
/// Lists a server's databases (background thread).
/// </summary>
internal sealed class LoadDatabasesJob
{
    private readonly ConnectionInfo _conInfo;

    public LoadDatabasesJob(ConnectionInfo conInfo)
    {
        _conInfo = conInfo;
    }

    public List<string> Result { get; set; } = new List<string>();
    public string Error { get; set; }

    public void Run()
    {
        try
        {
            if(!DbModelUtils.ValidateConnectionInfo(_conInfo, out var reason))
            {
                Error = $"Failed to connect! Please provide valid connection information.{Environment.NewLine}{reason}";
                return;
            }

            Result = DbModelUtils.GetAvailableDatabases(_conInfo);
        }
        catch(Exception ex)
        {
            LogManager.WriteErrorLog(ex, "Unable to list the databases");
            Error = $"Failed to connect! Please provide valid connection information.{Environment.NewLine}{ex.Message}";
        }
    }
}

/// <summary>
/// Reads a database's objects; for an update, selects the ones the model has (background thread).
/// </summary>
internal sealed class ReadObjectsJob
{
    private readonly ConnectionInfo _conInfo;
    private readonly Model _currentModel;

    public ReadObjectsJob(ConnectionInfo conInfo, Model currentModel)
    {
        _conInfo = conInfo;
        _currentModel = currentModel;
    }

    public DBObjectCategoryCollection Result { get; set; }
    public string Error { get; set; }

    public void Run()
    {
        try
        {
            if(!DbModelUtils.ValidateConnectionInfo(_conInfo, out var reason))
            {
                Error = $"Database not found or failed to connect!{Environment.NewLine}{reason}";
                return;
            }

            Result = _currentModel == null ? ModelBuilder.ReadDatabaseObjects(_conInfo) : ModelBuilder.ReadDatabaseObjectsForUpdate(_currentModel);
        }
        catch(Exception ex)
        {
            LogManager.WriteErrorLog(ex, "Unable to read the database objects");
            Error = $"Failed to load model from database!{Environment.NewLine}{ex.Message}";
        }
    }
}

/// <summary>
/// Builds a new model, or rebuilds an existing one (background thread).
/// </summary>
internal sealed class BuildJob
{
    public string ModelName { get; set; }
    public ConnectionInfo ConInfo { get; set; }
    public ModelLayers Layers { get; set; }
    public Model CurrentModel { get; set; }
    public DBObjectCategoryCollection Categories { get; set; }
    public IProgressLog Log { get; set; }
    public Model Result { get; set; }
    public Exception Exception { get; set; }

    public void Run()
    {
        try
        {
            Result = CurrentModel == null
                ? ModelBuilder.Populate(ModelName, ConInfo, Layers, Categories, Log)
                : ModelBuilder.Update(CurrentModel, Categories, Log);
        }
        catch(Exception ex)
        {
            Exception = ex;
        }
    }
}
