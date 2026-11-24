#region Usings
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using SF.App.Services;
using SF.App.Services.Updates;
using SF.App.ViewModels;
using SF.Engine.Sync;
using SF.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// The Sync page shows what Sync would change by itself (SF-217): what a stopped Sync leaves, files Sync will not
/// replace, and the pieces under it (the log, the release notes of the update question, the code tab's width).
/// </summary>
public sealed class SyncFlowTests
{
    [AvaloniaFact]
    public async Task ASyncThatStopsKeepsItsLogAndItsList()
    {
        using(var app = new AppHarness())
        {
            var page = await SyncedNewSolution(app);

            //a setting that changes every file, and one of them read-only: Sync stops before writing anything
            app.ViewModel.Document.Container.CommonSettings.IndentSize = 2;
            app.ViewModel.Document.MarkDirty();
            await page.CheckAsync();
            Assert.True(page.CanSync);
            var change = page.Report.FileChanges[0];
            var locked = change.FullPath ?? Path.Combine(app.Folder, change.RelativePath);
            File.SetAttributes(locked, FileAttributes.ReadOnly);
            try
            {
                await page.SyncCommand.ExecuteAsync(null);
                app.Pump();

                //the line says it stopped and where to look; the log is the Sync's, with its reason
                Assert.False(page.LastSync.Succeeded);
                Assert.Contains("stopped", page.LastSync.Title, StringComparison.Ordinal);
                Assert.Contains("Check again", page.LastSync.Text, StringComparison.Ordinal);
                Assert.StartsWith("Sync at ", page.LogTitle, StringComparison.Ordinal);
                Assert.True(page.Progress.HasProblems);
                Assert.Contains("read-only", page.Progress.Problems[0].Text, StringComparison.Ordinal);

                //the list it started from stays, and Sync is on again
                Assert.NotEmpty(page.FileChanges);
                Assert.True(page.CanSync);
            }
            finally
            {
                File.SetAttributes(locked, FileAttributes.Normal);
            }
        }
    }

    [AvaloniaFact]
    public async Task ChoosingDotNet11MovesTheGeneratedProjects()
    {
        using(var app = new AppHarness())
        {
            var page = await SyncedNewSolution(app);

            //the choice is in the strip, with the target closed
            Assert.False(page.IsTargetPanelOpen);
            var box = app.Find<ComboBox>("TargetFrameworkBox");
            Assert.True(box.IsEffectivelyVisible);
            Assert.Equal(".NET 10", page.SelectedTargetFramework.Name);

            //.NET 11: the check lists the five project files, with their diffs, and the file is unsaved
            page.SelectedTargetFramework = page.FrameworkOptions[1];
            await page.CheckAsync();
            Assert.True(app.ViewModel.Document.IsDirty);
            Assert.Equal(5, page.Report.TargetFrameworksChanged.Count);
            Assert.Equal(5, page.FileChanges.Count);
            Assert.Equal("Sync 5 files", page.SyncButtonText);
            Assert.Contains("5 projects to .NET 11", page.SummaryRest, StringComparison.Ordinal);
            foreach(var change in page.Report.FileChanges)
                Assert.EndsWith(".csproj", change.RelativePath, StringComparison.Ordinal);

            //Sync writes them, and then nothing is left to do
            await page.SyncCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(page.LastSync.Succeeded, page.LastSync.Text);
            foreach(var projectFile in Directory.GetFiles(Path.Combine(app.Folder, "DataAccess"), "*.csproj", SearchOption.AllDirectories))
                Assert.Contains("<TargetFramework>net11.0</TargetFramework>", File.ReadAllText(projectFile), StringComparison.Ordinal);

            await page.CheckAsync();
            Assert.True(page.Report.IsUpToDate);
        }
    }

