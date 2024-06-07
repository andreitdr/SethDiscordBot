using System;

namespace DiscordBotCore.Others;

/// <summary>
///     The output log type
/// </summary>
public enum LogType
{
    INFO,
    WARNING,
    ERROR,
    CRITICAL
}

public enum UnzipProgressType
{
    PERCENTAGE_FROM_NUMBER_OF_FILES,
    PERCENTAGE_FROM_TOTAL_SIZE
}

public enum InternalActionRunType
{
    ON_STARTUP,
    ON_CALL,
    BOTH
}

[Flags]
public enum OSType: byte
{
    NONE    = 0,
    WINDOWS = 1 << 0,
    LINUX   = 2 << 1,
    MACOSX  = 3 << 2
}

public enum PluginType
{
    UNKNOWN,
    COMMAND,
    EVENT,
    SLASH_COMMAND
}
