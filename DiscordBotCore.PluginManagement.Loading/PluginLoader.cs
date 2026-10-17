using System.Reflection;
using System.Runtime.Loader;
using Discord;
using Discord.WebSocket;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;
using DiscordBotCore.PluginCore.Interfaces;
using DiscordBotCore.PluginManagement.Models;
using DiscordBotCore.Utilities.Responses;

namespace DiscordBotCore.PluginManagement.Loading;

public class PluginLoader : IPluginLoader
{
    private static readonly string _HelpCommandNamespaceFullName = "DiscordBotCore.Commands.HelpCommand";

    private readonly IPluginManager _PluginManager;
    private readonly ILogger _Logger;
    private readonly IConfiguration _Configuration;

    private DiscordSocketClient? _DiscordClient;
    private PluginLoaderContext? PluginLoaderContext;
    private bool _IsLoaded;

    private readonly List<IDbCommand> _Commands = new List<IDbCommand>();
    private readonly List<IDbEvent> _Events = new List<IDbEvent>();
    private readonly List<IDbSlashCommand> _SlashCommands = new List<IDbSlashCommand>();
    private readonly List<SocketApplicationCommand> _ApplicationCommands = new List<SocketApplicationCommand>();
    private readonly List<Func<SocketInteraction, Task>> _InteractionHandlers = new List<Func<SocketInteraction, Task>>();
    private readonly List<LoadedPluginAssembly> _LoadedPlugins = new List<LoadedPluginAssembly>();

    public event Action? PluginsLoaded;
    public event Action? PluginsUnloading;

    public PluginLoader(IPluginManager pluginManager, ILogger logger, IConfiguration configuration)
    {
        _PluginManager = pluginManager;
        _Logger = logger;
        _Configuration = configuration;
    }

    public IReadOnlyList<IDbCommand> Commands => _Commands;
    public IReadOnlyList<IDbEvent> Events => _Events;
    public IReadOnlyList<IDbSlashCommand> SlashCommands => _SlashCommands;
    public IReadOnlyList<LoadedPluginAssembly> LoadedPlugins => _LoadedPlugins;
    public bool IsLoaded => _IsLoaded;

    public void SetDiscordClient(DiscordSocketClient discordSocketClient)
    {
        if (_DiscordClient is not null && discordSocketClient == _DiscordClient)
        {
            _Logger.Log("A client is already set. Please set the client only once.", this, LogType.Warning);
            return;
        }

        if (discordSocketClient.LoginState != LoginState.LoggedIn)
        {
            _Logger.Log("The client must be logged in before setting it.", this, LogType.Error);
            return;
        }

        _DiscordClient = discordSocketClient;
    }

    public void ClearDiscordClient()
    {
        _DiscordClient = null;
    }

    public async Task LoadPlugins()
    {
        if (_IsLoaded)
        {
            _Logger.Log("The plugins are already loaded", this, LogType.Warning);
            return;
        }

        _Events.Clear();
        _Commands.Clear();
        _SlashCommands.Clear();
        _ApplicationCommands.Clear();
        _InteractionHandlers.Clear();
        _LoadedPlugins.Clear();

        await LoadPluginFiles();

        foreach (IDbEvent dbEvent in CreateEverythingOfType<IDbEvent>())
        {
            InitializeEvent(dbEvent);
        }

        var helpCommand = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => assembly.DefinedTypes.Any(type => type.FullName == _HelpCommandNamespaceFullName)
                                        && assembly.FullName != null
                                        && assembly.FullName.StartsWith("DiscordBotCore"));

        if (helpCommand is not null)
        {
            var helpCommandType = helpCommand.DefinedTypes.FirstOrDefault(type => type.FullName == _HelpCommandNamespaceFullName &&
                                                                                 typeof(IDbCommand).IsAssignableFrom(type));
            if (helpCommandType is not null && TryCreateInstance(helpCommandType, out IDbCommand? helpCommandInstance))
            {
                InitializeDbCommand(helpCommandInstance);
            }
        }

        foreach (IDbCommand command in CreateEverythingOfType<IDbCommand>())
        {
            InitializeDbCommand(command);
        }

        await LoadSlashCommands(CreateEverythingOfType<IDbSlashCommand>());

        _IsLoaded = true;
        _Logger.Log("Loaded plugins", this);

