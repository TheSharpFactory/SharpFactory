#region Usings
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using SF.App.ViewModels;
using SF.Engine;
using SF.Engine.Sync;
using SF.Models;
using SF.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// The parity checklist (PLAN.md): every field and action of the WPF app, in the new UI.
/// </summary>
public sealed class ParityTests
{
    #region App (A)
    [AvaloniaFact]
    public async Task A1_OpenFromToolbarShowsTheModels()
    {
        using(var app = new AppHarness())
        {
            app.Dialogs.ModelFileToOpen = app.CopyFixture();
            await app.ViewModel.OpenCommand.ExecuteAsync(null);
            app.Pump();

            Assert.NotNull(app.ViewModel.Document);
            Assert.Same(app.ViewModel.Models, app.ViewModel.CurrentPage);
            Assert.Equal("TestDb", app.ViewModel.SelectedModel.Name);
            Assert.Contains(app.Dialogs.ModelFileToOpen, app.ViewModel.Settings.RecentFiles);
            app.Screenshot("01-models");
        }
    }

    [AvaloniaFact]
    public void A1_RecentFilesShowOnHome()
    {
        using(var app = new AppHarness())
        {
            var file = app.CopyFixture();
            app.ViewModel.OpenFile(file);
            app.ViewModel.Navigate(app.ViewModel.Welcome);
            app.Pump();

            Assert.Single(app.ViewModel.Welcome.RecentFiles);
            Assert.Equal("Coverage", app.ViewModel.Welcome.RecentFiles[0].Name);
            app.Screenshot("00-home");
        }
    }

    [AvaloniaFact]
    public void A1_AFileThatCannotBeReadShowsAnError()
    {
        using(var app = new AppHarness())
        {
            var bad = Path.Combine(app.Folder, "bad.edmj");
            File.WriteAllText(bad, "not json");

            Assert.False(app.ViewModel.OpenFile(bad));
            app.Pump();
            Assert.Null(app.ViewModel.Document);
            Assert.NotNull(app.ViewModel.Dialog);
            Assert.True(app.ViewModel.Dialog.IsError);
            Assert.Contains("It is not a model file", app.ViewModel.Dialog.Message);

            //valid JSON that is not a whole model file is refused with the reason, not opened half-way
            app.ViewModel.Dialog.ChooseCommand.Execute(app.ViewModel.Dialog.Buttons[0]);
            app.Pump();
            File.WriteAllText(bad, "{\"Models\":[{\"Name\":\"X\"}]}");
            Assert.False(app.ViewModel.OpenFile(bad));
            app.Pump();
            Assert.Null(app.ViewModel.Document);
            Assert.Contains("It has no common settings.", app.ViewModel.Dialog.Message);
        }
    }

    [AvaloniaFact]
    public async Task A3_SaveWritesTheChangesAndClearsTheMarker()
    {
        using(var app = new AppHarness())
        {
            var file = app.CopyFixture();
            app.ViewModel.OpenFile(file);
            app.ViewModel.SelectedModel.Layers.Repository.Namespace = "Data.Repos";

            Assert.True(app.ViewModel.Document.IsDirty);
            Assert.Contains("•", app.ViewModel.WindowTitle);

            await app.ViewModel.SaveCommand.ExecuteAsync(null);

            Assert.False(app.ViewModel.Document.IsDirty);
            Assert.DoesNotContain("•", app.ViewModel.WindowTitle);
            Assert.Equal("Data.Repos", ModelUtils.Load(file).Models[0].Layers.Repository.Namespace);
        }
    }

    [AvaloniaFact]
    public async Task A3_SaveAsWritesANewFile()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var newFile = Path.Combine(app.Folder, "Copy.edmj");
            app.Dialogs.ModelFileToSave = newFile;

            await app.ViewModel.SaveAsCommand.ExecuteAsync(null);

