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
using Spectre.Console.Rendering;

namespace DiscordBot.Bot.Actions;

public class Help: ICommandAction
{
    public string ActionName => "help";

    public string Description => "Shows the list of commands and their usage";

    public string Usage => "help <command?>";

    public IEnumerable<InternalActionOption> ListOfOptions => [
        new InternalActionOption("command", "The command to get help for")
        ];

    public InternalActionRunType RunType => InternalActionRunType.OnCall;
    
    public bool RequireOtherThread => false;

    public async Task Execute(string[] args)
    {
        TableData tableData = new TableData();
        
        if (args == null || args.Length == 0)
        {

            AnsiConsole.MarkupLine("[bold][green]Please make this window full screen to check all the commands.[/][/]");
         
            tableData.Columns = ["Command", "Usage", "Description", "Options"];

            foreach (var a in Application.CurrentApplication.InternalActionManager.GetActions())
            {
                Markup actionName = new Markup($"[bold]{a.ActionName}[/]");
                Markup usage = new Markup($"[italic]{a.Usage}[/]");
                Markup description = new Markup($"[dim]{a.Description}[/]");

                if (a.ListOfOptions.Any())
                {
                    tableData.AddRow([actionName, usage, description, CreateTableWithSubOptions(a.ListOfOptions)]);
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

        if (!Application.CurrentApplication.InternalActionManager.Exists(args[0]))
        {
            Console.WriteLine("Command not found");
            return;
        }

        var action = Application.CurrentApplication.InternalActionManager.GetAction(args[0]);
        tableData.Columns = ["Command", "Usage", "Description"];
        tableData.AddRow([action.ActionName, action.Usage, action.Description]);


        tableData.PrintTable();
    }

    private Table CreateTableWithSubOptions(IEnumerable<InternalActionOption> options)
    {
        var tableData = new TableData();
        tableData.Columns = ["Option", "Description", "SubOptions"];

        foreach (var option in options)
        {

            Markup optionName = new Markup($"{option.OptionName}");
            Markup description = new Markup($"{option.OptionDescription}");

            if(option.SubOptions.Any())
            {
                tableData.AddRow([optionName, description, CreateTableWithSubOptions(option.SubOptions)]);

            }else {
                tableData.AddRow([optionName, description]);
            }

        }

        return tableData.AsTable();
    }
}