    [AvaloniaFact]
    public async Task TheJsonSettingsGiveShortNamesAndAContext()
    {
        using(var app = new AppHarness())
        {
            var page = await SyncedNewSolution(app);
            var document = app.ViewModel.Document;
            var settings = document.Container.CommonSettings;

            //the two boxes on the Settings page, both off
            app.ViewModel.Navigate(app.ViewModel.SettingsPage);
            app.Pump();
            var enableBox = app.Find<CheckBox>("EnableSystemTextJsonBox");
            var optimizeBox = app.Find<CheckBox>("OptimizeJsonBox");
            Assert.False(enableBox.IsChecked);
            Assert.False(optimizeBox.IsChecked);
            enableBox.IsChecked = true;
            optimizeBox.IsChecked = true;
            app.Pump();
            Assert.Equal(YesOrNo.Yes, settings.EnableSystemTextJson);
            Assert.Equal(YesOrNo.Yes, settings.OptimizeJson);

            //the check gives the names (the file has them to save) and lists the entities and the new context
            app.ViewModel.Navigate(page);
            await page.CheckAsync();
            Assert.True(document.IsDirty);
            Assert.True(page.CanSync);
            var contextFile = Path.Combine(app.Folder, "DataAccess", "SFTest.Entity", "TestDb", "TestDbJsonContext.cs");
            Assert.Contains(page.Report.FileChanges, IsTheContext);

            //Sync writes them; the next check has nothing to do, and gives no names again
            await page.SyncCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(page.LastSync.Succeeded, page.LastSync.Text);
            Assert.True(File.Exists(contextFile));
            var region = File.ReadAllText(Path.Combine(app.Folder, "DataAccess", "SFTest.Entity", "TestDb", "Coverage", "Region.cs"));
            Assert.Contains("[JsonPropertyName(\"A\")]", region, StringComparison.Ordinal);

            var changeCount = document.ChangeCount;
            await page.CheckAsync();
            Assert.True(page.Report.IsUpToDate);
            Assert.Equal(changeCount, document.ChangeCount);
        }
    }

