using DiscordBotCore.PluginCore.Helpers.Execution.DbCommand;
using DiscordBotCore.PluginCore.Interfaces;

namespace WebPagesDemo;

/// <summary>A plugin with web pages is still a normal plugin: it can have commands and events too.</summary>
public class DemoCommand : IDbCommand
{
    public string Command => "webdemo";

    public List<string> Aliases => [];

    public string Description => "Points to the Web pages demo in the WebUI";

    public string Usage => "webdemo";

    public bool RequireAdmin => false;

    public async Task ExecuteServer(IDbCommandExecutingArgument args)
    {
        await args.Context.Channel.SendMessageAsync("Open the bot's WebUI and choose Pages → Overview to see this plugin's web pages.");
    }
}
