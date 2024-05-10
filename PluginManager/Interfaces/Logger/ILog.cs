using System;
using PluginManager.Others;

namespace PluginManager.Interfaces.Logger;

internal interface ILog
{
    string Message { get; set; }

    Type? Source { get; set; }

    LogType Type { get; set; }
    DateTime ThrowTime { get; set; }
}
