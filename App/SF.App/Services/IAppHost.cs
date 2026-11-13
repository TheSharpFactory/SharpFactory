#region Usings
using System;
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
