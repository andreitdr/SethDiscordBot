using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using PluginManager.Interfaces;
using PluginManager.Online;
using PluginManager.Online.Updates;
using PluginManager.Others;

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
                if (!Config.PluginVersionsContainsKey(name))
                    Config.SetPluginVersion(
                        name, (version.PackageVersionID + ".0.0"));

                if (await PluginUpdater.CheckForUpdates(name))
                    await PluginUpdater.Download(name);
            });



        //Save the new config file (after the updates)
        await Config.SaveConfig(SaveType.NORMAL);


        //Load all plugins

        Commands = new List<DBCommand>();
        Events = new List<DBEvent>();
        SlashCommands = new List<DBSlashCommand>();

        Functions.WriteLogFile("Starting plugin loader ... Client: " + _client.CurrentUser.Username);
        Console.WriteLine("Loading plugins");

        /*        var commandsLoader = new Loader<DBCommand>(pluginCMDFolder, pluginCMDExtension);
                var eventsLoader = new Loader<DBEvent>(pluginEVEFolder, pluginEVEExtension);
                var slashLoader = new Loader<DBSlashCommand>("./Data/Plugins/SlashCommands/", "dll");

                commandsLoader.FileLoaded += OnCommandFileLoaded;
                commandsLoader.PluginLoaded += OnCommandLoaded;

                eventsLoader.FileLoaded += EventFileLoaded;
                eventsLoader.PluginLoaded += OnEventLoaded;

                slashLoader.FileLoaded += SlashLoader_FileLoaded;
                slashLoader.PluginLoaded += SlashLoader_PluginLoaded;

                Commands = commandsLoader.Load();
                Events = eventsLoader.Load();
                SlashCommands = slashLoader.Load();*/

        var loader = new LoaderV2("./Data/Plugins", "dll");
        loader.FileLoaded += (args) => Functions.WriteLogFile($"{args.PluginName} file Loaded");
        loader.PluginLoaded += Loader_PluginLoaded;
        var res = loader.Load();
        Events = res.Item1;
        Commands = res.Item2;
        SlashCommands = res.Item3;
    }

    private async void Loader_PluginLoaded(LoaderArgs args)
    {
        // Console.WriteLine(args.TypeName);
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
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("Plugin: " + args.PluginName);
                    Console.WriteLine("Type: " + args.TypeName);
                    Console.WriteLine("IsLoaded: " + args.IsLoaded);
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
                    //Console.WriteLine("Loaded " + slash.Name);
                    onSLSHLoad?.Invoke(((DBSlashCommand)args.Plugin!).Name, args.TypeName, args.IsLoaded, args.Exception);
                    await _client.CreateGlobalApplicationCommandAsync(builder.Build());


                }
                //else Console.WriteLine("Failed to load " + args.PluginName + "\nException: " + args.Exception.Message);
                break;
        }
    }
    /*
        private async void SlashLoader_PluginLoaded(LoaderArgs args)
        {
            if (args.IsLoaded)
            {
                var slash = (DBSlashCommand)args.Plugin;
                SlashCommandBuilder builder = new SlashCommandBuilder();
                builder.WithName(slash.Name);
                builder.WithDescription(slash.Description);
                builder.WithDMPermission(slash.canUseDM);
                builder.Options = slash.Options;
                Console.WriteLine("Loaded " + slash.Name);
                await _client.CreateGlobalApplicationCommandAsync(builder.Build());


            }
            else Console.WriteLine("Failed to load " + args.PluginName + "\nException: " + args.Exception.Message);
        }

        private void SlashLoader_FileLoaded(LoaderArgs args)
        {
            if (!args.IsLoaded)
                Functions.WriteLogFile($"[SLASH] SlashCommand from file [{args.PluginName}] has been successfully created !");
        }

        private void EventFileLoaded(LoaderArgs e)
        {
            if (!e.IsLoaded)
                Functions.WriteLogFile($"[EVENT] Event from file [{e.PluginName}] has been successfully created !");
        }

        private void OnCommandFileLoaded(LoaderArgs e)
        {
            if (!e.IsLoaded)
                Functions.WriteLogFile($"[CMD] Command from file [{e.PluginName}] has been successfully loaded !");
        }

        private void OnEventLoaded(LoaderArgs e)
        {
            try
            {
                if (e.IsLoaded)
                    ((DBEvent)e.Plugin!).Start(_client);

                onEVELoad?.Invoke(((DBEvent)e.Plugin!).Name, e.TypeName!, e.IsLoaded, e.Exception);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Plugin: " + e.PluginName);
                Console.WriteLine("Type: " + e.TypeName);
                Console.WriteLine("IsLoaded: " + e.IsLoaded);
            }
        }

        private void OnCommandLoaded(LoaderArgs e)
        {
            onCMDLoad?.Invoke(((DBCommand)e.Plugin!).Command, e.TypeName!, e.IsLoaded, e.Exception);
        }*/
}