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

public class LocalPluginRepository : ILocalPluginRepository
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
        return await _LocalPluginDatabase.ReadObjectOfTypeAsync("select * from Plugins where name = @name", PluginConvertor, new KeyValuePair<string, object>("name", name));
    }

    public async Task<List<LocalDependencyInfo>> GetDependenciesForPluginAsync(int pluginId)
    {
        return await _LocalPluginDatabase.ReadListOfTypeAsync("select * from Dependencies where plugin_id=@pluginId", DependencyConvertor, new KeyValuePair<string, object>("plugin_id", pluginId));
    }

    public async Task<List<LocalDependencyInfo>> GetDependenciesForPluginAsync(string pluginName)
    {
        var plugin = await GetPluginByNameAsync(pluginName);

        if (plugin == null)
        {
            return new List<LocalDependencyInfo>();
        }
        
        return await _LocalPluginDatabase.ReadListOfTypeAsync("select * from Dependencies where plugin_id=@pluginId", DependencyConvertor, new KeyValuePair<string, object>("pluginId", plugin.Id));
    }

    public async Task<Guid> AddPluginAsync(LocalPlugin plugin)
    {
        string sql = "insert into Plugins (id, name, version, file_path, is_offline_added, is_enabled) values (@id, @name, @version, @file_path, @is_offline_added, @is_enabled)  returning id;";
        var guid = Guid.CreateVersion7();
        int result = await _LocalPluginDatabase.ExecuteNonQueryAsync(sql, 
            new  KeyValuePair<string, object>("id", guid.ToString()),
            new KeyValuePair<string, object>("name", plugin.PluginName),
            new KeyValuePair<string, object>("version", plugin.PluginVersion),
            new KeyValuePair<string, object>("file_path", plugin.FilePath),
            new KeyValuePair<string, object>("is_offline_added", plugin.IsOfflineAdded),
            new KeyValuePair<string, object>("is_enabled", plugin.IsEnabled));
        
        if(result <= 0)
        {
            throw new Exception("Failed to add plugin to database.");
        }
        
        return guid;
    }

    public async Task<bool> UpdatePluginAsync(LocalPlugin plugin)
    {
        string sql =
            "update Plugins set name=@name, version=@version, file_path=@filePath, is_offline_added=@isOfflineAdded, is_enabled=@isEnabled where id=@pluginId; ";

        var parameters = new[]
        {
            new KeyValuePair<string, object>("name", plugin.PluginName),
            new KeyValuePair<string, object>("version", plugin.PluginVersion),
            new KeyValuePair<string, object>("file_path", plugin.FilePath),
            new KeyValuePair<string, object>("is_offline_added", plugin.IsOfflineAdded),
            new KeyValuePair<string, object>("is_enabled", plugin.IsEnabled),
        };
        
        int result = await _LocalPluginDatabase.ExecuteNonQueryAsync(sql, parameters);
        return result > 0;
    }

    public async Task<bool> DeletePluginAsync(Guid pluginId)
    {
        string sql = "delete from Plugins where plugin_id=@pluginId; ";
        var parameters = new[]
        {
            new KeyValuePair<string, object>("pluginId", pluginId)
        };
        
        int result = await _LocalPluginDatabase.ExecuteNonQueryAsync(sql, parameters);
        return result > 0;
    }

    public async Task<Guid> AddDependencyAsync(LocalDependencyInfo dependency)
    {
        string sql = "insert into Dependencies (name, file_path, plugin_id) values (@name, @filePath, @pluginId) returning id";
        var guid = Guid.CreateVersion7();
        var result = await _LocalPluginDatabase.ExecuteNonQueryAsync(sql,
            new KeyValuePair<string, object>("id", guid.ToString()),
            new KeyValuePair<string, object>("name", dependency.DependencyName),
            new KeyValuePair<string, object>("filePath", dependency.DependencyLocation),
            new KeyValuePair<string, object>("pluginId", dependency.PluginId)
        );

        if (result <= 0)
        {
            throw new Exception("Failed to add dependency to database.");
        }
        
        return guid;
    }

    public async Task<bool> DeleteDependencyAsync(Guid dependencyId)
    {
        string sql = "delete from Dependencies where id=@dependencyId; ";
        var parameters = new[]
        {
            new KeyValuePair<string, object>("dependencyId", dependencyId)
        };
        
        int result = await _LocalPluginDatabase.ExecuteNonQueryAsync(sql, parameters);
        return result > 0;
    }

    public async Task<bool> DeleteAllDependenciesForPluginAsync(Guid pluginId)
    {
        string sql = "delete from Dependencies where plugin_id=@pluginId; ";
        var parameters = new[]
        {
            new KeyValuePair<string, object>("pluginId", pluginId)
        };

        int result = await _LocalPluginDatabase.ExecuteNonQueryAsync(sql, parameters);
        return result > 0;
    }

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

