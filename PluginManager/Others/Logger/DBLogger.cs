using System;
using System.Collections.Generic;

namespace PluginManager.Others.Logger;

public class DBLogger
{
    public delegate void LogHandler(string message, LogLevel logType);

    private readonly string _errFolder;

    private readonly string           _logFolder;
    private readonly List<LogMessage> ErrorHistory = new();
    private readonly List<LogMessage> LogHistory   = new();

    public DBLogger()
    {
        _logFolder = Config.Data["LogFolder"];
        _errFolder = Config.Data["ErrorFolder"];
    }

    public IReadOnlyList<LogMessage> Logs   => LogHistory;
    public IReadOnlyList<LogMessage> Errors => ErrorHistory;

    public event LogHandler LogEvent;

    public void Log(string message, string sender = "unknown", LogLevel type = LogLevel.INFO)
    {
        Log(new LogMessage(message, type, sender));
    }

    public void Error(Exception? e)
    {
        Log(e.Message, e.Source, LogLevel.ERROR);
    }

    public void Log(LogMessage message)
    {
        if (LogEvent is not null)
            LogEvent?.Invoke(message.Message, message.Type);

        if (message.Type != LogLevel.ERROR && message.Type != LogLevel.CRITICAL)
            LogHistory.Add(message);
        else
            ErrorHistory.Add(message);
    }

    public void Log(string message, object sender, LogLevel type = LogLevel.NONE)
    {
        Log(message, sender.GetType().Name, type);
    }

    public async void SaveToFile()
    {
        await Functions.SaveToJsonFile(_logFolder + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".json",
                                       LogHistory);
        await Functions.SaveToJsonFile(_errFolder + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".json",
                                       ErrorHistory);
    }
}
