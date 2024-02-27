using System;
using System.Linq;
using PluginManager.Interfaces.Logger;

namespace PluginManager.Others.Logger;

public class Log: ILog
{
    public string Message { get; set; }
    public Type? Source { get; set; }
    public LogType Type { get; set; }
    public DateTime ThrowTime { get; set; }

    public Log(string message, Type? source, LogType type, DateTime throwTime)
    {
        Message   = message;
        Source    = source;
        Type      = type;
        ThrowTime = throwTime;
    }

    public Log(string message, Type? source, LogType type)
    {
        Message   = message;
        Source    = source;
        Type      = type;
        ThrowTime = DateTime.Now;
    }

    public Log(string message, Type? source)
    {
        Message   = message;
        Source    = source;
        Type      = LogType.INFO;
        ThrowTime = DateTime.Now;
    }

    public Log(string message)
    {
        Message   = message;
        Source    = typeof(Log);
        Type      = LogType.INFO;
        ThrowTime = DateTime.Now;
    }

    public static implicit operator Log(string message)
    {
        return new Log(message);
    }

    public static implicit operator string(Log log)
    {
        return $"[{log.ThrowTime}] {log.Message}";
    }

    public string AsLongString()
    {
        return $"[{ThrowTime}] [{Source}] [{Type}] {Message}";
    }

    public string AsShortString()
    {
        return this;
    }

    public string FormatedLongString()
    {
        return $"[{ThrowTime}]\t[{Source}]\t\t\t[{Type}]\t{Message}";
    }
}
