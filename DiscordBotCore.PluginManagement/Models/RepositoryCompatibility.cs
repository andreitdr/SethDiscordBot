namespace DiscordBotCore.PluginManagement.Models;

public enum RepositoryCompatibility
{
    Compatible = 0,

    /// <summary>The repository needs a newer bot.</summary>
    BotTooOld,

    /// <summary>The repository is too old for this bot, or the BaseUrl doesn't point at a plugin repository.</summary>
    ServerTooOld,

    /// <summary>The repository couldn't be asked, for example because it is down. Says nothing about versions.</summary>
    Unreachable
}

public static class RepositoryCompatibilityExtensions
{
    public static string Describe(this RepositoryCompatibility compatibility) => compatibility switch
    {
        RepositoryCompatibility.BotTooOld =>
            "This plugin repository needs a newer version of the bot. Update the bot, or change RemotePluginConnectionConfigurationDetails.BaseUrl in config.json to another repository.",
        RepositoryCompatibility.ServerTooOld =>
            "This plugin repository is too old for this version of the bot, or the address isn't a plugin repository. Change RemotePluginConnectionConfigurationDetails.BaseUrl in config.json, or update the repository.",
        RepositoryCompatibility.Unreachable =>
            "The plugin repository couldn't be reached. Check that it is running and that RemotePluginConnectionConfigurationDetails.BaseUrl in config.json is correct.",
        _ => ""
    };
}
