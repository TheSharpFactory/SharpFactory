#region Usings
using Avalonia.Threading;
using SF.App.ViewModels;
using SF.Engine;
#endregion

namespace SF.App.Services;

/// <summary>
/// Collects the engine's progress messages (sent from a background thread) into a list the UI shows.
/// </summary>
internal sealed class UiProgressLog : IProgressLog
{
    private readonly ProgressViewModel _progress;
    private readonly int _run;

    /// <summary>
    /// Writes to the log of the run the progress has now (create it right after Clear).
    /// </summary>
    public UiProgressLog(ProgressViewModel progress)
    {
        _progress = progress;
        _run = progress.Run;
    }

    public void Info(string message)
    {
        Dispatcher.UIThread.Post(new LogPost(_progress, _run, message, null).Run);
    }

    public void Error(string message, string userMessage)
    {
        Dispatcher.UIThread.Post(new LogPost(_progress, _run, message, userMessage).Run);
    }

    /// <summary>
    /// One message on its way to the UI thread.
    /// </summary>
    private sealed class LogPost
    {
        private readonly ProgressViewModel _progress;
        private readonly int _run;
        private readonly string _message;
        private readonly string _userMessage;

        public LogPost(ProgressViewModel progress, int run, string message, string userMessage)
        {
            _progress = progress;
            _run = run;
            _message = message;
            _userMessage = userMessage;
        }

        public void Run()
        {
            //the log was cleared for a new run since: this line belongs to the one before
            if(_progress.Run != _run)
                return;

            _progress.Add(_message, _userMessage);
        }
    }
}
