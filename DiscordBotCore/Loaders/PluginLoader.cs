using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;


namespace DiscordBotCore.Loaders;

public class PluginLoader
{
    internal readonly DiscordSocketClient _Client;

    public delegate void CommandLoaded(PluginLoadResultData resultData);

    public delegate void EventLoaded(PluginLoadResultData resultData);

    public delegate void SlashCommandLoaded(PluginLoadResultData resultData);

    public delegate void ActionLoaded(PluginLoadResultData resultData);

    public CommandLoaded?      OnCommandLoaded;
    public EventLoaded?        OnEventLoaded;
    public SlashCommandLoaded? OnSlashCommandLoaded;
    public ActionLoaded?       OnActionLoaded;

    public static List<DBCommand> Commands { get; private set; } = new List<DBCommand>();
    public static List<DBEvent>  Events { get; private set; } = new List<DBEvent>();
    public static List<DBSlashCommand> SlashCommands { get; private set; } = new List<DBSlashCommand>();
    public static List<ICommandAction> Actions { get; private set; } = new List<ICommandAction>();

    public PluginLoader(DiscordSocketClient discordSocketClient)
    {
        _Client = discordSocketClient;
    }

    public async Task LoadPlugins()
    {

        if (_Client == null)
        {
            Application.CurrentApplication.Logger.Log("Discord client is null", this, LogType.ERROR);
            return;
        }

        Commands.Clear();
        Events.Clear();
        SlashCommands.Clear();
        Actions.Clear();

        Application.CurrentApplication.Logger.Log("Loading plugins...", this);
        
        var loader = new Loader();

        loader.OnFileLoadedException += FileLoadedException;
        loader.OnPluginLoaded        += OnPluginLoaded;

        await loader.Load();
    }

    private void FileLoadedException(FileLoaderResult result)
    {
        Application.CurrentApplication.Logger.Log(result.ErrorMessage, this, LogType.ERROR);
    }

    private async void OnPluginLoaded(PluginLoadResultData result)
    {
        switch (result.PluginType)
        {
            case PluginType.ACTION:
                ICommandAction action = (ICommandAction)result.Plugin;
                if (action.RunType == InternalActionRunType.ON_STARTUP || action.RunType == InternalActionRunType.BOTH)
                    action.ExecuteStartup();
                
                if(action.RunType == InternalActionRunType.ON_CALL || action.RunType == InternalActionRunType.BOTH)
                    Actions.Add(action);

                OnActionLoaded?.Invoke(result);

                break;
            case PluginType.COMMAND:
                Commands.Add((DBCommand)result.Plugin);
                OnCommandLoaded?.Invoke(result);
                break;
            case PluginType.EVENT:
                if (this.TryStartEvent((DBEvent)result.Plugin))
                {
                    Events.Add((DBEvent)result.Plugin);
                    OnEventLoaded?.Invoke(result);
                }

                break;
            case PluginType.SLASH_COMMAND:
                if (await this.TryStartSlashCommand((DBSlashCommand)result.Plugin))
                {
                    if(((DBSlashCommand)result.Plugin).HasInteraction)
                        _Client.InteractionCreated += ((DBSlashCommand)result.Plugin).ExecuteInteraction;
                    SlashCommands.Add((DBSlashCommand)result.Plugin);
                    OnSlashCommandLoaded?.Invoke(result);
                } 
                else 
                    Application.CurrentApplication.Logger.Log($"Failed to start slash command {result.PluginName}", this, LogType.ERROR);
                break;
            case PluginType.UNKNOWN:
            default:
                Application.CurrentApplication.Logger.Log("Unknown plugin type", this, LogType.ERROR);
                break;
        }
    }
}
