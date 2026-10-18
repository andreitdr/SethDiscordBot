using DiscordBotCore.Database.Sqlite;
using DiscordBotCore.PluginManagement.Models;

namespace DiscordBotCore.PluginManagement.Helpers;

/*

Plugins:
- id (guid)
- name (unique)
- version
- file_path
- is_offline_added
- is_enabled

Dependencies:
- id (guid)
- name (unique)
- file_path
- plugin_id

*/

public class LocalPluginRepository : ILocalPluginRepository, IAsyncDisposable
{
    private readonly SqlDatabase _LocalPluginDatabase;

    public LocalPluginRepository(string pluginDatabaseFile)
    {
        _LocalPluginDatabase = new SqlDatabase(pluginDatabaseFile);
        EnsureCreated();
    }
    
    public async Task<List<LocalPlugin>> GetAllPluginsAsync()
    {
        return await _LocalPluginDatabase.ReadListOfTypeAsync("select * from Plugins", PluginConvertor);
    }

    public async Task<LocalPlugin?> GetPluginByNameAsync(string name)
    {
        return await _LocalPluginDatabase.ReadObjectOfTypeAsync("select * from Plugins where name = @name", PluginConvertor, Parameter("name", name));
    }

    public async Task<List<LocalDependencyInfo>> GetDependenciesForPluginAsync(Guid pluginId)
    {
        return await _LocalPluginDatabase.ReadListOfTypeAsync("select * from Dependencies where plugin_id = @pluginId", DependencyConvertor, IdParameter("pluginId", pluginId));
    }

    public async Task<List<LocalDependencyInfo>> GetDependenciesForPluginAsync(string pluginName)
    {
        var plugin = await GetPluginByNameAsync(pluginName);

        if (plugin == null)
        {
            return new List<LocalDependencyInfo>();
        }
        
        return await GetDependenciesForPluginAsync(plugin.Id);
    }

    public async Task<Guid> AddPluginAsync(LocalPlugin plugin)
    {
        string sql = "insert into Plugins (id, name, version, file_path, is_offline_added, is_enabled) values (@id, @name, @version, @filePath, @isOfflineAdded, @isEnabled)";
        var guid = Guid.CreateVersion7();
        int result = await _LocalPluginDatabase.ExecuteNonQueryAsync(sql, 
            IdParameter("id", guid),
            Parameter("name", plugin.PluginName),
            Parameter("version", plugin.PluginVersion),
            Parameter("filePath", plugin.FilePath),
            Parameter("isOfflineAdded", plugin.IsOfflineAdded),
            Parameter("isEnabled", plugin.IsEnabled));
        
        if(result <= 0)
        {
            throw new Exception("Failed to add plugin to database.");
        }
        
        return guid;
    }

    public async Task<bool> UpdatePluginAsync(LocalPlugin plugin)
    {
        string sql =
            "update Plugins set name = @name, version = @version, file_path = @filePath, is_offline_added = @isOfflineAdded, is_enabled = @isEnabled where id = @pluginId";

        int result = await _LocalPluginDatabase.ExecuteNonQueryAsync(sql,
            Parameter("name", plugin.PluginName),
            Parameter("version", plugin.PluginVersion),
            Parameter("filePath", plugin.FilePath),
            Parameter("isOfflineAdded", plugin.IsOfflineAdded),
            Parameter("isEnabled", plugin.IsEnabled),
            IdParameter("pluginId", plugin.Id));
        return result > 0;
    }

    public async Task<bool> DeletePluginAsync(Guid pluginId)
    {
        // Dependencies reference the plugin, so they go first
        await DeleteAllDependenciesForPluginAsync(pluginId);

        int result = await _LocalPluginDatabase.ExecuteNonQueryAsync("delete from Plugins where id = @pluginId", IdParameter("pluginId", pluginId));
        return result > 0;
    }

    public async Task<Guid> AddDependencyAsync(LocalDependencyInfo dependency)
    {
        string sql = "insert into Dependencies (id, name, file_path, plugin_id) values (@id, @name, @filePath, @pluginId)";
        var guid = Guid.CreateVersion7();
        var result = await _LocalPluginDatabase.ExecuteNonQueryAsync(sql,
            IdParameter("id", guid),
            Parameter("name", dependency.DependencyName),
            Parameter("filePath", dependency.DependencyLocation),
            IdParameter("pluginId", dependency.PluginId)
        );

        if (result <= 0)
        {
            throw new Exception("Failed to add dependency to database.");
        }
        
        return guid;
    }

    public async Task<bool> DeleteDependencyAsync(Guid dependencyId)
    {
        int result = await _LocalPluginDatabase.ExecuteNonQueryAsync("delete from Dependencies where id = @dependencyId", IdParameter("dependencyId", dependencyId));
        return result > 0;
    }

    public async Task<bool> DeleteAllDependenciesForPluginAsync(Guid pluginId)
    {
        int result = await _LocalPluginDatabase.ExecuteNonQueryAsync("delete from Dependencies where plugin_id = @pluginId", IdParameter("pluginId", pluginId));
        return result > 0;
    }

    private static KeyValuePair<string, object> Parameter(string name, object value) => new(name, value);

    // Ids are stored as lowercase text; a Guid parameter would be bound differently and never match
    private static KeyValuePair<string, object> IdParameter(string name, Guid id) => new(name, id.ToString());

    public ValueTask DisposeAsync() => _LocalPluginDatabase.DisposeAsync();

    private LocalPlugin PluginConvertor(object[] values)
    {
        return new LocalPlugin
        {
            Id = Guid.Parse(values[0].ToString()),
            PluginName =  (string)values[1],
            PluginVersion = (string)values[2],
            FilePath = (string)values[3],
            IsOfflineAdded = values[4].ToString() == "1",
            IsEnabled = values[5].ToString() == "1",
        };
    }

    private LocalDependencyInfo DependencyConvertor(object[] values)
    {
        return new LocalDependencyInfo
        {
            Id = Guid.Parse(values[0].ToString()),
            DependencyName = (string)values[1],
            DependencyLocation = (string)values[2],
            PluginId = Guid.Parse(values[3].ToString()),
        };
    }

    private void EnsureCreated()
    {
        string sql = @"
        CREATE TABLE IF NOT EXISTS Plugins (
            id TEXT PRIMARY KEY,
            name TEXT UNIQUE NOT NULL,
            version TEXT NOT NULL,
            file_path TEXT NOT NULL,
            is_offline_added INTEGER NOT NULL DEFAULT 0,
            is_enabled INTEGER NOT NULL DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS Dependencies (
            id TEXT PRIMARY KEY,
            name TEXT UNIQUE NOT NULL,
            file_path TEXT NOT NULL,
            plugin_id TEXT NOT NULL,
            FOREIGN KEY (plugin_id) REFERENCES Plugins (id) ON DELETE CASCADE
        );";

        _LocalPluginDatabase.Execute(sql);
    }
}

