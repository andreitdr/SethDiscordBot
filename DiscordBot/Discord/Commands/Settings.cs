using System;
using System.Collections.Generic;
using Discord.Commands;
using PluginManager;
using PluginManager.Interfaces;

namespace DiscordBot.Discord.Commands;

internal class Settings : DBCommand
{
    /// <summary>
    ///     Command name
    /// </summary>
    public string Command => "set";

    public List<string> Aliases => null;

    /// <summary>
    ///     Command Description
    /// </summary>
    public string Description => "This command allows you change all settings. Use \"set help\" to show details";

    /// <summary>
    ///     Command usage
    /// </summary>
    public string Usage => "set [keyword] [new Value]";

    /// <summary>
    ///     Check if the command require administrator to be executed
    /// </summary>
    public bool requireAdmin => true;

    /// <summary>
    ///     The main body of the command
    /// </summary>
    /// <param name="context">The command context</param>
    public async void Execute(SocketCommandContext context)
    {
        var channel = context.Message.Channel;
        try
        {
            var content = context.Message.Content;
            var data    = content.Split(' ');
            var keyword = data[1];
            if (keyword.ToLower() == "help")
            {
                await channel.SendMessageAsync(
                    "set token [new value] -- set the value of the new token (require restart)");
                await channel.SendMessageAsync(
                    "set prefix [new value] -- set the value of the new preifx (require restart)");

                return;
            }

            switch (keyword.ToLower())
            {
                case "token":
                    if (data.Length != 3)
                    {
                        await channel.SendMessageAsync("Invalid token !");
                        return;
                    }

                    Config.SetValue("token", data[2]);
                    break;
                case "prefix":
                    if (data.Length != 3)
                    {
                        await channel.SendMessageAsync("Invalid token !");
                        return;
                    }

                    Config.SetValue("token", data[2]);
                    break;
                default:
                    return;
            }

            await channel.SendMessageAsync("Restart required ...");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            await channel.SendMessageAsync("Unknown usage to this command !\nUsage: " + Usage);
        }
    }
}