#region Usings
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using SF.App.Services;
using SF.Engine;
using SF.Engine.Settings;
using SF.Engine.Sync;
using SF.Engine.Targets;
using SF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// Sync: choose a target solution (synced before, near the model, browse, or new). As soon as there is one, the page
/// works out what Sync would change (a run that writes nothing) and shows it; it works it out again when the model or
/// the target changes. Sync writes it; the page then keeps a line about that Sync. A Sync into another solution than
/// the model's own is confirmed first.
/// </summary>
internal sealed partial class SyncPageViewModel : PageViewModel
{
    [ObservableProperty]
    private SolutionCandidateViewModel _selectedCandidate;

    [ObservableProperty]
    private string _targetPath;

    [ObservableProperty]
    private string _newSolutionFolder;

    [ObservableProperty]
    private string _newSolutionName;

    [ObservableProperty]
    private string _newSolutionFormat = ".slnx";

    [ObservableProperty]
    private bool _isScanning;

    /// <summary>
    /// A Sync is writing: the rest of the app waits.
    /// </summary>
    [ObservableProperty]
    private bool _isRunning;

    /// <summary>
    /// The page is working out what Sync would change: nothing is written, and the app stays usable.
    /// </summary>
    [ObservableProperty]
    private bool _isChecking;

    /// <summary>
    /// What Sync would change now: the last check (or an empty report right after a Sync), never a Sync's own report.
    /// </summary>
    [ObservableProperty]
    private SyncReport _report;

    /// <summary>
    /// The last Sync of this model file in this session, and when it ran.
    /// </summary>
    [ObservableProperty]
    private LastSyncViewModel _lastSync;

    [ObservableProperty]
    private ChangeFilterOption _changeFilter;

    [ObservableProperty]
    private List<ChangeFilterOption> _changeFilters = ChangeFilterOption.For(null);

    [ObservableProperty]
    private DiffViewModel _diff;

    /// <summary>
    /// The target chooser under the "Sync into" strip: open until a solution is chosen.
    /// </summary>
    [ObservableProperty]
    private bool _isTargetPanelOpen = true;

    [ObservableProperty]
    private string _changeSearch;

    [ObservableProperty]
    private FileChangeViewModel _selectedFileChange;

    [ObservableProperty]
    private string _targetError;

    /// <summary>
    /// Which run the Log tab shows: "Check at 21:16 (nothing written)" or "Sync at 21:15".
    /// </summary>
    [ObservableProperty]
    private string _logTitle;

    public SyncPageViewModel(MainWindowViewModel shell) : base(shell)
    {
        _changeFilter = _changeFilters[0];
    }

    /// <summary>
    /// What the changes area says when there is no file to list; null when it lists files.
    /// </summary>
    public string ChangesEmptyText
    {
        get
        {
            if(Report == null)
                return IsChecking ? "Working out what Sync would change…" : "What Sync would change shows here.";

            if(!Report.Success)
                return "Nothing to show: the check failed. The Log tab has the details.";

            if(FileChanges.Count > 0)
                return null;

            if(Report.FileChanges.Count == 0)
            {
                if(Report.IsUpToDate)
                    return "No files to list: Sync has nothing to write.";

                return "No file changes: Sync only updates the solution and its projects.";
            }

            return "No files of this kind.";
        }
    }

    public bool HasChangesEmptyText
    {
        get
        {
            return ChangesEmptyText != null;
        }
    }

    /// <summary>
    /// The file list and the difference: only when there are files to list, otherwise one line says why not.
    /// </summary>
    public bool HasPendingFiles
    {
        get
        {
            return Report != null && Report.Success && Report.FileChanges.Count > 0;
        }
    }

    /// <summary>
    /// The changes card takes the rest of the page when it lists files, or its log is shown; otherwise it is as tall as
    /// its content.
    /// </summary>
    public bool FillsPage
    {
        get
        {
            return HasPendingFiles || IsLogTabSelected;
        }
    }

