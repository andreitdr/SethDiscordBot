using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Interfaces.Modules;
using DiscordBotCore.Modules;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;

namespace DiscordBot.Bot.Actions
{
    internal class Module : ICommandAction
    {
        public string ActionName => "module";

        public string Description => "Access module commands";

        public string Usage => "module <command>";

        public IEnumerable<InternalActionOption> ListOfOptions => [
            new InternalActionOption("list", "List all loaded modules")
        ];

        public InternalActionRunType RunType => InternalActionRunType.OnCall;
        
        public bool RequireOtherThread => false;

        public Task Execute(string[] args)
        {
            string command = args?[0];
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
                Application.Logger.Log("Module: " + module.Key.ModuleName, this, LogType.Info);

            }
        }
    }
}
