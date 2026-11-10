#region Usings
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using SF.App.ViewModels;
using SF.Engine.Settings;
using SF.Models;
using SF.Utils;
using System.IO;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// The app findings of the 1.1.2.5 code review, one test each.
/// </summary>
public sealed class AppReviewTests
{
    /// <summary>
    /// Tables: dbo (Order, OrderLine, Person, Product), Sales (Invoice).
    /// </summary>
    private static DBObjectCategoryCollection Categories(params string[] selected)
    {
        var category = new DbObjectCategory { Name = "Tables", CategoryType = DbObjectType.Table };
        var dbo = new DbObjectSchema { Name = "dbo", CategoryType = DbObjectType.Table };
        foreach(var name in new[] { "Order", "OrderLine", "Person", "Product" })
            dbo.Objects.Add(new DbObject { Name = name, SchemaName = "dbo", Type = DbObjectType.Table, IsSelected = System.Array.IndexOf(selected, name) >= 0 });

        var sales = new DbObjectSchema { Name = "Sales", CategoryType = DbObjectType.Table };
        sales.Objects.Add(new DbObject { Name = "Invoice", SchemaName = "Sales", Type = DbObjectType.Table, IsSelected = System.Array.IndexOf(selected, "Invoice") >= 0 });
        category.Schemas.Add(dbo);
        category.Schemas.Add(sales);

        return new DBObjectCategoryCollection { category };
    }

    private static ChooserNode Node(ObjectChooserViewModel chooser, string title)
    {
        foreach(var category in chooser.Roots)
        {
            if(category.Title == title)
                return category;

            foreach(var schema in category.AllChildren)
            {
                if(schema.Title == title)
                    return schema;

                foreach(var item in schema.AllChildren)
                {
                    if(item.Title == title)
                        return item;
                }
            }
        }

        return null;
    }

    private static CheckBox CheckBoxOf(AppHarness app, string title)
    {
        foreach(var box in app.FindAll<CheckBox>())
        {
            if(box.DataContext is ChooserNode node && node.Title == title)
                return box;
        }

        return null;
    }

    private static void Click(AppHarness app, Control control)
    {
        var point = control.TranslatePoint(new Point(8, control.Bounds.Height / 2), app.Window).Value;
        app.Window.MouseDown(point, MouseButton.Left);
        app.Window.MouseUp(point, MouseButton.Left);
        app.Pump();
    }

    private static ObjectChooserViewModel ShowChooser(AppHarness app, DBObjectCategoryCollection categories)
    {
        var wizard = new AddModelWizardViewModel(app.ViewModel, null)
        {
            Chooser = new ObjectChooserViewModel(categories, true),
            Step = WizardStep.Objects,
        };
        app.ViewModel.Overlay = wizard;
        app.Pump();
        Node(wizard.Chooser, "dbo").IsExpanded = true;
        app.Pump();
        return wizard.Chooser;
    }

    [AvaloniaFact]
    public void ClickingAPartlyCheckedSchemaChecksAllOfIt()
    {
        using(var app = new AppHarness())
        {
            var chooser = ShowChooser(app, Categories("Order"));
            var dbo = Node(chooser, "dbo");
            Assert.Null(dbo.IsChecked);

            Click(app, CheckBoxOf(app, "dbo"));
            Assert.True(dbo.IsChecked);
            Assert.True(CheckBoxOf(app, "dbo").IsChecked);
            Assert.True(Node(chooser, "Person").IsChecked);
            Assert.Equal(4, chooser.SelectedCount);

            //all checked: the next click unchecks them
            Click(app, CheckBoxOf(app, "dbo"));
            Assert.False(dbo.IsChecked);
            Assert.False(CheckBoxOf(app, "dbo").IsChecked);
            Assert.Equal(0, chooser.SelectedCount);
        }
    }

    [AvaloniaFact]
    public void TickingDuringASearchTicksOnlyWhatItShows()
    {
        using(var app = new AppHarness())
        {
            var chooser = ShowChooser(app, Categories("Invoice"));
            chooser.SearchText = "Order";
            app.Pump();

            Click(app, CheckBoxOf(app, "dbo"));
            Assert.True(Node(chooser, "Order").IsChecked);
            Assert.True(Node(chooser, "OrderLine").IsChecked);
            Assert.False(Node(chooser, "Person").IsChecked);
            Assert.Null(Node(chooser, "dbo").IsChecked);//Person and Product are hidden, not ticked

            //None and Select all act on the search too: Invoice, hidden by it, stays ticked
            chooser.SelectNoneCommand.Execute(null);
            Assert.False(Node(chooser, "Order").IsChecked);
            Assert.True(Node(chooser, "Invoice").IsChecked);
            chooser.SelectAllCommand.Execute(null);
            Assert.Equal(3, chooser.SelectedCount);

            chooser.SearchText = null;
            chooser.SelectAllCommand.Execute(null);
            Assert.Equal(5, chooser.SelectedCount);
        }
    }

