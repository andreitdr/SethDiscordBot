using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using PluginManager.Interfaces;
using PluginManager.Others;

namespace DiscordBot.Discord.Actions
{
    public class ListPlugins : ICommandAction
    {
        public string ActionName => "listplugs";

        public string Description => "Lists all plugins";

        public string Usage => "listplugs";

        public InternalActionRunType RunType => InternalActionRunType.ON_CALL;

        public async Task Execute(string[] args)
        {
            
            var manager = new PluginManager.Online.PluginsManager(Program.URLs["PluginList"], Program.URLs["PluginVersions"]);
            if (manager == null)
            {
                Console.WriteLine("Plugin manager is null");
                return;
            }


            var data = await manager.GetAvailablePlugins();
            var items = new List<string[]>
                        {
                            new[] { "-", "-", "-", "-" },
                            new[] { "Name", "Type", "Description", "Required" },
                            new[] { "-", "-", "-", "-" }
                        };

            foreach (var plugin in data)
            {
                items.Add(new[] { plugin[0], plugin[1], plugin[2], plugin[3] });
            }

            items.Add(new[] { "-", "-", "-", "-" });

            DiscordBot.Utilities.Utilities.FormatAndAlignTable(items, Utilities.TableFormat.DEFAULT);
        }
    }
}