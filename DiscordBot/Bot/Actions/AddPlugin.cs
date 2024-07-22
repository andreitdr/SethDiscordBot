using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;
using DiscordBotCore.Plugin;

namespace DiscordBot.Bot.Actions
{
    internal class AddPlugin : ICommandAction
    {
        public string ActionName => "add-plugin";

        public string Description => "Add a local plugin to the database";

        public string Usage => "add-plugin <options> <fileName>";

        public IEnumerable<InternalActionOption> ListOfOptions => [
            new InternalActionOption("options", "Available options", [
                new InternalActionOption("-enabled", "Enable the plugin"),
            ]),
            new InternalActionOption("fileName", "The file name")
        ];

        public InternalActionRunType RunType => InternalActionRunType.OnCall;
        
        public bool RequireOtherThread => false;

        public async Task Execute(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Incorrect number of arguments !");
                return;
            }

            string fileName = args[^1] + ".dll";
            var path = Application.GetPluginFullPath(fileName);

            if(!File.Exists(path))
            {
                Console.WriteLine("The file does not exist !!");
                return;
            }
            
            PluginInfo pluginInfo = new PluginInfo(args[^1], new(1, 0, 0), [], false, true, args.Contains("-enabled"));
            Application.CurrentApplication.Logger.Log("Adding plugin: " + args[^1]);
            await Application.CurrentApplication.PluginManager.AppendPluginToDatabase(pluginInfo);
        }
    }
}
