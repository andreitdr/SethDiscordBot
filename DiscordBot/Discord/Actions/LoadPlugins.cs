using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PluginManager.Interfaces;
using PluginManager.Loaders;
using PluginManager.Others;

namespace DiscordBot.Discord.Actions
{
    public class LoadPlugins : ICommandAction
    {
        public string ActionName => "loadplugs";

        public string Description => "Loads all plugins";

        public string Usage => "loadplugs";

        private bool pluginsLoaded = false;

        public InternalActionRunType RunType => InternalActionRunType.ON_STARTUP;

        public async Task Execute(string[] args)
        {
            if (pluginsLoaded)
                return;
            var loader = new PluginLoader(PluginManager.Config.DiscordBot.client);
            var cc = Console.ForegroundColor;
            loader.onCMDLoad += (name, typeName, success, exception) =>
            {
                if (name == null || name.Length < 2)
                    name = typeName;
                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[CMD] Successfully loaded command : " + name);
                }

                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    if (exception is null)
                        Console.WriteLine("An error occured while loading: " + name);
                    else
                        Console.WriteLine("[CMD] Failed to load command : " + name + " because " + exception!.Message);
                }

                Console.ForegroundColor = cc;
            };
            loader.onEVELoad += (name, typeName, success, exception) =>
            {
                if (name == null || name.Length < 2)
                    name = typeName;

                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[EVENT] Successfully loaded event : " + name);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[EVENT] Failed to load event : " + name + " because " + exception!.Message);
                }

                Console.ForegroundColor = cc;
            };

            loader.onSLSHLoad += (name, typeName, success, exception) =>
            {
                if (name == null || name.Length < 2)
                    name = typeName;

                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[SLASH] Successfully loaded command : " + name);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[SLASH] Failed to load command : " + name + " because " + exception!.Message);
                }

                Console.ForegroundColor = cc;
            };

            loader.LoadPlugins();
            Console.ForegroundColor = cc;
            pluginsLoaded = true;
        }
    }
}