            Assert.True(File.Exists(newFile));
            Assert.Equal(Path.GetFullPath(newFile), app.ViewModel.Document.FilePath);
            Assert.Equal("Copy.edmj", app.ViewModel.Document.DisplayName);
        }
    }

    [AvaloniaFact]
    public async Task A3_OpeningAnotherFileAsksAboutUnsavedChanges()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.ViewModel.Document.Container.CommonSettings.RootNamespace = "Changed";
            app.Dialogs.ModelFileToOpen = app.CopyFixture();

            var open = app.ViewModel.OpenCommand.ExecuteAsync(null);
            app.Pump();
            Assert.NotNull(app.ViewModel.Dialog);
            Assert.Equal("Unsaved changes", app.ViewModel.Dialog.Title);

            app.ViewModel.Dialog.ChooseCommand.Execute(app.ViewModel.Dialog.Buttons[1]);//Cancel
            await open;

            Assert.Null(app.ViewModel.Dialog);
            Assert.Equal("Changed", app.ViewModel.Document.Container.CommonSettings.RootNamespace);
        }
    }

    [AvaloniaFact]
    public void A4_A5_A6_TitleVersionAndModelPicker()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.Pump();

            Assert.StartsWith("Coverage.edmj", app.ViewModel.WindowTitle);
            Assert.Equal("Coverage.edmj — The Sharp Factory", app.Window.Title);
            var picker = app.Find<ComboBox>("ModelPicker");
            Assert.NotNull(picker);
            Assert.Equal(1, picker.ItemCount);
            Assert.Same(app.ViewModel.SelectedModel, picker.SelectedItem);

            var versionShown = false;
            foreach(var text in app.FindAll<TextBlock>())
            {
                if(string.Equals(text.Text, "vtest", StringComparison.Ordinal))
                    versionShown = true;
            }

            Assert.True(versionShown);
        }
    }
    #endregion

    #region New model file (C) and Add model (M) without a database
    [AvaloniaFact]
    public async Task C1_C4_NewFileStartsWithCommonSettingsAndValidatesThem()
    {
        using(var app = new AppHarness())
        {
            await app.ViewModel.NewCommand.ExecuteAsync(null);
            var wizard = Assert.IsType<AddModelWizardViewModel>(app.ViewModel.Overlay);
            app.Pump();

            Assert.True(wizard.IsNewContainer);
            Assert.True(wizard.IsSettingsStep);
            Assert.Equal(4, wizard.Steps.Count);
            Assert.True(wizard.Steps[0].IsCurrent);
            Assert.Equal("Dependencies", wizard.DependenciesFolder);
            Assert.Equal("4", wizard.IndentSize);
            Assert.True(wizard.UseRootNamespaceInProjectName);
            app.Screenshot("10-new-settings");

            wizard.IndentSize = "0";
            await wizard.NextCommand.ExecuteAsync(null);
            Assert.Equal(ModelValidation.IndentSizeInvalid, wizard.IndentSizeError);
            Assert.Null(wizard.Error);

            wizard.IndentSize = "4";
            wizard.RootNamespace = " My Root ";
            wizard.DependenciesFolder = " Data Access ";
            await wizard.NextCommand.ExecuteAsync(null);
            app.Pump();

            Assert.True(wizard.IsConnectionStep);
            Assert.Equal("My_Root", wizard.RootNamespace);
            Assert.Equal("Data_Access", wizard.DependenciesFolder);
            Assert.Equal(Environment.MachineName, wizard.Server);
            Assert.True(wizard.IntegratedSecurity);
            app.Screenshot("11-new-connection");

            wizard.CancelCommand.Execute(null);
            Assert.Null(app.ViewModel.Overlay);
            Assert.Null(app.ViewModel.Document);
        }
    }

    [AvaloniaFact]
    public async Task M1_M7_ConnectionRulesAreChecked()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.ViewModel.StartAddModel();
            var wizard = Assert.IsType<AddModelWizardViewModel>(app.ViewModel.Overlay);
            Assert.True(wizard.IsConnectionStep);
            Assert.Equal(3, wizard.Steps.Count);

            wizard.ModelName = "SFTest";//the root namespace
            await wizard.NextCommand.ExecuteAsync(null);
            Assert.Equal(ModelValidation.ModelNameIsRootNamespace, wizard.ModelNameError);

            wizard.ModelName = "TestDb";//exists already
            Assert.Null(wizard.ModelNameError);//editing a field clears its error
            wizard.Server = "";
            wizard.IntegratedSecurity = false;
            await wizard.NextCommand.ExecuteAsync(null);
            Assert.Equal(ModelValidation.ModelNameExists, wizard.ModelNameError);
            Assert.Equal(ModelValidation.ServerRequired, wizard.ServerError);
            Assert.Equal(ModelValidation.DatabaseRequired, wizard.DatabaseError);
            Assert.Equal(ModelValidation.UserRequired, wizard.UserError);
            Assert.Equal(ModelValidation.PasswordRequired, wizard.PasswordError);
            Assert.Null(wizard.Error);//every problem is shown at its field

            await wizard.LoadDatabasesCommand.ExecuteAsync(null);
            Assert.Equal(ModelValidation.ServerRequired, wizard.ServerError);
            Assert.Null(wizard.ModelNameError);
            app.Pump();
            Assert.True(app.Find<TextBlock>("ServerError").IsVisible);
            app.Screenshot("12-add-connection-errors");
        }
    }
    #endregion

    #region Delete (D)
    [AvaloniaFact]
    public async Task D1_DeleteAsksThenRemovesTheModel()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var delete = app.ViewModel.Models.DeleteModelCommand.ExecuteAsync(null);
            app.Pump();

            Assert.NotNull(app.ViewModel.Dialog);
            Assert.Contains("Delete model TestDb?", app.ViewModel.Dialog.Message);
            Assert.True(app.ViewModel.Dialog.Buttons[1].IsDanger);//Delete is red and last
            app.Screenshot("20-delete-confirm");

            app.ViewModel.Dialog.ChooseCommand.Execute(app.ViewModel.Dialog.Buttons[1]);
            await delete;

            Assert.Empty(app.ViewModel.Document.Container.Models);
            Assert.Null(app.ViewModel.SelectedModel);
            Assert.True(app.ViewModel.Document.IsDirty);
            Assert.False(app.ViewModel.Models.HasModels);
        }
    }
    #endregion

    #region Model browser (B)
    [AvaloniaFact]
    public void B1_TheTreeHasEveryCategoryAndSearchFilters()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var page = app.ViewModel.Models;
            app.Pump();

            Assert.Equal(5, page.Roots.Count);
            Assert.Equal("14", page.Roots[0].Detail);
            Assert.Contains("14 tables", page.Summary);

            //a search keeps the matching objects (and the objects with a matching column or parameter)
            page.SearchText = "Warehouse";
            var tables = page.Roots[0];
            Assert.Equal("Tables", tables.Title);
            var shown = new System.Collections.Generic.List<string>();
            Assert.Equal("Coverage", tables.Note);//one schema: no schema level
            foreach(var node in tables.Children)
                shown.Add(node.Title);

            Assert.Contains("Warehouse", shown);
            Assert.Contains("Shipment", shown);//FromWarehouse, ToWarehouse
            Assert.DoesNotContain("Person", shown);
            page.SearchText = "zzz-no-match";
            Assert.Empty(page.Roots);
            page.ClearSearchCommand.Execute(null);
            Assert.Equal(5, page.Roots.Count);
        }
    }

    [AvaloniaFact]
    public void B2_B3_TableAndColumnDetails()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var page = app.ViewModel.Models;
            var table = FindNode(page, "Shipment");
            page.SelectedNode = table;
            app.Pump();

            Assert.Equal("Table", page.Details.Kind);
            Assert.Equal("Entity Name", page.Details.Rows[0].Label);
            Assert.Equal("Store Name", page.Details.Rows[1].Label);
            Assert.Equal("Primary Key", page.Details.Lists[0].Header);
            Assert.Equal("Navigation Properties", page.Details.Lists[1].Header);
            Assert.Equal("Unique Keys", page.Details.Lists[2].Header);
            Assert.True(page.HasCode);
            Assert.True(page.CodeFiles.Count >= 5);
            app.Screenshot("02-table-details");

            page.SelectedNode = table.AllChildren[0];
            Assert.Equal(12, page.Details.Rows.Count);
            Assert.Equal("Param Mode", page.Details.Rows[11].Label);
        }
    }

    [AvaloniaFact]
    public void B4_B5_B6_ProcedureFunctionAndSequenceDetails()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var page = app.ViewModel.Models;

            page.SelectedNode = FindNode(page, "GetShipments");
            Assert.Equal("Stored procedure", page.Details.Kind);
            Assert.True(page.Details.HasColumns);
            Assert.NotNull(page.Details.SelectedColumn);
            Assert.Equal(6, page.Details.ColumnRows.Count);

            page.SelectedNode = FindNode(page, "FullName");
            Assert.Equal("Function", page.Details.Kind);
            Assert.False(page.Details.HasColumns);
            Assert.Equal(6, page.Details.ColumnRows.Count);//the scalar return

            page.SelectedNode = FindNode(page, "EmployeesUnder");
            Assert.True(page.Details.HasColumns);

            page.SelectedNode = FindNode(page, "InvoiceNumber");
            Assert.Equal("Sequence", page.Details.Kind);
            Assert.Equal("Returns .NET Type", page.Details.Rows[2].Label);
            app.Pump();
            app.Screenshot("03-sequence-details");
        }
    }

    [AvaloniaFact]
    public void B7_AModelMarkedSyncedCannotSync()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.ViewModel.SelectedModel.Synced = YesOrNo.Yes;
            app.ViewModel.Navigate(app.ViewModel.Sync);
            app.ViewModel.Sync.TargetPath = Path.Combine(app.Folder, "App.slnx");

            Assert.True(app.ViewModel.Sync.IsBlockedBySyncedFlag);
            Assert.False(app.ViewModel.Sync.CanRun);
        }
    }
    #endregion

    #region Settings (S) and Layers (L)
    [AvaloniaFact]
    public void S1_S4_SettingsEditTheModelFile()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.ViewModel.Navigate(app.ViewModel.SettingsPage);
            app.Pump();
            var settings = app.ViewModel.Document.Container.CommonSettings;

            app.Find<TextBox>("RootNamespaceBox").Text = "Contoso";
            app.Find<TextBox>("DependenciesFolderBox").Text = "Generated";
            app.Find<CheckBox>("UseRootNamespaceBox").IsChecked = false;
            app.Find<TextBox>("IndentSizeBox").Text = "2";
            app.Pump();

            Assert.Equal("Contoso", settings.RootNamespace);
            Assert.Equal("Generated", settings.DependenciesFolder);
            Assert.Equal(YesOrNo.No, settings.UseRootNSInProjectName);
            Assert.Equal(2, settings.IndentSize);
            Assert.True(app.ViewModel.Document.IsDirty);

            app.Find<TextBox>("IndentSizeBox").Text = "x";
            app.Pump();
            Assert.Equal(ModelValidation.IndentSizeInvalid, app.ViewModel.SettingsPage.IndentSizeError);
            Assert.Equal(2, settings.IndentSize);
            app.Screenshot("05-settings");
        }
    }

    [AvaloniaFact]
    public void L1_L11_EveryLayerIsEditable()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.ViewModel.Navigate(app.ViewModel.Layers);
            app.Pump();
            var page = app.ViewModel.Layers;

            Assert.Equal(6, page.ModelLayers.Count);
            Assert.Equal(5, page.CommonLayers.Count);
            Assert.Equal("Entities", page.ModelLayers[0].Title);
            Assert.Equal("Repository Container Interface", page.CommonLayers[4].Title);

            //every layer has a project and a namespace text box: 6 + 5 layers x 2
            var boxes = TextBoxesIn(app.Find<ItemsControl>("ModelLayerList"));
            Assert.Equal(12, boxes.Count);
            Assert.Equal(10, TextBoxesIn(app.Find<ItemsControl>("CommonLayerList")).Count);
            boxes[0].Text = "Entities.Project";
            app.Pump();

            Assert.Equal("Entities.Project", app.ViewModel.SelectedModel.Layers.Entity.Project);
            Assert.Equal("SFTest.Entities.Project", page.ModelLayers[0].FullProjectName);
            Assert.True(app.ViewModel.Document.IsDirty);
            app.Screenshot("04-layers");
        }
    }
    #endregion

    #region Sync (Y)
    [AvaloniaFact]
    public async Task Y1_Y5_PreviewThenSyncIntoANewSlnx()
    {
        using(var app = new AppHarness())
        {
            var file = app.CopyFixture();
            app.ViewModel.OpenFile(file);
            app.ViewModel.Navigate(app.ViewModel.Sync);
            var page = app.ViewModel.Sync;

            Assert.Equal(Path.GetDirectoryName(file), page.NewSolutionFolder);
            Assert.Equal("SFTest", page.NewSolutionName);
            Assert.Equal(".slnx", page.NewSolutionFormat);
            Assert.True(page.IsTargetPanelOpen);//no target yet: the chooser is open
            app.Pump();
            app.Screenshot("05b-sync-target");
            page.UseNewSolutionCommand.Execute(null);
            Assert.False(page.IsTargetPanelOpen);
            var solution = Path.Combine(app.Folder, "SFTest.slnx");
            Assert.Equal(solution, page.TargetPath);
            Assert.True(page.CanRun);
            Assert.Equal(".NET 10", page.SelectedTargetFramework.Name);

            await page.PreviewCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(page.Report.Success, page.Report.ErrorMessage);
            Assert.True(page.Report.IsPreview);
            Assert.False(File.Exists(solution));
            Assert.Equal("Preview", page.SummaryTitle);
            Assert.NotEmpty(page.FileChanges);
            Assert.NotEmpty(page.Progress.Entries);

            page.SelectedFileChange = page.FileChanges[0];
            Assert.NotEmpty(page.Diff.Lines);
            app.Screenshot("06-sync-preview");

            await page.SyncCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(page.Report.Success, page.Report.ErrorMessage);
            Assert.True(File.Exists(solution));
            Assert.Equal("Synced", page.SummaryTitle);
            Assert.Single(app.ViewModel.Settings.SyncTargets);

            //the solution synced to is in the list (solutions open in this machine's IDEs can come before it)
            await page.RefreshTargetsCommand.ExecuteAsync(null);
            var listed = false;
            foreach(var candidate in page.Candidates)
            {
                if(string.Equals(candidate.SolutionPath, solution, StringComparison.OrdinalIgnoreCase))
                {
                    listed = true;
                    Assert.NotNull(candidate.LastSynced);
                }
            }

            Assert.True(listed);

            await page.SyncCommand.ExecuteAsync(null);
            Assert.True(page.Report.IsUpToDate);
            app.Screenshot("07-sync-done");

            //a setting that changes every file: the preview shows changed files with their changed lines
            app.ViewModel.Document.Container.CommonSettings.IndentSize = 2;
            await page.PreviewCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(page.Report.Success, page.Report.ErrorMessage);
            Assert.NotEmpty(page.FileChanges);
            Assert.Equal("M", page.FileChanges[0].Letter);
            Assert.True(page.Diff.IsTinted);
            app.Screenshot("07b-sync-changed");

            app.FindAll<TabControl>()[0].SelectedIndex = 1;
            app.Pump();
            app.Screenshot("07c-sync-log");
        }
    }

    [AvaloniaFact]
    public async Task Y1_BrowseRejectsFilesThatAreNotSolutions()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.Dialogs.SolutionFileToOpen = Path.Combine(app.Folder, "notes.txt");
            await app.ViewModel.Sync.BrowseCommand.ExecuteAsync(null);

            Assert.Equal("Choose a .sln or .slnx file.", app.ViewModel.Sync.TargetError);
            Assert.Null(app.ViewModel.Sync.TargetPath);

            app.Dialogs.SolutionFileToOpen = Path.Combine(app.Folder, "Existing.sln");
            await app.ViewModel.Sync.BrowseCommand.ExecuteAsync(null);
            Assert.Equal(app.Dialogs.SolutionFileToOpen, app.ViewModel.Sync.TargetPath);
        }
    }
    #endregion

    #region Update (U) without a database
    [AvaloniaFact]
    public void U1_UpdateShowsTheConnectionSummary()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.ViewModel.Models.UpdateModelCommand.Execute(null);
            var wizard = Assert.IsType<UpdateModelWizardViewModel>(app.ViewModel.Overlay);
            app.Pump();

            Assert.True(wizard.IsSummaryStep);
            Assert.Equal("TestDb", app.Find<TextBlock>("SummaryName").Text);
            Assert.Equal("Windows authentication", wizard.SignInText);
            app.Screenshot("13-update-summary");
        }
    }
    #endregion

    #region Help (H)
    [AvaloniaFact]
    public void H1_H2_DocumentationAndAbout()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.Navigate(app.ViewModel.Documentation);
            app.Pump();
            Assert.Equal(3, app.ViewModel.Documentation.Sections.Count);
            var links = 0;
            foreach(var section in app.ViewModel.Documentation.Sections)
                links += section.Links.Count;

            Assert.Equal(22, links);
            Assert.Equal(22, app.FindAll<HyperlinkButton>().Count);
            app.Screenshot("08-documentation");

            app.ViewModel.Navigate(app.ViewModel.About);
            app.Pump();
            Assert.Equal("The Sharp Factory App", app.Find<TextBlock>("AboutProductName").Text);
            Assert.Equal("Version: test", app.Find<TextBlock>("AboutVersion").Text);
            Assert.Equal("https://www.thesharpfactory.com/", app.Find<HyperlinkButton>("AboutWebsiteLink").NavigateUri.ToString());
            Assert.Equal("https://github.com/TheSharpFactory/SharpFactory/releases", app.Find<HyperlinkButton>("AboutReleasesLink").NavigateUri.ToString());
            Assert.Equal("https://github.com/TheSharpFactory/SharpFactory/issues", app.Find<HyperlinkButton>("AboutIssuesLink").NavigateUri.ToString());
            app.Screenshot("09-about");
        }
    }

    [AvaloniaFact]
    public void PagesThatNeedAModelStayOffWithoutOne()
    {
        using(var app = new AppHarness())
        {
            foreach(var item in app.ViewModel.NavItems)
                Assert.Equal(!item.RequiresDocument, item.IsEnabled);

            app.ViewModel.SelectedNavItem = app.ViewModel.NavItems[1];
            Assert.Same(app.ViewModel.Welcome, app.ViewModel.CurrentPage);
        }
    }

    #endregion

    internal static ModelTreeNode FindNode(ModelsPageViewModel page, string title)
    {
        foreach(var root in page.Roots)
        {
            var found = FindNode(root, title);
            if(found != null)
                return found;
        }

        throw new InvalidOperationException($"No node {title}");
    }

    private static ModelTreeNode FindNode(ModelTreeNode node, string title)
    {
        if(string.Equals(node.Title, title, StringComparison.Ordinal) && node.Kind != ModelNodeKind.Column && node.Kind != ModelNodeKind.Parameter)
            return node;

        foreach(var child in node.AllChildren)
        {
            var found = FindNode(child, title);
            if(found != null)
                return found;
        }

        return null;
    }

    private static System.Collections.Generic.List<TextBox> TextBoxesIn(Control parent)
    {
        var result = new System.Collections.Generic.List<TextBox>();
        foreach(var visual in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(parent))
        {
            if(visual is TextBox box)
                result.Add(box);
        }

        return result;
    }
}
