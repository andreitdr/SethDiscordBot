namespace DiscordBotCore.Logging;

public sealed class Logger : ILogger
{
    private FileStream _logFileStream;

    private readonly List<string>             _logMessageProperties = typeof(ILogMessage).GetProperties().Select(p => p.Name).ToList();
    private          Action<string, LogType>? _outFunction;
    public string LogMessageFormat { get ; set; }

    public Logger(string logFolder, string logMessageFormat, Action<string, LogType>? outFunction = null)
    {
        this.LogMessageFormat = logMessageFormat;
        var logFile = Path.Combine(logFolder, $"{DateTime.Now:yyyy-MM-dd}.log");
        _logFileStream = File.Open(logFile, FileMode.Append, FileAccess.Write, FileShare.Read);
        this._outFunction = outFunction ?? DefaultLogFunction;
    }

    private void DefaultLogFunction(string message, LogType logType)
    {
        Console.WriteLine($"[{logType}] {message}");
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

    private async void LogToFile(string message)
    {
        byte[] messageAsBytes = System.Text.Encoding.ASCII.GetBytes(message);
        await _logFileStream.WriteAsync(messageAsBytes, 0, messageAsBytes.Length);

        byte[] newLine = System.Text.Encoding.ASCII.GetBytes(Environment.NewLine);
        await _logFileStream.WriteAsync(newLine, 0, newLine.Length);

        await _logFileStream.FlushAsync();
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

    public void Log(ILogMessage message, string format)
    {
        string messageAsString = GenerateLogMessage(message, format);
        _outFunction?.Invoke(messageAsString, message.LogMessageType);
        LogToFile(messageAsString);
    }

    public void Log(ILogMessage message)
    {
        string messageAsString = GenerateLogMessage(message);
        _outFunction?.Invoke(messageAsString, message.LogMessageType);
        LogToFile(messageAsString);
        
    }

    public void Log(string message) => Log(new LogMessage(message, string.Empty, LogType.Info));
    public void Log(string message, LogType logType, string format) => Log(new LogMessage(message, logType), format);
    public void Log(string message, LogType logType) => Log(new LogMessage(message, logType));
    public void Log(string message, object sender) => Log(new LogMessage(message, sender));
    public void Log(string message, object sender, LogType type) => Log(new LogMessage(message, sender, type));
    public void LogException(Exception exception, object sender, bool logFullStack = false) => Log(LogMessage.CreateFromException(exception, sender, logFullStack));

    public void SetOutFunction(Action<string, LogType> outFunction)
    {
        this._outFunction = outFunction;
    }
    public string GetLogsHistory()
    {
        string fileName = _logFileStream.Name;
        
        _logFileStream.Flush();
        _logFileStream.Close();
        
        string[] logs = File.ReadAllLines(fileName);
        _logFileStream = File.Open(fileName, FileMode.Append, FileAccess.Write, FileShare.Read);
        
        return string.Join(Environment.NewLine, logs);
        
    }
}
