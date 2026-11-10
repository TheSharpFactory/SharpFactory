#region Usings
using Avalonia;
using SF.Common.Logging;
using SF.Engine.Settings;
using SF.Utils;
using System;
using System.Threading.Tasks;
#endregion

namespace SF.App;

internal static class Program
{
    /// <summary>
    /// Application entry point. Nothing that needs the UI may run before StartWithClassicDesktopLifetime.
    /// </summary>
    [STAThread]
    public static int Main(string[] args)
    {
        LogManager.Enable(AppPaths.LogsFolder);
        IOUtils.LogFolder = AppPaths.LogsFolder;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Also used by the designer and the headless tests.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogManager.WriteErrorLog(e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
    }

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        LogManager.WriteErrorLog(e.Exception, "UnobservedTaskException");
        e.SetObserved();
    }
}
