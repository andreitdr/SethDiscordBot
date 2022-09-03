using System.Collections.Generic;

using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;

public class Random : DBCommand
{
    public string Command => "random";

    public List<string> Aliases => new() { "rnd" };

    public string Description => "random number between number1 and number2";

    public string Usage => "random [number1] [number2]";
    public bool requireAdmin => false;

    public async void ExecuteDM(SocketCommandContext context) => ExecuteServer(context);

    public async void ExecuteServer(SocketCommandContext context)
    {
        try
        {
            var msg = context.Message.Content;
            var a = int.Parse(msg.Split(' ')[1]);
            var b = int.Parse(msg.Split(' ')[2]);

            if (a > b)
            {
                var temp = a;
                a = b;
                b = temp;
            }

            await context.Message.Channel.SendMessageAsync("Your random generated number is " + new System.Random().Next(a, b));
        }
        catch
        {
            await context.Message.Channel.SendMessageAsync("Invalid numbers or no numbers:\nUsage: " + Usage);
        }
    }
}