    [AvaloniaFact]
    public void AnyModelMarkedSyncedTurnsSyncOffForTheFile()
    {
        using(var app = new AppHarness())
        {
            var file = app.CopyFixture();
            app.ViewModel.OpenFile(file);
            var second = ModelUtils.Load(file).Models[0];
            second.Name = "Second";
            second.Synced = YesOrNo.Yes;
            app.ViewModel.Document.Container.Models.Add(second);

            //the picked model is not the synced one: Sync still generates both, so it stays off
            app.ViewModel.SelectedModel = app.ViewModel.Document.Container.Models[0];
            app.ViewModel.Navigate(app.ViewModel.Sync);
            app.ViewModel.Sync.TargetPath = Path.Combine(app.Folder, "App.slnx");
            Assert.True(app.ViewModel.Sync.IsBlockedBySyncedFlag);
            Assert.False(app.ViewModel.Sync.CanRun);
            Assert.Contains("Second", app.ViewModel.Sync.SyncedBlockText);
        }
    }

    [AvaloniaFact]
    public void TheCodePreviewFollowsALayerChange()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var page = app.ViewModel.Models;
            page.SelectedNode = ParityTests.FindNode(page, "Warehouse");
            Assert.DoesNotContain("Renamed.Entities", page.SelectedCodeFile.Content);

            app.ViewModel.SelectedModel.Layers.Entity.Namespace = "Renamed.Entities";
            app.ViewModel.Navigate(app.ViewModel.Layers);
            app.ViewModel.Navigate(page);
            Assert.Contains("Renamed.Entities", page.SelectedCodeFile.Content);
        }
    }

    [AvaloniaFact]
    public async Task EscapeCancelsADialogAndASecondDialogCancelsTheFirst()
    {
        using(var app = new AppHarness())
        {
            var first = app.ViewModel.ShowMessageAsync("First", "one", false);
            var second = app.ViewModel.ShowMessageAsync("Second", "two", true);
            Assert.Null(await first);
            Assert.Equal("Second", app.ViewModel.Dialog.Title);
            Assert.True(app.ViewModel.Dialog.HasSign);

            app.Pump();
            app.Window.KeyPress(Avalonia.Input.Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            app.Pump();
            Assert.Null(await second);
            Assert.Null(app.ViewModel.Dialog);
        }
    }

    [AvaloniaFact]
    public void AFailedBuildCanGoBackToTheObjects()
    {
        using(var app = new AppHarness())
        {
            var wizard = new AddModelWizardViewModel(app.ViewModel, null)
            {
                Chooser = new ObjectChooserViewModel(Categories("Order")),
                Step = WizardStep.Progress,
                IsFinished = true,
                Succeeded = false,
            };
            Assert.True(wizard.CanGoBack);
            Assert.True(wizard.ShowBack);

            wizard.BackCommand.Execute(null);
            Assert.Equal(WizardStep.Objects, wizard.Step);
            Assert.False(wizard.IsFinished);
            Assert.True(wizard.CanGoNext);
            Assert.Equal(1, wizard.Chooser.SelectedCount);//the choices are kept
        }
    }

    [AvaloniaFact]
    public void AFileThatIsThereButCannotBeReadStaysOnTheRecentList()
    {
        using(var app = new AppHarness())
        {
            var file = Path.Combine(app.Folder, "Broken.edmj");
            File.WriteAllText(file, "not a model");
            app.ViewModel.Settings.RecentFiles.Add(file);

            Assert.False(app.ViewModel.OpenFile(file));
            Assert.Contains(file, app.ViewModel.Settings.RecentFiles);

            File.Delete(file);
            app.ViewModel.Dialog.Cancel();
            Assert.False(app.ViewModel.OpenFile(file));
            Assert.DoesNotContain(file, app.ViewModel.Settings.RecentFiles);
        }
    }

    [AvaloniaFact]
    public async Task FilteringTheChangesKeepsTheSelectedFile()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var page = app.ViewModel.Sync;
            app.ViewModel.Navigate(page);
            page.NewSolutionFolder = app.Folder;
            page.NewSolutionName = "Filtered";
            page.UseNewSolutionCommand.Execute(null);
            await page.PreviewCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(page.Report.Success, page.Report.ErrorMessage);

            var picked = page.FileChanges[2];
            page.SelectedFileChange = picked;
            var diff = page.Diff;
            page.ChangeSearch = picked.FileName.Substring(0, 4);
            app.Pump();
            Assert.Same(picked, page.SelectedFileChange);
            Assert.Same(diff, page.Diff);
        }
    }

    [AvaloniaFact]
    public async Task LookingAgainKeepsTheChosenSolutionHighlighted()
    {
        using(var app = new AppHarness())
        {
            var file = app.CopyFixture();
            app.ViewModel.OpenFile(file);
            var solution = Path.Combine(app.Folder, "Kept.slnx");
            File.WriteAllText(solution, "<Solution />");
            AppSettingsStore.RecordSyncTarget(app.ViewModel.Settings, file, solution);

            var page = app.ViewModel.Sync;
            await page.RefreshTargetsCommand.ExecuteAsync(null);
            foreach(var candidate in page.Candidates)
            {
                if(string.Equals(candidate.SolutionPath, solution, System.StringComparison.OrdinalIgnoreCase))
                    page.SelectedCandidate = candidate;
            }

            Assert.True(page.HasTargetBadge);
            await page.RefreshTargetsCommand.ExecuteAsync(null);
            Assert.Equal(solution, page.SelectedCandidate?.SolutionPath, true);
            Assert.True(page.HasTargetBadge);
        }
    }
}
