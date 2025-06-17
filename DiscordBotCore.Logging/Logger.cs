namespace DiscordBotCore.Logging;

public sealed class Logger : ILogger
{
    private readonly string _LogFile;
    private readonly string _LogMessageFormat;
    private readonly int _MaxHistorySize;
    
    private readonly List<string> _logMessageProperties = typeof(ILogMessage)
                                .GetProperties()
                                .Select(p => p.Name)
                                .ToList();


    public List<ILogMessage> LogMessages { get; set; }
    public event Action<ILogMessage>? OnLogReceived;
    

    public Logger(string logFolder, string logMessageFormat, int maxHistorySize)
    {
        this._LogMessageFormat = logMessageFormat;
        this._LogFile = Path.Combine(logFolder, $"{DateTime.Now:yyyy-MM-dd}.log");
        this._MaxHistorySize = maxHistorySize;
        LogMessages = new List<ILogMessage>();
    }
    
    private string GenerateLogMessage(ILogMessage message)
    {
        string messageAsString = new string(_LogMessageFormat);
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

    private async void Log(ILogMessage message)
    {
        var messageAsString = GenerateLogMessage(message);
        OnLogReceived?.Invoke(message);
        LogMessages.Add(message);
        if (LogMessages.Count > _MaxHistorySize)
        {
            LogMessages.RemoveAt(0);
        }
        await LogToFile(messageAsString);
    }

    public void Log(string message) => Log(new LogMessage(message, string.Empty, LogType.Info));
    public void Log(string message, LogType logType) => Log(new LogMessage(message, logType));
    public void Log(string message, object sender) => Log(new LogMessage(message, sender));
    public void Log(string message, object sender, LogType type) => Log(new LogMessage(message, sender, type));
    public void LogException(Exception exception, object sender, bool logFullStack = false) => Log(LogMessage.CreateFromException(exception, sender, logFullStack));
}
