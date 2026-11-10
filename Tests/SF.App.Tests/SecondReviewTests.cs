#region Usings
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using SF.App.ViewModels;
using System.IO;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// The app findings of the second 1.1.2.6 review, one test each.
/// </summary>
public sealed class SecondReviewTests
{
    private static string CopyAs(AppHarness app, string name)
    {
        var target = Path.Combine(app.Folder, name);
        File.Copy(app.CopyFixture(), target, true);
        return target;
    }

    private static async Task PreviewNewSolution(AppHarness app, string name)
    {
        var page = app.ViewModel.Sync;
        app.ViewModel.Navigate(page);
        page.NewSolutionFolder = app.Folder;
        page.NewSolutionName = name;
        page.UseNewSolutionCommand.Execute(null);
        await page.PreviewCommand.ExecuteAsync(null);
        app.Pump();
        Assert.True(page.Report.Success, page.Report.ErrorMessage);
    }

    [AvaloniaFact]
    public void OpeningASecondFileShowsItsFirstModel()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.Pump();
            app.ViewModel.OpenFile(CopyAs(app, "Second.edmj"));
            app.Pump();

            var first = app.ViewModel.Document.Container.Models[0];
            Assert.Same(first, app.ViewModel.SelectedModel);
            Assert.Same(first, app.Find<ComboBox>("ModelPicker").SelectedItem);
            Assert.NotEmpty(app.ViewModel.Models.Roots);
        }
    }

    [AvaloniaFact]
    public void PickingAColumnOnTheCodeTabShowsItsDetails()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var page = app.ViewModel.Models;
            var table = ParityTests.FindNode(page, "Warehouse");
            page.SelectedNode = table;
            app.Pump();

            var tabs = app.Find<TabControl>("DetailsTabs");
            tabs.SelectedIndex = 1;
            app.Pump();

            //a column has no code: the details come back
            page.SelectedNode = table.AllChildren[0];
            app.Pump();
            Assert.False(page.HasCode);
            Assert.Equal(0, tabs.SelectedIndex);
        }
    }

    [AvaloniaFact]
    public void AWizardLocksTheTopBarAndTheFilesTheSystemHandsOver()
    {
        using(var app = new AppHarness())
        {
            var file = app.CopyFixture();
            app.ViewModel.OpenFile(file);
            app.ViewModel.StartAddModel();
            app.Pump();

            Assert.False(app.Find<Button>("OpenButton").IsEffectivelyEnabled);
            Assert.False(app.Find<ComboBox>("ModelPicker").IsEffectivelyEnabled);
            Assert.False(app.ViewModel.OpenWithConfirmationAsync(CopyAs(app, "Other.edmj")).Result);
            Assert.Equal(file, app.ViewModel.Document.FilePath, true);

            app.ViewModel.CloseOverlay();
            app.Pump();
            Assert.True(app.Find<Button>("OpenButton").IsEffectivelyEnabled);
        }
    }

    [AvaloniaFact]
    public async Task AnotherFileStartsTheSyncLogOver()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            await PreviewNewSolution(app, "LogFirst");
            Assert.NotEmpty(app.ViewModel.Sync.Progress.Entries);

            app.ViewModel.OpenFile(CopyAs(app, "Second.edmj"));
            Assert.Empty(app.ViewModel.Sync.Progress.Entries);
        }
    }

    [AvaloniaFact]
    public async Task ALayerChangeAfterAPreviewClearsIt()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            await PreviewNewSolution(app, "Stale");

            //the same page, nothing changed: the preview stays
            app.ViewModel.Navigate(app.ViewModel.Layers);
            app.ViewModel.Navigate(app.ViewModel.Sync);
            Assert.NotNull(app.ViewModel.Sync.Report);

            app.ViewModel.Navigate(app.ViewModel.Layers);
            app.ViewModel.SelectedModel.Layers.Entity.Namespace = "Renamed.Entities";
            app.ViewModel.Navigate(app.ViewModel.Sync);
            Assert.Null(app.ViewModel.Sync.Report);
        }
    }

    [AvaloniaFact]
    public async Task FilteringTheChangesNeverBlanksTheDiff()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            await PreviewNewSolution(app, "NoBlank");
            var page = app.ViewModel.Sync;
            page.SelectedFileChange = page.FileChanges[2];
            app.Pump();

            var watcher = new DiffWatcher(page);
            page.ChangeSearch = page.SelectedFileChange.FileName.Substring(0, 4);
            app.Pump();
            Assert.False(watcher.Blanked);
        }
    }

    /// <summary>
    /// Notes whether the Sync page's diff was ever emptied.
    /// </summary>
    private sealed class DiffWatcher
    {
        private readonly SyncPageViewModel _page;

        public DiffWatcher(SyncPageViewModel page)
        {
            _page = page;
            _page.PropertyChanged += OnChanged;
        }

        public bool Blanked { get; private set; }

        private void OnChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(string.Equals(e.PropertyName, nameof(SyncPageViewModel.Diff), System.StringComparison.Ordinal) && _page.Diff == null)
                Blanked = true;
        }
    }
}
