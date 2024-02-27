using System;
using Discord;
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

    internal static bool TryStartSlashCommand(this PluginLoader pluginLoader, DBSlashCommand? dbSlashCommand)
    {
        try
        {
            if (dbSlashCommand is null)
            {
                throw new ArgumentNullException(nameof(dbSlashCommand));
            }

            var builder = new SlashCommandBuilder();
            builder.WithName(dbSlashCommand.Name);
            builder.WithDescription(dbSlashCommand.Description);
            builder.WithDMPermission(dbSlashCommand.canUseDM);
            builder.Options = dbSlashCommand.Options;

            pluginLoader._Client.CreateGlobalApplicationCommandAsync(builder.Build());
            return true;
        }
        catch (Exception e)
        {
            Config.Logger.Log($"Error starting slash command {dbSlashCommand.Name}: {e.Message}", typeof(PluginLoader), LogType.ERROR);
            return false;
        }
    }
}
