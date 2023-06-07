using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PluginManager.Others;
using PluginManager.Interfaces;

namespace DiscordBot.Discord.Actions
{
    public class Help : ICommandAction
    {
        public string ActionName => "help";

        public string Description => "Shows the list of commands and their usage";

        public string Usage => "help [command]";

        public InternalActionRunType RunType => InternalActionRunType.ON_CALL;

        public async Task Execute(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                var items = new List<string[]>
                            {
                                new[] { "-", "-", "-" },
                                new[] { "Command", "Usage", "Description" },
                                new[] { "-", "-", "-" }
                            };

                foreach (var a in Program.internalActionManager.Actions)
                {
                    items.Add(new[] { a.Key, a.Value.Usage, a.Value.Description });
                }

                items.Add(new[] { "-", "-", "-" });

                DiscordBot.Utilities.Utilities.FormatAndAlignTable(items, Utilities.TableFormat.CENTER_EACH_COLUMN_BASED);
                return;
            }

            if (!Program.internalActionManager.Actions.ContainsKey(args[0]))
            {
                Console.WriteLine("Command not found");
                return;
            }

            var action = Program.internalActionManager.Actions[args[0]];
            var actionData = new List<string[]>
                        {
                            new[] { "-", "-", "-" },
                            new[] { "Command", "Usage", "Description" },
                            new[] { "-", "-", "-"},
                            new[] { action.ActionName, action.Usage, action.Description },
                            new[] { "-", "-", "-" }
                        };

            DiscordBot.Utilities.Utilities.FormatAndAlignTable(actionData, Utilities.TableFormat.CENTER_EACH_COLUMN_BASED);
        }
    }
}