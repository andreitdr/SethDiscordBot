using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using PluginManager.Interfaces;
using PluginManager.Others;

namespace PluginManager.Loaders;

public class PluginLoader
{
    internal readonly DiscordSocketClient _Client;

    public delegate void CommandLoaded(PluginLoadResultData resultData);

    public delegate void EventLoaded(PluginLoadResultData resultData);

    public delegate void SlashCommandLoaded(PluginLoadResultData resultData);

    public CommandLoaded?      OnCommandLoaded;
    public EventLoaded?        OnEventLoaded;
    public SlashCommandLoaded? OnSlashCommandLoaded;

    public static List<DBCommand>?      Commands;
    public static List<DBEvent>?        Events;
    public static List<DBSlashCommand>? SlashCommands;

    public PluginLoader(DiscordSocketClient discordSocketClient)
    {
        _Client = discordSocketClient;
    }

    public async Task LoadPlugins()
    {

        Commands      = new List<DBCommand>();
        Events        = new List<DBEvent>();
        SlashCommands = new List<DBSlashCommand>();

        Config.Logger.Log("Loading plugins...", typeof(PluginLoader));

        var loader = new Loader(Config.AppSettings["PluginFolder"], "dll");

        loader.OnFileLoadedException += FileLoadedException;
        loader.OnPluginLoaded        += OnPluginLoaded;

        await loader.Load();
    }

    private void FileLoadedException(FileLoaderResult result)
    {
        Config.Logger.Log(result.ErrorMessage, typeof(PluginLoader), LogType.ERROR);
    }

    private void OnPluginLoaded(PluginLoadResultData result)
    {
        switch (result.PluginType)
        {
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
                if (this.TryStartSlashCommand((DBSlashCommand)result.Plugin))
                {
                    SlashCommands.Add((DBSlashCommand)result.Plugin);
                    OnSlashCommandLoaded?.Invoke(result);
                }
                break;
            case PluginType.UNKNOWN:
            default:
                Config.Logger.Log("Unknown plugin type", typeof(PluginLoader), LogType.ERROR);
                break;
        }
    }


}
