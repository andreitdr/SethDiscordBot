using Discord;
using Discord.WebSocket;
using DiscordBotCore.PluginCore.Helpers.Execution.DbCommand;
using DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;
using DiscordBotCore.PluginCore.Interfaces;

namespace Tests.FixturePlugin;

public class FixtureCommand : IDbCommand
{
    public string Command => "fixture";
    public List<string> Aliases => ["fx"];
    public string Description => "A command from the fixture plugin";
    public string Usage => "fixture";
    public bool RequireAdmin => false;
}

public class FixtureAdminCommand : IDbCommand
{
    public string Command => "fixture-admin";
    public List<string> Aliases => [];
    public string Description => "An admin command from the fixture plugin";
    public string Usage => "fixture-admin";
    public bool RequireAdmin => true;
}

/// <summary>Never instantiated: abstract types are skipped.</summary>
public abstract class AbstractFixtureCommand : IDbCommand
{
    public string Command => "abstract";
    public List<string> Aliases => [];
    public string Description => "";
    public string Usage => "";
    public bool RequireAdmin => false;
}

/// <summary>Its constructor throws: the loader logs it and goes on.</summary>
public class BrokenCommand : IDbCommand
{
    public BrokenCommand() => throw new InvalidOperationException("broken on purpose");

    public string Command => "broken";
    public List<string> Aliases => [];
    public string Description => "";
    public string Usage => "";
    public bool RequireAdmin => false;
}

/// <summary>Records what the loader does with it; the tests read these through reflection.</summary>
public class FixtureEvent : IDbEvent
{
    private DiscordSocketClient? _client;

    public string Name => "Fixture event";
    public string Description => "An event from the fixture plugin";

    public int StartCount { get; private set; }
    public int StopCount { get; private set; }
    public string? BotPrefix { get; private set; }
    public string? PluginBaseDirectory { get; private set; }
    public bool HandlerAttached { get; private set; }

    public void Start(IDbEventExecutingArgument args)
    {
        StartCount++;
        BotPrefix = args.BotPrefix;
        PluginBaseDirectory = args.PluginBaseDirectory.FullName;
        _client = args.Client;
        _client.MessageReceived += OnMessage;
        HandlerAttached = true;
    }

    public void Stop()
    {
        StopCount++;
        if (_client is not null)
        {
            _client.MessageReceived -= OnMessage;
            HandlerAttached = false;
        }
    }

    private Task OnMessage(SocketMessage message) => Task.CompletedTask;
}

/// <summary>Uses the default (empty) Stop.</summary>
public class EventWithoutStop : IDbEvent
{
    public string Name => "Event without stop";
    public string Description => "";

    public void Start(IDbEventExecutingArgument args)
    {
    }
}

public class FailingEvent : IDbEvent
{
    public string Name => "Failing event";
    public string Description => "";

    public void Start(IDbEventExecutingArgument args) => throw new InvalidOperationException("fails on purpose");
}

public class FixtureSlashCommand : IDbSlashCommand
{
    public string Name => "fixture-slash";
    public string Description => "A slash command from the fixture plugin";
    public bool CanUseDm => false;
    public bool HasInteraction => false;
    public List<SlashCommandOptionBuilder> Options => [];
}
