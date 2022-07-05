using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;

public class Random : DBCommand
{
    public string Command => "random";

    public string Description => "random number between number1 and number2";

    public string Usage => "random [number1] [number2]";

    public bool canUseDM     => true;
    public bool canUseServer => true;
    public bool requireAdmin => false;

    public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        try
        {
            var msg = message.Content;
            var a   = int.Parse(msg.Split(' ')[1]);
            var b   = int.Parse(msg.Split(' ')[2]);

            if (a > b)
            {
                var temp = a;
                a        = b;
                b        = temp;
            }

            await message.Channel.SendMessageAsync("Your random generated number is " + new System.Random().Next(a, b));
        }
        catch
        {
            await message.Channel.SendMessageAsync("Invalid numbers or no numbers:\nUsage: " + Usage);
        }
    }
}
