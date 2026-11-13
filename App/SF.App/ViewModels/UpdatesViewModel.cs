#region Usings
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SF.App.Services.Updates;
using SF.Common.Logging;
using SF.Engine.Settings;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// Automatic updates: a check once a day (and from the About page), the question, and the install and restart once
/// the user accepts. Nothing is downloaded before the user says yes.
/// </summary>
internal sealed partial class UpdatesViewModel : ViewModelBase
{
    private const int _notesLength = 700;
    private static readonly TimeSpan _timerInterval = TimeSpan.FromHours(1);
    private static readonly HttpClient _sharedHttp = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

    private readonly MainWindowViewModel _shell;
    private DispatcherTimer _timer;
    private InstallLocation _location;

    //set while an update downloads: Cancel stops it
    private UpdateCancellation _installCancellation;

    //a newer release was found while the user was busy (a dialog, a wizard, a Sync): asked at the next tick
    private bool _promptPending;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private bool _isInstalling;

    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    private ReleaseInfo _availableRelease;

    /// <summary>
    /// 0 to 100 while downloading; -1 when the size is not known.
    /// </summary>
    [ObservableProperty]
    private int _downloadPercent;

    /// <summary>
    /// The files are being replaced: too late to cancel.
    /// </summary>
    [ObservableProperty]
    private bool _isReplacing;

    public UpdatesViewModel(MainWindowViewModel shell)
    {
        _shell = shell;
        Http = _sharedHttp;
    }

    /// <summary>
    /// Tests give a client that answers without the network.
    /// </summary>
    public HttpClient Http { get; set; }

    /// <summary>
    /// Where the running app was unpacked (tests point it at a temporary folder).
    /// </summary>
    public InstallLocation Location
    {
        get
        {
            if(_location == null)
                _location = InstallLocation.Find(Environment.ProcessPath, InstallLocation.CurrentPlatform);

            return _location;
        }
        set
        {
            _location = value;
        }
    }

    public Version CurrentVersion
    {
        get
        {
            if(UpdateChecker.TryParseVersion(_shell.Version, out var version))
                return version;

            return new Version(0, 0, 0, 0);
        }
    }

    /// <summary>
    /// The once-a-day check (the About page's check box).
    /// </summary>
    public bool CheckAutomatically
    {
        get
        {
            return _shell.Settings.CheckForUpdates;
        }
        set
        {
            if(_shell.Settings.CheckForUpdates == value)
                return;

            _shell.Settings.CheckForUpdates = value;
            AppSettingsStore.Save(_shell.Settings);
            OnPropertyChanged();
        }
    }

    public bool HasUpdate
    {
        get
        {
            return AvailableRelease != null;
        }
    }

    public string UpdateButtonText
    {
        get
        {
            return AvailableRelease == null ? null : $"Update to {AvailableRelease.Version}";
        }
    }

    /// <summary>
    /// Check for updates and Update are off while a check or an install runs.
    /// </summary>
    public bool IsIdle
    {
        get
        {
            return !IsChecking && !IsInstalling;
        }
    }

    /// <summary>
    /// The About page's Cancel: while an update downloads and unpacks, not once the files are being replaced.
    /// </summary>
    public bool CanCancel
    {
        get
        {
            return IsInstalling && !IsReplacing;
        }
    }

    public bool IsDownloadPercentKnown
    {
        get
        {
            return DownloadPercent >= 0;
        }
    }

