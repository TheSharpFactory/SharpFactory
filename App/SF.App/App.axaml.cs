#region Usings
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SF.App.Services;
using SF.App.ViewModels;
using SF.App.Views;
using SF.Common.Logging;
using SF.Engine.Settings;
using System;
#endregion

namespace SF.App;

internal sealed partial class App : Application
{
    private MainWindowViewModel _viewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += OnUiUnhandledException;

        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = AppSettingsStore.Load();

            var window = new MainWindow();
            var viewModel = new MainWindowViewModel(new StorageFileDialogs(window), settings, new Versionable().CurrentVersion) { Host = new WindowAppHost(window) };
            window.DataContext = viewModel;
            desktop.MainWindow = window;
            _viewModel = viewModel;

            var modelFile = FindModelFileArgument(desktop.Args);
            if(modelFile != null)
                viewModel.OpenFile(modelFile);

            //macOS hands over a double-clicked .edmj through an activation event, not the command line
            if(TryGetFeature(typeof(IActivatableLifetime)) is IActivatableLifetime activatable)
                activatable.Activated += OnActivated;

            //the once-a-day update check (never in the tests, which build the view model themselves)
            viewModel.Updates.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnActivated(object sender, ActivatedEventArgs e)
    {
        if(_viewModel == null || e.Kind != ActivationKind.File || e is not FileActivatedEventArgs fileArgs)
            return;

        foreach(var item in fileArgs.Files)
        {
            var path = item.TryGetLocalPath();
            if(path != null && path.EndsWith(".edmj", StringComparison.OrdinalIgnoreCase))
            {
                _ = _viewModel.OpenWithConfirmationAsync(path);
                return;
            }
        }
    }

    /// <summary>
    /// An .edmj passed on the command line (for example by double-clicking it) opens at start.
    /// </summary>
    private static string FindModelFileArgument(string[] args)
    {
        if(args == null)
            return null;

        foreach(var arg in args)
        {
            if(arg.EndsWith(".edmj", StringComparison.OrdinalIgnoreCase))
                return arg;
        }

        return null;
    }

    private void OnUiUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        //keep the app running so unsaved model changes are not lost
        LogManager.WriteErrorLog(e.Exception, "DispatcherUnhandledException");
        e.Handled = true;

        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.DataContext is MainWindowViewModel viewModel)
            _ = viewModel.ShowMessageAsync("Something went wrong", $"{e.Exception.Message}{Environment.NewLine}{Environment.NewLine}The details are in the error log.", true);
    }
}
