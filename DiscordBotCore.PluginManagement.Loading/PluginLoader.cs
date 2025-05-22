using System.Collections.ObjectModel;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;
using DiscordBotCore.PluginCore.Interfaces;
using DiscordBotCore.PluginManagement.Loading.Exceptions;

namespace DiscordBotCore.PluginManagement.Loading;

public class PluginLoader : IPluginLoader
{
    private static readonly string _HelpCommandNamespaceFullName = "DiscordBotCore.Commands.HelpCommand";
    
    private readonly IPluginManager _PluginManager;
    private readonly ILogger _Logger;
    private readonly IConfiguration _Configuration;
    
    private DiscordSocketClient? _DiscordClient;
    private PluginLoaderContext? PluginLoaderContext;
    
    private readonly List<IDbCommand> _Commands = new List<IDbCommand>();
    private readonly List<IDbEvent> _Events = new List<IDbEvent>();
    private readonly List<IDbSlashCommand> _SlashCommands = new List<IDbSlashCommand>();
    
    private bool _IsFirstLoad = true;

    public PluginLoader(IPluginManager pluginManager, ILogger logger, IConfiguration configuration)
    {
        _PluginManager = pluginManager;
        _Logger = logger;
        _Configuration = configuration;
    }
    
    public IReadOnlyList<IDbCommand> Commands => _Commands;
    public IReadOnlyList<IDbEvent> Events => _Events;
    public IReadOnlyList<IDbSlashCommand> SlashCommands => _SlashCommands;

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

    public async Task LoadPlugins()
    {
        UnloadAllPlugins();
        
        _Events.Clear();
        _Commands.Clear();
        _SlashCommands.Clear();
        
        await LoadPluginFiles();

        LoadEverythingOfType<IDbEvent>();
        var helpCommand = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => assembly.DefinedTypes.Any(type => type.FullName == _HelpCommandNamespaceFullName)
                                        && assembly.FullName != null
                                        && assembly.FullName.StartsWith("DiscordBotCore"));
        
        if (helpCommand is not null)
        {
            var helpCommandType = helpCommand.DefinedTypes.FirstOrDefault(type => type.FullName == _HelpCommandNamespaceFullName && 
                                                                                 typeof(IDbCommand).IsAssignableFrom(type));
            if (helpCommandType is not null)
            {
                InitializeType<IDbCommand>(helpCommandType);
            }
        }
        
        LoadEverythingOfType<IDbCommand>();
        LoadEverythingOfType<IDbSlashCommand>();
        

