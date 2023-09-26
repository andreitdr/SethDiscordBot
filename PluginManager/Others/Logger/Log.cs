using System;
using System.Linq;
using PluginManager.Interfaces.Logger;

namespace PluginManager.Others.Logger;

public class Log : ILog
{
    public string   Message    { get; set; }
    public string   OutputFile { get; set; }
    public Type?    Source     { get; set; }
    public LogType  Type       { get; set; }
    public DateTime ThrowTime  { get; set; }
    
    public Log(string message, string outputFile, Type? source, LogType type, DateTime throwTime)
    {
        Message    = message;
        OutputFile = outputFile;
        Source     = source;
        Type       = type;
        ThrowTime  = throwTime;
    }
    
    public Log(string message, string outputFile, Type? source, LogType type)
    {
        Message    = message;
        OutputFile = outputFile;
        Source     = source;
        Type       = type;
        ThrowTime  = DateTime.Now;
    }
    
    public Log(string message, string outputFile, Type? source)
    {
        Message    = message;
        OutputFile = outputFile;
        Source     = source;
        Type       = LogType.INFO;
        ThrowTime  = DateTime.Now;
    }
    
    public Log(string message, string outputFile)
    {
        Message    = message;
        OutputFile = outputFile;
        Source     = typeof(Log);
        Type       = LogType.INFO;
        ThrowTime  = DateTime.Now;
    }
    
    public Log(string message)
    {
        Message    = message;
        OutputFile = "";
        Source     = typeof(Log);
        Type       = LogType.INFO;
        ThrowTime  = DateTime.Now;
    }
    
    public static implicit operator Log(string message) => new (message);

    public static implicit operator string(Log log) => $"[{log.ThrowTime}] {log.Message}";

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
