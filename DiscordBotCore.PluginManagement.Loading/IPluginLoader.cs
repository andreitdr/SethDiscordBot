using Discord.WebSocket;
using DiscordBotCore.PluginCore.Interfaces;

namespace DiscordBotCore.PluginManagement.Loading;

public interface IPluginLoader
{
    public IReadOnlyList<IDbCommand> Commands { get; }
    public IReadOnlyList<IDbEvent> Events { get; }
    public IReadOnlyList<IDbSlashCommand> SlashCommands { get; }

    /// <summary>
    /// The main assembly of every plugin loaded by the last <see cref="LoadPlugins"/>, paired with its database entry.
    /// Empty while no plugins are loaded.
    /// </summary>
    public IReadOnlyList<LoadedPluginAssembly> LoadedPlugins { get; }

    /// <summary>
    /// Raised at the end of <see cref="LoadPlugins"/>, once <see cref="LoadedPlugins"/> is filled.
    /// </summary>
    public event Action? PluginsLoaded;

    /// <summary>
    /// Raised at the start of <see cref="UnloadAllPlugins"/>, before the load context is unloaded. Subscribers must drop
    /// every reference to plugin types here, or the context can't be collected.
    /// </summary>
    public event Action? PluginsUnloading;

    /// <summary>
    /// True between a completed <see cref="LoadPlugins"/> and the next <see cref="UnloadAllPlugins"/>.
    /// </summary>
    public bool IsLoaded { get; }

    /// <summary>
    /// Sets the Discord client for the plugin loader. This is used to initialize the slash commands and events.
    /// </summary>
    /// <param name="discordSocketClient">The socket client that represents the running Discord Bot</param>
    public void SetDiscordClient(DiscordSocketClient discordSocketClient);

    /// <summary>
    /// Forgets the Discord client, for example after the bot has stopped and the client was disposed.
    /// </summary>
    public void ClearDiscordClient();

    /// <summary>
    /// Loads all the plugins that are installed.
    /// </summary>
    public Task LoadPlugins();

    /// <summary>
    /// Unload all plugins from the plugin manager. Does nothing when no plugins are loaded.
    /// </summary>
    public Task UnloadAllPlugins();
}