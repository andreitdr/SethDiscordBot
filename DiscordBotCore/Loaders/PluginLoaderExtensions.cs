using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using ContextType = Discord.Commands.ContextType;

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
            Application.CurrentApplication.Logger.Log($"Error starting event {dbEvent.Name}: {e.Message}", typeof(PluginLoader), LogType.Error);
            Application.CurrentApplication.Logger.LogException(e, typeof(PluginLoader));
            return false;
        }
    }
    
    internal static Task ResetSlashCommands(this PluginLoader pluginLoader)
    {
        throw new NotImplementedException("This method is not implemented yet.");
        
        // await pluginLoader._Client.Rest.DeleteAllGlobalCommandsAsync();
        //
        // if(pluginLoader._Client.Guilds.Count == 0) return;
        // if (!ulong.TryParse(Application.CurrentApplication.ServerID, out _))
        // {
        //     Application.CurrentApplication.Logger.Log("Invalid ServerID in config file. Can not reset specific guild commands", typeof(PluginLoader), LogType.Error);
        //     return;
        // }
        //
        // SocketGuild? guild = pluginLoader._Client.GetGuild(ulong.Parse(Application.CurrentApplication.ServerID));
        // if(guild is null)
        // {
        //     Application.CurrentApplication.Logger.Log("Failed to get guild with ID " + Application.CurrentApplication.ServerID, typeof(PluginLoader), LogType.Error);
        //     return;
        // }
        //
        // await guild.DeleteApplicationCommandsAsync();
        //
        // Application.CurrentApplication.Logger.Log($"Cleared all slash commands from guild {guild.Id}", typeof(PluginLoader));
    }
    
    internal static async Task<bool> TryStartSlashCommand(this PluginLoader pluginLoader, DBSlashCommand? dbSlashCommand)
    {
        try
        {
            if (dbSlashCommand is null)
            {
                return false;
            }

            if (pluginLoader._Client.Guilds.Count == 0)
            {
                return false;
            }

            var builder = new SlashCommandBuilder();
            builder.WithName(dbSlashCommand.Name);
            builder.WithDescription(dbSlashCommand.Description);
            builder.Options = dbSlashCommand.Options;

            if (dbSlashCommand.canUseDM)
                builder.WithContextTypes(InteractionContextType.BotDm, InteractionContextType.Guild);
            else 
                builder.WithContextTypes(InteractionContextType.Guild);

            // if (uint.TryParse(Application.CurrentApplication.ServerID, out uint result))
            // {
            //     SocketGuild? guild = pluginLoader._Client.GetGuild(result);
            //     if (guild is null)
            //     {
            //         Application.CurrentApplication.Logger.Log("Failed to get guild with ID " + Application.CurrentApplication.ServerID, typeof(PluginLoader), LogType.Error);
            //         return false;
            //     }
            //
            //     await guild.CreateApplicationCommandAsync(builder.Build());
            // }
            // else 
            
            foreach(ulong guildId in Application.CurrentApplication.ServerIDs)
            {
                await pluginLoader.EnableSlashCommandPerGuild(guildId, builder);
            }
            
            await pluginLoader._Client.CreateGlobalApplicationCommandAsync(builder.Build());

            return true;
        }
        catch (Exception e)
        {
            Application.CurrentApplication.Logger.Log($"Error starting slash command {dbSlashCommand.Name}: {e.Message}", typeof(PluginLoader), LogType.Error);
            return false;
        }
    }

    private static async Task<bool> EnableSlashCommandPerGuild(this PluginLoader pluginLoader, ulong guildId, SlashCommandBuilder builder)
    {
        SocketGuild? guild = pluginLoader._Client.GetGuild(guildId);
        if (guild is null)
        {
            Application.CurrentApplication.Logger.Log("Failed to get guild with ID " + guildId, typeof(PluginLoader), LogType.Error);
            return false;
        }
        
        await guild.CreateApplicationCommandAsync(builder.Build());
        
        return true;
    }
    
}
