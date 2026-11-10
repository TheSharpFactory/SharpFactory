#region Usings
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SF.App.Services;
using SF.App.ViewModels;
using SF.App.Views;
using SF.Engine.Settings;
using Xunit;
#endregion

[assembly: AvaloniaTestApplication(typeof(SF.App.Tests.TestAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SF.App.Tests;

/// <summary>
/// Runs the real app headless: Skia rendering, no window on screen.
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}

/// <summary>
/// File pickers that return what the test set.
/// </summary>
internal sealed class FakeFileDialogs : IFileDialogs
{
    public string ModelFileToOpen { get; set; }
    public string ModelFileToSave { get; set; }
    public string SolutionFileToOpen { get; set; }
    public string FolderToPick { get; set; }

    public Task<string> OpenModelFileAsync()
    {
        return Task.FromResult(ModelFileToOpen);
    }

    public Task<string> SaveModelFileAsync(string suggestedFileName)
    {
        return Task.FromResult(ModelFileToSave);
    }

    public Task<string> OpenSolutionFileAsync()
    {
        return Task.FromResult(SolutionFileToOpen);
    }

    public Task<string> PickFolderAsync(string title)
    {
        return Task.FromResult(FolderToPick);
    }
}

/// <summary>
/// One test's app: a window with its view model, isolated settings, and a temporary folder.
/// </summary>
internal sealed class AppHarness : IDisposable
{
    private readonly string _previousHome;

    public AppHarness() : this("test")
    {
    }

    /// <summary>
    /// The app showing the given version (the real one for screenshots that go into the docs).
    /// </summary>
    public AppHarness(string version)
    {
        Folder = Path.Combine(Path.GetTempPath(), "sharpfactory-app-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Folder);
        _previousHome = Environment.GetEnvironmentVariable("SHARPFACTORY_HOME");
        Environment.SetEnvironmentVariable("SHARPFACTORY_HOME", Path.Combine(Folder, "home"));

        Dialogs = new FakeFileDialogs();
        ViewModel = new MainWindowViewModel(Dialogs, AppSettingsStore.Load(), version);
        Window = new MainWindow { DataContext = ViewModel, Width = 1280, Height = 820 };
        Window.Show();
        Pump();
    }

    public string Folder { get; }
    public FakeFileDialogs Dialogs { get; }
    public MainWindowViewModel ViewModel { get; }
    public MainWindow Window { get; }

    /// <summary>
    /// A copy of the public test model, so tests can change and save it.
    /// </summary>
    public string CopyFixture()
    {
        var target = Path.Combine(Folder, "Coverage.edmj");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Coverage.edmj"), target, true);
        return target;
    }

    /// <summary>
    /// Runs the queued UI work (bindings, posted messages) and renders a frame.
    /// </summary>
    public void Pump()
    {
        Dispatcher.UIThread.RunJobs();
        Window.CaptureRenderedFrame();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Pumps until <paramref name="done"/> is true, for background work the UI waits on (like the Sync page's
    /// search for solutions); fails when it takes longer than <paramref name="timeoutMs"/>.
    /// </summary>
    public void PumpUntil(Func<bool> done, int timeoutMs = 30000)
    {
        var watch = Stopwatch.StartNew();
        while(!done())
        {
            if(watch.ElapsedMilliseconds > timeoutMs)
                throw new TimeoutException("The UI did not reach the expected state in time.");

            Pump();
            Thread.Sleep(10);
        }
    }

    /// <summary>
    /// Saves what the window shows, when SF_SCREENSHOTS names a folder (for reviewing the UI).
    /// </summary>
    public void Screenshot(string name)
    {
        Pump();
        var folder = Environment.GetEnvironmentVariable("SF_SCREENSHOTS");
        if(string.IsNullOrWhiteSpace(folder))
            return;

        Directory.CreateDirectory(folder);
        var frame = Window.CaptureRenderedFrame();
        frame?.Save(Path.Combine(folder, name + ".png"), PngBitmapEncoderOptions.Default);
    }

    public T Find<T>(string name) where T : Control
    {
        foreach(var visual in Window.GetVisualDescendants())
        {
            if(visual is T control && string.Equals(control.Name, name, StringComparison.Ordinal))
                return control;
        }

        return null;
    }

    public List<T> FindAll<T>() where T : Control
    {
        var result = new List<T>();
        foreach(var visual in Window.GetVisualDescendants())
        {
            if(visual is T control)
                result.Add(control);
        }

        return result;
    }

    public void Dispose()
    {
        Window.Close();
        Environment.SetEnvironmentVariable("SHARPFACTORY_HOME", _previousHome);
        try
        {
            Directory.Delete(Folder, true);
        }
        catch(IOException)
        {
            //cleaned with the temp folder later
        }
    }
}
