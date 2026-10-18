using DiscordBotCore.Logging;

namespace Tests.DiscordBotCore.Logging;

public class LoggerTests : IDisposable
{
    private const string Format = "{Message} | {SenderName} | {LogMessageType}";

    private readonly string _logFolder = Path.Combine(Path.GetTempPath(), "seth-logger-tests", Guid.NewGuid().ToString("N"));

    public LoggerTests()
    {
        Directory.CreateDirectory(_logFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_logFolder))
        {
            Directory.Delete(_logFolder, true);
        }
    }

    private Logger CreateLogger(int maxHistorySize = 100, string format = Format) => new(_logFolder, format, maxHistorySize);

    #region History

    [Fact]
    public void Log_AddsMessageToList()
    {
        ILogger logger = CreateLogger(maxHistorySize: 5);

        logger.Log("Test message");

        Assert.Single(logger.LogMessages);
        Assert.Equal("Test message", logger.LogMessages[0].Message);
    }

    [Fact]
    public void Log_TriggersOnLogReceivedWithTheLoggedMessage()
    {
        ILogger logger = CreateLogger();
        ILogMessage? received = null;
        logger.OnLogReceived += message => received = message;

        logger.Log("Test message", LogType.Warning);

        Assert.NotNull(received);
        Assert.Same(logger.LogMessages[0], received);
    }

    [Fact]
    public void Log_RespectsMaxHistorySize()
    {
        ILogger logger = CreateLogger(maxHistorySize: 3);

        logger.Log("1");
        logger.Log("2");
        logger.Log("3");
        logger.Log("4");

        Assert.Equal(["2", "3", "4"], logger.LogMessages.Select(m => m.Message));
    }

    [Fact]
    public void LogMessages_IsASnapshot()
    {
        ILogger logger = CreateLogger();
        logger.Log("first");

        IReadOnlyList<ILogMessage> snapshot = logger.LogMessages;
        logger.Log("second");

        Assert.Single(snapshot);
        Assert.Equal(2, logger.LogMessages.Count);
    }

    [Fact]
    public void ClearLogMessages_EmptiesTheHistory()
    {
        ILogger logger = CreateLogger();
        logger.Log("1");
        logger.Log("2");

        logger.ClearLogMessages();

        Assert.Empty(logger.LogMessages);
    }

    #endregion

    #region Overloads

    [Fact]
    public void Log_WithMessageOnly_IsInfo()
    {
        ILogger logger = CreateLogger();

        logger.Log("plain");

        Assert.Equal(LogType.Info, logger.LogMessages[0].LogMessageType);
    }

    [Fact]
    public void Log_WithType_KeepsTheTypeAndHasNoSender()
    {
        ILogger logger = CreateLogger();

        logger.Log("careful", LogType.Critical);

        ILogMessage message = logger.LogMessages[0];
        Assert.Equal(LogType.Critical, message.LogMessageType);
        Assert.Equal(string.Empty, message.SenderName);
    }

    [Fact]
    public void Log_WithSender_UsesTheSendersTypeNameAndInfo()
    {
        ILogger logger = CreateLogger();

        logger.Log("from a test", this);

        ILogMessage message = logger.LogMessages[0];
        Assert.Equal(typeof(LoggerTests).FullName, message.SenderName);
        Assert.Equal(LogType.Info, message.LogMessageType);
    }

    [Fact]
    public void Log_WithSenderAndType_KeepsBoth()
    {
        ILogger logger = CreateLogger();

        logger.Log("broken", this, LogType.Error);

        ILogMessage message = logger.LogMessages[0];
        Assert.Equal(typeof(LoggerTests).FullName, message.SenderName);
        Assert.Equal(LogType.Error, message.LogMessageType);
    }

    [Fact]
    public void Log_SetsThrowTimeToNow()
    {
        ILogger logger = CreateLogger();
        DateTime before = DateTime.Now;

        logger.Log("now");

        Assert.InRange(logger.LogMessages[0].ThrowTime, before, DateTime.Now);
    }

    [Fact]
    public void LogException_LogsTheMessageAsError()
    {
        ILogger logger = CreateLogger();

        logger.LogException(new InvalidOperationException("it failed"), this);

        ILogMessage message = logger.LogMessages[0];
        Assert.Equal("it failed", message.Message);
        Assert.Equal(LogType.Error, message.LogMessageType);
        Assert.Equal(typeof(LoggerTests).FullName, message.SenderName);
    }

    [Fact]
    public void LogException_WithFullStack_LogsTheWholeException()
    {
        ILogger logger = CreateLogger();
        Exception exception = CaptureException(() => throw new InvalidOperationException("it failed"));

        logger.LogException(exception, this, logFullStack: true);

        string message = logger.LogMessages[0].Message;
        Assert.Contains(nameof(InvalidOperationException), message);
        Assert.Contains("it failed", message);
        Assert.Contains(nameof(CaptureException), message);
    }

    #endregion

    #region Log file

    [Fact]
    public async Task Log_WritesTheFormattedLineToTodaysFile()
    {
        Logger logger = CreateLogger();

        logger.Log("written", this, LogType.Warning);

        string file = logger.GetLogFilePath(logger.LogMessages[0].ThrowTime);
        Assert.Equal(Path.Combine(_logFolder, $"{DateTime.Now:yyyy-MM-dd}.log"), file);
        string[] lines = await WaitForLinesAsync(file, 1);
        Assert.Equal($"written | {typeof(LoggerTests).FullName} | Warning", lines[0]);
    }

    [Fact]
    public async Task Log_KeepsUnknownPlaceholdersAndFillsThrowTime()
    {
        Logger logger = CreateLogger(format: "[{Unknown}] {ThrowTime} {Message}");

        logger.Log("text");

        ILogMessage message = logger.LogMessages[0];
        string[] lines = await WaitForLinesAsync(logger.GetLogFilePath(message.ThrowTime), 1);
        Assert.Equal($"[{{Unknown}}] {message.ThrowTime} text", lines[0]);
    }

    [Fact]
    public void GetLogFilePath_IsOneFilePerDay()
    {
        Logger logger = CreateLogger();

        Assert.Equal(Path.Combine(_logFolder, "2026-01-02.log"), logger.GetLogFilePath(new DateTime(2026, 1, 2, 23, 59, 0)));
        Assert.Equal(Path.Combine(_logFolder, "2026-01-03.log"), logger.GetLogFilePath(new DateTime(2026, 1, 3, 0, 0, 1)));
    }

    [Fact]
    public async Task Log_FromManyThreads_KeepsEveryMessageAndEveryLine()
    {
        const int threads = 8;
        const int perThread = 50;
        Logger logger = CreateLogger(maxHistorySize: threads * perThread);

        await Task.WhenAll(Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            for (int i = 0; i < perThread; i++)
            {
                logger.Log($"thread {t} message {i}");
            }
        })));

        Assert.Equal(threads * perThread, logger.LogMessages.Count);
        string[] lines = await WaitForLinesAsync(logger.GetLogFilePath(DateTime.Now), threads * perThread);
        Assert.All(lines, line => Assert.Matches(@"^thread \d message \d+ \|", line));
        Assert.Equal(threads * perThread, lines.Distinct().Count());
    }

    [Fact]
    public async Task Log_WhenTheFolderIsMissing_DoesNotThrowAndKeepsTheMessage()
    {
        Logger logger = new(Path.Combine(_logFolder, "missing"), Format, 10);
        int received = 0;
        logger.OnLogReceived += _ => received++;

        logger.Log("still logged");
        await Task.Delay(100);

        Assert.Single(logger.LogMessages);
        Assert.Equal(1, received);
        Assert.False(Directory.Exists(Path.Combine(_logFolder, "missing")));
    }

    #endregion

    private static Exception CaptureException(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException("The action didn't throw.");
    }

    /// <summary>File writes run in the background, so wait until the expected number of lines is there.</summary>
    private static async Task<string[]> WaitForLinesAsync(string file, int count)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            if (File.Exists(file))
            {
                string[] lines = await File.ReadAllLinesAsync(file);
                if (lines.Length >= count || DateTime.UtcNow > deadline)
                {
                    return lines;
                }
            }
            else if (DateTime.UtcNow > deadline)
            {
                return [];
            }

            await Task.Delay(20);
        }
    }
}
