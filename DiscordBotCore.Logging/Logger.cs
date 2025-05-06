namespace DiscordBotCore.Logging;

public sealed class Logger : ILogger
{
    private readonly string _LogFile;
    private readonly List<string> _logMessageProperties = typeof(ILogMessage).GetProperties().Select(p => p.Name).ToList();
    public event Action<ILogMessage>? OnLogReceived;
    public string LogMessageFormat { get ; set; }

    public Logger(string logFolder, string logMessageFormat)
    {
        this.LogMessageFormat = logMessageFormat;
        this._LogFile = Path.Combine(logFolder, $"{DateTime.Now:yyyy-MM-dd}.log");
    }

    /// <summary>
    /// Generate a formatted string based on the default parameters of the ILogMessage and a string defined as model
    /// </summary>
    /// <param name="message">The message</param>
    /// <returns>A formatted string with the message values</returns>
    private string GenerateLogMessage(ILogMessage message)
    {
        string messageAsString = new string(LogMessageFormat);
        foreach (var prop in _logMessageProperties)
        {
            Type messageType = typeof(ILogMessage);
            messageAsString = messageAsString.Replace("{" + prop + "}", messageType.GetProperty(prop)?.GetValue(message)?.ToString());
        }

        return messageAsString;
    }

    private async Task LogToFile(string message)
    {
        await using var streamWriter = new StreamWriter(_LogFile, true);
        await streamWriter.WriteLineAsync(message);
    }

    private string GenerateLogMessage(ILogMessage message, string customFormat)
    {
        string messageAsString = customFormat;
        foreach (var prop in _logMessageProperties)
        {
            Type messageType = typeof(ILogMessage);
            messageAsString = messageAsString.Replace("{" + prop + "}", messageType.GetProperty(prop)?.GetValue(message)?.ToString());
        }

        return messageAsString;
    }

    private void Log(ILogMessage message, string format)
    {
        string messageAsString = GenerateLogMessage(message, format);
        OnLogReceived?.Invoke(message);
        LogToFile(messageAsString);
    }

    private void Log(ILogMessage message)
    {
        string messageAsString = GenerateLogMessage(message);
        OnLogReceived?.Invoke(message);
        LogToFile(messageAsString);
        
    }

    public void Log(string message) => Log(new LogMessage(message, string.Empty, LogType.Info));
    public void Log(string message, LogType logType, string format) => Log(new LogMessage(message, logType), format);
    public void Log(string message, LogType logType) => Log(new LogMessage(message, logType));
    public void Log(string message, object sender) => Log(new LogMessage(message, sender));
    public void Log(string message, object sender, LogType type) => Log(new LogMessage(message, sender, type));
    public void LogException(Exception exception, object sender, bool logFullStack = false) => Log(LogMessage.CreateFromException(exception, sender, logFullStack));
}
