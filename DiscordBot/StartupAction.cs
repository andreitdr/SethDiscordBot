using System;

namespace DiscordBot
{
    internal class StartupAction : IStartupAction
    {
        public string Command { get; init; }
        public Action<string[]> RunAction { get; init; }

        public StartupAction(string command, Action<string[]> runAction)
        { 
            this.Command = command;
            this.RunAction = runAction;
        }

        public StartupAction(string command, Action runAction)
        {
            this.Command = command;
            this.RunAction = (args) => runAction();
        }
    }
}
