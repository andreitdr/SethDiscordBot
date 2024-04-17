using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using PluginManager.Interfaces;
using PluginManager.Others;

namespace PluginManager.Loaders;

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
            Config.Logger.Log($"Error starting event {dbEvent.Name}: {e.Message}", typeof(PluginLoader), LogType.ERROR);
            return false;
        }
    }

    internal static async Task ResetSlashCommands(this PluginLoader pluginLoader)
    {
        await pluginLoader._Client.Rest.DeleteAllGlobalCommandsAsync();

        if(pluginLoader._Client.Guilds.Count == 0) return;
        if (!ulong.TryParse(Config.ServerID, out _))
        {
            Config.Logger.Log("Invalid ServerID in config file. Can not reset specific guild commands", typeof(PluginLoader), LogType.ERROR);
            return;
        }

        SocketGuild? guild = pluginLoader._Client.GetGuild(ulong.Parse(Config.ServerID));
        if(guild is null)
        {
            Config.Logger.Log("Failed to get guild with ID " + Config.ServerID, typeof(PluginLoader), LogType.ERROR);
            return;
        }

        await guild.DeleteApplicationCommandsAsync();

        Config.Logger.Log($"Cleared all slash commands from guild {guild.Id}", typeof(PluginLoader));
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

            if (uint.TryParse(Config.ServerID, out uint result))
            {
                SocketGuild? guild = pluginLoader._Client.GetGuild(result);
                if (guild is null)
                {
                    Config.Logger.Log("Failed to get guild with ID " + Config.ServerID, typeof(PluginLoader), LogType.ERROR);
                    return false;
                }

                await guild.CreateApplicationCommandAsync(builder.Build());
            }else await pluginLoader._Client.CreateGlobalApplicationCommandAsync(builder.Build());

            return true;
        }
        catch (Exception e)
        {
            Config.Logger.Log($"Error starting slash command {dbSlashCommand.Name}: {e.Message}", typeof(PluginLoader), LogType.ERROR);
            return false;
        }
    }
}
