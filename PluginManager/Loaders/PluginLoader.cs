using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using PluginManager.Interfaces;
using PluginManager.Online;
using PluginManager.Online.Updates;

namespace PluginManager.Loaders;

public class PluginLoader
{
    public delegate void CMDLoaded(string name, string typeName, bool success, Exception? e = null);

    public delegate void EVELoaded(string name, string typeName, bool success, Exception? e = null);

    public delegate void SLSHLoaded(string name, string tyypename, bool success, Exception? e = null);

    private const string pluginFolder = @"./Data/Plugins/";

    internal const string pluginExtension = "dll";
    private readonly DiscordSocketClient _client;

    /// <summary>
    ///     Event that is fired when a <see cref="DBCommand" /> is successfully loaded into commands list
    /// </summary>
    public CMDLoaded? onCMDLoad;

    /// <summary>
    ///     Event that is fired when a <see cref="DBEvent" /> is successfully loaded into events list
    /// </summary>
    public EVELoaded? onEVELoad;

    /// <summary>
    ///     Event that is fired when a <see cref="DBEvent" /> is successfully loaded into events list
    /// </summary>
    public SLSHLoaded? onSLSHLoad;

    /// <summary>
    ///     The Plugin Loader constructor
    /// </summary>
    /// <param name="discordSocketClient">The discord bot client where the plugins will pe attached to</param>
    public PluginLoader(DiscordSocketClient discordSocketClient)
    {
        _client = discordSocketClient;
    }


    /// <summary>
    ///     A list of <see cref="DBCommand" /> commands
    /// </summary>
    public static List<DBCommand>? Commands { get; set; }

    /// <summary>
    ///     A list of <see cref="DBEvent" /> commands
    /// </summary>
    public static List<DBEvent>? Events { get; set; }

    /// <summary>
    ///     A list of <see cref="DBSlashCommand"/> commands
    /// </summary>
    public static List<DBSlashCommand>? SlashCommands { get; set; }

    public static int PluginsLoaded { get => Commands!.Count + Events!.Count + SlashCommands!.Count;}

    /// <summary>
    ///     The main mathod that is called to load all events
    /// </summary>
    public async void LoadPlugins()
    {
        //Check for updates in commands
        foreach (var file in Directory.GetFiles("./Data/Plugins/", $"*.{pluginExtension}",
                                                SearchOption.AllDirectories))
            await Task.Run(async () =>
            {
                var name = new FileInfo(file).Name.Split('.')[0];
                var version = await ServerCom.GetVersionOfPackageFromWeb(name);
                if (version is null)
                    return;
                if (Config.Plugins[name] is not null)
                    Config.Plugins[name] = version.ToShortString();

                if (await PluginUpdater.CheckForUpdates(name))
                    await PluginUpdater.Download(name);
            });


        //Load all plugins

        Commands = new List<DBCommand>();
        Events = new List<DBEvent>();
        SlashCommands = new List<DBSlashCommand>();

        Logger.WriteLogFile("Starting plugin loader ... Client: " + _client.CurrentUser.Username);
        Logger.WriteLine("Loading plugins");

        var loader = new Loader("./Data/Plugins", "dll");
        loader.FileLoaded += (args) => Logger.WriteLogFile($"{args.PluginName} file Loaded");
        loader.PluginLoaded += Loader_PluginLoaded;
        var res = loader.Load();
        Events = res.Item1;
        Commands = res.Item2;
        SlashCommands = res.Item3;
    }

    private async void Loader_PluginLoaded(LoaderArgs args)
    {

        switch (args.TypeName)
        {
            case "DBCommand":
                onCMDLoad?.Invoke(((DBCommand)args.Plugin!).Command, args.TypeName!, args.IsLoaded, args.Exception);
                break;
            case "DBEvent":
                try
                {
                    if (args.IsLoaded)
                        ((DBEvent)args.Plugin!).Start(_client);

                    onEVELoad?.Invoke(((DBEvent)args.Plugin!).Name, args.TypeName!, args.IsLoaded, args.Exception);
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(ex.ToString());
                    Logger.WriteLine("Plugin: " + args.PluginName);
                    Logger.WriteLine("Type: " + args.TypeName);
                    Logger.WriteLine("IsLoaded: " + args.IsLoaded);
                }
                break;
            case "DBSlashCommand":
                if (args.IsLoaded)
                {
                    var slash = (DBSlashCommand)args.Plugin;
                    SlashCommandBuilder builder = new SlashCommandBuilder();
                    builder.WithName(slash.Name);
                    builder.WithDescription(slash.Description);
                    builder.WithDMPermission(slash.canUseDM);
                    builder.Options = slash.Options;

                    onSLSHLoad?.Invoke(((DBSlashCommand)args.Plugin!).Name, args.TypeName, args.IsLoaded, args.Exception);
                    await _client.CreateGlobalApplicationCommandAsync(builder.Build());


                }
                break;
        }
    }
    public static async Task LoadPluginFromAssembly(Assembly asmb, DiscordSocketClient client)
    {
        var types = asmb.GetTypes();
        foreach (var type in types)
            if (type.IsClass && typeof(DBEvent).IsAssignableFrom(type))
            {
                var instance = (DBEvent)Activator.CreateInstance(type);
                instance.Start(client);
                PluginLoader.Events.Add(instance);
                Logger.WriteLine($"[EVENT] Loaded external {type.FullName}!");
            }
            else if (type.IsClass && typeof(DBCommand).IsAssignableFrom(type))
            {
                var instance = (DBCommand)Activator.CreateInstance(type);
                PluginLoader.Commands.Add(instance);
                Logger.WriteLine($"[CMD] Instance: {type.FullName} loaded !");
            }
            else if (type.IsClass && typeof(DBSlashCommand).IsAssignableFrom(type))
            {
                var instance = (DBSlashCommand)Activator.CreateInstance(type);
                SlashCommandBuilder builder = new SlashCommandBuilder();
                builder.WithName(instance.Name);
                builder.WithDescription(instance.Description);
                builder.WithDMPermission(instance.canUseDM);
                builder.Options = instance.Options;

                await client.CreateGlobalApplicationCommandAsync(builder.Build());
                PluginLoader.SlashCommands.Add(instance);
                Logger.WriteLine($"[SLASH] Instance: {type.FullName} loaded !");

            }
    }
}