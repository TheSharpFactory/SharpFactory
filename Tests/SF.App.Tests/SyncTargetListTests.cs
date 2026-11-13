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

    [AvaloniaFact]
    public async Task TheSolutionLastSyncedIsSuggestedThenTheOneNearTheModel()
    {
        using(var app = new AppHarness())
        {
            //the model in a repository with its solution; another solution elsewhere
            var file = Path.Combine(app.Folder, "Repo", "Models", "Coverage.edmj");
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.Move(app.CopyFixture(), file);
            var near = Path.Combine(app.Folder, "Repo", "Repo.slnx");
            File.WriteAllText(near, "<Solution />");
            var elsewhere = Path.Combine(app.Folder, "Other", "Other.slnx");
            Directory.CreateDirectory(Path.GetDirectoryName(elsewhere));
            File.WriteAllText(elsewhere, "<Solution />");

            //never synced: the solution near the model
            app.ViewModel.OpenFile(file);
            var page = app.ViewModel.Sync;
            await page.RefreshTargetsCommand.ExecuteAsync(null);
            Assert.Equal(near, page.TargetPath, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("Near the model file", page.TargetBadge);

            //synced into the other one before: that one, wherever it is
            AppSettingsStore.RecordSyncTarget(app.ViewModel.Settings, file, elsewhere);
            page.TargetPath = null;
            await page.RefreshTargetsCommand.ExecuteAsync(null);
            Assert.Equal(elsewhere, page.TargetPath, StringComparer.OrdinalIgnoreCase);
            Assert.StartsWith("Synced", page.TargetBadge, StringComparison.Ordinal);
        }
    }

    [AvaloniaFact]
    public async Task ASyncIntoAnotherSolutionThanLastTimeAsksFirst()
    {
        using(var app = new AppHarness())
        {
            var file = app.CopyFixture();
            var own = Path.Combine(app.Folder, "Own.slnx");
            var other = Path.Combine(app.Folder, "Other", "Other.slnx");
            app.ViewModel.OpenFile(file);
            var page = app.ViewModel.Sync;
            app.ViewModel.Navigate(page);
            await page.RefreshTargetsCommand.ExecuteAsync(null);

            //the model's own solution (its folder has the model): no question
            page.TargetPath = own;
            await page.SyncCommand.ExecuteAsync(null);
            Assert.True(page.Report.Success, page.Report.ErrorMessage);
            Assert.Null(app.ViewModel.Dialog);

            //another solution: asked first, naming both; Cancel writes nothing
            page.TargetPath = other;
            var cancelled = page.SyncCommand.ExecuteAsync(null);
            app.PumpUntil(() => app.ViewModel.Dialog != null);
            Assert.Equal("Sync into another solution?", app.ViewModel.Dialog.Title);
            Assert.Equal("Coverage.edmj was last synced into Own.slnx. Sync it into Other.slnx instead?", app.ViewModel.Dialog.Message);
            Assert.Contains(own, app.ViewModel.Dialog.Details, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(other, app.ViewModel.Dialog.Details, StringComparison.OrdinalIgnoreCase);
            app.Screenshot("sync-other-solution-question");
            app.ViewModel.Dialog.Cancel();
            app.PumpUntil(() => cancelled.IsCompleted);
            Assert.False(File.Exists(other));
            Assert.False(Directory.Exists(Path.Combine(app.Folder, "Other", "DataAccess")));

            //and Sync goes ahead
            var confirmed = page.SyncCommand.ExecuteAsync(null);
            app.PumpUntil(() => app.ViewModel.Dialog != null);
            app.ViewModel.Dialog.ChooseCommand.Execute(app.ViewModel.Dialog.Buttons[1]);
            app.PumpUntil(() => confirmed.IsCompleted);
            Assert.True(page.Report.Success, page.Report.ErrorMessage);
            Assert.True(File.Exists(other));

            //a preview never asks
            page.TargetPath = own;
            await page.PreviewCommand.ExecuteAsync(null);
            Assert.Null(app.ViewModel.Dialog);
        }
    }

    [AvaloniaFact]
    public async Task ASolutionWhoseFolderDoesNotHaveTheModelAsksFirst()
    {
        using(var app = new AppHarness())
        {
            //never synced; the solution is in a sibling folder, not around the model
            var file = Path.Combine(app.Folder, "Repo", "Coverage.edmj");
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.Move(app.CopyFixture(), file);
            var sibling = Path.Combine(app.Folder, "Sibling", "Sibling.slnx");
            app.ViewModel.OpenFile(file);
            var page = app.ViewModel.Sync;
            await page.RefreshTargetsCommand.ExecuteAsync(null);
            Assert.Null(page.SelectedCandidate);

            page.TargetPath = sibling;
            var run = page.SyncCommand.ExecuteAsync(null);
            app.PumpUntil(() => app.ViewModel.Dialog != null);
            Assert.Equal("Sync into another solution?", app.ViewModel.Dialog.Title);
            Assert.Equal("Sibling.slnx is not in the folder of Coverage.edmj. Sync into it anyway?", app.ViewModel.Dialog.Message);
            Assert.Contains(file, app.ViewModel.Dialog.Details, StringComparison.OrdinalIgnoreCase);
            app.ViewModel.Dialog.Cancel();
            app.PumpUntil(() => run.IsCompleted);
            Assert.False(File.Exists(sibling));
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
