using DiscordBotCore.Utilities;

namespace Tests.DiscordBotCore.Utilities;

public class JsonManagerTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-json-tests", Guid.NewGuid().ToString("N"));

    public JsonManagerTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    public sealed class Settings
    {
        public int SecondsToWait { get; set; }
        public string? Name { get; set; }
        public string? Secret { get; set; }
    }

    [Fact]
    public async Task SaveToJsonFile_ThenConvertFromJson_RoundTrips()
    {
        string file = Path.Combine(_folder, "settings.json");

        await JsonManager.SaveToJsonFile(file, new Settings { SecondsToWait = 5, Name = "Seth" });
        Settings settings = await JsonManager.ConvertFromJson<Settings>(file);

        Assert.Equal(5, settings.SecondsToWait);
        Assert.Equal("Seth", settings.Name);
        Assert.Contains(Environment.NewLine, await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task ConvertFromJson_ReadsJsonText_CaseInsensitively()
    {
        Settings settings = await JsonManager.ConvertFromJson<Settings>("""{ "secondstowait": 7, "NAME": "x" }""");

        Assert.Equal(7, settings.SecondsToWait);
        Assert.Equal("x", settings.Name);
    }

    [Fact]
    public async Task ConvertFromJson_KeepsNonAsciiText()
    {
        Settings settings = await JsonManager.ConvertFromJson<Settings>("""{ "Name": "Café ☕ Ș" }""");

        Assert.Equal("Café ☕ Ș", settings.Name);
    }

    [Fact]
    public async Task ConvertFromJson_KeepsNonAsciiTextFromAFile()
    {
        string file = Path.Combine(_folder, "unicode.json");
        await File.WriteAllTextAsync(file, """{ "Name": "Café ☕" }""");

        Settings settings = await JsonManager.ConvertFromJson<Settings>(file);

        Assert.Equal("Café ☕", settings.Name);
    }

    [Fact]
    public async Task ConvertFromJson_ReadsLists()
    {
        List<int> values = await JsonManager.ConvertFromJson<List<int>>("[1, 2, 3]");

        Assert.Equal([1, 2, 3], values);
    }

    [Fact]
    public async Task ConvertFromJson_WithInvalidJson_Throws()
    {
        await Assert.ThrowsAnyAsync<System.Text.Json.JsonException>(() => JsonManager.ConvertFromJson<Settings>("{ nope"));
    }

    [Fact]
    public async Task ConvertToJsonString_IsCompactAndRoundTrips()
    {
        string json = await JsonManager.ConvertToJsonString(new Settings { SecondsToWait = 1, Name = "Café" });

        Assert.DoesNotContain(Environment.NewLine, json);
        Settings settings = await JsonManager.ConvertFromJson<Settings>(json);
        Assert.Equal("Café", settings.Name);
    }

    [Fact]
    public async Task ConvertToJson_KeepsOnlyTheRequestedProperties()
    {
        var data = new List<Settings>
        {
            new() { SecondsToWait = 1, Name = "a", Secret = "hidden" },
            new() { SecondsToWait = 2, Name = null, Secret = "hidden" }
        };

        string json = await JsonManager.ConvertToJson(data, [nameof(Settings.SecondsToWait), nameof(Settings.Name)]);

        Assert.DoesNotContain("hidden", json);
        Assert.Contains("\"SecondsToWait\": 1", json);
        Assert.Contains("\"Name\": \"a\"", json);
        Assert.Contains("\"Name\": null", json);
    }

    [Fact]
    public async Task ConvertToJson_RejectsNullArguments()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => JsonManager.ConvertToJson<Settings>(null!, []));
        await Assert.ThrowsAsync<ArgumentNullException>(() => JsonManager.ConvertToJson(new List<Settings>(), null!));
    }
}
