namespace PluginManager.Others;

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

internal enum ExceptionExitCode : int
{
    CONFIG_FAILED_TO_LOAD = 1,
    CONFIG_KEY_NOT_FOUND = 2,
}
