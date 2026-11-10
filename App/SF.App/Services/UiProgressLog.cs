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

    public UiProgressLog(ProgressViewModel progress)
    {
        _progress = progress;
    }

    public void Info(string message)
    {
        Dispatcher.UIThread.Post(new LogPost(_progress, message, null).Run);
    }

    public void Error(string message, string userMessage)
    {
        Dispatcher.UIThread.Post(new LogPost(_progress, message, userMessage).Run);
    }

    /// <summary>
    /// One message on its way to the UI thread.
    /// </summary>
    private sealed class LogPost
    {
        private readonly ProgressViewModel _progress;
        private readonly string _message;
        private readonly string _userMessage;

        public LogPost(ProgressViewModel progress, string message, string userMessage)
        {
            _progress = progress;
            _message = message;
            _userMessage = userMessage;
        }

        public void Run()
        {
            _progress.Add(_message, _userMessage);
        }
    }
}