        RaiseSafely(PluginsLoaded, nameof(PluginsLoaded));
    }

    public async Task UnloadAllPlugins()
    {
        if (!_IsLoaded)
        {
            return;
        }

        RaiseSafely(PluginsUnloading, nameof(PluginsUnloading));

        await UnloadSlashCommands();

        _Commands.Clear();
        _Events.Clear();
        _SlashCommands.Clear();
        _LoadedPlugins.Clear();

        PluginLoaderContext?.Unload();
        PluginLoaderContext = null;
        _IsLoaded = false;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _Logger.Log("Unloaded plugins", this);
    }

    private void RaiseSafely(Action? handlers, string eventName)
    {
        if (handlers is null)
        {
            return;
        }

        foreach (Action handler in handlers.GetInvocationList().Cast<Action>())
        {
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                _Logger.Log($"A {eventName} handler failed: {ex.Message}", this, LogType.Error);
            }
        }
    }

    private async Task UnloadSlashCommands()
    {
        if (_DiscordClient is not null)
        {
            foreach (var handler in _InteractionHandlers)
            {
                _DiscordClient.InteractionCreated -= handler;
            }

            foreach (SocketApplicationCommand command in _ApplicationCommands)
            {
                try
                {
                    await command.DeleteAsync();
                }
                catch (Exception ex)
                {
                    _Logger.Log($"Failed to delete slash command {command.Name}: {ex.Message}", this, LogType.Warning);
                }
            }
        }

        _InteractionHandlers.Clear();
        _ApplicationCommands.Clear();
    }

    private async Task LoadPluginFiles()
    {
        PluginLoaderContext = new PluginLoaderContext(_Logger, "PluginLoader");

        List<LocalPlugin> installedPlugins = await _PluginManager.GetInstalledPlugins();
        List<LocalPlugin> enabledPlugins = installedPlugins.Where(plugin => plugin.IsEnabled).ToList();

        if (enabledPlugins.Count == 0)
        {
            _Logger.Log(installedPlugins.Count == 0 ? "No plugins installed" : "No enabled plugins", this);
            return;
        }

        foreach (LocalPlugin plugin in enabledPlugins)
        {
            if (string.IsNullOrWhiteSpace(plugin.FilePath))
            {
                _Logger.Log($"Plugin {plugin.PluginName} has no file path. Reinstall or re-add it.", this, LogType.Error);
                continue;
            }

            string fullFilePath = Path.GetFullPath(plugin.FilePath);
            if (!File.Exists(fullFilePath))
            {
                _Logger.Log($"Plugin {plugin.PluginName}: file {fullFilePath} does not exist.", this, LogType.Error);
                continue;
            }

            try
            {
                Assembly assembly = LoadFromMemory(PluginLoaderContext, fullFilePath);
                _LoadedPlugins.Add(new LoadedPluginAssembly(plugin, assembly));
            }
            catch (Exception ex)
            {
                _Logger.Log($"Plugin {plugin.PluginName}: failed to load {fullFilePath}: {ex.Message}", this, LogType.Error);
            }
        }

        _Logger.Log($"Loaded {PluginLoaderContext.Assemblies.Count()} assemblies", this);
    }

    /// <summary>
    /// Loads the plugin from a copy in memory instead of from its path. The runtime shares an assembly image by path
    /// while any earlier load context that loaded it is still alive, and an unloaded context is only collected some time
    /// later, so loading by path after a plugin's file was replaced would keep returning the old version. It also
    /// leaves the file unlocked, so it can be replaced while the plugin is loaded.
    /// </summary>
    private static Assembly LoadFromMemory(AssemblyLoadContext context, string assemblyPath)
    {
        using var assemblyStream = new MemoryStream(File.ReadAllBytes(assemblyPath));

        string symbolsPath = Path.ChangeExtension(assemblyPath, ".pdb");
        if (!File.Exists(symbolsPath))
        {
            return context.LoadFromStream(assemblyStream);
        }

        using var symbolsStream = new MemoryStream(File.ReadAllBytes(symbolsPath));
        return context.LoadFromStream(assemblyStream, symbolsStream);
    }

    private List<T> CreateEverythingOfType<T>() where T : class
    {
        var instances = new List<T>();
        if (PluginLoaderContext is null)
        {
            return instances;
        }

        foreach (Assembly assembly in PluginLoaderContext.Assemblies)
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                if (!type.IsClass || type.IsAbstract || !typeof(T).IsAssignableFrom(type))
                {
                    continue;
                }

                if (TryCreateInstance(type, out T? instance))
                {
                    instances.Add(instance);
                }
            }
        }

        return instances;
    }

    private IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            string reasons = string.Join("; ", ex.LoaderExceptions.Where(e => e is not null).Select(e => e!.Message).Distinct().Take(3));
            _Logger.Log($"Some types in {assembly.GetName().Name} could not be loaded: {reasons}", this, LogType.Warning);
            return ex.Types.Where(type => type is not null).Cast<Type>();
        }
    }

    private bool TryCreateInstance<T>(Type type, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? instance) where T : class
    {
        instance = null;
        try
        {
            instance = Activator.CreateInstance(type) as T;
        }
        catch (Exception ex)
        {
            _Logger.Log($"Failed to create instance of plugin type {type.FullName} [{type.Assembly.GetName().Name}]: {(ex.InnerException ?? ex).Message}", this, LogType.Error);
            return false;
        }

        if (instance is null)
        {
            _Logger.Log($"Failed to create instance of plugin type {type.FullName} [{type.Assembly.GetName().Name}]", this, LogType.Error);
            return false;
        }

        return true;
    }

    private void InitializeDbCommand(IDbCommand command)
    {
        _Commands.Add(command);
        _Logger.Log("Command loaded: " + command.Command, this);
    }

    private void InitializeEvent(IDbEvent eEvent)
    {
        if (!TryStartEvent(eEvent))
        {
            return;
        }

        _Events.Add(eEvent);
        _Logger.Log("Event loaded: " + eEvent.Name, this);
    }

    private async Task LoadSlashCommands(List<IDbSlashCommand> slashCommands)
    {
        if (slashCommands.Count == 0)
        {
            return;
        }

        if (_DiscordClient is null)
        {
            _Logger.Log($"Skipping {slashCommands.Count} slash command(s): the Discord client is not set.", this, LogType.Warning);
            return;
        }

        if (_DiscordClient.Guilds.Count == 0)
        {
            _Logger.Log($"Skipping {slashCommands.Count} slash command(s): the bot is not in any guild.", this, LogType.Warning);
            return;
        }

        foreach (IDbSlashCommand slashCommand in slashCommands)
        {
            try
            {
                await InitializeSlashCommand(_DiscordClient, slashCommand);
            }
            catch (Exception ex)
            {
                _Logger.Log($"Failed to register slash command {slashCommand.Name}: {ex.Message}", this, LogType.Error);
            }
        }
    }

    private async Task InitializeSlashCommand(DiscordSocketClient client, IDbSlashCommand slashCommand)
    {
        bool result = await TryStartSlashCommand(client, slashCommand);

        if (!result)
        {
            return;
        }

        if (slashCommand.HasInteraction)
        {
            Func<SocketInteraction, Task> handler = interaction => slashCommand.ExecuteInteraction(_Logger, interaction);
            client.InteractionCreated += handler;
            _InteractionHandlers.Add(handler);
        }

        _SlashCommands.Add(slashCommand);
        _Logger.Log("Slash command loaded: " + slashCommand.Name, this);
    }

    private bool TryStartEvent(IDbEvent dbEvent)
    {
        string? botPrefix = _Configuration.Get<string>("prefix");
        if (string.IsNullOrEmpty(botPrefix))
        {
            _Logger.Log("Bot prefix is not set. Please set the bot prefix in the configuration.", this, LogType.Error);
            return false;
        }

        if (_DiscordClient is null)
        {
            _Logger.Log($"Skipping event {dbEvent.Name}: the Discord client is not set.", this, LogType.Warning);
            return false;
        }

        string? resourcesFolder = _Configuration.Get<string>("ResourcesFolder");
        if (string.IsNullOrEmpty(resourcesFolder))
        {
            _Logger.Log("Resources folder is not set. Please set the resources folder in the configuration.", this, LogType.Error);
            return false;
        }

        if (!Directory.Exists(resourcesFolder))
        {
            _Logger.Log("Resources folder does not exist. Please create the resources folder.", this, LogType.Error);
            return false;
        }

        string eventConfigDirectory = Path.Combine(resourcesFolder, dbEvent.GetType().Assembly.GetName().Name ?? dbEvent.Name);

        try
        {
            Directory.CreateDirectory(eventConfigDirectory);

            IDbEventExecutingArgument args = new DbEventExecutingArgument(
                _Logger,
                _DiscordClient,
                botPrefix,
                new DirectoryInfo(eventConfigDirectory));

            dbEvent.Start(args);
        }
        catch (Exception ex)
        {
            _Logger.Log($"Event {dbEvent.Name} failed to start: {ex.Message}", this, LogType.Error);
            return false;
        }

        return true;
    }

    private async Task<bool> TryStartSlashCommand(DiscordSocketClient client, IDbSlashCommand dbSlashCommand)
    {
        var builder = new SlashCommandBuilder();
        builder.WithName(dbSlashCommand.Name);
        builder.WithDescription(dbSlashCommand.Description);
        builder.Options = dbSlashCommand.Options;

        if (dbSlashCommand.CanUseDm)
            builder.WithContextTypes(InteractionContextType.BotDm, InteractionContextType.Guild);
        else
            builder.WithContextTypes(InteractionContextType.Guild);

        List<ulong> serverIds = _Configuration.GetList("ServerIds", new List<ulong>());

        if (serverIds.Any())
        {
            foreach(ulong guildId in serverIds)
            {
                IResponse<SocketApplicationCommand> result = await EnableSlashCommandPerGuild(client, guildId, builder);

                if (!result.IsSuccess)
                {
                    _Logger.Log($"Failed to enable slash command {dbSlashCommand.Name} for guild {guildId}: {result.Message}", this, LogType.Warning);
                    continue;
                }

                if (result.Data is null)
                {
                    continue;
                }

                _ApplicationCommands.Add(result.Data);
            }

            return true;
        }

        var command = await client.CreateGlobalApplicationCommandAsync(builder.Build());
        _ApplicationCommands.Add(command);
        return true;
    }

    private static async Task<IResponse<SocketApplicationCommand>> EnableSlashCommandPerGuild(DiscordSocketClient client, ulong guildId, SlashCommandBuilder builder)
    {
        SocketGuild? guild = client.GetGuild(guildId);
        if (guild is null)
        {
            return Response<SocketApplicationCommand>.Failure("the bot is not in a guild with this ID");
        }

        var command = await guild.CreateApplicationCommandAsync(builder.Build());
        return Response<SocketApplicationCommand>.Success(command);
    }
}
