namespace DiscordBotWebUI.StartupActions;

internal interface IStartupAction
{
    string Command { get; }
    void RunAction(string[] args);
}