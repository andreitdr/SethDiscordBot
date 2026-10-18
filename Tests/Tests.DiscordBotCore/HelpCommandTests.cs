using DiscordBotCore.Commands;
using DiscordBotCore.PluginCore.Interfaces;

namespace Tests.DiscordBotCore;

public class HelpCommandTests
{
    [Fact]
    public void HelpCommand_DescribesItself()
    {
        var help = new HelpCommand();

        Assert.Equal("help", help.Command);
        Assert.Empty(help.Aliases);
        Assert.False(help.RequireAdmin);
        Assert.Equal("help <command>", help.Usage);
    }

    [Fact]
    public void GenerateCommandList_IsNullWithoutCommands()
    {
        Assert.Null(HelpCommand.GenerateCommandList([]));
    }

    [Fact]
    public void GenerateCommandList_SeparatesAdminCommands()
    {
        IDbCommand[] commands = [new TestCommand("level"), new TestCommand("ban", true), new TestCommand("help"), new TestCommand("kick", true)];

        var embed = HelpCommand.GenerateCommandList(commands)!.Build();

        Assert.Equal(2, embed.Fields.Length);
        Assert.Equal("Admin Commands", embed.Fields[0].Name);
        Assert.Equal("ban kick", embed.Fields[0].Value.Trim());
        Assert.Equal("Normal Commands", embed.Fields[1].Name);
        Assert.Equal("level help", embed.Fields[1].Value.Trim());
    }

    [Fact]
    public void GenerateCommandList_OmitsAnEmptySection()
    {
        var embed = HelpCommand.GenerateCommandList([new TestCommand("level")])!.Build();

        Assert.Equal("Normal Commands", Assert.Single(embed.Fields).Name);
    }

    [Fact]
    public void GenerateHelpCommand_ShowsUsageAndAliases()
    {
        var embed = HelpCommand.GenerateHelpCommand(new TestCommand("level", false, "lvl", "xp")).Build();

        Assert.Equal("Command: level", embed.Title);
        Assert.Equal("Does level", embed.Description);
        Assert.Equal("level <x>", embed.Fields.Single(f => f.Name == "Usage").Value);
        Assert.Equal("lvl xp", embed.Fields.Single(f => f.Name == "Aliases").Value.Trim());
    }

    [Fact]
    public void GenerateHelpCommand_SaysNoneWithoutAliases()
    {
        var embed = HelpCommand.GenerateHelpCommand(new TestCommand("level")).Build();

        Assert.Equal("None", embed.Fields.Single(f => f.Name == "Aliases").Value);
    }
}