    partial void OnIsCheckingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsIdle));
    }

    partial void OnIsInstallingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(CanCancel));
    }

    partial void OnIsReplacingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCancel));
    }

    partial void OnAvailableReleaseChanged(ReleaseInfo value)
    {
        OnPropertyChanged(nameof(HasUpdate));
        OnPropertyChanged(nameof(UpdateButtonText));
    }

    partial void OnDownloadPercentChanged(int value)
    {
        OnPropertyChanged(nameof(IsDownloadPercentKnown));
    }

    /// <summary>
    /// Called once the window is up: removes what the last update left, checks if a day has passed, and looks again
    /// every hour while the app stays open.
    /// </summary>
    public void Start()
    {
        if(Location.Problem == null)
            _ = UpdateInstaller.CleanUpAsync(Location.Folder);

        _timer = new DispatcherTimer { Interval = _timerInterval };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        _ = CheckIfDueAsync();
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        _ = CheckIfDueAsync();
    }

    /// <summary>
    /// The automatic check: only when it is on and a day has passed since the last one. A failure says nothing
    /// (no network is normal); the next tick tries again.
    /// </summary>
    public async Task CheckIfDueAsync()
    {
        if(!CheckAutomatically || !IsIdle)
            return;

        if(_promptPending && AvailableRelease != null)
        {
            await AskToUpdateAsync(false);
            return;
        }

        if(UpdateSchedule.IsCheckDue(_shell.Settings.LastUpdateCheck, DateTime.UtcNow))
            await CheckAsync(false);
    }

    [RelayCommand]
    private async Task CheckNowAsync()
    {
        await CheckAsync(true);
    }

    [RelayCommand]
    private async Task UpdateNowAsync()
    {
        await InstallAsync();
    }

    /// <summary>
    /// Asks GitHub for the latest release. A newer one is offered right away (an automatic check waits until the
    /// user is not in the middle of something).
    /// </summary>
    public async Task CheckAsync(bool userAsked)
    {
        if(!IsIdle)
            return;

        IsChecking = true;
        if(userAsked)
            StatusText = "Checking for updates…";

        var result = (UpdateCheckResult)null;
        try
        {
            var packageName = UpdateChecker.GetPackageName(RuntimeInformation.RuntimeIdentifier);
            var checker = new UpdateChecker(Http, UpdateChecker.GetFeedUri(), CurrentVersion, packageName);
            result = await checker.CheckAsync(CancellationToken.None);
        }
        finally
        {
            IsChecking = false;
        }

        if(result.Status == UpdateCheckStatus.Failed)
        {
            if(userAsked)
                StatusText = $"Could not check for updates. {result.Problem}";

            return;
        }

        _shell.Settings.LastUpdateCheck = DateTime.UtcNow;
        AppSettingsStore.Save(_shell.Settings);

        if(result.Status == UpdateCheckStatus.UpToDate)
        {
            AvailableRelease = null;
            _promptPending = false;
            StatusText = $"You have the latest version ({CurrentVersion}).";
            return;
        }

        AvailableRelease = result.Release;
        StatusText = $"Version {result.Release.Version} is available. You have {CurrentVersion}.";
        await AskToUpdateAsync(userAsked);
    }

    /// <summary>
    /// "Version X is available" with what is new, Later or Update now.
    /// </summary>
    private async Task AskToUpdateAsync(bool userAsked)
    {
        if(!userAsked && (_shell.Dialog != null || _shell.Overlay != null || _shell.IsBusy))
        {
            _promptPending = true;
            return;
        }

        _promptPending = false;
        var release = AvailableRelease;
        var message = $"Version {release.Version} of The Sharp Factory is available (you have {CurrentVersion}). Update now downloads it, replaces this copy and restarts the app.";
        var notes = ReleaseNotes.Summarize(release.Notes, _notesLength);
        if(notes != null)
            message += $"{Environment.NewLine}{Environment.NewLine}What's new:{Environment.NewLine}{notes}";

        var dialog = new DialogViewModel
        {
            Title = "An update is available",
            Message = message,
            Buttons = new List<DialogButton>
            {
                new DialogButton { Text = "Later", Result = DialogResults.No },
                new DialogButton { Text = "Update now", Result = DialogResults.Yes, IsPrimary = true },
            },
        };

        var answer = await _shell.ShowDialogAsync(dialog);
        if(string.Equals(answer, DialogResults.Yes, StringComparison.Ordinal))
            await InstallAsync();
    }

    /// <summary>
    /// Asks about unsaved changes, downloads and checks the release, replaces this copy and restarts. Where the app
    /// cannot replace itself, offers the release page instead.
    /// </summary>
    public async Task InstallAsync()
    {
        var release = AvailableRelease;
        if(release == null || !IsIdle)
            return;

        if(_shell.IsBusy || _shell.Overlay != null)
        {
            await _shell.ShowMessageAsync("Please wait", "Finish the model build, Sync or wizard first, then update.", false);
            return;
        }

        var location = Location;
        var problem = location.Problem;
        if(problem == null && !release.CanInstall)
            problem = $"Version {release.Version} has no download the app can install by itself on this system.";

        if(problem != null)
        {
            await OfferReleasePageAsync(release, problem);
            return;
        }

        if(!await _shell.ConfirmDiscardChangesAsync())
            return;

        var modelFile = _shell.Document?.FilePath;
        _shell.Navigate(_shell.About);
        IsReplacing = false;
        IsInstalling = true;
        DownloadPercent = 0;
        _shell.IsBusy = true;
        StatusText = $"Downloading version {release.Version}…";
        using(var cancellation = new UpdateCancellation())
        {
            _installCancellation = cancellation;
            try
            {
                var installer = new UpdateInstaller(Http, release, location, CurrentVersion, new Progress<UpdateProgress>(OnInstallProgress), cancellation);
                await Task.Run(installer.RunAsync);
            }
            catch(OperationCanceledException) when(cancellation.IsCancelled)
            {
                StopInstalling($"The update was cancelled. You still have version {CurrentVersion}.");
                return;
            }
            catch(Exception ex)
            {
                LogManager.WriteErrorLog(ex, "Unable to install the update", release.Version.ToString());
                StopInstalling($"The update failed. You still have version {CurrentVersion}.");
                await _shell.ShowMessageAsync("Update failed", ex.Message, true);
                return;
            }
            finally
            {
                _installCancellation = null;
            }
        }

        StatusText = $"Version {release.Version} is installed. Restarting…";
        if(_shell.Host != null && _shell.Host.StartAndExit(location.CreateStartInfo(modelFile)))
            return;

        StopInstalling($"Version {release.Version} is installed. Close the app and start it again to use it.");
        await _shell.ShowMessageAsync("Restart the app", $"Version {release.Version} is installed, but the app could not start it. Close the app and start it again.", false);
    }

    /// <summary>
    /// Stops the download (the About page's Cancel). Once the files are being replaced it is too late, and Cancel says
    /// so: the swap takes a moment and finishes.
    /// </summary>
    [RelayCommand]
    private void CancelUpdate()
    {
        if(_installCancellation == null || _installCancellation.IsCancelled)
            return;

        StatusText = _installCancellation.TryCancel() ? "Cancelling the update…" : "Too late to cancel: the files are being replaced.";
    }

    private void StopInstalling(string statusText)
    {
        IsInstalling = false;
        IsReplacing = false;
        _shell.IsBusy = false;
        StatusText = statusText;
    }

    private void OnInstallProgress(UpdateProgress progress)
    {
        if(progress.Stage == UpdateStage.Replacing)
        {
            IsReplacing = true;
            if(AvailableRelease != null)
                StatusText = $"Replacing the files with version {AvailableRelease.Version}…";

            return;
        }

        DownloadPercent = progress.Percent;
        if(progress.Percent >= 0 && AvailableRelease != null && _installCancellation != null && !_installCancellation.IsCancelled)
            StatusText = $"Downloading version {AvailableRelease.Version}… {progress.Percent}%";
    }

    private async Task OfferReleasePageAsync(ReleaseInfo release, string problem)
    {
        var dialog = new DialogViewModel
        {
            Title = "Update from GitHub",
            Message = $"{problem}{Environment.NewLine}{Environment.NewLine}Download version {release.Version} from its release page on GitHub and unpack it in place of this copy.",
            Buttons = new List<DialogButton>
            {
                new DialogButton { Text = "Close", Result = DialogResults.No },
                new DialogButton { Text = "Open the release page", Result = DialogResults.Yes, IsPrimary = true },
            },
        };

        var answer = await _shell.ShowDialogAsync(dialog);
        if(string.Equals(answer, DialogResults.Yes, StringComparison.Ordinal) && release.PageUri != null && _shell.Host != null)
            await _shell.Host.OpenUriAsync(release.PageUri);
    }
}
