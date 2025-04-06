using System.Net.Mime;
using Discord;
using Discord.WebSocket;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore;
using DiscordBotCore.PluginCore.Helpers;
using DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;
using DiscordBotCore.PluginCore.Interfaces;
using DiscordBotCore.Utilities;

namespace DiscordBotCore.PluginManagement.Loading;

public sealed class PluginLoader : IPluginLoader
{
    private readonly IPluginManager _PluginManager;
    private readonly ILogger _Logger;
    private readonly IConfiguration _Configuration;

    public List<IDbCommand> Commands { get; private set; } = new List<IDbCommand>();
    public List<IDbEvent>  Events { get; private set; } = new List<IDbEvent>();
    public List<IDbSlashCommand> SlashCommands { get; private set; } = new List<IDbSlashCommand>();

    private DiscordSocketClient? _discordClient;

    public PluginLoader(IPluginManager pluginManager, ILogger logger, IConfiguration configuration)
    {
        _PluginManager = pluginManager;
        _Logger = logger;
        _Configuration = configuration;
    }

    public async Task LoadPlugins()
    {
        Commands.Clear();
        Events.Clear();
        SlashCommands.Clear();

        _Logger.Log("Loading plugins...", this);
        
        var loader = new Loader(_PluginManager);

        loader.OnFileLoadedException += FileLoadedException;
        loader.OnPluginLoaded        += OnPluginLoaded;

        await loader.Load();
    }

    public void SetClient(DiscordSocketClient client)
    {
        if (_discordClient is not null)
        {
            _Logger.Log("A client is already set. Please set the client only once.", this, LogType.Error);
            return;
        }
        
        if (client.LoginState != LoginState.LoggedIn)
        {
            _Logger.Log("Client is not logged in. Retry after the client is logged in", this, LogType.Error);
            return;
        }
        
        _Logger.Log("Client is set to the plugin loader", this);
        _discordClient = client;
    }

    private void FileLoadedException(string fileName, Exception exception)
    {
        _Logger.LogException(exception, this);
    }

    private void InitializeDbCommand(IDbCommand command)
    {
        Commands.Add(command);
        _Logger.Log("Command loaded: " + command.Command, this);
    }
    
    private void InitializeEvent(IDbEvent eEvent)
    {
        if (!TryStartEvent(eEvent))
        {
            return;
        }
        
        Events.Add(eEvent);
        _Logger.Log("Event loaded: " + eEvent, this);
    }

    private async void InitializeSlashCommand(IDbSlashCommand slashCommand)
    {
        Result result = await TryStartSlashCommand(slashCommand);
        result.Match(
            () =>
            {
                if (slashCommand.HasInteraction)
                    _discordClient.InteractionCreated += interaction => slashCommand.ExecuteInteraction(_Logger, interaction);
                SlashCommands.Add(slashCommand);
                _Logger.Log("Slash command loaded: " + slashCommand.Name, this);
            },
            HandleError
        );
    }

    private void HandleError(Exception exception)
    {
        _Logger.LogException(exception, this);
    }

    private void OnPluginLoaded(PluginLoaderResult result)
    {
        result.Match(
            InitializeDbCommand,
            InitializeEvent,
            InitializeSlashCommand,
            HandleError
        );
    }
    
    private bool TryStartEvent(IDbEvent dbEvent)
    {
        string? botPrefix = _Configuration.Get<string>("prefix");
        if (string.IsNullOrEmpty(botPrefix))
        {
            _Logger.Log("Bot prefix is not set. Please set the bot prefix in the configuration.", this, LogType.Error);
            return false;
        }
        
        if (_discordClient is null)
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
            _discordClient, 
            botPrefix,
            new DirectoryInfo(eventConfigDirectory));
        
        dbEvent.Start(args);
        
        _Logger.Log("Event started: " + dbEvent.Name, this);
        return true;
    }
    
    private async Task<Result> TryStartSlashCommand(IDbSlashCommand? dbSlashCommand)
    {
        try
        {
            if (dbSlashCommand is null)
            {
                return Result.Failure(new Exception("dbSlashCommand is null"));
            }

            if (_discordClient.Guilds.Count == 0)
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

            List<ulong> serverIds = _Configuration.GetList("ServerIds", new List<ulong>());
            
            foreach(ulong guildId in serverIds)
            {
                bool result = await EnableSlashCommandPerGuild(guildId, builder);
                
                if (!result)
                {
                    return Result.Failure($"Failed to enable slash command {dbSlashCommand.Name} for guild {guildId}");
                }
            }
            
            await _discordClient.CreateGlobalApplicationCommandAsync(builder.Build());

            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure("Error starting slash command");
        }
    }

    private async Task<bool> EnableSlashCommandPerGuild(ulong guildId, SlashCommandBuilder builder)
    {
        SocketGuild? guild = _discordClient.GetGuild(guildId);
        if (guild is null)
        {
            _Logger.Log("Failed to get guild with ID " + guildId, typeof(PluginLoader), LogType.Error);
            return false;
        }
        
        await guild.CreateApplicationCommandAsync(builder.Build());
        
        return true;
    }
}
