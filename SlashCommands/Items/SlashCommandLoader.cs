using Discord.WebSocket;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SlashCommands.Items
{
    public class SlashCommandLoader
    {
        internal DiscordSocketClient client;
        
        internal delegate void FileLoadedEventHandler(string[] args);

        internal delegate void PluginLoadedEventHandler(string[] args);

        internal event FileLoadedEventHandler? FileLoaded;

        internal event PluginLoadedEventHandler? PluginLoaded;

        private string location, extension;
        internal SlashCommandLoader(string location, string extension, DiscordSocketClient client)
        {
            this.location = location;
            this.extension = extension;
            this.client = client;
        }
        
        internal async Task<List<DBSlashCommand>> Load()
        {
            List<DBSlashCommand> slashCommands = new();
            var files = Directory.GetFiles(location, $"*.{extension}", SearchOption.AllDirectories);
            foreach(var file in files)
            {
                Assembly.LoadFrom(file);
                if(FileLoaded != null)
                {
                    var args = new string[] { file, "Loaded" };
                    FileLoaded.Invoke(args);
                }
            }

            try
            {
                var interfaceType = typeof(DBSlashCommand);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                                     .SelectMany(a => a.GetTypes())
                                     .Where(p => interfaceType.IsAssignableFrom(p) && p.IsClass)
                                     .ToArray();

                foreach(var type in types)
                {
                    try
                    {
                        var plugin = (DBSlashCommand)Activator.CreateInstance(type);
                        slashCommands.Add(plugin);
                        if (PluginLoaded != null)
                        {
                            var args = new string[] { plugin.Command, "Loaded successfully" };
                            PluginLoaded.Invoke(args);

                            await plugin.InitializeCommand(client);
                        }
                    }
                    catch {
                        var args = new string[] { type.Name, "Failed to load" };
                        PluginLoaded!.Invoke(args);
                    }
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return slashCommands;
        }

        
    }
}
