using System.Net.Mime;
using Discord.WebSocket;
using DiscordBotCore.Database.Sqlite;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore;
using DiscordBotCore.PluginCore.Helpers;
using DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;
using DiscordBotCore.PluginCore.Interfaces;
using static LevelingSystem.Variables;

namespace LevelingSystem;

internal class LevelEvent : IDbEvent
{
    public string Name => "Leveling System Event Handler";
    public string Description => "The Leveling System Event Handler";

    public async Task Start(IDbEventExecutingArgument args)
    {

        Directory.CreateDirectory(DataFolder);
        await Task.Delay(200);
        Database = new SqlDatabase(DataFolder + "Users.db");
        await Database.Open();


        if (!File.Exists(DataFolder + "Settings.txt"))
        {
            GlobalSettings = new Settings
            {
                SecondsToWaitBetweenMessages = 5,
                MaxExp                       = 7,
                MinExp                       = 1
            };
            
            await DiscordBotCore.Utilities.JsonManager.SaveToJsonFile(DataFolder + "Settings.txt", GlobalSettings);
        }
        else
            GlobalSettings = await DiscordBotCore.Utilities.JsonManager.ConvertFromJson<Settings>(DataFolder + "Settings.txt");

        if (!await Database.TableExistsAsync("Levels"))
            await Database.CreateTableAsync("Levels", "UserID VARCHAR(128)", "Level INT", "EXP INT");

        if (!await Database.TableExistsAsync("Users"))
            await Database.CreateTableAsync("Users", "UserID VARCHAR(128)", "UserMention VARCHAR(128)", "Username VARCHAR(128)", "Discriminator VARCHAR(128)");

        args.Client.MessageReceived += (message) => ClientOnMessageReceived(message, args.BotPrefix);
    }

    private async Task ClientOnMessageReceived(SocketMessage arg, string botPrefix)
    {
        if (arg.Author.IsBot || arg.IsTTS || arg.Content.StartsWith(botPrefix))
            return;

        if (WaitingList.ContainsKey(arg.Author.Id) && WaitingList[arg.Author.Id] > DateTime.Now.AddSeconds(-GlobalSettings.SecondsToWaitBetweenMessages))
            return;

        var userID = arg.Author.Id.ToString();

        object[] userData = await Database.ReadDataArrayAsync($"SELECT * FROM Levels WHERE userID='{userID}'");
        if (userData is null)
        {
            await Database.ExecuteAsync($"INSERT INTO Levels (UserID, Level, EXP) VALUES ('{userID}', 1, 0)");
            await Database.ExecuteAsync($"INSERT INTO Users (UserID, UserMention) VALUES ('{userID}', '{arg.Author.Mention}')");
            return;
        }

        var level = (long)userData[1];
        var exp   = (long)userData[2];


        var random = new Random().Next(GlobalSettings.MinExp, GlobalSettings.MaxExp);
        if (exp + random >= level * 8 + 24)
        {
            await Database.ExecuteAsync($"UPDATE Levels SET Level={level + 1}, EXP={random - (level * 8 + 24 - exp)} WHERE UserID='{userID}'");
            await arg.Channel.SendMessageAsync($"{arg.Author.Mention} has leveled up to level {level + 1}!");
        }
        else await Database.ExecuteAsync($"UPDATE Levels SET EXP={exp + random} WHERE UserID='{userID}'");

        WaitingList.Add(arg.Author.Id, DateTime.Now);
    }

}
