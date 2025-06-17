using DiscordBotCore.Logging;

namespace Tests.DiscordBotCore.Logging;

public class LoggerTests
{
    private readonly string logFolder = Path.GetTempPath();
    private readonly string format = "{Message} | {SenderName} | {LogMessageType}";

    [Fact]
    public void Log_AddsMessageToList()
    {
        ILogger logger = new Logger(logFolder, format, maxHistorySize: 5);

        logger.Log("Test message");

        Assert.Single(logger.LogMessages);
        Assert.Equal("Test message", logger.LogMessages[0].Message);
    }

    [Fact]
    public void Log_TriggersOnLogReceived()
    {
        ILogger logger = new Logger(logFolder, format, maxHistorySize: 5);
        bool triggered = false;
        logger.OnLogReceived += (_) => triggered = true;

        logger.Log("Test message");

        Assert.True(triggered);
    }

    [Fact]
    public void Log_RespectsMaxHistorySize()
    {
        ILogger logger = new Logger(logFolder, format, maxHistorySize: 3);

        logger.Log("1");
        logger.Log("2");
        logger.Log("3");
        logger.Log("4");

        Assert.Equal(3, logger.LogMessages.Count);
        Assert.DoesNotContain(logger.LogMessages, m => m.Message == "1");
    }
}