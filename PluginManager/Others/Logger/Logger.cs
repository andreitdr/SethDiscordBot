using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using PluginManager.Interfaces.Logger;

namespace PluginManager.Others.Logger;

public sealed class Logger : ILogger
{
    public bool IsEnabled       { get; init; }
    public bool OutputToFile    { get; init; }
    
    public  LogType LowestLogLevel  { get; set; }
    private bool    UseShortVersion { get; }
    
    public Logger(bool useShortVersion, bool outputToFile, LogType lowestLogLevel = LogType.INFO)
    {
        UseShortVersion = useShortVersion;
        OutputToFile    = outputToFile;
        IsEnabled       = true;
        LowestLogLevel  = lowestLogLevel;
    }

    public event EventHandler<Log>? OnLog;
    
    private async Task Log(Log logMessage)
    {
        if (!IsEnabled) return;
        
        OnLog?.Invoke(this, logMessage);
        
        if (logMessage.Type < LowestLogLevel) return;

        if (OutputToFile)
            await File.AppendAllTextAsync(
                logMessage.OutputFile,
                (UseShortVersion ? logMessage : logMessage.AsLongString()) + "\n");
    }
    
    public async void Log(string message = "", string outputFile = "", Type? source = default, LogType type = LogType.INFO, DateTime throwTime = default)
    {
        if (!IsEnabled) return;
        
        if (type < LowestLogLevel) return;

        if (string.IsNullOrEmpty(message)) return;

        if (string.IsNullOrEmpty(outputFile)) outputFile = Config.AppSettings["LogFolder"] + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
        
        if(throwTime == default) throwTime = DateTime.Now;
        
        if (source == default) source = typeof(Log);
        
        await Log(new Log(message, outputFile, source, type, throwTime));
        
    }

    public async void Log(Exception exception, LogType logType = LogType.ERROR, Type? source = null)
    {
        if (!IsEnabled) return;
        
        if (logType < LowestLogLevel) return;

        await Log(new Log(exception.Message, 
            Config.AppSettings["LogFolder"] + "/" + DateTime.Now.ToString("yyyy-MM-dd") + ".log",
            source, logType, DateTime.Now));
    }
}
