﻿using System;

namespace DiscordBotCore.Others;

/// <summary>
///     The output log type
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
    SLASH_COMMAND,
    ACTION
}
