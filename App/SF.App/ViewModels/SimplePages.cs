#region Usings
using CommunityToolkit.Mvvm.Input;
using SF.Engine.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
#endregion

namespace SF.App.ViewModels;

/// <summary>
/// The start page: new, open and recent model files.
/// </summary>
internal sealed partial class WelcomePageViewModel : PageViewModel
{
    public WelcomePageViewModel(MainWindowViewModel shell) : base(shell)
    {
        Refresh();
    }

    public override string Title
    {
        get
        {
            return "Home";
        }
    }

    public ObservableCollection<RecentFileViewModel> RecentFiles { get; } = new ObservableCollection<RecentFileViewModel>();

    public bool HasRecentFiles
    {
        get
        {
            return RecentFiles.Count > 0;
        }
    }

    /// <summary>
    /// The getting-started steps show until there is a recent file.
    /// </summary>
    public bool ShowGettingStarted
    {
        get
        {
            return RecentFiles.Count == 0;
        }
    }

    [RelayCommand]
    private void OpenDocumentation()
    {
        Shell.Navigate(Shell.Documentation);
    }

    public override void OnShown()
    {
        Refresh();
    }

    public override void OnDocumentChanged()
    {
        Refresh();
    }

    private void Refresh()
    {
        RecentFiles.Clear();
        var now = DateTime.Now;
        foreach(var file in Shell.Settings.RecentFiles)
        {
            var exists = File.Exists(file);
            RecentFiles.Add(new RecentFileViewModel
            {
                FilePath = file,
                Name = Path.GetFileNameWithoutExtension(file),
                Folder = Path.GetDirectoryName(file),
                Exists = exists,
                When = exists ? TimeText.Ago(File.GetLastWriteTime(file), now) : "Not found",
            });
        }

        OnPropertyChanged(nameof(HasRecentFiles));
        OnPropertyChanged(nameof(ShowGettingStarted));
    }

    /// <summary>
    /// Takes a file off the recent list (the file itself is not touched).
    /// </summary>
    [RelayCommand]
    private void RemoveRecent(string filePath)
    {
        AppSettingsStore.RemoveRecentFile(Shell.Settings, filePath);
        AppSettingsStore.Save(Shell.Settings);
        Refresh();
    }
}

/// <summary>
/// "5 minutes ago", "yesterday", or the date for anything older than a week.
/// </summary>
internal static class TimeText
{
    public static string Ago(DateTime time, DateTime now)
    {
        var span = now - time;
        if(span.TotalMinutes < 1)
            return "Just now";

        if(span.TotalHours < 1)
            return ModelTreeBuilder.Plural((int)span.TotalMinutes, "minute") + " ago";

        if(span.TotalDays < 1 && time.Date == now.Date)
            return ModelTreeBuilder.Plural((int)span.TotalHours, "hour") + " ago";

        if(time.Date == now.Date.AddDays(-1))
            return "Yesterday";

        if(span.TotalDays < 7)
            return ModelTreeBuilder.Plural((int)Math.Ceiling(span.TotalDays), "day") + " ago";

        return time.ToString("yyyy-MM-dd");
    }
}

/// <summary>
/// One recently opened model file.
/// </summary>
internal sealed class RecentFileViewModel
{
    public string FilePath { get; set; }
    public string Name { get; set; }
    public string Folder { get; set; }
    public bool Exists { get; set; }

    /// <summary>
    /// When the file last changed, or "Not found".
    /// </summary>
    public string When { get; set; }
}

/// <summary>
/// Links to the documentation on thesharpfactory.com.
/// </summary>
internal sealed class DocumentationPageViewModel : PageViewModel
{
    private const string _site = "https://www.thesharpfactory.com/Documentation/";

    public DocumentationPageViewModel(MainWindowViewModel shell) : base(shell)
    {
        Sections = new List<DocSection>
        {
            new DocSection
            {
                Header = "Getting started",
                Links = new List<DocLink>
                {
                    Link("Introduction", "GettingStarted/Introduction"),
                    Link("Installation", "GettingStarted/Installation"),
                    Link("Project Setup", "GettingStarted/ProjectSetup"),
                    Link("Model Generation", "GettingStarted/ModelGeneration"),
                    Link("Updating The Model", "GettingStarted/UpdatingTheModel"),
                    Link("Dependency Injection", "GettingStarted/DependencyInjection"),
                    Link("Query Logging", "GettingStarted/QueryLogging"),
                },
            },
            new DocSection
            {
                Header = "Query composition",
                Links = new List<DocLink>
                {
                    Link("Syntax", "QueryComposition/Syntax"),
                    Link("Filtering", "QueryComposition/Filtering"),
                    Link("Conditions", "QueryComposition/Conditions"),
                    Link("Sorting", "QueryComposition/Sorting"),
                    Link("Pagination", "QueryComposition/Pagination"),
                    Link("Navigation Properties", "QueryComposition/NavigationProperties"),
                    Link("Complex Expressions", "QueryComposition/ComplexExpressions"),
                },
            },
            new DocSection
            {
                Header = "Data access",
                Links = new List<DocLink>
                {
                    Link("Repositories", "DataAccess/Repositories"),
                    Link("Create", "DataAccess/Create"),
                    Link("Retrieve", "DataAccess/Retrieve"),
                    Link("Update", "DataAccess/Update"),
                    Link("Delete", "DataAccess/Delete"),
                    Link("Stored Procedures", "DataAccess/StoredProcedures"),
                    Link("Functions", "DataAccess/Functions"),
                    Link("Sequences", "DataAccess/Sequences"),
                },
            },
        };
    }

    public override string Title
    {
        get
        {
            return "Documentation";
        }
    }

    public List<DocSection> Sections { get; }

    private static DocLink Link(string text, string path)
    {
        return new DocLink { Text = text, Uri = new Uri(_site + path) };
    }
}

internal sealed class DocSection
{
    public string Header { get; set; }
    public List<DocLink> Links { get; set; }
}

internal sealed class DocLink
{
    public string Text { get; set; }
    public Uri Uri { get; set; }
}

/// <summary>
/// Product name, edition, version and notice.
/// </summary>
internal sealed class AboutPageViewModel : PageViewModel
{
    public AboutPageViewModel(MainWindowViewModel shell) : base(shell)
    {
    }

    public override string Title
    {
        get
        {
            return "About";
        }
    }

    public string ProductName { get; } = "The Sharp Factory App";

    public string ProductEdition { get; } = "Free and open source";

    public string ProductVersion
    {
        get
        {
            return "Version: " + Shell.Version;
        }
    }

    public Uri Website { get; } = new Uri("https://www.thesharpfactory.com");

    //opened in the browser only when clicked: the app connects to nothing but the database
    public Uri Releases { get; } = new Uri("https://github.com/TheSharpFactory/SharpFactory/releases");

    public Uri Issues { get; } = new Uri("https://github.com/TheSharpFactory/SharpFactory/issues");

    public string LogsFolder { get; } = AppPaths.LogsFolder;

    public string SettingsFile { get; } = AppPaths.SettingsFile;

    /// <summary>
    /// Opens the logs folder in the file manager.
    /// </summary>
    public Uri LogsFolderUri
    {
        get
        {
            return new Uri(LogsFolder);
        }
    }

    /// <summary>
    /// Opens the folder that holds the settings file.
    /// </summary>
    public Uri SettingsFolderUri
    {
        get
        {
            return new Uri(Path.GetDirectoryName(SettingsFile));
        }
    }
}
