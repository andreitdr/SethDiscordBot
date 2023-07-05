using System;

namespace PluginManager.Others.Logger;

public class LogMessage
{
    public LogMessage(string message, LogLevel type)
    {
        Message    = message;
        Type       = type;
        Time       = DateTime.Now.ToString("HH:mm:ss");
        isInternal = false;
    }

    public LogMessage(string message, LogLevel type, string sender, bool isInternal) : this(message, type)
    {
        Sender          = sender;
        this.isInternal = isInternal;
    }

    public LogMessage(string message, LogLevel type, string sender) : this (message, type, sender, false)
    {
        
    }

    public string   Message    { get; set; }
    public LogLevel Type       { get; set; }
    public string   Time       { get; set; }
    public string   Sender     { get; set; }
    public bool     isInternal { get; set; }

    public override string ToString()
    {
        return $"[{Time}] {Message}";
    }

    public static explicit operator LogMessage(string message)
    {
        return new LogMessage(message, LogLevel.INFO);
    }

    public static explicit operator LogMessage((string message, LogLevel type) tuple)
    {
        return new LogMessage(tuple.message, tuple.type);
    }

    public static explicit operator LogMessage((string message, LogLevel type, string sender) tuple)
    {
        return new LogMessage(tuple.message, tuple.type, tuple.sender);
    }
}
