using System;
using System.Threading;

namespace BMBF.Backend.Util;

/// <summary>
/// Utility class for debouncing events.
/// e.g. waiting for file changes to stop in a folder before rescanning it
/// </summary>
public class Debouncey : IDisposable
{
    private Timer? _debounceTimer;

    private readonly object _timerLock = new();
    private readonly int _debounceInterval;

    private bool _disposed;

    /// <summary>
    /// Invoked when the debounce interval has passed since the last invocation.
    /// </summary>
    public event EventHandler? Debounced;
    
    /// <summary>
    /// Creates a new debouncey that will wait for <paramref name="debounceInterval"/> milliseconds with no calls to <see cref="Invoke"/> before calling <see cref="Debounced"/>
    /// </summary>
    /// <param name="debounceInterval"></param>
    public Debouncey(int debounceInterval)
    {
        _debounceInterval = debounceInterval;
    }

    /// <summary>
    /// Sends an event for debouncing.
    /// </summary>
    public void Invoke()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Debouncey));
        
        lock (_timerLock)
        {
            _debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(OnTimerCallback, null, _debounceInterval, Timeout.Infinite);
        }
    }

    private void OnTimerCallback(object? state) => Debounced?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _debounceTimer?.Dispose();
    }
}