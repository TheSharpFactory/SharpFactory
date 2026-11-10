#region Usings
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Newtonsoft.Json.Linq;
using SF.App.ViewModels;
using SF.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
#endregion

namespace SF.App.Tests;

/// <summary>
/// The app findings of the 1.1.0.3 code review, one test each.
/// </summary>
public sealed class ReviewFixTests
{
    [Fact]
    public void TheAppCanUseComToFindOpenSolutions()
    {
        //SF-142: with COM interop off, WMI (System.Management) throws and Sync never sees a solution open in an IDE
        var config = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TheSharpFactory.runtimeconfig.json")));
        var setting = config.SelectToken("runtimeOptions.configProperties['System.Runtime.InteropServices.BuiltInComInterop.IsSupported']");

        Assert.True(setting == null || setting.Value<bool>());
    }

    [Fact]
    public void TheAppHasTheVersionInTheVersionFile()
    {
        //the build stamps VERSION into the assemblies (Directory.Build.props); the About page shows the app's
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while(!File.Exists(Path.Combine(folder.FullName, "VERSION")))
            folder = folder.Parent;

        Assert.Equal(File.ReadAllText(Path.Combine(folder.FullName, "VERSION")).Trim(), new Versionable().CurrentVersion);
    }

    [AvaloniaFact]
    public void NavigationAndModelPickerLockWhileBusy()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            app.Pump();
            Assert.True(app.Find<ListBox>("NavList").IsEnabled);

            app.ViewModel.IsBusy = true;
            app.Pump();
            Assert.False(app.Find<ListBox>("NavList").IsEnabled);
            Assert.False(app.Find<ComboBox>("ModelPicker").IsEnabled);

            app.ViewModel.IsBusy = false;
            app.Pump();
            Assert.True(app.Find<ListBox>("NavList").IsEnabled);
        }
    }

    [AvaloniaFact]
    public void SwitchingModelsKeepsTheSyncTarget()
    {
        using(var app = new AppHarness())
        {
            var file = app.CopyFixture();
            app.ViewModel.OpenFile(file);
            var second = ModelUtils.Load(file).Models[0];
            second.Name = "Second";
            app.ViewModel.Document.Container.Models.Add(second);

            var sync = app.ViewModel.Sync;
            sync.NewSolutionName = "Typed";
            sync.UseNewSolutionCommand.Execute(null);
            var target = sync.TargetPath;
            Assert.EndsWith("Typed.slnx", target);

            app.ViewModel.SelectedModel = second;
            Assert.Equal(target, sync.TargetPath);
            Assert.Equal("Typed", sync.NewSolutionName);

            //the other pages follow the picked model
            Assert.Equal("Second", app.ViewModel.Layers.ModelName);
        }
    }

    [AvaloniaFact]
    public void ClosingDuringARunIsRefused()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.IsBusy = true;
            app.Window.Close();
            app.Pump();

            Assert.True(app.Window.IsVisible);
            Assert.NotNull(app.ViewModel.Dialog);
            Assert.Equal("Please wait", app.ViewModel.Dialog.Title);

            app.ViewModel.Dialog.Cancel();
            app.ViewModel.IsBusy = false;
        }
    }

    [AvaloniaFact]
    public async Task AFileHandedOverByTheSystemOpensUnlessBusy()
    {
        using(var app = new AppHarness())
        {
            var file = app.CopyFixture();

            app.ViewModel.IsBusy = true;
            Assert.False(await app.ViewModel.OpenWithConfirmationAsync(file));
            Assert.Null(app.ViewModel.Document);

            app.ViewModel.IsBusy = false;
            Assert.True(await app.ViewModel.OpenWithConfirmationAsync(file));
            Assert.NotNull(app.ViewModel.Document);
        }
    }

    [AvaloniaFact]
    public void AnExistingNewSolutionKeepsItsWarning()
    {
        using(var app = new AppHarness())
        {
            app.ViewModel.OpenFile(app.CopyFixture());
            var sync = app.ViewModel.Sync;
            File.WriteAllText(Path.Combine(app.Folder, "SFTest.slnx"), "<Solution />");

            sync.UseNewSolutionCommand.Execute(null);

            Assert.Equal("That solution already exists: it will be synced as it is.", sync.TargetError);
            Assert.EndsWith("SFTest.slnx", sync.TargetPath);
        }
    }

    [AvaloniaFact]
    public void OpeningSyncDoesNotChangeAnOldModelFile()
    {
        using(var app = new AppHarness())
        {
            //a model file saved before the .NET version setting existed
            var file = app.CopyFixture();
            var json = JObject.Parse(File.ReadAllText(file));
            ((JObject)json["CommonSettings"]).Remove("TargetFramework");
            File.WriteAllText(file, json.ToString());
            Assert.DoesNotContain("TargetFramework", File.ReadAllText(file));

            app.ViewModel.OpenFile(file);
            app.ViewModel.Navigate(app.ViewModel.Sync);
            app.Pump();

            Assert.False(app.ViewModel.Document.IsDirty);
            Assert.Equal(".NET 10", app.ViewModel.Sync.SelectedTargetFramework.Name);
        }
    }
}
