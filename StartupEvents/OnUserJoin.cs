using PluginManager.Others;
using PluginManager.Interfaces;

public class OnUserJoin : DBEvent
{
    public string name => "OnPlayerJoin";

    public string description => "An event that is triggered when an user joins the server";

    public async void Start(Discord.WebSocket.DiscordSocketClient client)
    {

        string UtilsPath = Functions.dataFolder + "StartupEvents/";
        string ConfigFile = UtilsPath + "UserJoinEvent.txt";

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
            "MessageFooter=Today: {time.date} at {time.time}");
        }

        if (Functions.readCodeFromFile(ConfigFile, "Enabled", '=') != "True") return;

        client.UserJoined += async (user) =>
        {
            Console_Utilities.WriteColorText("da");
            Discord.EmbedBuilder embed = new Discord.EmbedBuilder
            {
                Title = Functions.readCodeFromFile(ConfigFile, "MessageTitle", '='),
                Description = Functions.readCodeFromFile(ConfigFile, "MessageDescription", '=')
            };

            embed
        .AddField(Functions.readCodeFromFile(ConfigFile, "MessageField1Title", '=').Replace("{user.Name}", user.Username).Replace("{time.date}", System.DateTime.Now.ToShortDateString()).Replace("{time.time}", System.DateTime.Now.ToShortTimeString()), Functions.readCodeFromFile(ConfigFile, "MessageField1Text", '=').Replace("{user.Name}", user.Username).Replace("{time.date}", System.DateTime.Now.ToShortDateString()).Replace("{time.time}", System.DateTime.Now.ToShortTimeString()))
        .AddField(Functions.readCodeFromFile(ConfigFile, "MessageField2Title", '=').Replace("{user.Name}", user.Username).Replace("{time.date}", System.DateTime.Now.ToShortDateString()).Replace("{time.time}", System.DateTime.Now.ToShortTimeString()), Functions.readCodeFromFile(ConfigFile, "MessageField2Text", '=').Replace("{user.Name}", user.Username).Replace("{time.date}", System.DateTime.Now.ToShortDateString()).Replace("{time.time}", System.DateTime.Now.ToShortTimeString()))
        .WithFooter(Functions.readCodeFromFile(ConfigFile, "MessageFooter", '=').Replace("{user.Name}", user.Username).Replace("{time.date}", System.DateTime.Now.ToShortDateString()).Replace("{time.time}", System.DateTime.Now.ToShortTimeString()));
            Console_Utilities.WriteColorText("da");
            //await user.Guild.DefaultChannel.SendMessageAsync(embed: embed.Build());
            await (await user.CreateDMChannelAsync()).SendMessageAsync(embed: embed.Build());
        };


    }
}