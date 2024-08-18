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
    internal static bool TryStartEvent(this IDbEvent? dbEvent)
    {
        try
        {
            if (dbEvent is null)
            {
                throw new ArgumentNullException(nameof(dbEvent));
            }

            dbEvent.Start(Application.CurrentApplication.DiscordBotClient.Client);
            return true;
        }
        catch (Exception e)
        {
            Application.Logger.Log($"Error starting event {dbEvent.Name}: {e.Message}", typeof(PluginLoader), LogType.Error);
            Application.Logger.LogException(e, typeof(PluginLoader));
            return false;
        }
    }
    
    internal static async Task<Result> TryStartSlashCommand(this IDbSlashCommand? dbSlashCommand)
    {
        try
        {
            if (dbSlashCommand is null)
            {
                return Result.Failure(new Exception("dbSlashCommand is null"));
            }

            if (Application.CurrentApplication.DiscordBotClient.Client.Guilds.Count == 0)
            {
                return Result.Failure(new Exception("No guilds found"));
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
                bool result = await EnableSlashCommandPerGuild(guildId, builder);
                
                if (!result)
                {
                    return Result.Failure($"Failed to enable slash command {dbSlashCommand.Name} for guild {guildId}");
                }
            }
            
            await Application.CurrentApplication.DiscordBotClient.Client.CreateGlobalApplicationCommandAsync(builder.Build());

            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure("Error starting slash command");
        }
    }

    private static async Task<bool> EnableSlashCommandPerGuild(ulong guildId, SlashCommandBuilder builder)
    {
        SocketGuild? guild = Application.CurrentApplication.DiscordBotClient.Client.GetGuild(guildId);
        if (guild is null)
        {
            Application.Logger.Log("Failed to get guild with ID " + guildId, typeof(PluginLoader), LogType.Error);
            return false;
        }
        
        await guild.CreateApplicationCommandAsync(builder.Build());
        
        return true;
    }
    
}
