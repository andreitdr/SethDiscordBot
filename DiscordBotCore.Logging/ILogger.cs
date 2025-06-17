namespace DiscordBotCore.Logging;

public interface ILogger
{
    List<ILogMessage> LogMessages { get; protected set; }
    event Action<ILogMessage>? OnLogReceived;

    void Log(string message);
    void Log(string message, LogType logType);
    void Log(string message, object sender);
    void Log(string message, object sender, LogType type);
    void LogException(Exception exception, object sender, bool logFullStack = false);
}