namespace DiscordBotCore.Logging;

internal sealed class LogMessage : ILogMessage
{
    private static readonly string _DefaultLogMessageSender = "\b";
    public string Message { get; set; }
    public DateTime ThrowTime { get; set; }
    public string SenderName { get; set; }
    public LogType LogMessageType { get; set; }

    public LogMessage(string message, LogType logMessageType)
    {
        Message = message;
        LogMessageType = logMessageType;
        ThrowTime = DateTime.Now;
        SenderName = string.Empty;
    }

    public LogMessage(string message, object sender)
    {
        Message = message;
        SenderName = sender is string && sender as string == string.Empty ? _DefaultLogMessageSender : sender.GetType().FullName ?? sender.GetType().Name;
        ThrowTime = DateTime.Now;
        LogMessageType = LogType.Info;
    }

    public LogMessage(string message, object sender, DateTime throwTime)
    {
        Message = message;
        SenderName = sender is string && sender as string == string.Empty ? _DefaultLogMessageSender : sender.GetType().FullName ?? sender.GetType().Name;
        ThrowTime = throwTime;
        LogMessageType = LogType.Info;
    }

    public LogMessage(string message, object sender, LogType logMessageType)
    {
        Message = message;
        SenderName = sender is string && sender as string == string.Empty ? _DefaultLogMessageSender : sender.GetType().FullName ?? sender.GetType().Name;
        ThrowTime = DateTime.Now;
        LogMessageType = logMessageType;

    }

    public LogMessage(string message, DateTime throwTime, object sender, LogType logMessageType)
    {
        Message = message;
        ThrowTime = throwTime;
        SenderName = sender is string && sender as string == string.Empty ? _DefaultLogMessageSender : sender.GetType().FullName ?? sender.GetType().Name;
        LogMessageType = logMessageType;
    }

    public LogMessage WithMessage(string message)
    {
        this.Message = message;
        return this;
    }

    public LogMessage WithCurrentThrowTime()
    {
        this.ThrowTime = DateTime.Now;
        return this;
    }

    public LogMessage WithMessageType(LogType logType)
    {
        this.LogMessageType = logType;
        return this;
    }

    public static LogMessage CreateFromException(Exception exception, object Sender, bool logFullStack)
    {
        LogMessage message = new LogMessage(logFullStack? exception.ToString() : exception.Message, Sender, LogType.Error);
        return message;
    }
}