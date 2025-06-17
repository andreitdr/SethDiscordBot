using Discord.WebSocket;
using DiscordBotCore.PluginCore.Interfaces;

namespace DiscordBotCore.PluginManagement.Loading;

public interface IPluginLoader
{
    public IReadOnlyList<IDbCommand> Commands { get; }
    public IReadOnlyList<IDbEvent> Events { get; }
    public IReadOnlyList<IDbSlashCommand> SlashCommands { get; }

    /// <summary>
    /// Sets the Discord client for the plugin loader. This is used to initialize the slash commands and events.
    /// </summary>
    /// <param name="discordSocketClient">The socket client that represents the running Discord Bot</param>
    public void SetDiscordClient(DiscordSocketClient discordSocketClient);

    /// <summary>
    /// Loads all the plugins that are installed.
    /// </summary>
    public Task LoadPlugins();

    /// <summary>
    /// Unload all plugins from the plugin manager.
    /// </summary>
    public Task UnloadAllPlugins();
}