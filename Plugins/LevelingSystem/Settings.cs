using DiscordBotCore.Database.Sqlite;

namespace LevelingSystem;

public class Settings
{
    public int SecondsToWaitBetweenMessages { get; set; }
    public int MinExp { get; set; }
    public int MaxExp { get; set; }
}

internal static class Variables
{
    internal static readonly string DataFolder = "./Data/Resources/LevelingSystem/";
    internal static SqlDatabase? Database;
    internal static readonly Dictionary<ulong, DateTime> WaitingList = new();
    internal static Settings GlobalSettings = new();
}