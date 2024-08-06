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

        if (_Client == null)
        {
            Application.Logger.Log("Discord client is null", this, LogType.Error);
            return;
        }

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

    private async void OnPluginLoaded(PluginLoadResultData result)
    {
        switch (result.PluginType)
        {
            case PluginType.ACTION:
                ICommandAction action = (ICommandAction)result.Plugin;
                if (action.RunType == InternalActionRunType.OnStartup || action.RunType == InternalActionRunType.OnStartupAndCall)
                    await Application.CurrentApplication.InternalActionManager.StartAction(action, null);
                
                if(action.RunType == InternalActionRunType.OnCall || action.RunType == InternalActionRunType.OnStartupAndCall)
                    Actions.Add(action);

                OnActionLoaded?.Invoke(result);

                break;
            case PluginType.COMMAND:
                Commands.Add((IDbCommand)result.Plugin);
                OnCommandLoaded?.Invoke(result);
                break;
            case PluginType.EVENT:
                if (this.TryStartEvent((IDbEvent)result.Plugin))
                {
                    Events.Add((IDbEvent)result.Plugin);
                    OnEventLoaded?.Invoke(result);
                }

                break;
            case PluginType.SLASH_COMMAND:
                if (await this.TryStartSlashCommand((IDbSlashCommand)result.Plugin))
                {
                    if(((IDbSlashCommand)result.Plugin).HasInteraction)
                        _Client.InteractionCreated += ((IDbSlashCommand)result.Plugin).ExecuteInteraction;
                    SlashCommands.Add((IDbSlashCommand)result.Plugin);
                    OnSlashCommandLoaded?.Invoke(result);
                } 
                else 
                    Application.Logger.Log($"Failed to start slash command {result.PluginName}", this, LogType.Error);
                break;
            case PluginType.UNKNOWN:
            default:
                Application.Logger.Log("Unknown plugin type", this, LogType.Error);
                break;
        }
    }
}
