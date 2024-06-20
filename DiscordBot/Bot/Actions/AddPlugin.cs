using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public string Usage => "add-plugin <path>";

        public IEnumerable<InternalActionOption> ListOfOptions => [
            new InternalActionOption("fileName", "The file name")
        ];

        public InternalActionRunType RunType => InternalActionRunType.ON_CALL;

        public async Task Execute(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Invalid arguments given. Please use the following format:");
                Console.WriteLine("add-plugin <fileName>");
                Console.WriteLine("fileName: The file name");

                return;
            }

            string fileName = args[0] + ".dll";
            var path = Application.GetPluginFullPath(fileName);

            if(!System.IO.File.Exists(path))
            {
                Console.WriteLine("The file does not exist !!");
                return;
            }

            FileInfo fileInfo = new FileInfo(path);
            PluginInfo pluginInfo = new PluginInfo(args[0], new(1, 0, 0), [], false, true);
            await Application.CurrentApplication.PluginManager.AppendPluginToDatabase(pluginInfo);
        }
    }
}
