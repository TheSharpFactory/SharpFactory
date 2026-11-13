#region Usings
using System;
using System.Threading;
#endregion

namespace SF.App.Services.Updates;

/// <summary>
/// Cancel for an update: it works until the files start being replaced, never after. Cancel and the start of the
/// swap go through one atomic state, so they cannot both win: either the update stops with the app unchanged, or
/// Cancel is refused because the files are already being replaced.
/// </summary>
internal sealed class UpdateCancellation : IDisposable
{
    private const int _running = 0;
    private const int _cancelled = 1;
    private const int _replacing = 2;

    private readonly CancellationTokenSource _source = new CancellationTokenSource();
    private int _state;

    public CancellationToken Token
    {
        get
        {
            return _source.Token;
        }
    }

    public bool IsCancelled
    {
        get
        {
            return Volatile.Read(ref _state) == _cancelled;
        }
    }

    /// <summary>
    /// Stops the update; false when the files are already being replaced (too late).
    /// </summary>
    public bool TryCancel()
    {
        if(Interlocked.CompareExchange(ref _state, _cancelled, _running) != _running)
            return Volatile.Read(ref _state) == _cancelled;

        _source.Cancel();
        return true;
    }

    /// <summary>
    /// The installer's point of no return: false when the update was cancelled first.
    /// </summary>
    public bool TryBeginReplacing()
    {
        return Interlocked.CompareExchange(ref _state, _replacing, _running) == _running;
    }

    public void Dispose()
    {
        _source.Dispose();
    }
}
