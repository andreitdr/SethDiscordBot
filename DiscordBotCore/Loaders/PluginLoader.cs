using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Exceptions;


namespace DiscordBotCore.Loaders;

public sealed class PluginLoader
{
    private readonly DiscordSocketClient _Client;
    public delegate void CommandLoaded(IDbCommand eCommand);
    public delegate void EventLoaded(IDbEvent eEvent);
    public delegate void SlashCommandLoaded(IDbSlashCommand eSlashCommand);
    public delegate void ActionLoaded(ICommandAction eAction);

    public CommandLoaded?      OnCommandLoaded;
    public EventLoaded?        OnEventLoaded;
    public SlashCommandLoaded? OnSlashCommandLoaded;
    public ActionLoaded?       OnActionLoaded;

    public static List<IDbCommand> Commands { get; private set; } = new List<IDbCommand>();
    public static List<IDbEvent>  Events { get; private set; } = new List<IDbEvent>();
    public static List<IDbSlashCommand> SlashCommands { get; private set; } = new List<IDbSlashCommand>();
    public static List<ICommandAction> Actions { get; private set; } = new List<ICommandAction>();

    public PluginLoader(DiscordSocketClient discordSocketClient)
    {
        _Client = discordSocketClient;
    }

    public async Task LoadPlugins()
    {
        Commands.Clear();
        Events.Clear();
        SlashCommands.Clear();
        Actions.Clear();

        Application.Logger.Log("Loading plugins...", this);
        
        var loader = new Loader();

        loader.OnFileLoadedException += FileLoadedException;
        loader.OnPluginLoaded        += OnPluginLoaded;

        await loader.Load();
    }

    private void FileLoadedException(FileLoaderResult result)
    {
        Application.Logger.Log(result.ErrorMessage, this, LogType.Error);
    }

    private async void InitializeCommand(ICommandAction action)
    {
        if (action.RunType == InternalActionRunType.OnStartup || action.RunType == InternalActionRunType.OnStartupAndCall)
            await Application.CurrentApplication.InternalActionManager.StartAction(action, null);
        
        if(action.RunType == InternalActionRunType.OnCall || action.RunType == InternalActionRunType.OnStartupAndCall)
            Actions.Add(action);
        
        OnActionLoaded?.Invoke(action);
    }

    private void InitializeDbCommand(IDbCommand command)
    {
        Commands.Add(command);
        OnCommandLoaded?.Invoke(command);
    }
    
    private void InitializeEvent(IDbEvent eEvent)
    {
        if (!eEvent.TryStartEvent())
        {
            return;
        }
        
        Events.Add(eEvent);
        OnEventLoaded?.Invoke(eEvent);
    }

    private async void InitializeSlashCommand(IDbSlashCommand slashCommand)
    {
        Result result = await slashCommand.TryStartSlashCommand();
        result.Match(
            () =>
            {
                if (slashCommand.HasInteraction)
                    _Client.InteractionCreated += slashCommand.ExecuteInteraction;
                SlashCommands.Add(slashCommand);
                OnSlashCommandLoaded?.Invoke(slashCommand);
            },
            HandleError
        );
    }

    private void HandleError(Exception exception)
    {
        Application.Logger.Log(exception.Message, this, LogType.Error);
    }

    private void OnPluginLoaded(PluginLoaderResult result)
    {
        result.Match(
            InitializeDbCommand,
            InitializeEvent,
            InitializeSlashCommand,
            InitializeCommand,
            HandleError
        );
    }
}
