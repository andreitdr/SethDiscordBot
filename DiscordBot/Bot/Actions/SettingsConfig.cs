using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Bot.Actions.Extra;
using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;

namespace DiscordBot.Bot.Actions;

public class SettingsConfig: ICommandAction
{
    public string ActionName => "config";
    public string Description => "Change the settings of the bot";
    public string Usage => "config <options!>";

    public IEnumerable<InternalActionOption> ListOfOptions => new List<InternalActionOption>
    {
        new InternalActionOption("help", "Displays this message"),
        new InternalActionOption("set", "Set a setting"),
        new InternalActionOption("remove", "Remove a setting"),
        new InternalActionOption("add", "Add a setting")
    };

    public InternalActionRunType RunType => InternalActionRunType.OnCall;

    public bool RequireOtherThread => false;

    public Task Execute(string[] args)
    {
        if (args is null)
        {
            PrintAllSettings();

            return Task.CompletedTask;
        }

        switch (args[0])
        {
            case "-s":
            case "set":
                if (args.Length < 3)
                    return Task.CompletedTask;
                SettingsConfigExtra.SetSettings(args[1], args[2..]);
                break;

            case "-r":
            case "remove":
                if (args.Length < 2)
                    return Task.CompletedTask;
                SettingsConfigExtra.RemoveSettings(args[1]);
                break;

            case "-a":
            case "add":
                if (args.Length < 3)
                    return Task.CompletedTask;
                SettingsConfigExtra.AddSettings(args[1], args[2..]);
                break;

            case "-h":
            case "help":
                Console.WriteLine("Options:");
                Console.WriteLine("-s <settingName> <newValue>: Set a setting");
                Console.WriteLine("-r <settingName>: Remove a setting");
                Console.WriteLine("-a <settingName> <newValue>: Add a setting");
                Console.WriteLine("-h: Show this help message");
                break;

            default:
                Console.WriteLine("Invalid option");
                return Task.CompletedTask;
        }



        return Task.CompletedTask;
    }
    private void PrintList(IList<object> list, int indentLevel)
    {
        bool isListOfDictionaries = list.All(item => item is IDictionary<string, object>);

        if (isListOfDictionaries)
        {
            foreach (var item in list)
            {
                if (item is IDictionary<string, object> dict)
                {
                    PrintDictionary(dict, indentLevel + 1);
                }
            }
        }
        else
        {
            PrintIndent(indentLevel);
            Console.WriteLine(string.Join(",", list));
        }
    }

    private void PrintDictionary(IDictionary<string, object> dictionary, int indentLevel)
    {
        foreach (var kvp in dictionary)
        {
            PrintIndent(indentLevel);
            Console.Write(kvp.Key + ": ");

            var value = kvp.Value;
            if (value is IDictionary<string, object> dict)
            {
                Console.WriteLine();
                PrintDictionary(dict, indentLevel + 1);
            }
            else if (value is IList<object> list)
            {
                if (list.All(item => item is IDictionary<string, object>))
                {
                    Console.WriteLine();
                    PrintList(list, indentLevel + 1);
                }
                else
                {
                    PrintList(list, indentLevel);
                }
            }
            else
            {
                Console.WriteLine(value);
            }
        }
    }

    private void PrintIndent(int indentLevel)
    {
        for (int i = 0; i < indentLevel; i++)
        {
            Console.Write("  "); // Two spaces for each indentation level
        }
    }

    private void PrintAllSettings()
    {
        var settings = Application.CurrentApplication.ApplicationEnvironmentVariables;
        foreach (var setting in settings)
        {
            Console.WriteLine("Setting: " + setting.Key);
            if (setting.Value is IDictionary<string, object> dict)
            {
                PrintDictionary(dict, 1);
            }
            else if (setting.Value is IList<object> list)
            {
                PrintList(list, 1);
            }
            else
            {
                Console.WriteLine(setting.Value);
            }
        }
    }
}
