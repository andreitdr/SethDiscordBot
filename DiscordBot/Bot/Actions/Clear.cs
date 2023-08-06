using System;
using System.Threading.Tasks;
using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Others;

namespace DiscordBot.Bot.Actions;

public class Clear : ICommandAction
{
    public string                ActionName  => "clear";
    public string                Description => "Clears the console";
    public string                Usage       => "clear";
    public InternalActionRunType RunType     => InternalActionRunType.ON_CALL;

    public Task Execute(string[] args)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("===== Seth Discord Bot =====");
        Console.ResetColor();
        return Task.CompletedTask;
    }
}
