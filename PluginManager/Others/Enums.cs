using PluginManager.Interfaces;

namespace PluginManager.Others;

/// <summary>
///     A list of operating systems
/// </summary>
public enum OperatingSystem
{
    WINDOWS, LINUX, MAC_OS, UNKNOWN
}

/// <summary>
///     A list with all errors
/// </summary>
public enum Error
{
    UNKNOWN_ERROR, GUILD_NOT_FOUND, STREAM_NOT_FOUND, INVALID_USER, INVALID_CHANNEL, INVALID_PERMISSIONS
}

/// <summary>
///     The output log type
/// </summary>
public enum OutputLogLevel { NONE, INFO, WARNING, ERROR, CRITICAL }

/// <summary>
/// Plugin Type
/// </summary>
public enum PluginType { Command, Event, Unknown }

public enum UnzipProgressType { PercentageFromNumberOfFiles, PercentageFromTotalSize }

public enum TableFormat { CENTER_EACH_COLUMN_BASED, CENTER_OVERALL_LENGTH, DEFAULT }