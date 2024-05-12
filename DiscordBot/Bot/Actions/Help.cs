using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Utilities;

using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;
using Spectre.Console;

namespace DiscordBot.Bot.Actions;

public class Help: ICommandAction
{
    public string ActionName => "help";

    public string Description => "Shows the list of commands and their usage";

    public string Usage => "help <command?>";

    public IEnumerable<InternalActionOption> ListOfOptions => [];

    public InternalActionRunType RunType => InternalActionRunType.ON_CALL;

    public async Task Execute(string[] args)
    {
        TableData tableData = new TableData();
        if (args == null || args.Length == 0)
        {

            tableData.Columns = ["Command", "Usage", "Description", "Options"];

            foreach (var a in Application.CurrentApplication.InternalActionManager.Actions)
            {
                Markup actionName = new Markup($"[bold]{a.Key}[/]");
                Markup usage = new Markup($"[italic]{a.Value.Usage}[/]");
                Markup description = new Markup($"[dim]{a.Value.Description}[/]");

                if (a.Value.ListOfOptions.Any())
                {

                    var optionsTable = new Table();
                    optionsTable.AddColumn("Option");
                    optionsTable.AddColumn("Description");

                    foreach (var option in a.Value.ListOfOptions)
                    {

                        optionsTable.AddRow(option.OptionName, option.OptionDescription);
                    }

                    tableData.AddRow([actionName, usage, description, optionsTable]);
                }
                else
                {
                    tableData.AddRow([actionName, usage, description]);
                }


            }

            // render the table
            tableData.HasRoundBorders = true;
            tableData.DisplayLinesBetweenRows = true;
            tableData.PrintTable();


            return;
        }

        if (!Application.CurrentApplication.InternalActionManager.Actions.ContainsKey(args[0]))
        {
            Console.WriteLine("Command not found");
            return;
        }

        var action = Application.CurrentApplication.InternalActionManager.Actions[args[0]];
        tableData.Columns = ["Command", "Usage", "Description"];
        tableData.AddRow([action.ActionName, action.Usage, action.Description]);


        tableData.PrintTable();
    }
}
