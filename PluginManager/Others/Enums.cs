﻿namespace PluginManager.Others;

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

public enum SaveType
{
    TXT,
    JSON
}

public enum InternalActionRunType
{
    ON_STARTUP,
    ON_CALL
}
