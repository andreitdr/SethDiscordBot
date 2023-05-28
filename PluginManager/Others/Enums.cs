﻿namespace PluginManager.Others;

/// <summary>
///     A list of operating systems
/// </summary>
public enum OperatingSystem
{
    WINDOWS,
    LINUX,
    MAC_OS,
    UNKNOWN
}

/// <summary>
///     The output log type
/// </summary>
public enum LogLevel
{
    NONE,
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

public enum SaveType
{
    NORMAL,
    BACKUP
}

