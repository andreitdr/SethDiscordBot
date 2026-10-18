using Discord;
using Discord.WebSocket;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Helpers.Execution.DbCommand;
using DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;
using DiscordBotCore.PluginCore.Interfaces;
using Moq;

namespace Tests.DiscordBotCore.PluginCore;

/// <summary>
/// What a plugin gets without implementing the optional members. Existing plugins rely on these defaults, so changing
/// one is a breaking change of the plugin contract.
/// </summary>
public class ContractDefaultsTests
{
    private sealed class MinimalCommand : IDbCommand
    {
        public string Command => "minimal";
        public List<string> Aliases => [];
        public string Description => "";
        public string Usage => "";
        public bool RequireAdmin => false;
    }

    private sealed class MinimalEvent : IDbEvent
    {
        public string Name => "minimal";
        public string Description => "";
        public int Starts { get; private set; }

        public void Start(IDbEventExecutingArgument args) => Starts++;
    }

    private sealed class MinimalSlashCommand : IDbSlashCommand
    {
        public string Name => "minimal";
        public string Description => "";
        public bool CanUseDm => false;
        public bool HasInteraction => false;
        public List<SlashCommandOptionBuilder> Options => [];
    }

    [Fact]
    public async Task Command_ExecuteMethods_DefaultToDoingNothing()
    {
        IDbCommand command = new MinimalCommand();
        var args = new Mock<IDbCommandExecutingArgument>().Object;

        Task server = command.ExecuteServer(args);
        Task dm = command.ExecuteDm(args);

        Assert.True(server.IsCompletedSuccessfully);
        Assert.True(dm.IsCompletedSuccessfully);
        await Task.WhenAll(server, dm);
    }

    [Fact]
    public void Event_Stop_DefaultsToDoingNothing()
    {
        IDbEvent dbEvent = new MinimalEvent();

        var exception = Record.Exception(dbEvent.Stop);

        Assert.Null(exception);
    }

    [Fact]
    public void Event_CanBeStoppedBeforeOrWithoutStarting()
    {
        IDbEvent dbEvent = new MinimalEvent();

        dbEvent.Stop();
        dbEvent.Stop();

        Assert.Equal(0, ((MinimalEvent)dbEvent).Starts);
    }

    [Fact]
    public async Task SlashCommand_ExecuteMethods_DefaultToDoingNothing()
    {
        IDbSlashCommand command = new MinimalSlashCommand();
        var logger = new Mock<ILogger>().Object;

        command.ExecuteServer(logger, null!);
        command.ExecuteDm(logger, null!);
        Task interaction = command.ExecuteInteraction(logger, null!);

        Assert.True(interaction.IsCompletedSuccessfully);
        await interaction;
    }

    [Fact]
    public void DbEventExecutingArgument_KeepsWhatItWasGiven()
    {
        var logger = new Mock<ILogger>().Object;
        using var client = new DiscordSocketClient();
        var folder = new DirectoryInfo(Path.GetTempPath());

        IDbEventExecutingArgument args = new DbEventExecutingArgument(logger, client, ";", folder);

        Assert.Same(logger, args.Logger);
        Assert.Same(client, args.Client);
        Assert.Equal(";", args.BotPrefix);
        Assert.Same(folder, args.PluginBaseDirectory);
    }

    [Fact]
    public void DbCommandExecutingArgument_KeepsWhatItWasGiven()
    {
        var logger = new Mock<ILogger>().Object;
        var folder = new DirectoryInfo(Path.GetTempPath());

        IDbCommandExecutingArgument args = new DbCommandExecutingArgument(logger, null!, "level 5", "level", ["5"], folder);

        Assert.Same(logger, args.Logger);
        Assert.Equal("level 5", args.CleanContent);
        Assert.Equal("level", args.CommandUsed);
        Assert.Equal(["5"], args.Arguments!);
        Assert.Same(folder, args.PluginBaseDirectory);
    }

    [Fact]
    public void DbCommandExecutingArgument_WithoutArguments_HasNull()
    {
        IDbCommandExecutingArgument args = new DbCommandExecutingArgument(new Mock<ILogger>().Object, null!, "level", "level", null, new DirectoryInfo("."));

        Assert.Null(args.Arguments);
    }
}