        _Logger.Log("Loaded plugins", this);
    }

    public void UnloadAllPlugins()
    {
        if (_IsFirstLoad)
        {
            // Allow unloading only after the first load
            _IsFirstLoad = false;
            return;
        }
        
        if (PluginLoaderContext is null)
        {
            _Logger.Log("The plugins are not loaded. Please load the plugins before unloading them.", this, LogType.Error);
            return;
        }
        
        PluginLoaderContext.Unload();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        PluginLoaderContext = null;
    }

    private async Task LoadPluginFiles()
    {
        if (PluginLoaderContext is not null)
        {
            _Logger.Log("The plugins are already loaded", this, LogType.Error);
            return;
        }
        
        var installedPlugins = await _PluginManager.GetInstalledPlugins();

        if (installedPlugins.Count == 0)
        {
            _Logger.Log("No plugin files found. Please check the plugin files.", this, LogType.Error);
            return;
        }
        
        var files = installedPlugins.Where(plugin => plugin.IsEnabled).Select(plugin => plugin.FilePath);
        
        PluginLoaderContext = new PluginLoaderContext(_Logger, "PluginLoader");
        
        foreach (var file in files)
        {
            string fullFilePath = Path.GetFullPath(file);
            if (string.IsNullOrEmpty(fullFilePath))
            {
                _Logger.Log("The file path is empty. Please check the plugin file path.", PluginLoaderContext, LogType.Error);
                continue;
            }
            
            if (!File.Exists(fullFilePath))
            {
                _Logger.Log("The file does not exist. Please check the plugin file path.", PluginLoaderContext, LogType.Error);
                continue;
            }

            try
            {
                PluginLoaderContext.LoadFromAssemblyPath(fullFilePath);
            }
            catch (Exception ex)
            {
                _Logger.LogException(ex, this);
            }
        }
        
        _Logger.Log($"Loaded {PluginLoaderContext.Assemblies.Count()} assemblies", this);
    }

    private void LoadEverythingOfType<T>()
    {
        if (PluginLoaderContext is null)
        {
            _Logger.Log("The plugins are not loaded. Please load the plugins before loading them.", this, LogType.Error);
            return;
        }
        
        var types = PluginLoaderContext.Assemblies
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(T).IsAssignableFrom(p) && !p.IsInterface);
        
        foreach (var type in types)
        {
            InitializeType<T>(type);
        }
    }

    private void InitializeType<T>(Type type)
    {
        T? plugin = (T?)Activator.CreateInstance(type);
        if (plugin is null)
        {
            _Logger.Log($"Failed to create instance of plugin with type {type.FullName} [{type.Assembly}]", this, LogType.Error);
        }
            
        switch (plugin)
        {
            case IDbEvent dbEvent:
                InitializeEvent(dbEvent);
                break;
            case IDbCommand dbCommand:
                InitializeDbCommand(dbCommand);
                break;
            case IDbSlashCommand dbSlashCommand:
                InitializeSlashCommand(dbSlashCommand);
                break;
            default:
                throw new PluginNotFoundException($"Unknown plugin type {plugin.GetType().FullName}");
        }
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
        _Logger.Log("Event loaded: " + eEvent, this);
    }

    private async void InitializeSlashCommand(IDbSlashCommand slashCommand)
    {
        bool result = await TryStartSlashCommand(slashCommand);

        if (!result)
        {
            return;
        }

        if (_DiscordClient is null)
        {
            return;
        }

        if (slashCommand.HasInteraction)
        {
            _DiscordClient.InteractionCreated += interaction => slashCommand.ExecuteInteraction(_Logger, interaction);
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
            _Logger.Log("Discord client is not set. Please set the discord client before starting events.", this, LogType.Error);
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
        
        string? eventConfigDirectory = Path.Combine(resourcesFolder, dbEvent.GetType().Assembly.GetName().Name);
        
        Directory.CreateDirectory(eventConfigDirectory);
        
        IDbEventExecutingArgument args = new DbEventExecutingArgument(
            _Logger,
            _DiscordClient, 
            botPrefix,
            new DirectoryInfo(eventConfigDirectory));
        
        dbEvent.Start(args);
        return true;
    }
    
    private async Task<bool> TryStartSlashCommand(IDbSlashCommand? dbSlashCommand)
    {
        if (dbSlashCommand is null)
        {
            _Logger.Log("The loaded slash command was null. Please check the plugin.", this, LogType.Error);
            return false;
        }

        if (_DiscordClient is null)
        {
            _Logger.Log("The client is not set. Please set the client before starting slash commands.", this, LogType.Error);
            return false;
        }

        if (_DiscordClient.Guilds.Count == 0)
        {
            _Logger.Log("The client is not connected to any guilds. Please check the client.", this, LogType.Error);
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

        List<ulong> serverIds = _Configuration.GetList("ServerIds", new List<ulong>());
            
        foreach(ulong guildId in serverIds)
        {
            bool result = await EnableSlashCommandPerGuild(guildId, builder);
                
            if (!result)
            {
                _Logger.Log($"Failed to enable slash command {dbSlashCommand.Name} for guild {guildId}", this, LogType.Error);
                return false;
            }
        }
            
        await _DiscordClient.CreateGlobalApplicationCommandAsync(builder.Build());

        return true;
    }

    private async Task<bool> EnableSlashCommandPerGuild(ulong guildId, SlashCommandBuilder builder)
    {
        SocketGuild? guild = _DiscordClient?.GetGuild(guildId);
        if (guild is null)
        {
            _Logger.Log("Failed to get guild with ID " + guildId, this, LogType.Error);
            return false;
        }
        
        await guild.CreateApplicationCommandAsync(builder.Build());
        
        return true;
    }
}