#region Usings
using SF.App.Services;
using SF.Common.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
#endregion

namespace SF.App.Views;

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
