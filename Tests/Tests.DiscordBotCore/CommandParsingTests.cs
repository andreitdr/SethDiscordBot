using DiscordBotCore.Bot;
using DiscordBotCore.PluginCore.Interfaces;

namespace Tests.DiscordBotCore;

public class CommandParsingTests
{
    private static readonly IDbCommand Level = new TestCommand("level", false, "lvl", "xp");
    private static readonly IDbCommand Ban = new TestCommand("ban", true);
    private static readonly IDbCommand[] Commands = [Level, Ban];

    [Theory]
    [InlineData(";level", "level")]
    [InlineData(";lvl", "lvl")]
    [InlineData(";xp", "xp")]
    [InlineData(";ban", "ban")]
    public void Prefix_FindsCommandsByNameOrAlias(string content, string used)
    {
        ParsedCommand? parsed = CommandHandler.ParseCommand(content, ";", null, Commands);

        Assert.NotNull(parsed);
        Assert.Equal(used, parsed.CommandUsed);
        Assert.Equal(content[1..], parsed.CleanContent);
        Assert.Null(parsed.Arguments);
    }

    [Fact]
    public void Prefix_SplitsTheArguments()
    {
        ParsedCommand? parsed = CommandHandler.ParseCommand(";level @user extra words", ";", null, Commands);

        Assert.NotNull(parsed);
        Assert.Same(Level, parsed.Command);
        Assert.Equal(["@user", "extra", "words"], parsed.Arguments!);
        Assert.Equal("level @user extra words", parsed.CleanContent);
    }

    [Fact]
    public void Prefix_CanBeLongerThanOneCharacter()
    {
        Assert.Same(Level, CommandHandler.ParseCommand("!!level", "!!", null, Commands)?.Command);
        Assert.Null(CommandHandler.ParseCommand("!level", "!!", null, Commands));
    }

    [Theory]
    [InlineData("level")]
    [InlineData("hello ;level")]
    [InlineData(";")]
    [InlineData("; level")]
    [InlineData(";unknown")]
    [InlineData(";Level")]
    [InlineData("")]
    public void MessagesThatAreNotKnownCommands_AreIgnored(string content)
    {
        Assert.Null(CommandHandler.ParseCommand(content, ";", null, Commands));
    }

    [Fact]
    public void EmptyPrefix_DoesNotTurnEveryMessageIntoACommand()
    {
        Assert.Null(CommandHandler.ParseCommand("level", "", null, Commands));
    }

    [Theory]
    [InlineData("<@123> level 5", 6)]
    [InlineData("<@!123> level 5", 7)]
    [InlineData("<@123>   level 5", 6)]
    public void Mention_UsesTheTextAfterTheMention(string content, int mentionEnd)
    {
        ParsedCommand? parsed = CommandHandler.ParseCommand(content, ";", mentionEnd, Commands);

        Assert.NotNull(parsed);
        Assert.Same(Level, parsed.Command);
        Assert.Equal("level 5", parsed.CleanContent);
        Assert.Equal(["5"], parsed.Arguments!);
    }

    [Fact]
    public void Mention_FindsAliases()
    {
        Assert.Equal("xp", CommandHandler.ParseCommand("<@123> xp", ";", 6, Commands)?.CommandUsed);
    }

    [Theory]
    [InlineData("<@123>")]
    [InlineData("<@123>   ")]
    public void BareMention_IsIgnored(string content)
    {
        Assert.Null(CommandHandler.ParseCommand(content, ";", 6, Commands));
    }

    [Fact]
    public void Mention_DoesNotNeedThePrefix()
    {
        Assert.Null(CommandHandler.ParseCommand("<@123> ;level", ";", 6, Commands));
    }

    [Fact]
    public void WithoutCommands_NothingMatches()
    {
        Assert.Null(CommandHandler.ParseCommand(";level", ";", null, []));
    }

    [Fact]
    public void FirstMatchingCommand_Wins()
    {
        var first = new TestCommand("dup");
        var second = new TestCommand("other", false, "dup");

        Assert.Same(first, CommandHandler.ParseCommand(";dup", ";", null, [first, second])?.Command);
    }
}
