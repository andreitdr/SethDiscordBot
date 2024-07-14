using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;

namespace DiscordBot.Bot.Actions
{
    internal class Module : ICommandAction
    {
        public string ActionName => "module";

        public string Description => "Access module commands";

        public string Usage => "module <name>";

        public IEnumerable<InternalActionOption> ListOfOptions => [];

        public InternalActionRunType RunType => InternalActionRunType.ON_CALL;

        public Task Execute(string[] args)
        {
            string command = args[0];
            switch(command)
            {
                case "list":
                    ListLoadedModules();
                    break;
                default:
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        private void ListLoadedModules()
        {

            var modules = DiscordBotCore.Application.CurrentApplication.GetLoadedCoreModules();
            foreach (var module in modules)
            {
                Type moduleType = module.Key;
                List<object> moduleList = module.Value;

                Console.WriteLine($"Module Type: {moduleType.Name}");

                foreach (dynamic mod in moduleList)
                {
                   Console.WriteLine($"Module: {mod.Name}");
                }

            }
        }
    }
}
