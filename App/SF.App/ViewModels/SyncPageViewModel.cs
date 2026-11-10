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
using System.IO;
using System.Threading.Tasks;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// Sync: choose a target solution (open in an IDE, synced before, near the model, browse, or new), preview
/// what changes, then write it.
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

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private SyncReport _report;

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

    public SyncPageViewModel(MainWindowViewModel shell) : base(shell)
    {
        _changeFilter = _changeFilters[0];
    }

    /// <summary>
    /// What the changes list says when it has nothing to show; null when it has rows.
    /// </summary>
    public string ChangesEmptyText
    {
        get
        {
            if(Report == null)
                return "Run Preview to see what Sync would add, change or delete. Nothing is written until you Sync.";

            if(!Report.Success)
                return "Nothing to show: the run failed. The Log tab has the details.";

            if(FileChanges.Count > 0)
                return null;

            if(Report.FileChanges.Count == 0)
                return Report.IsPreview ? "Nothing to change: the solution is up to date." : "Nothing changed: the solution was already up to date.";

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

            //the projects Sync creates change: a preview made before no longer says what Sync would do
            ClearReport();
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

    public bool CanRun
    {
        get
        {
            return !IsRunning && !string.IsNullOrWhiteSpace(TargetPath) && Shell.Document != null && !IsBlockedBySyncedFlag;
        }
    }

    public string TargetDescription
    {
        get
        {
            if(string.IsNullOrWhiteSpace(TargetPath))
                return "Choose a solution";

            return File.Exists(TargetPath) ? TargetPath : $"{TargetPath} (new)";
        }
    }

    public string TargetName
    {
        get
        {
            if(string.IsNullOrWhiteSpace(TargetPath))
                return "Choose a solution";

            return File.Exists(TargetPath) ? Path.GetFileName(TargetPath) : $"{Path.GetFileName(TargetPath)} (new)";
        }
    }

    public string TargetFolder
    {
        get
        {
            return string.IsNullOrWhiteSpace(TargetPath) ? null : Path.GetDirectoryName(TargetPath);
        }
    }

    public override void OnShown()
    {
        OnPropertyChanged(nameof(SelectedTargetFramework));
        RaiseSyncedBlock();

        //the Layers or Settings page changed the model since the run: its report no longer says what Sync would do
        if(Report != null && Shell.Document != null && Shell.Document.ChangeCount != _reportChangeCount)
            ClearReport();

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
    }

    private void RaiseSyncedBlock()
    {
        OnPropertyChanged(nameof(IsBlockedBySyncedFlag));
        OnPropertyChanged(nameof(SyncedBlockText));
        OnPropertyChanged(nameof(CanRun));
    }

    public override void OnDocumentChanged()
    {
        Candidates.Clear();
        TargetPath = null;
        ClearReport();
        Progress.Clear();
        var modelFile = Shell.Document?.FilePath;
        NewSolutionFolder = string.IsNullOrWhiteSpace(modelFile) ? null : Path.GetDirectoryName(modelFile);
        NewSolutionName = Shell.Document?.Container.CommonSettings.RootNamespace;
        OnPropertyChanged(nameof(HasCandidates));
        OnPropertyChanged(nameof(ShowNoCandidates));
        RaiseSyncedBlock();
    }

    /// <summary>
    /// Sync works on the whole model file: picking another model keeps the target and the report.
    /// </summary>
    public override void OnSelectedModelChanged()
    {
        RaiseSyncedBlock();
    }

    partial void OnSelectedCandidateChanged(SolutionCandidateViewModel value)
    {
        if(value != null)
            TargetPath = value.SolutionPath;

        OnPropertyChanged(nameof(TargetBadge));
        OnPropertyChanged(nameof(HasTargetBadge));
    }

    partial void OnTargetPathChanged(string value)
    {
        IsTargetPanelOpen = string.IsNullOrWhiteSpace(value);
        OnPropertyChanged(nameof(TargetBadge));
        OnPropertyChanged(nameof(HasTargetBadge));
        TargetError = null;
        ClearReport();
        OnPropertyChanged(nameof(CanRun));
        OnPropertyChanged(nameof(TargetDescription));
        OnPropertyChanged(nameof(TargetName));
        OnPropertyChanged(nameof(TargetFolder));
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
        OnPropertyChanged(nameof(CanRun));
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
        OnPropertyChanged(nameof(SummaryTitle));
        OnPropertyChanged(nameof(SummaryRest));
        OnPropertyChanged(nameof(SummaryState));
        OnPropertyChanged(nameof(AddedText));
        OnPropertyChanged(nameof(ChangedText));
        OnPropertyChanged(nameof(DeletedText));
        OnPropertyChanged(nameof(UnchangedText));
        OnPropertyChanged(nameof(ShowCounts));
    }

    /// <summary>
    /// What the "Sync into" strip says about the target: open in an IDE, synced before, or new.
    /// </summary>
    public string TargetBadge
    {
        get
        {
            if(string.IsNullOrWhiteSpace(TargetPath))
                return null;

            if(!File.Exists(TargetPath))
                return "New solution";

            if(SelectedCandidate != null && string.Equals(SelectedCandidate.SolutionPath, TargetPath, StringComparison.OrdinalIgnoreCase))
            {
                if(SelectedCandidate.IsOpen)
                    return "Open in an IDE";

                if(SelectedCandidate.LastSynced.HasValue)
                    return "Synced " + TimeText.Ago(SelectedCandidate.LastSynced.Value, DateTime.Now).ToLowerInvariant();
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
    /// preview, success or error: the colour of the summary bar.
    /// </summary>
    public string SummaryState
    {
        get
        {
            if(Report == null)
                return null;

            if(!Report.Success)
                return "error";

            return Report.IsPreview ? "preview" : "success";
        }
    }

    public string SummaryTitle
    {
        get
        {
            if(Report == null)
                return null;

            if(!Report.Success)
                return "Sync stopped";

            if(Report.IsUpToDate)
                return "Already up to date";

            return Report.IsPreview ? "Preview" : "Synced";
        }
    }

    public string SummaryRest
    {
        get
        {
            if(Report == null)
                return null;

            if(!Report.Success)
                return Report.ErrorMessage + " The Log tab has the details.";

            if(Report.IsUpToDate)
                return "nothing to write";

            var projects = ModelTreeBuilder.Plural(Report.ProjectsCreated.Count, "new project");
            var references = ModelTreeBuilder.Plural(Report.ReferencesAdded.Count, "reference");
            if(Report.IsPreview && Report.UnmarkedFiles.Count > 0)
                return $"Sync will stop: {ModelTreeBuilder.Plural(Report.UnmarkedFiles.Count, "file")} at generated paths not written by Sharp Factory (the Log tab lists them) · {projects} · {references}";

            if(Report.IsPreview)
                return $"nothing is written until you Sync · {projects} · {references}";

            return $"written into {Path.GetFileName(TargetPath)} · {projects} · {references}";
        }
    }

    public bool ShowCounts
    {
        get
        {
            return Report != null && Report.Success && !Report.IsUpToDate;
        }
    }

    public string AddedText
    {
        get
        {
            return Report == null ? null : $"+{Report.CountFiles(SyncFileChangeKind.Added)} added";
        }
    }

    public string ChangedText
    {
        get
        {
            return Report == null ? null : $"~{Report.CountFiles(SyncFileChangeKind.Changed)} changed";
        }
    }

    public string DeletedText
    {
        get
        {
            return Report == null ? null : $"−{Report.CountFiles(SyncFileChangeKind.Deleted)} deleted";
        }
    }

    public string UnchangedText
    {
        get
        {
            return Report == null ? null : $"{Report.UnchangedFileCount} unchanged";
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
    private ModelDocument _scannedDocument;
    private string _scannedPath;
    private int _reportChangeCount;
    private bool _fillingFileChanges;
    private SyncFileChange _diffFor;
    private DiffViewModel _diffShown;

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
            var job = new FindTargetsJob(document.FilePath, Shell.Settings);
            await Task.Run(job.Run);

            //another model file was opened during the scan: these solutions are not for it
            if(Shell.Document != document)
                return;

            var previousTarget = TargetPath;
            Candidates.Clear();
            foreach(var candidate in job.Result)
                Candidates.Add(new SolutionCandidateViewModel(candidate));

            //keep the current choice (highlighted when it is in the list); otherwise suggest the first open or
            //recently synced solution
            if(!string.IsNullOrWhiteSpace(previousTarget))
                SelectedCandidate = FindCandidate(previousTarget);
            else if(Candidates.Count > 0 && (Candidates[0].IsOpen || Candidates[0].LastSynced != null))
                SelectedCandidate = Candidates[0];
        }
        finally
        {
            IsScanning = false;
            OnPropertyChanged(nameof(HasCandidates));
            OnPropertyChanged(nameof(ShowNoCandidates));
        }
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
    private async Task PreviewAsync()
    {
        await RunAsync(true);
    }

    [RelayCommand]
    private async Task SyncAsync()
    {
        await RunAsync(false);
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
    }

    /// <summary>
    /// Runs a preview or a Sync on a background thread and shows the report.
    /// </summary>
    public async Task RunAsync(bool preview)
    {
        if(!CanRun)
            return;

        ClearReport();
        Progress.Clear();
        IsRunning = true;
        Progress.IsRunning = true;

        try
        {
            var request = new SyncRequest { ModelContainer = Shell.Document.Container, SolutionPath = TargetPath, Preview = preview };
            var job = new SyncJob(request, new UiProgressLog(Progress));
            await Task.Run(job.Run);

            Report = job.Report;
            _reportChangeCount = Shell.Document.ChangeCount;
            OnPropertyChanged(nameof(TargetBadge));
            OnPropertyChanged(nameof(HasTargetBadge));
            //a new filter object: its change handler fills the list (and selects the first file) once
            ChangeFilters = ChangeFilterOption.For(job.Report);
            ChangeFilter = ChangeFilters[0];

            if(job.Report.Success && !preview)
            {
                if(!string.IsNullOrWhiteSpace(Shell.Document.FilePath))
                {
                    AppSettingsStore.RecordSyncTarget(Shell.Settings, Shell.Document.FilePath, TargetPath);
                    AppSettingsStore.Save(Shell.Settings);
                }

                Shell.StatusText = $"Synced to {Path.GetFileName(TargetPath)}";
                OnPropertyChanged(nameof(TargetDescription));
                OnPropertyChanged(nameof(TargetName));
            }
        }
        finally
        {
            Progress.IsRunning = false;
            IsRunning = false;
        }
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
        if(!Report.IsPreview)
        {
            result.Message = "Sync keeps file contents for previews only. Run Preview to see the differences.";
            Diff = result;
            return;
        }

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
    /// Finds the target candidates (scans running IDEs, so it runs on a background thread).
    /// </summary>
    private sealed class FindTargetsJob
    {
        private readonly string _modelFile;
        private readonly AppSettings _settings;

        public FindTargetsJob(string modelFile, AppSettings settings)
        {
            _modelFile = modelFile;
            _settings = settings;
        }

        public List<SolutionCandidate> Result { get; set; } = new List<SolutionCandidate>();

        public void Run()
        {
            Result = TargetSolutionFinder.Find(_modelFile, _settings, RunningIdeScanner.Scan(), RecentSolutionsReader.Read());
        }
    }

    /// <summary>
    /// One Sync or preview run.
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
            Report = SolutionSync.Run(_request, _log);
        }
    }
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
        IsOpen = candidate.IsOpen;
        LastSynced = candidate.LastSynced;
        Extension = Path.GetExtension(candidate.SolutionPath);
    }

    public string SolutionPath { get; }
    public string Name { get; }
    public string Description { get; }
    public bool IsOpen { get; }
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
    /// Shown instead of the text (after a real Sync, which keeps no contents).
    /// </summary>
    public string Message { get; set; }

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

    public bool HasMessage
    {
        get
        {
            return Message != null;
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
