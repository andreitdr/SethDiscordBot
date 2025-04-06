using Discord.WebSocket;
using DiscordBotCore.PluginCore;
using DiscordBotCore.PluginCore.Interfaces;

namespace DiscordBotCore.PluginManagement.Loading;

public interface IPluginLoader
{
    List<IDbCommand> Commands { get; }
    List<IDbEvent> Events { get; }
    List<IDbSlashCommand> SlashCommands { get; }
    Task LoadPlugins();

    void SetClient(DiscordSocketClient client);
}