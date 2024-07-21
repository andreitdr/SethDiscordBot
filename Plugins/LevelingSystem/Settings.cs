using DiscordBotCore;
using DiscordBotCore.Database;

namespace LevelingSystem;

public class Settings
{
    public int SecondsToWaitBetweenMessages { get; set; }
    public int MinExp { get; set; }
    public int MaxExp { get; set; }
}

internal static class Variables
{
    internal static readonly string                      DataFolder = Application.GetResourceFullPath("LevelingSystem/");
    internal static          SqlDatabase?                Database;
    internal static readonly Dictionary<ulong, DateTime> WaitingList    = new();
    internal static          Settings                    GlobalSettings = new();
}
