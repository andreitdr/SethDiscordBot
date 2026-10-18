namespace DiscordBotCore.Logging;

public sealed class Logger : ILogger
{
    private readonly string _LogFolder;
    private readonly string _LogMessageFormat;
    private readonly int _MaxHistorySize;
    
    private readonly List<string> _logMessageProperties = typeof(ILogMessage)
                                .GetProperties()
                                .Select(p => p.Name)
                                .ToList();


    private readonly List<ILogMessage> _LogMessages = new List<ILogMessage>();
    private readonly object _LogMessagesLock = new object();
    // Messages come from many threads (gateway, dashboard, plugins); appending to the file one at a time keeps
    // lines whole and avoids "file in use" errors.
    private readonly SemaphoreSlim _FileLock = new SemaphoreSlim(1, 1);

    public IReadOnlyList<ILogMessage> LogMessages
    {
        get
        {
            lock (_LogMessagesLock)
            {
                return _LogMessages.ToArray();
            }
        }
    }

    public event Action<ILogMessage>? OnLogReceived;
    

    public Logger(string logFolder, string logMessageFormat, int maxHistorySize)
    {
        this._LogMessageFormat = logMessageFormat;
        this._LogFolder = logFolder;
        this._MaxHistorySize = maxHistorySize;
    }
    
    public void Log(string message) => Log(new LogMessage(message, string.Empty, LogType.Info));
    public void Log(string message, LogType logType) => Log(new LogMessage(message, logType));
    public void Log(string message, object sender) => Log(new LogMessage(message, sender));
    public void Log(string message, object sender, LogType type) => Log(new LogMessage(message, sender, type));
    public void LogException(Exception exception, object sender, bool logFullStack = false) => Log(LogMessage.CreateFromException(exception, sender, logFullStack));

    
    public void ClearLogMessages()
    {
        lock (_LogMessagesLock)
        {
            _LogMessages.Clear();
        }
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

    /// <summary>The file a message is written to: one per day, named after the day the message was logged.</summary>
    public string GetLogFilePath(DateTime time) => Path.Combine(_LogFolder, $"{time:yyyy-MM-dd}.log");

    private async Task LogToFile(string message, DateTime time)
    {
        await _FileLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(GetLogFilePath(time), message + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Logging must never take the app down; the message is still in memory and on the console.
        }
        finally
        {
            _FileLock.Release();
        }
    }

    private async void Log(ILogMessage message)
    {
        var messageAsString = GenerateLogMessage(message);
        lock (_LogMessagesLock)
        {
            _LogMessages.Add(message);
            if (_LogMessages.Count > _MaxHistorySize)
            {
                _LogMessages.RemoveAt(0);
            }
        }
        
        OnLogReceived?.Invoke(message);
        await LogToFile(messageAsString, message.ThrowTime);
    }

}
