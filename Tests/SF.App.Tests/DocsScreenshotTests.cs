#region Usings
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using SF.App.ViewModels;
using System;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// Walks the Add wizard and the Models page against a database that holds only the public Coverage schema
/// (Tests/SF.Tests/Fixtures/Coverage.sql), so the screens can go into the website's documentation.
/// Runs when SHARPFACTORY_DOCS_DB is "server;database"; with SF_SCREENSHOTS set it saves the screens there,
/// named like the images on the Model Generation and Updating The Model pages.
/// </summary>
public sealed class DocsScreenshotTests
{
    [AvaloniaFact]
    public async Task TheModelGenerationPageScreens()
    {
        var setting = Environment.GetEnvironmentVariable("SHARPFACTORY_DOCS_DB");
        if(string.IsNullOrWhiteSpace(setting) || setting.IndexOf(';') < 0)
            Assert.Skip("SHARPFACTORY_DOCS_DB is not set to \"server;database\".");

        var parts = setting.Split(';');
        using(var app = new AppHarness(new Versionable().CurrentVersion))
        {
            await app.ViewModel.NewCommand.ExecuteAsync(null);
            var wizard = Assert.IsType<AddModelWizardViewModel>(app.ViewModel.Overlay);
            wizard.RootNamespace = "MyApp";
            app.Screenshot("common_settings");
            await wizard.NextCommand.ExecuteAsync(null);
            Assert.True(wizard.IsConnectionStep);

            //the server as typed (the default is this machine's name); "server;database" signs in with Windows (as
            //the website's screens show), "server;database;user;password" with a SQL Server login
            wizard.ModelName = "AppDb";
            wizard.Server = parts[0];
            wizard.IntegratedSecurity = parts.Length < 4;
            if(parts.Length >= 4)
            {
                wizard.User = parts[2];
                wizard.Password = parts[3];
            }

            await wizard.LoadDatabasesCommand.ExecuteAsync(null);
            Assert.Null(wizard.Error);
            var who = parts.Length < 4 ? "(Windows)" : "as " + parts[2];
            Assert.StartsWith($"Connected to {parts[0]} {who} · ", wizard.ConnectedText);
            wizard.SelectedDatabase = parts[1];
            app.Screenshot("connection_and_model");
            await wizard.NextCommand.ExecuteAsync(null);
            Assert.Null(wizard.Error);
            Assert.True(wizard.IsObjectsStep);

            wizard.Chooser.SelectAllCommand.Execute(null);
            wizard.Chooser.Roots[0].AllChildren[0].IsExpanded = true;
            app.Screenshot("database_objects");
            await wizard.NextCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(wizard.Succeeded, wizard.Progress.ErrorText);
            app.Screenshot("generating_model");
            wizard.DoneCommand.Execute(null);

            var page = app.ViewModel.Models;
            page.SelectedNode = FindNode(page, "Shipment");
            app.Pump();
            Assert.Equal("Table", page.Details.Kind);
            app.Screenshot("model_details");

            Assert.True(page.HasCode);
            app.FindAll<TabControl>()[0].SelectedIndex = 1;
            app.Screenshot("model_code");

            //the Updating The Model page
            app.ViewModel.StartUpdateModel();
            var update = Assert.IsType<UpdateModelWizardViewModel>(app.ViewModel.Overlay);
            app.Screenshot("update_connection");
            await update.NextCommand.ExecuteAsync(null);
            Assert.Null(update.Error);
            Assert.True(update.IsObjectsStep);
            Assert.Equal(update.Chooser.TotalCount, update.Chooser.SelectedCount);
            update.Chooser.Roots[0].AllChildren[0].IsExpanded = true;
            app.Screenshot("update_objects");

            //take one table out: the list marks it and says what the update does
            update.Chooser.Roots[0].AllChildren[0].AllChildren[0].IsChecked = false;
            app.Pump();
            Assert.Contains("removes 1 object", update.Chooser.ChangeText);
            app.Screenshot("update_objects_changed");
            await update.NextCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(update.Succeeded, update.Progress.ErrorText);
            app.Screenshot("update_done");
            update.DoneCommand.Execute(null);
        }
    }

    private static ModelTreeNode FindNode(ModelsPageViewModel page, string title)
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
        if(string.Equals(node.Title, title, StringComparison.Ordinal) && node.Kind == ModelNodeKind.Table)
            return node;

        foreach(var child in node.AllChildren)
        {
            var found = FindNode(child, title);
            if(found != null)
                return found;
        }

        return null;
    }
}
