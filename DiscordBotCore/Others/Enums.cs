using System;

namespace DiscordBotCore.Others;

/// <summary>
///     The output log type. This must be used by other loggers in order to provide logging information
/// </summary>
public enum LogType
{
    Info,
    Warning,
    Error,
    Critical
}

public enum UnzipProgressType
{
    PERCENTAGE_FROM_NUMBER_OF_FILES,
    PERCENTAGE_FROM_TOTAL_SIZE
}

public enum InternalActionRunType
{
    OnStartup,
    OnCall,
    OnStartupAndCall
}

public enum PluginType
{
    UNKNOWN,
    COMMAND,
    EVENT,
    SLASH_COMMAND,
    ACTION
}
