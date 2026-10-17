using DiscordBotCore.Logging;
using ILogger = DiscordBotCore.Logging.ILogger;

namespace WebUI.Services;

/// <summary>
/// Republishes the logger's messages at most once per <see cref="ThrottleInterval"/>, so a burst of log lines
/// costs one render per open console instead of one render per line.
/// </summary>
public sealed class LogStreamService : IStateNotifier, IDisposable
{
    private static readonly TimeSpan ThrottleInterval = TimeSpan.FromMilliseconds(120);

    private readonly ILogger _Logger;
    private int _FlushScheduled;

    public event Action? Changed;

    public LogStreamService(ILogger logger)
    {
        _Logger = logger;
        _Logger.OnLogReceived += OnLogReceived;
    }

    /// <summary>A thread-safe snapshot of the in-memory log history, oldest first.</summary>
    public IReadOnlyList<ILogMessage> Messages => _Logger.LogMessages;

    public void Clear()
    {
        _Logger.ClearLogMessages();
        Changed?.Invoke();
    }

    private void OnLogReceived(ILogMessage message)
    {
        if (Interlocked.Exchange(ref _FlushScheduled, 1) == 1)
        {
            return;
        }

        _ = FlushAfterDelay();
    }

    private async Task FlushAfterDelay()
    {
        await Task.Delay(ThrottleInterval);
        Interlocked.Exchange(ref _FlushScheduled, 0);
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _Logger.OnLogReceived -= OnLogReceived;
    }
}
