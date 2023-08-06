using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PluginManager.Others.Logger;

public class DBLogger
{
    public delegate void LogHandler(string message, LogLevel logType, bool isInternal = false);

    private readonly string _errFolder;

    private readonly string           _logFolder;
    private readonly List<LogMessage> ErrorHistory = new();
    private readonly List<LogMessage> LogHistory   = new();

    public DBLogger()
    {
        _logFolder = Config.AppSettings["LogFolder"];
        _errFolder = Config.AppSettings["ErrorFolder"];
    }

    public IReadOnlyList<LogMessage> Logs   => LogHistory;
    public IReadOnlyList<LogMessage> Errors => ErrorHistory;

    public event LogHandler? LogEvent;
    
    public void Log(string message, LogLevel type = LogLevel.INFO)
    {
        Log(new LogMessage(message, type));
    }
    
    public void Log(string message, LogLevel type= LogLevel.INFO, bool isInternal = false)
    {
        Log(new LogMessage(message, type,"unknown", isInternal));
    }

    public void Log(string message, string sender = "unknown", LogLevel type = LogLevel.INFO, bool isInternal = false)
    {
        Log(new LogMessage(message, type,sender,isInternal));
    }

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
        LogEvent?.Invoke(message.Message, message.Type);

        if (message.Type != LogLevel.ERROR && message.Type != LogLevel.CRITICAL)
            LogHistory.Add(message);
        else
            ErrorHistory.Add(message);
    }

    public void Log(string message, object sender, LogLevel type = LogLevel.INFO)
    {
        Log(message, sender.GetType().Name, type);
    }

    public async Task SaveToFile(bool ErrorsOnly = true)
    {
        if(!ErrorsOnly)
        await JsonManager.SaveToJsonFile(_logFolder + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".json",
                                       LogHistory);
        await JsonManager.SaveToJsonFile(_errFolder + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".json",
                                       ErrorHistory);
    }
}