    private static bool IsTheContext(SyncFileChange change)
    {
        return change.RelativePath.EndsWith("TestDbJsonContext.cs", StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public async Task GivingTheJsonNamesStartsOneCheck()
    {
        using(var app = new AppHarness())
        {
            var page = await SyncedNewSolution(app);
            var run = page.Progress.Run;

            //on the Sync page: the setting starts a check, which gives the names; their change starts no second one
            //(two at once would generate through the same static writer)
            app.ViewModel.Document.Container.CommonSettings.OptimizeJson = YesOrNo.Yes;
            app.PumpUntil(new CheckWait(page).IsDone);

            Assert.Equal(run + 1, page.Progress.Run);
            Assert.True(page.CanSync);
            Assert.Equal("A", FindRegionCountryCode(app).JN);
        }
    }

    private static Property FindRegionCountryCode(AppHarness app)
    {
        var schema = app.ViewModel.Document.Container.Models[0].Categories.GetByName(CategoryNames.Entities).Schemas.GetByName("Coverage");
        return schema.Entities.GetByName("Region").Ps.GetByName("CountryCode");
    }

    /// <summary>
    /// Done when a check ran and none is running, its report in.
    /// </summary>
    private sealed class CheckWait
    {
        private readonly SyncPageViewModel _page;

        public CheckWait(SyncPageViewModel page)
        {
            _page = page;
        }

        public bool IsDone()
        {
            return !_page.IsChecking && _page.Report != null && !_page.Report.IsUpToDate;
        }
    }

    [AvaloniaFact]
    public async Task AFileSharpFactoryDidNotWriteStopsSyncBeforeItRuns()
    {
        using(var app = new AppHarness())
        {
            var page = await SyncedNewSolution(app);

            //a hand-written file where a generated one goes: the check says Sync would stop, and Sync is off
            File.WriteAllText(Path.Combine(app.Folder, "DataAccess", "SFTest.Query", "Query", "Query.cs"), "// mine");
            await page.CheckAsync();
            Assert.Equal("Sync would stop", page.SummaryTitle);
            Assert.Equal("error", page.SummaryState);
            Assert.False(page.CanSync);
            Assert.Contains("move or rename them", page.SyncDisabledReason, StringComparison.Ordinal);
        }
    }

    [AvaloniaFact]
    public void ALineFromAnEarlierRunStaysOutOfTheLog()
    {
        using(var app = new AppHarness())
        {
            var progress = new ProgressViewModel();
            var earlier = new UiProgressLog(progress);
            progress.Clear();
            earlier.Info("from the run before");
            var now = new UiProgressLog(progress);
            now.Info("from this run");
            app.Pump();

            Assert.Single(progress.Entries);
            Assert.Equal("from this run", progress.Entries[0].Text);
        }
    }

    [AvaloniaFact]
    public void TheLogShowsItsProblemsAlone()
    {
        var progress = new ProgressViewModel();
        progress.Add("Processing Models....", null);
        progress.Add("Skipped sql_variant column Coverage.AllTypes.SqlVariantCol", null);
        progress.Add("Synchronization failed..... locked", "There was an error.");

        Assert.True(progress.Entries[1].IsWarning);
        Assert.False(progress.Entries[2].IsWarning);
        Assert.True(progress.Entries[2].IsError);
        Assert.Equal(2, progress.Problems.Count);
        Assert.Equal("2 problems", progress.ProblemCountText);
        Assert.Same(progress.Entries, progress.ShownEntries);
        Assert.Contains("WARNING Skipped", progress.CopyText, StringComparison.Ordinal);

        progress.ShowProblemsOnly = true;
        Assert.Same(progress.Problems, progress.ShownEntries);

        //a new run starts with the whole log, and no problems
        progress.Clear();
        Assert.False(progress.ShowProblemsOnly);
        Assert.False(progress.HasProblems);
    }

    [Fact]
    public void ReleaseNotesAreLaidOutLineByLine()
    {
        var markdown = "Regenerate your code: it\nmatters.\n\n### What's new\n\n- **One** thing\n  wrapped over\n- [Two](https://example.com)\n\n### Downloads\n\n| a | b |";
        var lines = ReleaseNotes.Lines(markdown, 1000);

        Assert.Equal(4, lines.Count);
        Assert.True(lines[0].IsParagraph);
        Assert.Equal("Regenerate your code: it matters.", lines[0].Text);
        Assert.True(lines[1].IsHeading);
        Assert.Equal("What's new", lines[1].Text);
        Assert.True(lines[2].IsListItem);
        Assert.Equal("One thing wrapped over", lines[2].Text);
        Assert.Equal("Two", lines[3].Text);

        //cut after the block that passes the length, like the plain text
        var cut = ReleaseNotes.Lines(markdown, 40);
        Assert.Equal("…", cut[cut.Count - 1].Text);
        Assert.True(cut.Count < lines.Count + 1);
    }

    [AvaloniaFact]
    public void TheCodeTabTakesTheWidthAndGivesItBack()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var page = app.ViewModel.Models;
            app.Pump();
            var grid = app.Find<Grid>("ModelsGrid");
            grid.ColumnDefinitions[0].Width = new GridLength(600);
            page.SelectedNode = ParityTests.FindNode(page, "Shipment");
            app.Pump();

            var tabs = app.Find<TabControl>("DetailsTabs");
            tabs.SelectedIndex = 1;
            app.Pump();
            Assert.True(grid.ColumnDefinitions[0].ActualWidth <= (grid.Bounds.Width * 0.38) + 1, $"tree {grid.ColumnDefinitions[0].ActualWidth} of {grid.Bounds.Width}");

            //Details: the tree is as wide as before
            tabs.SelectedIndex = 0;
            app.Pump();
            Assert.Equal(600, grid.ColumnDefinitions[0].Width.Value);
        }
    }

    /// <summary>
    /// The fixture synced into a new solution: nothing left to write.
    /// </summary>
    private static async Task<SyncPageViewModel> SyncedNewSolution(AppHarness app)
    {
        app.ViewModel.OpenFile(app.CopyFixture());
        app.ViewModel.Navigate(app.ViewModel.Sync);
        var page = app.ViewModel.Sync;
        app.PumpUntil(IsNotScanning(page));
        page.NewSolutionFolder = app.Folder;
        page.UseNewSolutionCommand.Execute(null);
        await page.CheckAsync();
        await page.SyncCommand.ExecuteAsync(null);
        app.Pump();
        Assert.True(page.LastSync.Succeeded, page.LastSync.Text);
        Assert.True(page.Report.IsUpToDate);
        return page;
    }

    private static Func<bool> IsNotScanning(SyncPageViewModel page)
    {
        return new ScanWait(page).IsDone;
    }

    private sealed class ScanWait
    {
        private readonly SyncPageViewModel _page;

        public ScanWait(SyncPageViewModel page)
        {
            _page = page;
        }

        public bool IsDone()
        {
            return !_page.IsScanning;
        }
    }
}
