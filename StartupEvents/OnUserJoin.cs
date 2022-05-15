using PluginManager.Others;
using PluginManager.Interfaces;
using Discord;

public class OnUserJoin : DBEvent
{
    public string name => "OnPlayerJoin";

    public string description => "An event that is triggered when an user joins the server";

    private string UtilsPath = Functions.dataFolder + "/StartupEvents";
    private string ConfigFile = Functions.dataFolder + "/StartupEvents/" + "UserJoinEvent.txt";

    public async void Start(Discord.WebSocket.DiscordSocketClient client)
    {

        System.IO.Directory.CreateDirectory(UtilsPath);

        if (!System.IO.File.Exists(ConfigFile))
        {
            await System.IO.File.WriteAllTextAsync(ConfigFile,
            "Enabled=True\nEmbed=True\n" +
            "#Available placeholders:\n" +
            "#{user.Name} => Username of the user\n" +
            "#{time.date} => Current Date\n" +
            "#{time.time} => Current time (hh:mm::ss)\n" +
            "MessageTitle = Welcome {user.Name}\n" +
            "MessageDescription=Embed description\n" +
            "MessageField1Title=Custom Title\n" +
            "MessageFiled1Text=Custom Filed 1 text\n" +
            "MessageField2Title=Custom Title\n" +
            "MessageFiled2Text=Custom Filed 2 text\n" +
            "MessageFooter=Today: {time.date} at {time.time}\n");
        }

        if (Functions.readCodeFromFile(ConfigFile, "Enabled", '=') != "True") return;
        //System.Console.WriteLine("Awaiting user join event ...");

        client.UserJoined += Client_UserJoined;

    }

    private async System.Threading.Tasks.Task Client_UserJoined(Discord.WebSocket.SocketGuildUser user)
    {
        Console_Utilities.WriteColorText("A new user joins: " + user.Username);
        EmbedBuilder embed = new EmbedBuilder
        {
            Title = Functions.readCodeFromFile(ConfigFile, "MessageTitle", '='),
            Description = Functions.readCodeFromFile(ConfigFile, "MessageDescription", '=')
        };
        embed
        .AddField(Functions.readCodeFromFile(ConfigFile, "MessageField1Title", '=').Replace("{user.Name}", user.Username).Replace("{time.date}", System.DateTime.Now.ToShortDateString()).Replace("{time.time}", System.DateTime.Now.ToShortTimeString()), Functions.readCodeFromFile(ConfigFile, "MessageField1Text", '=').Replace("{user.Name}", user.Username).Replace("{time.date}", System.DateTime.Now.ToShortDateString()).Replace("{time.time}", System.DateTime.Now.ToShortTimeString()))
        .AddField(Functions.readCodeFromFile(ConfigFile, "MessageField2Title", '=').Replace("{user.Name}", user.Username).Replace("{time.date}", System.DateTime.Now.ToShortDateString()).Replace("{time.time}", System.DateTime.Now.ToShortTimeString()), Functions.readCodeFromFile(ConfigFile, "MessageField2Text", '=').Replace("{user.Name}", user.Username).Replace("{time.date}", System.DateTime.Now.ToShortDateString()).Replace("{time.time}", System.DateTime.Now.ToShortTimeString()))
        .WithFooter(Functions.readCodeFromFile(ConfigFile, "MessageFooter", '=').Replace("{user.Name}", user.Username).Replace("{time.date}", System.DateTime.Now.ToShortDateString()).Replace("{time.time}", System.DateTime.Now.ToShortTimeString()));
        await user.Guild.DefaultChannel.SendMessageAsync(embed: embed.Build());
    }
}