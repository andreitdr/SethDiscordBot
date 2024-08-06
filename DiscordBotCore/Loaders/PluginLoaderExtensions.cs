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
    internal static bool TryStartEvent(this PluginLoader pluginLoader, IDbEvent? dbEvent)
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
            Application.Logger.Log($"Error starting event {dbEvent.Name}: {e.Message}", typeof(PluginLoader), LogType.Error);
            Application.Logger.LogException(e, typeof(PluginLoader));
            return false;
        }
    }
    
    internal static async Task<bool> TryStartSlashCommand(this PluginLoader pluginLoader, IDbSlashCommand? dbSlashCommand)
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

            if (dbSlashCommand.CanUseDm)
                builder.WithContextTypes(InteractionContextType.BotDm, InteractionContextType.Guild);
            else 
                builder.WithContextTypes(InteractionContextType.Guild);
            
            foreach(ulong guildId in Application.CurrentApplication.ServerIDs)
            {
                bool result = await pluginLoader.EnableSlashCommandPerGuild(guildId, builder);
                
                if (!result)
                {
                    Application.Logger.Log($"Failed to enable slash command {dbSlashCommand.Name} for guild {guildId}", typeof(PluginLoader), LogType.Error);
                }
            }
            
            await pluginLoader._Client.CreateGlobalApplicationCommandAsync(builder.Build());

            return true;
        }
        catch (Exception e)
        {
            Application.Logger.Log($"Error starting slash command {dbSlashCommand.Name}: {e.Message}", typeof(PluginLoader), LogType.Error);
            return false;
        }
    }

    private static async Task<bool> EnableSlashCommandPerGuild(this PluginLoader pluginLoader, ulong guildId, SlashCommandBuilder builder)
    {
        SocketGuild? guild = pluginLoader._Client.GetGuild(guildId);
        if (guild is null)
        {
            Application.Logger.Log("Failed to get guild with ID " + guildId, typeof(PluginLoader), LogType.Error);
            return false;
        }
        
        await guild.CreateApplicationCommandAsync(builder.Build());
        
        return true;
    }
    
}
