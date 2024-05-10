using System;
using System.Threading.Tasks;
using PluginManager.Others;
using PluginManager.Others.Logger;

namespace PluginManager.Interfaces.Logger;

internal interface ILogger
{
    bool IsEnabled { get; init; }
    bool OutputToFile { get; init; }

    string OutputFile { get; init; }

    event EventHandler<Log> OnLog;
    void Log(
        string message = "", Type? source = default, LogType type = LogType.INFO,
        DateTime throwTime = default);
}
