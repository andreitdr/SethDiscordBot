using System.IO.Compression;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.Utilities;
using Moq;

namespace Tests.DiscordBotCore.Utilities;

public class ArchiveManagerTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-archive-tests", Guid.NewGuid().ToString("N"));
    private readonly string _archives;
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly ArchiveManager _manager;

    public ArchiveManagerTests()
    {
        _archives = Path.Combine(_folder, "archives");
        Directory.CreateDirectory(_archives);
        _configuration.Setup(c => c.Get<string>("ArchiveFolder")).Returns(_archives);
        _manager = new ArchiveManager(new Mock<ILogger>().Object, _configuration.Object);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    private string CreateZip(string name, params (string Entry, string Content)[] entries)
    {
        string path = Path.Combine(_archives, name);
        using ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (entry, content) in entries)
        {
            if (entry.EndsWith('/'))
            {
                zip.CreateEntry(entry);
                continue;
            }

            using var writer = new StreamWriter(zip.CreateEntry(entry).Open());
            writer.Write(content);
        }

        return path;
    }

    [Fact]
    public void CreateFromFile_ZipsTheFileIntoTheFolder()
    {
        string source = Path.Combine(_folder, "notes.txt");
        File.WriteAllText(source, "hello");
        string target = Path.Combine(_folder, "out");

        _manager.CreateFromFile(source, target);

        string archive = Path.Combine(target, "notes.zip");
        Assert.True(File.Exists(archive));
        using ZipArchive zip = ZipFile.OpenRead(archive);
        Assert.Equal("notes.txt", Assert.Single(zip.Entries).FullName);
    }

    [Fact]
    public void CreateFromFile_ReplacesAnExistingArchive()
    {
        string source = Path.Combine(_folder, "notes.txt");
        File.WriteAllText(source, "first");
        _manager.CreateFromFile(source, _folder);
        File.WriteAllText(source, "second");

        _manager.CreateFromFile(source, _folder);

        using ZipArchive zip = ZipFile.OpenRead(Path.Combine(_folder, "notes.zip"));
        using var reader = new StreamReader(Assert.Single(zip.Entries).Open());
        Assert.Equal("second", reader.ReadToEnd());
    }

    [Fact]
    public async Task ReadAllBytes_ReadsAnEntryByNameOrPath()
    {
        CreateZip("pack.zip", ("data/a.txt", "AAA"));

        Assert.Equal("AAA"u8.ToArray(), await _manager.ReadAllBytes("a.txt", "pack.zip"));
        Assert.Equal("AAA"u8.ToArray(), await _manager.ReadAllBytes("data/a.txt", "pack.zip"));
    }

    [Fact]
    public async Task ReadAllBytes_FailsForMissingArchiveOrEntry()
    {
        CreateZip("pack.zip", ("a.txt", "AAA"));

        await Assert.ThrowsAsync<Exception>(() => _manager.ReadAllBytes("a.txt", "missing.zip"));
        await Assert.ThrowsAsync<Exception>(() => _manager.ReadAllBytes("b.txt", "pack.zip"));
    }

    [Fact]
    public async Task ReadAllBytes_RequiresTheArchiveFolderSetting()
    {
        _configuration.Setup(c => c.Get<string>("ArchiveFolder")).Returns((string?)null);

        await Assert.ThrowsAsync<Exception>(() => _manager.ReadAllBytes("a.txt", "pack.zip"));
    }

    [Fact]
    public async Task ReadFromPakAsync_ReadsTextOrNull()
    {
        CreateZip("pack.zip", ("folder/config.txt", "value=1"));

        Assert.Equal("value=1", await _manager.ReadFromPakAsync("config.txt", "pack.zip"));
        Assert.Null(await _manager.ReadFromPakAsync("other.txt", "pack.zip"));
    }

    [Fact]
    public async Task ReadFromPakAsync_FailsForAMissingArchive()
    {
        await Assert.ThrowsAsync<Exception>(() => _manager.ReadFromPakAsync("config.txt", "missing.zip"));
    }

    [Fact]
    public async Task ReadFromPakAsync_FailsForACorruptArchiveInsteadOfRetryingForever()
    {
        File.WriteAllText(Path.Combine(_archives, "corrupt.zip"), "not a zip");

        Task<string?> read = _manager.ReadFromPakAsync("config.txt", "corrupt.zip");

        Assert.Same(read, await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(5))));
        await Assert.ThrowsAnyAsync<Exception>(() => read);
    }

    [Theory]
    [InlineData(UnzipProgressType.PercentageFromNumberOfFiles)]
    [InlineData(UnzipProgressType.PercentageFromTotalSize)]
    public async Task ExtractArchive_ExtractsFoldersAndFilesAndReportsProgress(UnzipProgressType type)
    {
        string zip = CreateZip("pack.zip", ("empty/", ""), ("a.txt", "A"), ("sub/b.txt", "B"));
        string target = Path.Combine(_folder, "extracted");
        var reports = new List<float>();

        await _manager.ExtractArchive(zip, target, new SynchronousProgress(reports.Add), type);

        Assert.True(Directory.Exists(Path.Combine(target, "empty")));
        Assert.Equal("A", File.ReadAllText(Path.Combine(target, "a.txt")));
        Assert.Equal("B", File.ReadAllText(Path.Combine(target, "sub", "b.txt")));
        Assert.NotEmpty(reports);
        Assert.Equal(100f, reports[^1], 0.01f);
        Assert.Equal(reports.OrderBy(r => r), reports);
    }

    [Theory]
    [InlineData(UnzipProgressType.PercentageFromNumberOfFiles)]
    [InlineData(UnzipProgressType.PercentageFromTotalSize)]
    public async Task ExtractArchive_DoesNotWriteOutsideTheTargetFolder(UnzipProgressType type)
    {
        string zip = CreateZip("evil.zip", ("../escaped.txt", "x"), ("ok.txt", "y"));
        string target = Path.Combine(_folder, "extracted");

        await _manager.ExtractArchive(zip, target, new SynchronousProgress(_ => { }), type);

        Assert.False(File.Exists(Path.Combine(_folder, "escaped.txt")));
        Assert.True(File.Exists(Path.Combine(target, "ok.txt")));
    }

    private sealed class SynchronousProgress(Action<float> report) : IProgress<float>
    {
        public void Report(float value) => report(value);
    }
}
