#region Usings
using Avalonia.Headless.XUnit;
using SF.App.ViewModels;
using SF.Engine.Settings;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// The Sync page's solution list follows the settings: the slow searches run on another thread, the list is built on
/// the UI thread from the settings as they are, and a Sync updates it at once (no refresh needed).
/// </summary>
public sealed class SyncTargetListTests
{
    [AvaloniaFact]
    public async Task ASyncShowsInTheSolutionListAtOnce()
    {
        using(var app = new AppHarness())
        {
            var file = app.CopyFixture();
            app.ViewModel.OpenFile(file);
            app.ViewModel.Navigate(app.ViewModel.Sync);
            var page = app.ViewModel.Sync;

            //the scan for this file is done before the Sync
            await page.RefreshTargetsCommand.ExecuteAsync(null);
            var solution = Path.Combine(app.Folder, "Fresh.slnx");
            Assert.Null(Find(page, solution));

            page.NewSolutionName = "Fresh";
            page.UseNewSolutionCommand.Execute(null);
            Assert.Equal("New solution", page.TargetBadge);

            var before = DateTime.Now.AddSeconds(-1);
            await page.SyncCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(page.Report.Success, page.Report.ErrorMessage);

            //no refresh: the solution this Sync created is listed, chosen, with today's Sync date
            var listed = Find(page, solution);
            Assert.NotNull(listed);
            Assert.NotNull(listed.LastSynced);
            Assert.True(listed.LastSynced.Value >= before);
            Assert.Same(listed, page.SelectedCandidate);
            Assert.StartsWith("Synced", page.TargetBadge, StringComparison.Ordinal);

            //rebuilding the list is no new target: the report of the Sync stays
            Assert.NotNull(page.Report);
            Assert.Equal("Synced", page.SummaryTitle);
        }
    }

    [AvaloniaFact]
    public async Task ASyncDateRecordedAfterTheScanIsShownWhenTheListIsBuilt()
    {
        using(var app = new AppHarness())
        {
            var file = app.CopyFixture();
            var solution = Path.Combine(app.Folder, "Earlier.slnx");
            File.WriteAllText(solution, "<Solution />");
            app.ViewModel.OpenFile(file);
            var page = app.ViewModel.Sync;
            await page.RefreshTargetsCommand.ExecuteAsync(null);
            Assert.Null(Find(page, solution)?.LastSynced);

            //a Sync recorded meanwhile (from the command line, or a Sync that finished during a scan): the next list has it
            AppSettingsStore.RecordSyncTarget(app.ViewModel.Settings, file, solution);
            await page.RefreshTargetsCommand.ExecuteAsync(null);
            Assert.NotNull(Find(page, solution)?.LastSynced);
        }
    }

    private static SolutionCandidateViewModel Find(SyncPageViewModel page, string solution)
    {
        foreach(var candidate in page.Candidates)
        {
            if(string.Equals(candidate.SolutionPath, solution, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return null;
    }
}
