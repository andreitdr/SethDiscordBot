using Discord.WebSocket;
using PluginManager.Interfaces;
using PluginManager.Others;
using System;
using System.Collections.Generic;

namespace PluginManager.Loaders
{
    public class PluginLoader
    {
        private readonly DiscordSocketClient _client;

        /// <summary>
        /// The Plugin Loader constructor
        /// </summary>
        /// <param name="discordSocketClient">The discord bot client where the plugins will pe attached to</param>
        public PluginLoader(DiscordSocketClient discordSocketClient) { this._client = discordSocketClient; }

        private const string pluginCMDFolder = @"./Data/Plugins/Commands/";
        private const string pluginEVEFolder = @"./Data/Plugins/Events/";

        private const string pluginCMDExtension = "dll";
        private const string pluginEVEExtension = "dll";


        /// <summary>
        /// A list of <see cref="DBCommand"/> commands
        /// </summary>
        public static List<DBCommand>? Commands { get; set; }

        /// <summary>
        /// A list of <see cref="DBEvent"/> commands
        /// </summary>
        public static List<DBEvent>? Events { get; set; }


        public delegate void CMDLoaded(string name, string typeName, bool success, Exception? e = null);

        public delegate void EVELoaded(string name, string typeName, bool success, Exception? e = null);

        /// <summary>
        /// Event that is fired when a <see cref="DBCommand"/> is successfully loaded into commands list
        /// </summary>
        public CMDLoaded? onCMDLoad;

        /// <summary>
        /// Event that is fired when a <see cref="DBEvent"/> is successfully loaded into events list
        /// </summary>
        public EVELoaded? onEVELoad;

        /// <summary>
        /// The main mathod that is called to load all events
        /// </summary>
        public void LoadPlugins()
        {
            Commands = new List<DBCommand>();
            Events   = new List<DBEvent>();

            Functions.WriteLogFile("Starting plugin loader ... Client: " + _client.CurrentUser.Username);
            Console.WriteLine("Loading plugins");

            Loader<DBCommand> commandsLoader = new Loader<DBCommand>(pluginCMDFolder, pluginCMDExtension);
            Loader<DBEvent>   eventsLoader   = new Loader<DBEvent>(pluginEVEFolder, pluginEVEExtension);

            commandsLoader.FileLoaded   += OnCommandFileLoaded;
            commandsLoader.PluginLoaded += OnCommandLoaded;

            eventsLoader.FileLoaded   += EventFileLoaded;
            eventsLoader.PluginLoaded += OnEventLoaded;

            Commands = commandsLoader.Load();
            Events   = eventsLoader.Load();
        }

        private void EventFileLoaded(LoaderArgs e)
        {
            if (e.IsLoaded) Functions.WriteLogFile($"[EVENT] Event from file [{e.PluginName}] has been successfully created !");
        }

        private void OnCommandFileLoaded(LoaderArgs e)
        {
            if (e.IsLoaded) Functions.WriteLogFile($"[CMD] Command from file [{e.PluginName}] has been successfully loaded !");
        }

        private void OnEventLoaded(LoaderArgs e)
        {
            if (e.IsLoaded) { ((DBEvent)e.Plugin!).Start(_client); }

            if (onEVELoad != null) onEVELoad.Invoke(((DBEvent)e.Plugin!).name, e.TypeName!, e.IsLoaded, e.Exception);
        }

        private void OnCommandLoaded(LoaderArgs e)
        {
            if (onCMDLoad != null) onCMDLoad.Invoke(((DBCommand)e.Plugin!).Command, e.TypeName!, e.IsLoaded, e.Exception);
        }
    }
}
