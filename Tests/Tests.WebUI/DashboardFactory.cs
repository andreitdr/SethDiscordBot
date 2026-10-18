using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.WebUI;

/// <summary>The whole WebUI in memory, with its own config file and folders.</summary>
public sealed class DashboardFactory : WebApplicationFactory<Program>
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-webui-tests", Guid.NewGuid().ToString("N"));
    private readonly string? _password;
    private readonly bool _allowRemote;

    public DashboardFactory(string? password, bool allowRemote = false)
    {
        _password = password;
        _allowRemote = allowRemote;

        Directory.CreateDirectory(_folder);
        string json = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["PluginFolder"] = Path.Combine(_folder, "Plugins"),
            ["ResourcesFolder"] = Path.Combine(_folder, "Resources"),
            ["PluginDatabase"] = Path.Combine(_folder, "plugins.db"),
            ["prefix"] = ";",
        });
        File.WriteAllText(Path.Combine(_folder, "config.json"), json);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConfigFile", Path.Combine(_folder, "config.json"));
        builder.UseSetting("Logger:LogFolder", Path.Combine(_folder, "Logs"));
        builder.UseSetting("Dashboard:Password", _password ?? string.Empty);
        builder.UseSetting("Dashboard:AllowRemoteWithoutPassword", _allowRemote.ToString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_folder, true);
        }
        catch (IOException)
        {
        }
    }
}
