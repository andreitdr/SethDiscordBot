using Discord.WebSocket;

using PluginManager.Interfaces;
using PluginManager.Others;

using System;
using System.Collections.Generic;
namespace PluginManager.Loaders
{
    public class PluginLoader
    {
        private DiscordSocketClient client;

        /// <summary>
        /// The Plugin Loader constructor
        /// </summary>
        /// <param name="discordSocketClient">The discord bot client where the plugins will pe attached to</param>
        public PluginLoader(DiscordSocketClient discordSocketClient)
        {
            this.client = discordSocketClient;
        }

        private const string pluginCMDFolder = @"./Data/Plugins/Commands/";
        private const string pluginEVEFolder = @"./Data/Plugins/Events/";

        private const string pluginCMDExtension = ".dll";
        private const string pluginEVEExtension = ".dll";

        /// <summary>
        /// A list of <see cref="DBCommand"/> commands
        /// </summary>
        public static List<DBCommand>? Plugins { get; set; }

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

            Plugins = new List<DBCommand>();
            Events = new List<DBEvent>();

            Functions.WriteLogFile("Starting plugin loader...");
            if (LanguageSystem.Language.ActiveLanguage != null)
                Console_Utilities.WriteColorText(
                    LanguageSystem.Language.ActiveLanguage.FormatText(
                        LanguageSystem.Language.ActiveLanguage.LanguageWords["PLUGIN_LOADING_START"]
                    )
                );

            //Load commands
            CommandsLoader CMDLoader = new CommandsLoader(pluginCMDFolder, pluginCMDExtension);
            CMDLoader.OnCommandLoaded += OnCommandLoaded!;
            CMDLoader.OnCommandFileLoaded += OnCommandFileLoaded;
            Plugins = CMDLoader.LoadCommands();


            //Load Events
            EventsLoader EVLoader = new EventsLoader(pluginEVEFolder, pluginEVEExtension);
            EVLoader.EventLoad += OnEventLoaded!;
            EVLoader.EventFileLoaded += EventFileLoaded;
            Events = EVLoader.LoadEvents();

        }

        private void EventFileLoaded(string path)
        {
            if (path != null)
                Functions.WriteLogFile($"[EVENT] Event from file [{path}] has been successfully created !");
        }

        private void OnCommandFileLoaded(string path)
        {
            if (path != null)
                Functions.WriteLogFile($"[CMD] Command from file [{path}] has been successfully loaded !");
        }

        private void OnEventLoaded(string typename, bool success, DBEvent eve, Exception exception)
        {
            if (eve != null && success)
                eve.Start(client);
            if (onEVELoad != null)
                onEVELoad.Invoke(eve!.name, typename, success, exception);
        }

        private void OnCommandLoaded(string name, bool success, DBCommand command, Exception exception)
        {
            if (onCMDLoad != null)
                onCMDLoad.Invoke(command.Command, name, success, exception);
        }
    }
}
