namespace PluginManager.Others
{

    /// <summary>
    /// A list of operating systems
    /// </summary>
    public enum OperatingSystem
    { WINDOWS, LINUX, MAC_OS, UNKNOWN }

    /// <summary>
    /// A list with all errors
    /// </summary>
    public enum Error
    { UNKNOWN_ERROR, GUILD_NOT_FOUND, STREAM_NOT_FOUND }

    /// <summary>
    /// The output log type
    /// </summary>
    public enum OutputLogLevel { NONE, INFO, WARNING, ERROR, CRITICAL }
}