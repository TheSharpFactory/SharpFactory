#region Usings
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using SF.App.ViewModels;
using SF.App.Views;
using SF.Engine.Settings;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// The polish the reviewers asked for after the first round (SF-109).
/// </summary>
public sealed class PolishTests
{
    [AvaloniaFact]
    public void HomeShowsGettingStartedUntilThereIsARecentFile()
    {
        using(var app = new AppHarness())
        {
            var home = app.ViewModel.Welcome;
            Assert.True(home.ShowGettingStarted);
            Assert.True(app.Find<Border>("GettingStarted").IsVisible);
            app.Screenshot("40-home-first-run");

            home.OpenDocumentationCommand.Execute(null);
            Assert.Same(app.ViewModel.Documentation, app.ViewModel.CurrentPage);

            app.ViewModel.OpenFile(app.CopyFixture());
            app.ViewModel.Navigate(home);
            app.Pump();
            Assert.False(home.ShowGettingStarted);
            Assert.False(app.Find<Border>("GettingStarted").IsVisible);
        }
    }

    [AvaloniaFact]
    public void TreeSearchCountsAndHighlightsTheMatches()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var page = app.ViewModel.Models;
            Assert.Null(page.MatchText);

            page.SearchText = "warehouse";
            var warehouse = ParityTests.FindNode(page, "Warehouse");
            Assert.Equal(0, warehouse.MatchStart);
            Assert.Equal("Warehouse", warehouse.TitleMatch);
            Assert.Equal("", warehouse.TitleBefore);

            //Shipment shows for its columns: it is not a match itself, its FromWarehouse column is
            var shipment = ParityTests.FindNode(page, "Shipment");
            Assert.Equal(-1, shipment.MatchStart);
            Assert.Equal("Shipment", shipment.TitleBefore);
            var column = shipment.Children[0];
            Assert.True(column.MatchStart >= 0);
            Assert.Equal("Warehouse", column.TitleMatch);

            var count = 0;
            foreach(var root in page.Roots)
                count += root.CountMatches();

            Assert.True(count > 2);
            Assert.Equal($"{count} matches", page.MatchText);

            //the groups say how many of their objects the search left (SF-115)
            Assert.Equal("Tables", page.Roots[0].Title);
            Assert.Equal("2 of 14", page.Roots[0].ShownDetail);
            shipment.IsExpanded = true;
            app.Screenshot("41-models-search");

            //a selected match stays readable
            page.SelectedNode = warehouse;
            app.Screenshot("45-models-search-selected");

