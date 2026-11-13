#region Usings
using SF.App.Views;
using SF.Common.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
#endregion

namespace SF.App.Services;

/// <summary>
/// What the view models need from the running app outside the window: open an address in the browser, and start
/// another program and close (the restart after an update). Tests replace it.
/// </summary>
internal interface IAppHost
{
    Task OpenUriAsync(Uri uri);

    /// <summary>
    /// Starts the program and closes the app without asking about unsaved changes (the caller already asked).
    /// Returns false, and keeps the app open, when the program could not be started.
    /// </summary>
    bool StartAndExit(ProcessStartInfo startInfo);
}

/// <summary>
/// The real app: the window's launcher, and closing the main window (which ends the app).
/// </summary>
internal sealed class WindowAppHost : IAppHost
{
    private readonly MainWindow _window;

    public WindowAppHost(MainWindow window)
    {
        _window = window;
    }

    public async Task OpenUriAsync(Uri uri)
    {
        await _window.Launcher.LaunchUriAsync(uri);
    }

    public bool StartAndExit(ProcessStartInfo startInfo)
    {
        try
        {
            using(Process.Start(startInfo))
            {
            }
        }
        catch(Exception ex) when(ex is Win32Exception || ex is InvalidOperationException)
        {
            LogManager.WriteErrorLog(ex, "Unable to start the updated app", startInfo.FileName);
            return false;
        }

        _window.CloseWithoutAsking();
        return true;
    }
}
