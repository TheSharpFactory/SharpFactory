namespace SF.App.Services;

/// <summary>
/// The C# writer keeps the file it writes in one shared buffer, so two generations at once would write into each
/// other: the Sync page's checks and Syncs (on a background thread) and the Models page's code preview take turns.
/// </summary>
internal static class GenerationLock
{
    public static readonly object Gate = new object();
}
