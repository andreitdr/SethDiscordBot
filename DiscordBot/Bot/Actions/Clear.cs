using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;

namespace DiscordBot.Bot.Actions;

public class Clear: ICommandAction
{
    public string ActionName => "clear";
    public string Description => "Clears the console";
    public string Usage => "clear";
    public IEnumerable<InternalActionOption> ListOfOptions => [];

    public InternalActionRunType RunType => InternalActionRunType.OnCall;
    
    public bool RequireOtherThread => false;

    public Task Execute(string[] args)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("===== Seth Discord Bot =====");
        Console.ResetColor();
        return Task.CompletedTask;
    }
}
