using System;
using System.Collections.Generic;
using System.Linq;

using DiscordBotCore.Interfaces.Logger;

namespace DiscordBotCore.Others.Logger;

public sealed class Logger : ILogger
{

    public List<string> LogMessageProperties = typeof(ILogMessage).GetProperties().Select(p => p.Name).ToList();
    public string LogMessageFormat { get ; set; }

    public event EventHandler<ILogger.FormattedMessage> OnFormattedLog;
    public event EventHandler<ILogMessage> OnRawLog;

    public Logger(string logMessageFormat)
    {
        this.LogMessageFormat = logMessageFormat;
    }

    /// <summary>
    /// Generate a formatted string based on the default parameters of the ILogMessage and a string defined as model
    /// </summary>
    /// <param name="message">The message</param>
    /// <returns>A formatted string with the message values</returns>
    private string GenerateLogMessage(ILogMessage message)
    {
        string messageAsString = new string(LogMessageFormat);
        foreach (var prop in LogMessageProperties)
        {
            Type messageType = typeof(ILogMessage);
            messageAsString = messageAsString.Replace("{" + prop + "}", messageType?.GetProperty(prop)?.GetValue(message)?.ToString());
        }

        return messageAsString;
    }

    private void LogToFile(string message)
    {
        System.IO.File.AppendAllText(Application.CurrentApplication.LogFile, message);
    }

    private string GenerateLogMessage(ILogMessage message, string customFormat)
    {
        string messageAsString = customFormat;
        foreach (var prop in LogMessageProperties)
        {
            Type messageType = typeof(ILogMessage);
            messageAsString = messageAsString.Replace("{" + prop + "}", messageType?.GetProperty(prop)?.GetValue(message)?.ToString());
        }

        return messageAsString;
    }

    public void Log(ILogMessage message, string format)
    {
        OnRawLog?.Invoke(this, message);
        string messageAsString = GenerateLogMessage(message, format);
        OnFormattedLog?.Invoke(this, new ILogger.FormattedMessage() { Message = messageAsString, Type = message.LogMessageType });
        LogToFile(messageAsString);
    }

    public void Log(ILogMessage message)
    {
        OnRawLog?.Invoke(this, message);
        string messageAsString = GenerateLogMessage(message);
        OnFormattedLog?.Invoke(this, new ILogger.FormattedMessage() { Message = messageAsString, Type = message.LogMessageType }) ;
        LogToFile(messageAsString);
    }

    public void Log(string message, LogType logType, string format) => Log(new LogMessage(message, logType), format);
    public void Log(string message, LogType logType) => Log(new LogMessage(message, logType));
    public void Log(string message, object Sender) => Log(new LogMessage(message, Sender));
    public void Log(string message, object Sender, LogType type) => Log(new LogMessage(message, Sender, type));
    public void LogException(Exception exception, object Sender) => Log(LogMessage.CreateFromException(exception, Sender));

}
