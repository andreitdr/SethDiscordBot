namespace PluginManager.Others
{

    /// <summary>
    /// A list of operating systems
    /// </summary>
    public enum OperatingSystem
    { WINDOWS, LINUX, MAC_OS, UNKNOWN }

    public enum Error
    { UNKNOWN_ERROR, GUILD_NOT_FOUND, STREAM_NOT_FOUND }

    public enum OutputLogLevel { NONE, INFO, WARNING, ERROR, CRITICAL }
}