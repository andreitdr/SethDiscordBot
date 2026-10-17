namespace DiscordBotCore.Logging;

public interface ILogger
{
    /// <summary>
    ///     A snapshot of the in-memory log history. Safe to enumerate while other threads keep logging.
    /// </summary>
    IReadOnlyList<ILogMessage> LogMessages { get; }
    event Action<ILogMessage>? OnLogReceived;

    void Log(string message);
    void Log(string message, LogType logType);
    void Log(string message, object sender);
    void Log(string message, object sender, LogType type);
    void LogException(Exception exception, object sender, bool logFullStack = false);
    void ClearLogMessages();
}