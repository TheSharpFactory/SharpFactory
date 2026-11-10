#region Usings
using Avalonia.Headless.XUnit;
using SF.App.ViewModels;
using SF.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// Builds models through the new UI against a real test database and compares them with a model file the old
/// WPF app built from the same database. Runs only when SHARPFACTORY_TEST_EDMJ names that file (it needs its
/// SQL Server); otherwise the tests are skipped.
/// </summary>
public sealed class DatabaseParityTests
{
    private const string _variable = "SHARPFACTORY_TEST_EDMJ";

    [AvaloniaFact]
    public async Task NewFileWizardBuildsTheSameModelFileAsTheOldApp()
    {
        var reference = Environment.GetEnvironmentVariable(_variable);
        if(string.IsNullOrWhiteSpace(reference) || !File.Exists(reference))
            Assert.Skip($"{_variable} is not set to a model file built by the old app.");

        var old = ModelUtils.Load(reference);
        var oldModel = old.Models[0];

        using(var app = new AppHarness())
        {
            await app.ViewModel.NewCommand.ExecuteAsync(null);
            var wizard = Assert.IsType<AddModelWizardViewModel>(app.ViewModel.Overlay);

            //step 1: the reference file's common settings
            wizard.RootNamespace = old.CommonSettings.RootNamespace;
            wizard.DependenciesFolder = old.CommonSettings.DependenciesFolder;
            wizard.IndentSize = old.CommonSettings.IndentSize.ToString();
            wizard.UseRootNamespaceInProjectName = old.CommonSettings.UseRootNSInProjectName == Models.YesOrNo.Yes;
            await wizard.NextCommand.ExecuteAsync(null);

            //step 2: its connection
            wizard.ModelName = oldModel.Name;
            wizard.Server = oldModel.ConInfo.Server;
            wizard.IntegratedSecurity = !(oldModel.ConInfo.DisableIntegratedSecurity ?? false);
            if(!wizard.IntegratedSecurity)
            {
                wizard.User = oldModel.ConInfo.User;
                wizard.Password = oldModel.ConInfo.Password;
            }

            await wizard.LoadDatabasesCommand.ExecuteAsync(null);
            Assert.Null(wizard.Error);
            Assert.Contains(oldModel.ConInfo.Database, wizard.Databases);
            wizard.SelectedDatabase = oldModel.ConInfo.Database;
            await wizard.NextCommand.ExecuteAsync(null);
            Assert.Null(wizard.Error);
            Assert.True(wizard.IsObjectsStep);
            app.Screenshot("14-add-objects");

            //step 3: everything, as the reference was built
            wizard.Chooser.SelectAllCommand.Execute(null);
            Assert.Equal(wizard.Chooser.TotalCount, wizard.Chooser.SelectedCount);
            await wizard.NextCommand.ExecuteAsync(null);
            app.Pump();

            Assert.True(wizard.Succeeded, wizard.Progress.ErrorText);
            Assert.True(wizard.IsFinished);
            app.Screenshot("15-add-progress");
            wizard.DoneCommand.Execute(null);

            var saved = Path.Combine(app.Folder, "Built.edmj");
            app.Dialogs.ModelFileToSave = saved;
            await app.ViewModel.SaveCommand.ExecuteAsync(null);

            Assert.Equal(Normalize(File.ReadAllText(reference)), Normalize(File.ReadAllText(saved)));
        }
    }

    [AvaloniaFact]
    public async Task UpdateKeepsTheModelTheSame()
    {
        var reference = Environment.GetEnvironmentVariable(_variable);
        if(string.IsNullOrWhiteSpace(reference) || !File.Exists(reference))
            Assert.Skip($"{_variable} is not set to a model file built by the old app.");

        using(var app = new AppHarness())
        {
            var copy = Path.Combine(app.Folder, "Reference.edmj");
            File.Copy(reference, copy);
            app.ViewModel.OpenFile(copy);

            app.ViewModel.StartUpdateModel();
            var wizard = Assert.IsType<UpdateModelWizardViewModel>(app.ViewModel.Overlay);
            await wizard.NextCommand.ExecuteAsync(null);
            Assert.Null(wizard.Error);
            Assert.True(wizard.IsObjectsStep);
            Assert.True(wizard.Chooser.SelectedCount > 0);

            await wizard.NextCommand.ExecuteAsync(null);
            app.Pump();
            Assert.True(wizard.Succeeded, wizard.Progress.ErrorText);
            wizard.DoneCommand.Execute(null);

            await app.ViewModel.SaveCommand.ExecuteAsync(null);
            Assert.Equal(Normalize(File.ReadAllText(reference)), Normalize(File.ReadAllText(copy)));
        }
    }

    private static string Normalize(string json)
    {
        return json.Replace("\r\n", "\n");
    }
}
