using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;

namespace DiscordBotCore.Loaders;

internal static class PluginLoaderExtensions
{
    internal static bool TryStartEvent(this PluginLoader pluginLoader, DBEvent? dbEvent)
    {
        try
        {
            if (dbEvent is null)
            {
                throw new ArgumentNullException(nameof(dbEvent));
            }


            dbEvent.Start(pluginLoader._Client);
            return true;
        }
        catch (Exception e)
        {
            Application.CurrentApplication.Logger.Log($"Error starting event {dbEvent.Name}: {e.Message}", typeof(PluginLoader), LogType.ERROR);
            return false;
        }
    }

    internal static async Task ResetSlashCommands(this PluginLoader pluginLoader)
    {
        await pluginLoader._Client.Rest.DeleteAllGlobalCommandsAsync();

        if(pluginLoader._Client.Guilds.Count == 0) return;
        if (!ulong.TryParse(Application.CurrentApplication.ServerID, out _))
        {
            Application.CurrentApplication.Logger.Log("Invalid ServerID in config file. Can not reset specific guild commands", typeof(PluginLoader), LogType.ERROR);
            return;
        }

        SocketGuild? guild = pluginLoader._Client.GetGuild(ulong.Parse(Application.CurrentApplication.ServerID));
        if(guild is null)
        {
            Application.CurrentApplication.Logger.Log("Failed to get guild with ID " + Application.CurrentApplication.ServerID, typeof(PluginLoader), LogType.ERROR);
            return;
        }

        await guild.DeleteApplicationCommandsAsync();

        Application.CurrentApplication.Logger.Log($"Cleared all slash commands from guild {guild.Id}", typeof(PluginLoader));
    }

    internal static async Task<bool> TryStartSlashCommand(this PluginLoader pluginLoader, DBSlashCommand? dbSlashCommand)
    {
        try
        {
            if (dbSlashCommand is null)
            {
                //throw new ArgumentNullException(nameof(dbSlashCommand));
                return false;
            }

            if (pluginLoader._Client.Guilds.Count == 0) return false;


            var builder = new SlashCommandBuilder();
            builder.WithName(dbSlashCommand.Name);
            builder.WithDescription(dbSlashCommand.Description);
            builder.WithDMPermission(dbSlashCommand.canUseDM);
            builder.Options = dbSlashCommand.Options;

            if (uint.TryParse(Application.CurrentApplication.ServerID, out uint result))
            {
                SocketGuild? guild = pluginLoader._Client.GetGuild(result);
                if (guild is null)
                {
                    Application.CurrentApplication.Logger.Log("Failed to get guild with ID " + Application.CurrentApplication.ServerID, typeof(PluginLoader), LogType.ERROR);
                    return false;
                }

                await guild.CreateApplicationCommandAsync(builder.Build());
            }else await pluginLoader._Client.CreateGlobalApplicationCommandAsync(builder.Build());

            return true;
        }
        catch (Exception e)
        {
            Application.CurrentApplication.Logger.Log($"Error starting slash command {dbSlashCommand.Name}: {e.Message}", typeof(PluginLoader), LogType.ERROR);
            return false;
        }
    }
}
