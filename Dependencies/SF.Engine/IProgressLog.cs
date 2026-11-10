namespace SF.Engine;

/// <summary>
/// Receives the progress messages of a long operation (building a model, Sync).
/// The app shows them in its progress view; the command line prints them.
/// </summary>
public interface IProgressLog
{
    /// <summary>
    /// A progress step.
    /// </summary>
    void Info(string message);

    /// <summary>
    /// A problem. <paramref name="userMessage"/> is the short text to show to the user.
    /// </summary>
    void Error(string message, string userMessage);
}

/// <summary>
/// Discards every message.
/// </summary>
public sealed class NullProgressLog : IProgressLog
{
    public static NullProgressLog Instance { get; } = new NullProgressLog();

    public void Info(string message)
    {
    }

    public void Error(string message, string userMessage)
    {
    }
}