            page.SearchText = "zzz-no-match";
            Assert.Equal("No matches", page.MatchText);
            page.ClearSearchCommand.Execute(null);
            Assert.Null(page.MatchText);
            Assert.Equal("14", page.Roots[0].ShownDetail);
            Assert.Equal(-1, warehouse.MatchStart);
            Assert.Equal("Warehouse", warehouse.TitleBefore);
        }
    }

    [AvaloniaFact]
    public void CodeTabsShowWhenThereAreMore()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var page = app.ViewModel.Models;
            app.Pump();
            app.Find<Grid>("ModelsGrid").ColumnDefinitions[0].Width = new GridLength(620);
            page.SelectedNode = ParityTests.FindNode(page, "Shipment");
            app.Pump();
            app.FindAll<TabControl>()[0].SelectedIndex = 1;
            app.Pump();

            Assert.False(app.Find<Border>("TabsMoreLeft").IsVisible);
            Assert.True(app.Find<Border>("TabsMoreRight").IsVisible);
            app.Screenshot("42-models-code-tabs");

            //choosing the last file brings its tab into view (SF-115)
            page.SelectedCodeFile = page.CodeFiles[page.CodeFiles.Count - 1];
            app.Pump();
            Assert.True(app.Find<Border>("TabsMoreLeft").IsVisible);
            Assert.False(app.Find<Border>("TabsMoreRight").IsVisible);
            app.Screenshot("46-models-code-last-tab");
        }
    }

    [AvaloniaFact]
    public void PaneWidthsAreRememberedAndRestored()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.Pump();
            var settings = app.ViewModel.Settings;
            var grid = app.Find<Grid>("ModelsGrid");

            grid.ColumnDefinitions[0].Width = new GridLength(420);
            app.Pump();
            PaneWidths.Remember(grid, PaneWidths.ModelsTree, settings);
            Assert.Equal(420, settings.PaneWidths[PaneWidths.ModelsTree]);
            Assert.Equal(420, AppSettingsStore.Load().PaneWidths[PaneWidths.ModelsTree]);

            grid.ColumnDefinitions[0].Width = new GridLength(340);
            PaneWidths.Restore(grid, PaneWidths.ModelsTree, settings);
            Assert.Equal(420, grid.ColumnDefinitions[0].Width.Value);

            //a width below the column's minimum is not restored
            settings.PaneWidths[PaneWidths.ModelsTree] = 100;
            PaneWidths.Restore(grid, PaneWidths.ModelsTree, settings);
            Assert.Equal(420, grid.ColumnDefinitions[0].Width.Value);
        }
    }

    [AvaloniaFact]
    public async Task TheLogHasTimesAndCanBeCopied()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.ViewModel.Navigate(app.ViewModel.Sync);
            var page = app.ViewModel.Sync;
            page.UseNewSolutionCommand.Execute(null);
            await page.PreviewCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(page.Report.Success, page.Report.ErrorMessage);

            var first = page.Progress.Entries[0];
            Assert.Matches(new Regex(@"^\d\d:\d\d:\d\d$"), first.Time);
            var text = page.Progress.CopyText;
            Assert.StartsWith(first.Time + "  " + first.Text, text);
            Assert.Equal(page.Progress.Entries.Count, text.TrimEnd().Split('\n').Length);

            app.FindAll<TabControl>()[0].SelectedIndex = 1;
            app.Pump();
            Assert.True(app.Find<Button>("CopySyncLog").IsVisible);
            app.Screenshot("43-sync-log");
        }
    }

    [AvaloniaFact]
    public void TheSolutionListSaysItIsLooking()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.ViewModel.Navigate(app.ViewModel.Sync);
            var page = app.ViewModel.Sync;

            //opening the page starts the real search for solutions; on a fast machine it ends during the next
            //Pump and turns IsScanning off, so wait for it before setting IsScanning here
            app.PumpUntil(() => !page.IsScanning);
            page.IsScanning = true;
            app.Pump();

            Assert.True(app.Find<StackPanel>("ScanningPanel").IsVisible);
            Assert.Equal("Looking for solutions", app.Find<TextBlock>("CandidatesTitle").Text);
            app.Screenshot("44-sync-scanning");
            page.IsScanning = false;
            app.Pump();
            Assert.False(app.Find<StackPanel>("ScanningPanel").IsVisible);
            Assert.Equal("Solutions found", app.Find<TextBlock>("CandidatesTitle").Text);
        }
    }

    [AvaloniaFact]
    public async Task TheConnectedLineGoesWhenTheSignInChanges()
    {
        using(var app = new AppHarness())
        {
            await app.ViewModel.NewCommand.ExecuteAsync(null);
            var wizard = Assert.IsType<AddModelWizardViewModel>(app.ViewModel.Overlay);
            wizard.ConnectedText = "Connected to SQL01 as sa · 3 databases";
            wizard.User = "someone";
            Assert.Null(wizard.ConnectedText);

            wizard.ConnectedText = "Connected";
            wizard.Server = "SQL02";
            Assert.Null(wizard.ConnectedText);

            wizard.ConnectedText = "Connected";
            wizard.IntegratedSecurity = !wizard.IntegratedSecurity;
            Assert.Null(wizard.ConnectedText);
        }
    }
}
