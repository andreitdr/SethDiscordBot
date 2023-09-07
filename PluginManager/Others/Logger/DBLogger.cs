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
    
    private readonly bool _continuousSave;
    private readonly bool _LogErrorsOnly;

    public DBLogger(bool continuousSave = true, bool logErrorsOnly = true)
    {
        _logFolder = Config.AppSettings["LogFolder"];
        _errFolder = Config.AppSettings["ErrorFolder"];
        
        _continuousSave = continuousSave;
        _LogErrorsOnly  = logErrorsOnly;
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

    private async void Log(LogMessage message)
    {
        LogEvent?.Invoke(message.Message, message.Type);

        if (message.Type != LogLevel.ERROR && message.Type != LogLevel.CRITICAL)
            LogHistory.Add(message);
        else
            ErrorHistory.Add(message);

        if (_continuousSave)
            await SaveToFile();
    }

    public void Log(string message, object sender, LogLevel type = LogLevel.INFO)
    {
        Log(message, sender.GetType().Name, type);
    }

    public async Task SaveToFile()
    { 
        await SaveToTxt();
    }

    private async Task SaveToTxt()
    {
        if (!_LogErrorsOnly)
        {
            var logFile = new LogFile(_logFolder + $"/{DateTime.Today.ToShortDateString().Replace('/', '_')}_log.txt");
            foreach (var logMessage in LogHistory)
                logFile.Write(logMessage);
        }

        var errFile = new LogFile(_errFolder + $"/{DateTime.Today.ToShortDateString().Replace('/', '_')}_err.txt");
        foreach (var logMessage in ErrorHistory)
            errFile.Write(logMessage);
    }
}