    /// <summary>
    /// The Log tab of the changes card is open.
    /// </summary>
    public bool IsLogTabSelected
    {
        get
        {
            return _isLogTabSelected;
        }
        set
        {
            if(_isLogTabSelected == value)
                return;

            _isLogTabSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FillsPage));
        }
    }

    public override string Title
    {
        get
        {
            return "Sync";
        }
    }

    public ObservableCollection<SolutionCandidateViewModel> Candidates { get; } = new ObservableCollection<SolutionCandidateViewModel>();
    public ObservableCollection<FileChangeViewModel> FileChanges { get; } = new ObservableCollection<FileChangeViewModel>();
    public ProgressViewModel Progress { get; } = new ProgressViewModel();

    public List<string> SolutionFormats { get; } = new List<string> { ".slnx", ".sln" };

    public List<TargetFrameworkOption> FrameworkOptions { get; } = new List<TargetFrameworkOption>
    {
        new TargetFrameworkOption { Value = Models.TargetFrameworks.NET10, Name = ProjectFileTemplate.GetDisplayName(Models.TargetFrameworks.NET10) },
        new TargetFrameworkOption { Value = Models.TargetFrameworks.NET11, Name = ProjectFileTemplate.GetDisplayName(Models.TargetFrameworks.NET11) },
    };

    public TargetFrameworkOption SelectedTargetFramework
    {
        get
        {
            var current = Shell.Document?.Container.CommonSettings.TargetFramework ?? Models.TargetFrameworks.NET10;
            foreach(var option in FrameworkOptions)
            {
                if(option.Value == current)
                    return option;
            }

            return FrameworkOptions[0];
        }
        set
        {
            //the combo box can hand back the option it shows: only a real change edits the model file
            if(value == null || Shell.Document == null || Shell.Document.Container.CommonSettings.TargetFramework == value.Value)
                return;

            Shell.Document.Container.CommonSettings.TargetFramework = value.Value;
            OnPropertyChanged();

            //the projects Sync creates change: work out again what Sync would do
            ClearReport();
            if(IsCurrentPage)
                _ = CheckAsync();
        }
    }

    /// <summary>
    /// The old app turned Sync off for a model marked as synced. Sync generates every model of the file, so one
    /// such model turns it off for the file.
    /// </summary>
    public bool IsBlockedBySyncedFlag
    {
        get
        {
            return ModelFileCheck.SyncedModelNames(Shell.Document == null ? null : Shell.Document.Container).Count > 0;
        }
    }

    /// <summary>
    /// Why Sync is off: the models marked as synced.
    /// </summary>
    public string SyncedBlockText
    {
        get
        {
            return ModelFileCheck.SyncBlockText(Shell.Document == null ? null : Shell.Document.Container);
        }
    }

    public bool HasCandidates
    {
        get
        {
            return Candidates.Count > 0;
        }
    }

    /// <summary>
    /// "No solutions found" waits until the search is over.
    /// </summary>
    public bool ShowNoCandidates
    {
        get
        {
            return !HasCandidates && !IsScanning;
        }
    }

    public bool HasTarget
    {
        get
        {
            return !string.IsNullOrWhiteSpace(TargetPath);
        }
    }

    /// <summary>
    /// The page can work out what Sync would change: a target, a model, no Sync running and none of its models marked
    /// as synced.
    /// </summary>
    public bool CanCheck
    {
        get
        {
            return !IsRunning && HasTarget && Shell.Document != null && !IsBlockedBySyncedFlag;
        }
    }

    /// <summary>
    /// Sync is on when the last check (for the model as it is now) found something to write. Before any check, or when
    /// the check is out of date, Sync works it out itself.
    /// </summary>
    public bool CanSync
    {
        get
        {
            if(!CanCheck || IsChecking)
                return false;

            if(!IsReportCurrent)
                return true;

            return Report.Success && !Report.IsUpToDate && Report.UnmarkedFiles.Count == 0;
        }
    }

    /// <summary>
    /// Why Sync is off, for its tooltip; null when it is on.
    /// </summary>
    public string SyncDisabledReason
    {
        get
        {
            if(CanSync)
                return null;

            if(Shell.Document == null)
                return "Open a model file first.";

            if(!HasTarget)
                return "Choose a solution first.";

            if(IsBlockedBySyncedFlag)
                return SyncedBlockText;

            if(IsRunning)
                return "Sync is running.";

            if(IsChecking)
                return "Working out what Sync would change…";

            if(Report != null && !Report.Success)
                return "The check failed: the Log tab has the details.";

            if(Report != null && Report.UnmarkedFiles.Count > 0)
                return "Files that Sharp Factory did not write sit at generated paths: move or rename them first.";

            return "Nothing to write: the solution is up to date.";
        }
    }

    /// <summary>
    /// "Sync 165 files" when the count is known, otherwise "Sync".
    /// </summary>
    public string SyncButtonText
    {
        get
        {
            if(IsReportCurrent && Report.Success && Report.FileChanges.Count > 0)
                return "Sync " + ModelTreeBuilder.Plural(Report.FileChanges.Count, "file");

            return "Sync";
        }
    }

    /// <summary>
    /// The report says what Sync would do to the model as it is now, in the chosen target.
    /// </summary>
    private bool IsReportCurrent
    {
        get
        {
            return Report != null && Shell.Document != null && Shell.Document.ChangeCount == _reportChangeCount
                && string.Equals(_reportTarget, TargetPath, StringComparison.OrdinalIgnoreCase);
        }
    }

    private bool IsCurrentPage
    {
        get
        {
            return Shell.CurrentPage == this;
        }
    }

    public string TargetDescription
    {
        get
        {
            if(!HasTarget)
                return "Choose a solution";

            return File.Exists(TargetPath) ? TargetPath : $"{TargetPath} (new)";
        }
    }

    public string TargetName
    {
        get
        {
            if(!HasTarget)
                return "Choose a solution";

            return Path.GetFileName(TargetPath);
        }
    }

    public string TargetFolder
    {
        get
        {
            return HasTarget ? Path.GetDirectoryName(TargetPath) : null;
        }
    }

    private string TargetFileName
    {
        get
        {
            return HasTarget ? Path.GetFileName(TargetPath) : "The solution";
        }
    }

    /// <summary>
    /// The model file has changes that are not saved: the line about the last Sync offers Save.
    /// </summary>
    public bool ShowSaveHint
    {
        get
        {
            return LastSync != null && LastSync.Succeeded && Shell.Document != null && Shell.Document.IsDirty;
        }
    }

    public override void OnShown()
    {
        OnPropertyChanged(nameof(SelectedTargetFramework));
        RaiseSyncedBlock();

        //a model file saved for the first time since it was opened: a new solution goes next to it
        var modelFile = Shell.Document?.FilePath;
        if(string.IsNullOrWhiteSpace(NewSolutionFolder) && !string.IsNullOrWhiteSpace(modelFile))
            NewSolutionFolder = Path.GetDirectoryName(modelFile);

        if(string.IsNullOrWhiteSpace(NewSolutionName))
            NewSolutionName = Shell.Document?.Container.CommonSettings.RootNamespace;

        //the solutions are looked for once per model file (and again once a new one is saved, for the solutions
        //next to it); the refresh button looks again
        if(_scannedDocument != Shell.Document || !string.Equals(_scannedPath, Shell.Document?.FilePath, StringComparison.OrdinalIgnoreCase))
            _ = RefreshTargetsAsync();

        //the model changed since the last check (Update, Layers, Settings): the old list goes, and the page works out
        //again what Sync would do
        if(Report != null && !IsReportCurrent)
            ClearReport();

        if(CanCheck && !IsReportCurrent)
            _ = CheckAsync();
    }

    private void RaiseSyncedBlock()
    {
        OnPropertyChanged(nameof(IsBlockedBySyncedFlag));
        OnPropertyChanged(nameof(SyncedBlockText));
        RaiseCanSync();
    }

    private void RaiseCanSync()
    {
        OnPropertyChanged(nameof(CanCheck));
        OnPropertyChanged(nameof(CanSync));
        OnPropertyChanged(nameof(SyncDisabledReason));
        OnPropertyChanged(nameof(SyncButtonText));
    }

    public override void OnDocumentChanged()
    {
        if(_document != null)
            _document.PropertyChanged -= OnDocumentPropertyChanged;

        _document = Shell.Document;
        if(_document != null)
            _document.PropertyChanged += OnDocumentPropertyChanged;

        _found = null;
        _reportTarget = null;
        Candidates.Clear();
        TargetPath = null;
        LastSync = null;
        LogTitle = null;
        ClearReport();
        Progress.Clear();
        var modelFile = Shell.Document?.FilePath;
        NewSolutionFolder = string.IsNullOrWhiteSpace(modelFile) ? null : Path.GetDirectoryName(modelFile);
        NewSolutionName = Shell.Document?.Container.CommonSettings.RootNamespace;
        OnPropertyChanged(nameof(HasCandidates));
        OnPropertyChanged(nameof(ShowNoCandidates));
        RaiseSyncedBlock();
        UpdateNavBadge();
    }

    /// <summary>
    /// Sync works on the whole model file: picking another model keeps the target and the report.
    /// </summary>
    public override void OnSelectedModelChanged()
    {
        RaiseSyncedBlock();
    }

    /// <summary>
    /// The model changed (Update, Layers, Settings): the badge says so at once, and on this page the changes are worked
    /// out again.
    /// </summary>
    private void OnDocumentPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if(string.Equals(e.PropertyName, nameof(ModelDocument.IsDirty), StringComparison.Ordinal))
            OnPropertyChanged(nameof(ShowSaveHint));

        if(!string.Equals(e.PropertyName, nameof(ModelDocument.ChangeCount), StringComparison.Ordinal))
            return;

        RaiseSyncedBlock();
        if(IsCurrentPage)
        {
            //the list says what Sync would do to the model as it was: it goes, and is worked out again
            ClearReport();
            if(CanCheck)
                _ = CheckAsync();
        }

        UpdateNavBadge();
    }

    partial void OnSelectedCandidateChanged(SolutionCandidateViewModel value)
    {
        //selecting the solution that is already the target (the list was rebuilt) is no new target: the report stays
        if(value != null && !string.Equals(TargetPath, value.SolutionPath, StringComparison.OrdinalIgnoreCase))
            TargetPath = value.SolutionPath;

        OnPropertyChanged(nameof(TargetBadge));
        OnPropertyChanged(nameof(HasTargetBadge));
    }

    partial void OnTargetPathChanged(string value)
    {
        IsTargetPanelOpen = string.IsNullOrWhiteSpace(value);
        OnPropertyChanged(nameof(TargetBadge));
        OnPropertyChanged(nameof(HasTargetBadge));
        OnPropertyChanged(nameof(HasTarget));
        TargetError = null;
        ClearReport();
        OnPropertyChanged(nameof(TargetDescription));
        OnPropertyChanged(nameof(TargetName));
        OnPropertyChanged(nameof(TargetFolder));
        if(IsCurrentPage && CanCheck)
            _ = CheckAsync();
    }

    partial void OnIsScanningChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowNoCandidates));
        OnPropertyChanged(nameof(CandidatesTitle));
    }

    /// <summary>
    /// The heading of the solution list: what it is doing while it scans.
    /// </summary>
    public string CandidatesTitle
    {
        get
        {
            return IsScanning ? "Looking for solutions" : "Solutions found";
        }
    }

    partial void OnIsRunningChanged(bool value)
    {
        Shell.IsBusy = value;
        RaiseCanSync();
    }

    partial void OnIsCheckingChanged(bool value)
    {
        RaiseCanSync();
        RaiseSummary();
        RaiseChangesEmpty();
    }

    partial void OnLastSyncChanged(LastSyncViewModel value)
    {
        OnPropertyChanged(nameof(ShowSaveHint));
    }

    partial void OnChangeFilterChanged(ChangeFilterOption value)
    {
        //a new list of filters clears the selection first: the filter set right after it fills the list
        if(value != null)
            FillFileChanges();
    }

    partial void OnChangeSearchChanged(string value)
    {
        FillFileChanges();
    }

    partial void OnReportChanged(SyncReport value)
    {
        OnPropertyChanged(nameof(HasReport));
        OnPropertyChanged(nameof(HasPendingFiles));
        RaiseSummary();
        RaiseCanSync();
        UpdateNavBadge();
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(IsSummaryInfo));
        OnPropertyChanged(nameof(SummaryTitle));
        OnPropertyChanged(nameof(SummaryRest));
        OnPropertyChanged(nameof(SummaryState));
        OnPropertyChanged(nameof(HasSummary));
    }

    /// <summary>
    /// What the "Sync into" strip says about the target: synced before, near the model, or new.
    /// </summary>
    public string TargetBadge
    {
        get
        {
            if(!HasTarget)
                return null;

            if(!File.Exists(TargetPath))
                return "New solution";

            if(SelectedCandidate != null && string.Equals(SelectedCandidate.SolutionPath, TargetPath, StringComparison.OrdinalIgnoreCase))
            {
                if(SelectedCandidate.LastSynced.HasValue)
                    return "Synced " + TimeText.Ago(SelectedCandidate.LastSynced.Value, DateTime.Now).ToLowerInvariant();

                if(SelectedCandidate.NearModel)
                    return "Near the model file";
            }

            return null;
        }
    }

    public bool HasTargetBadge
    {
        get
        {
            return TargetBadge != null;
        }
    }

    public bool HasReport
    {
        get
        {
            return Report != null;
        }
    }

    /// <summary>
    /// The bar over the changes: while checking, and once there is a result.
    /// </summary>
    public bool HasSummary
    {
        get
        {
            return IsChecking || Report != null;
        }
    }

    /// <summary>
    /// checking, pending, uptodate or error: the colour and icon of the bar over the changes.
    /// </summary>
    public string SummaryState
    {
        get
        {
            if(IsChecking)
                return "checking";

            if(Report == null)
                return null;

            if(!Report.Success || Report.UnmarkedFiles.Count > 0)
                return "error";

            return Report.IsUpToDate ? "uptodate" : "pending";
        }
    }

    /// <summary>
    /// The bar is blue while checking and when there is something to write.
    /// </summary>
    public bool IsSummaryInfo
    {
        get
        {
            var state = SummaryState;
            return string.Equals(state, "checking", StringComparison.Ordinal) || string.Equals(state, "pending", StringComparison.Ordinal);
        }
    }

    public string SummaryTitle
    {
        get
        {
            if(IsChecking)
                return "Working out what Sync would change…";

            if(Report == null)
                return null;

            if(!Report.Success)
                return "The check failed";

            if(Report.UnmarkedFiles.Count > 0)
                return "Sync would stop";

            if(Report.IsUpToDate)
                return "Up to date";

            if(Report.FileChanges.Count > 0)
                return ModelTreeBuilder.Plural(Report.FileChanges.Count, "file") + " to write";

            return "Projects to update";
        }
    }

    public string SummaryRest
    {
        get
        {
            if(IsChecking)
                return "nothing is written";

            if(Report == null)
                return null;

            if(!Report.Success)
                return Report.ErrorMessage + " The Log tab has the details.";

            if(Report.UnmarkedFiles.Count > 0)
                return $"{ModelTreeBuilder.Plural(Report.UnmarkedFiles.Count, "file")} at generated paths were not written by Sharp Factory (the Log tab lists them): move or rename them, then check again";

            if(Report.IsUpToDate)
                return $"{TargetFileName} matches the model";

            var parts = new List<string> { "into " + TargetFileName };
            if(Report.ProjectsCreated.Count > 0)
                parts.Add(ModelTreeBuilder.Plural(Report.ProjectsCreated.Count, "new project"));

            if(Report.ReferencesAdded.Count > 0)
                parts.Add(ModelTreeBuilder.Plural(Report.ReferencesAdded.Count, "project reference"));

            if(Report.TargetFrameworksChanged.Count > 0)
                parts.Add(ModelTreeBuilder.Plural(Report.TargetFrameworksChanged.Count, "project") + " to " + ProjectFileTemplate.GetDisplayName(Shell.Document?.Container.CommonSettings.TargetFramework ?? Models.TargetFrameworks.NET10));

            if(Report.UnchangedFileCount > 0)
                parts.Add($"{Report.UnchangedFileCount} unchanged");

            return string.Join(" · ", parts);
        }
    }

    partial void OnSelectedFileChangeChanged(FileChangeViewModel value)
    {
        //emptying the list unselects the file for a moment: the diff stays until the list is filled
        if(_fillingFileChanges)
            return;

        ShowDiff(value);
    }

    private Task _currentScan;
    //the last scan's searches, for the open document: the list is rebuilt from them when the settings change
    private FindTargetsJob _found;
    private ModelDocument _scannedDocument;
    private ModelDocument _document;
    private string _scannedPath;
    private int _reportChangeCount;
    private string _reportTarget;
    private bool _fillingFileChanges;
    private SyncFileChange _diffFor;
    private DiffViewModel _diffShown;
    private Task _checkLoop;
    private bool _checkAgain;
    private bool _isLogTabSelected;

    [RelayCommand]
    private async Task RefreshTargetsAsync()
    {
        if(Shell.Document == null)
            return;

        //a refresh during a scan waits for it, then scans again
        if(_currentScan != null && !_currentScan.IsCompleted)
            await _currentScan;

        _currentScan = ScanAsync();
        await _currentScan;
    }

    private async Task ScanAsync()
    {
        var document = Shell.Document;
        _scannedDocument = document;
        _scannedPath = document.FilePath;
        IsScanning = true;
        try
        {
            //the slow searches (open IDEs, recent lists, folders) run on another thread; the settings are only read here
            var job = new FindTargetsJob(document.FilePath);
            await Task.Run(job.Run);

            //another model file was opened during the scan: these solutions are not for it
            if(Shell.Document != document)
                return;

            _found = job;
            RebuildCandidates();
        }
        finally
        {
            IsScanning = false;
            OnPropertyChanged(nameof(HasCandidates));
            OnPropertyChanged(nameof(ShowNoCandidates));
        }
    }

    /// <summary>
    /// Builds the solution list from the last searches and the settings as they are now, on the UI thread where the
    /// settings change: after a scan, and after a Sync records its target (its "Synced" date, or a solution it created).
    /// </summary>
    private void RebuildCandidates()
    {
        if(_found == null || Shell.Document == null)
            return;

        var found = TargetSolutionFinder.Find(Shell.Document.FilePath, Shell.Settings, _found.NearModel);
        var previousTarget = TargetPath;
        Candidates.Clear();
        foreach(var candidate in found)
            Candidates.Add(new SolutionCandidateViewModel(candidate));

        //keep the current choice (highlighted when it is in the list); otherwise suggest the solution this model
        //was last synced to, or when it never was, the one nearest the model file; otherwise none
        var suggested = TargetSolutionFinder.FindSuggested(found);
        if(!string.IsNullOrWhiteSpace(previousTarget))
            SelectedCandidate = FindCandidate(previousTarget);
        else if(suggested != null)
            SelectedCandidate = FindCandidate(suggested.SolutionPath);

        OnPropertyChanged(nameof(HasCandidates));
        OnPropertyChanged(nameof(ShowNoCandidates));
    }

    private SolutionCandidateViewModel FindCandidate(string solutionPath)
    {
        foreach(var candidate in Candidates)
        {
            if(string.Equals(candidate.SolutionPath, solutionPath, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return null;
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        var path = await Shell.FileDialogs.OpenSolutionFileAsync();
        if(path == null)
            return;

        if(!SolutionFile.IsSupported(path))
        {
            TargetError = "Choose a .sln or .slnx file.";
            return;
        }

        SelectedCandidate = null;
        TargetPath = Path.GetFullPath(path);
    }

    [RelayCommand]
    private async Task ChooseNewSolutionFolderAsync()
    {
        var folder = await Shell.FileDialogs.PickFolderAsync("Folder for the new solution");
        if(folder != null)
            NewSolutionFolder = folder;
    }

    [RelayCommand]
    private void UseNewSolution()
    {
        TargetError = null;
        if(string.IsNullOrWhiteSpace(NewSolutionFolder) || string.IsNullOrWhiteSpace(NewSolutionName))
        {
            TargetError = "Choose a folder and a name for the new solution.";
            return;
        }

        var name = NewSolutionName.Trim();
        if(name.IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
        {
            TargetError = "The solution name has characters a file name cannot have.";
            return;
        }

        var path = Path.GetFullPath(Path.Combine(NewSolutionFolder, name + NewSolutionFormat));
        SelectedCandidate = null;
        TargetPath = path;

        //after TargetPath: changing the target clears the message
        if(File.Exists(path))
            TargetError = "That solution already exists: it will be synced as it is.";
    }

    [RelayCommand]
    private Task Check()
    {
        return CheckAsync();
    }

    /// <summary>
    /// Works out what Sync would change, on a background thread; asked again while it runs, it runs once more after.
    /// </summary>
    public Task CheckAsync()
    {
        if(_checkLoop != null && !_checkLoop.IsCompleted)
        {
            _checkAgain = true;
            return _checkLoop;
        }

        _checkLoop = CheckLoopAsync();
        return _checkLoop;
    }

    private async Task CheckLoopAsync()
    {
        do
        {
            _checkAgain = false;
            await CheckOnceAsync();
        }
        while(_checkAgain && CanCheck && IsCurrentPage);
    }

    private async Task CheckOnceAsync()
    {
        if(!CanCheck)
            return;

        var document = Shell.Document;
        var target = TargetPath;
        var changeCount = document.ChangeCount;
        Progress.Clear();
        IsChecking = true;
        Progress.IsRunning = true;
        try
        {
            var request = new SyncRequest { ModelContainer = document.Container, SolutionPath = target, Preview = true };
            var job = new SyncJob(request, new UiProgressLog(Progress));
            await Task.Run(job.Run);

            //the model, the file or the target changed meanwhile: this result is about something that is gone
            if(Shell.Document != document || document.ChangeCount != changeCount || !string.Equals(TargetPath, target, StringComparison.OrdinalIgnoreCase))
            {
                _checkAgain = true;
                return;
            }

            LogTitle = $"Check at {Now()} · nothing was written";
            ShowReport(job.Report, changeCount, target);
        }
        finally
        {
            Progress.IsRunning = false;
            IsChecking = false;
        }
    }

    [RelayCommand]
    private async Task SyncAsync()
    {
        await RunSyncAsync();
    }

    [RelayCommand]
    private void ClearReport()
    {
        Report = null;
        FileChanges.Clear();
        _diffFor = null;
        _diffShown = null;
        Diff = null;
        SelectedFileChange = null;
        ChangeFilters = ChangeFilterOption.For(null);
        ChangeFilter = ChangeFilters[0];
        RaiseChangesEmpty();
    }

    private void RaiseChangesEmpty()
    {
        OnPropertyChanged(nameof(ChangesEmptyText));
        OnPropertyChanged(nameof(HasChangesEmptyText));
        OnPropertyChanged(nameof(HasPendingFiles));
        OnPropertyChanged(nameof(FillsPage));
    }

    /// <summary>
    /// Shows what Sync would change: the list, its filters (with counts) and the first file's difference.
    /// </summary>
    private void ShowReport(SyncReport report, int changeCount, string target)
    {
        _reportChangeCount = changeCount;
        _reportTarget = target;
        Report = report;

        //a new filter object: its change handler fills the list (and selects the first file) once
        ChangeFilters = ChangeFilterOption.For(report);
        ChangeFilter = ChangeFilters[0];
        RaiseChangesEmpty();
        RaiseCanSync();
        UpdateNavBadge();
    }

    /// <summary>
    /// The Sync item of the navigation says how many files Sync would write, or that the model changed since the last
    /// check (a dot), until the page has worked it out again.
    /// </summary>
    private void UpdateNavBadge()
    {
        var badge = (string)null;
        var tip = (string)null;
        if(Shell.Document != null && HasTarget && _reportTarget != null)
        {
            if(Shell.Document.ChangeCount != _reportChangeCount || !string.Equals(_reportTarget, TargetPath, StringComparison.OrdinalIgnoreCase))
            {
                badge = "•";
                tip = "The model changed since the last check: open Sync to see what changes.";
            }
            else if(Report != null && Report.Success && Report.FileChanges.Count > 0)
            {
                badge = Report.FileChanges.Count.ToString(CultureInfo.InvariantCulture);
                tip = ModelTreeBuilder.Plural(Report.FileChanges.Count, "file") + " to write";
            }
        }

        Shell.SetNavBadge(this, badge, tip);
    }

    private static string Now()
    {
        return DateTime.Now.ToString("HH:mm", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Asks before a Sync into a solution that is not the model's own: another one than the model was last synced
    /// to, or, when it never was, one whose folder does not have the model file. True to go on.
    /// </summary>
    private async Task<bool> ConfirmTargetAsync()
    {
        var modelFile = Shell.Document.FilePath;
        if(string.IsNullOrWhiteSpace(modelFile))
            return true;

        var target = Path.GetFullPath(TargetPath);
        var lastSynced = TargetSolutionFinder.FindLastSynced(modelFile, Shell.Settings);
        var modelName = Path.GetFileName(modelFile);
        var message = (string)null;
        var details = (string)null;
        if(lastSynced != null)
        {
            if(!TargetSolutionFinder.IsSamePath(lastSynced, target))
            {
                message = $"{modelName} was last synced into a different solution.";
                details = $"Last synced into{Environment.NewLine}{lastSynced}{Environment.NewLine}{Environment.NewLine}This Sync{Environment.NewLine}{target}";
            }
        }
        else if(!TargetSolutionFinder.IsModelInSolutionFolder(modelFile, target))
        {
            message = $"This solution is not in the folder of {modelName}.";
            details = $"Model file{Environment.NewLine}{Path.GetFullPath(modelFile)}{Environment.NewLine}{Environment.NewLine}This Sync{Environment.NewLine}{target}";
        }

        if(message == null)
            return true;

        var dialog = new DialogViewModel
        {
            Title = "Sync into another solution?",
            IsWarning = true,
            Message = message,
            DetailsTitle = "Where",
            Details = details,
            Buttons = new List<DialogButton>
            {
                new DialogButton { Text = "Cancel", Result = DialogResults.No },
                new DialogButton { Text = "Sync into " + Path.GetFileName(target), Result = DialogResults.Yes, IsPrimary = true },
            },
        };

        var answer = await Shell.ShowDialogAsync(dialog);
        return string.Equals(answer, DialogResults.Yes, StringComparison.Ordinal);
    }

    /// <summary>
    /// Writes the model's code into the target on a background thread. The page then keeps a line about it, and,
    /// when it worked, the solution matches the model: nothing is left to write.
    /// </summary>
    public async Task RunSyncAsync()
    {
        //a check still running finishes first: Sync goes on only when it found something to write
        if(_checkLoop != null && !_checkLoop.IsCompleted)
            await _checkLoop;

        if(!CanSync)
            return;

        if(!await ConfirmTargetAsync())
            return;

        var document = Shell.Document;
        var target = TargetPath;
        var changeCount = document.ChangeCount;
        Progress.Clear();
        IsRunning = true;
        Progress.IsRunning = true;
        var succeeded = false;
        try
        {
            var request = new SyncRequest { ModelContainer = document.Container, SolutionPath = target, Preview = false };
            var job = new SyncJob(request, new UiProgressLog(Progress));
            await Task.Run(job.Run);

            var report = job.Report;
            LastSync = new LastSyncViewModel(report, Path.GetFileName(target), DateTime.Now);
            LogTitle = $"Sync at {Now()}";
            OnPropertyChanged(nameof(TargetBadge));
            OnPropertyChanged(nameof(HasTargetBadge));
            succeeded = report.Success;

            if(succeeded)
            {
                //everything the model generates is in the solution now: nothing is left to write
                var written = report.CountFiles(SyncFileChangeKind.Added) + report.CountFiles(SyncFileChangeKind.Changed);
                ShowReport(new SyncReport { Success = true, IsPreview = true, SolutionPath = report.SolutionPath, UnchangedFileCount = report.UnchangedFileCount + written }, changeCount, target);

                if(!string.IsNullOrWhiteSpace(document.FilePath))
                {
                    AppSettingsStore.RecordSyncTarget(Shell.Settings, document.FilePath, target);
                    AppSettingsStore.Save(Shell.Settings);

                    //the list shows the new "Synced" date, and a solution this Sync created
                    RebuildCandidates();
                }

                Shell.StatusText = $"Synced to {Path.GetFileName(target)}";
                OnPropertyChanged(nameof(TargetDescription));
                OnPropertyChanged(nameof(TargetName));
            }
        }
        finally
        {
            Progress.IsRunning = false;
            IsRunning = false;
        }

        //a Sync that stopped keeps its log (the reason is in it) and the list it started from; Check again works out
        //what is left to write
        if(!succeeded)
            RaiseCanSync();
    }

    [RelayCommand]
    private async Task SaveModel()
    {
        await Shell.SaveDocumentAsync();
        OnPropertyChanged(nameof(ShowSaveHint));
    }

    private void FillFileChanges()
    {
        //the selected file stays selected while it is still in the list
        var previous = SelectedFileChange;
        var keep = (FileChangeViewModel)null;
        _fillingFileChanges = true;
        try
        {
            FileChanges.Clear();
        }
        finally
        {
            _fillingFileChanges = false;
        }

        if(Report != null)
        {
            foreach(var change in Report.FileChanges)
            {
                var kindMatches = ChangeFilter == null || ChangeFilter.Kind == null || ChangeFilter.Kind.Value == change.Kind;
                var textMatches = string.IsNullOrWhiteSpace(ChangeSearch) || change.RelativePath.Contains(ChangeSearch.Trim(), StringComparison.OrdinalIgnoreCase);
                if(!kindMatches || !textMatches)
                    continue;

                if(previous != null && previous.Change == change)
                {
                    keep = previous;
                    FileChanges.Add(previous);
                }
                else
                {
                    FileChanges.Add(new FileChangeViewModel(change));
                }
            }
        }

        RaiseChangesEmpty();
        if(keep != null)
            SelectedFileChange = keep;
        else if(FileChanges.Count > 0)
            SelectedFileChange = FileChanges[0];
        else
        {
            //nothing to show; the list may already have unselected the file while it was emptied
            SelectedFileChange = null;
            ShowDiff(null);
        }
    }

    private void ShowDiff(FileChangeViewModel change)
    {
        if(change == null)
        {
            Diff = null;
            return;
        }

        //the same file again (the list was filtered): its difference is already worked out
        if(change.Change == _diffFor && _diffShown != null)
        {
            Diff = _diffShown;
            return;
        }

        var result = new DiffViewModel { Title = change.FileName, Folder = change.Folder, Letter = change.Letter };
        var oldText = change.Change.OldContent ?? "";
        var newText = change.Change.NewContent ?? "";
        var diff = InlineDiffBuilder.Diff(oldText, newText, false);
        var added = 0;
        var removed = 0;
        var oldNumber = 0;
        var newNumber = 0;
        foreach(var line in diff.Lines)
        {
            var kind = ToKind(line.Type);
            var isDeleted = string.Equals(kind, "Deleted", StringComparison.Ordinal);
            var isInserted = string.Equals(kind, "Inserted", StringComparison.Ordinal);
            if(!isInserted)
                oldNumber++;

            if(!isDeleted)
                newNumber++;

            result.Lines.Add(new DiffLineViewModel
            {
                Kind = kind,
                Text = line.Text,
                Number = isDeleted ? null : newNumber.ToString(),
                OldNumber = isInserted ? null : oldNumber.ToString(),
            });
            if(string.Equals(kind, "Inserted", StringComparison.Ordinal))
                added++;
            else if(string.Equals(kind, "Deleted", StringComparison.Ordinal))
                removed++;
        }

        //a whole new or deleted file is not tinted line by line: the header says it
        switch(change.Change.Kind)
        {
            case SyncFileChangeKind.Added:
                result.Header = $"New file · {LineCount(result.Lines.Count)}";
                break;
            case SyncFileChangeKind.Deleted:
                result.Header = $"Deleted file · {LineCount(result.Lines.Count)}";
                break;
            default:
                result.Header = $"+{added} −{removed} lines";
                result.IsTinted = true;
                break;
        }

        _diffFor = change.Change;
        _diffShown = result;
        Diff = result;
    }

    private static string LineCount(int count)
    {
        return count == 1 ? "1 line" : $"{count} lines";
    }

    private static string ToKind(ChangeType type)
    {
        switch(type)
        {
            case ChangeType.Inserted:
                return "Inserted";
            case ChangeType.Deleted:
                return "Deleted";
            case ChangeType.Modified:
                return "Modified";
            default:
                return "Unchanged";
        }
    }

    /// <summary>
    /// Finds the solutions near the model (a folder search, so it runs on a background thread).
    /// </summary>
    private sealed class FindTargetsJob
    {
        private readonly string _modelFile;

        public FindTargetsJob(string modelFile)
        {
            _modelFile = modelFile;
        }

        public List<string> NearModel { get; set; } = new List<string>();

        /// <summary>
        /// Only the searches: nothing here reads the app settings, which the UI thread changes.
        /// </summary>
        public void Run()
        {
            NearModel = TargetSolutionFinder.FindNearModel(_modelFile);
        }
    }

    /// <summary>
    /// One check or Sync. The generator writes through one shared buffer: the code preview of the Models page waits
    /// for it, and it waits for that preview.
    /// </summary>
    private sealed class SyncJob
    {
        private readonly SyncRequest _request;
        private readonly IProgressLog _log;

        public SyncJob(SyncRequest request, IProgressLog log)
        {
            _request = request;
            _log = log;
        }

        public SyncReport Report { get; set; }

        public void Run()
        {
            lock(GenerationLock.Gate)
            {
                try
                {
                    Report = SolutionSync.Run(_request, _log);
                }
                catch(Exception ex)
                {
                    //the model can change while a check reads it (it is then thrown away); anything else is an answer too
                    Report = new SyncReport { Success = false, IsPreview = _request.Preview, ErrorMessage = ex.Message };
                }
            }
        }
    }
}

/// <summary>
/// The line about the last Sync: when, where, and what it wrote (or why it stopped).
/// </summary>
internal sealed class LastSyncViewModel
{
    public LastSyncViewModel(SyncReport report, string targetName, DateTime when)
    {
        Succeeded = report.Success;
        var time = when.ToString("HH:mm", CultureInfo.CurrentCulture);
        if(!report.Success)
        {
            Title = $"Last Sync at {time} stopped";
            Text = report.ErrorMessage + " The Log tab has the details; Check again shows what is left to write.";
            return;
        }

        var written = report.FileChanges.Count;
        if(report.IsUpToDate)
        {
            Title = $"Last Sync at {time}";
            Text = $"{targetName} was already up to date";
            return;
        }

        var parts = new List<string>();
        var added = report.CountFiles(SyncFileChangeKind.Added);
        var changed = report.CountFiles(SyncFileChangeKind.Changed);
        var deleted = report.CountFiles(SyncFileChangeKind.Deleted);
        if(added > 0)
            parts.Add($"{added} added");

        if(changed > 0)
            parts.Add($"{changed} changed");

        if(deleted > 0)
            parts.Add($"{deleted} deleted");

        if(report.ProjectsCreated.Count > 0)
            parts.Add(ModelTreeBuilder.Plural(report.ProjectsCreated.Count, "new project"));

        Title = $"Last Sync at {time}";
        Text = written > 0 ? $"wrote {ModelTreeBuilder.Plural(written, "file")} into {targetName}" : $"updated {targetName}";
        if(parts.Count > 0)
            Text += " (" + string.Join(", ", parts) + ")";
    }

    public bool Succeeded { get; }
    public string Title { get; }
    public string Text { get; }
}

internal sealed class TargetFrameworkOption
{
    public TargetFrameworks Value { get; set; }
    public string Name { get; set; }
}

internal sealed class SolutionCandidateViewModel
{
    public SolutionCandidateViewModel(SolutionCandidate candidate)
    {
        SolutionPath = candidate.SolutionPath;
        Name = candidate.Name;
        Description = candidate.Description;
        NearModel = candidate.NearModel;
        LastSynced = candidate.LastSynced;
        Extension = Path.GetExtension(candidate.SolutionPath);
    }

    public string SolutionPath { get; }
    public string Name { get; }
    public string Description { get; }
    public bool NearModel { get; }
    public DateTime? LastSynced { get; }
    public string Extension { get; }
}

/// <summary>
/// One choice of the changes filter, with its count: "Added (152)".
/// </summary>
internal sealed class ChangeFilterOption
{
    public SyncFileChangeKind? Kind { get; set; }
    public string Name { get; set; }
    public int Count { get; set; }

    public string Display
    {
        get
        {
            return $"{Name} {Count}";
        }
    }

    public static List<ChangeFilterOption> For(SyncReport report)
    {
        return new List<ChangeFilterOption>
        {
            new ChangeFilterOption { Name = "All", Count = report == null ? 0 : report.FileChanges.Count },
            new ChangeFilterOption { Kind = SyncFileChangeKind.Added, Name = "Added", Count = report == null ? 0 : report.CountFiles(SyncFileChangeKind.Added) },
            new ChangeFilterOption { Kind = SyncFileChangeKind.Changed, Name = "Changed", Count = report == null ? 0 : report.CountFiles(SyncFileChangeKind.Changed) },
            new ChangeFilterOption { Kind = SyncFileChangeKind.Deleted, Name = "Deleted", Count = report == null ? 0 : report.CountFiles(SyncFileChangeKind.Deleted) },
        };
    }
}

/// <summary>
/// The selected file's difference, shown in the code editor: the text, one kind per line, and a header.
/// </summary>
internal sealed class DiffViewModel
{
    public string Title { get; set; }
    public string Folder { get; set; }
    public string Letter { get; set; }
    public string Header { get; set; }

    /// <summary>
    /// False for whole new or deleted files: every line is the same kind.
    /// </summary>
    public bool IsTinted { get; set; }

    public List<DiffLineViewModel> Lines { get; } = new List<DiffLineViewModel>();

    /// <summary>
    /// The first inserted or deleted line, counted from 1; 0 when there is none.
    /// </summary>
    public int FirstChangedLine
    {
        get
        {
            for(var i = 0; i < Lines.Count; i++)
            {
                if(!string.Equals(Lines[i].Kind, "Unchanged", StringComparison.Ordinal))
                    return i + 1;
            }

            return 0;
        }
    }

    public string Text
    {
        get
        {
            var lines = new List<string>();
            foreach(var line in Lines)
                lines.Add(line.Text);

            return string.Join("\n", lines);
        }
    }
}

internal sealed class FileChangeViewModel
{
    public FileChangeViewModel(SyncFileChange change)
    {
        Change = change;
    }

    public SyncFileChange Change { get; }

    public string FileName
    {
        get
        {
            return Path.GetFileName(Change.RelativePath);
        }
    }

    public string Folder
    {
        get
        {
            return Path.GetDirectoryName(Change.RelativePath);
        }
    }

    /// <summary>
    /// The status chip: A, M or D.
    /// </summary>
    public string Letter
    {
        get
        {
            switch(Change.Kind)
            {
                case SyncFileChangeKind.Added:
                    return "A";
                case SyncFileChangeKind.Deleted:
                    return "D";
                default:
                    return "M";
            }
        }
    }

    public string RelativePath
    {
        get
        {
            return Change.RelativePath;
        }
    }

    public string Kind
    {
        get
        {
            return Change.Kind.ToString();
        }
    }
}

internal sealed class DiffLineViewModel
{
    public string Kind { get; set; }
    public string Text { get; set; }

    /// <summary>
    /// The line number in the new file; null for a deleted line.
    /// </summary>
    public string Number { get; set; }

    /// <summary>
    /// The line number in the old file; null for an inserted line.
    /// </summary>
    public string OldNumber { get; set; }
}
