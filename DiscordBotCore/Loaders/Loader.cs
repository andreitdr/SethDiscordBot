using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;

namespace DiscordBotCore.Loaders;

internal class Loader
{
    private readonly string _SearchPath;
    private readonly string _FileExtension;

    internal delegate void FileLoadedHandler(FileLoaderResult result);

    internal delegate void PluginLoadedHandler(PluginLoadResultData result);

    internal event FileLoadedHandler? OnFileLoadedException;
    internal event PluginLoadedHandler? OnPluginLoaded;

    internal Loader(string searchPath, string fileExtension)
    {
        _SearchPath    = searchPath;
        _FileExtension = fileExtension;
    }

    internal async Task Load()
    {
        if (!Directory.Exists(_SearchPath))
        {
            Directory.CreateDirectory(_SearchPath);
            return;
        }

        var installedPlugins = await Application.CurrentApplication.PluginManager.GetInstalledPlugins();
        var files = installedPlugins.Select(plugin => plugin.FilePath).ToArray();
        
        //var files = Directory.GetFiles(_SearchPath, $"*.{_FileExtension}", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            try
            {
                Assembly.LoadFrom(file);
            }
            catch
            {
                OnFileLoadedException?.Invoke(new FileLoaderResult(file, $"Failed to load file {file}"));
            }
        }

        await LoadEverythingOfType<DBEvent>();
        await LoadEverythingOfType<DBCommand>();
        await LoadEverythingOfType<DBSlashCommand>();
    }

    private async Task LoadEverythingOfType<T>()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
                             .SelectMany(s => s.GetTypes())
                             .Where(p => typeof(T).IsAssignableFrom(p) && !p.IsInterface);

        foreach (var type in types)
        {
            try
            {
                var plugin = (T?)Activator.CreateInstance(type);

                if (plugin is null)
                {
                    throw new Exception($"Failed to create instance of plugin with type {type.FullName} [{type.Assembly}]");
                }

                var pluginType = plugin switch
                {
                    DBEvent        => PluginType.EVENT,
                    DBCommand      => PluginType.COMMAND,
                    DBSlashCommand => PluginType.SLASH_COMMAND,
                    _              => PluginType.UNKNOWN
                };

                OnPluginLoaded?.Invoke(new PluginLoadResultData(type.FullName, pluginType, true, plugin: plugin));
            }
            catch (Exception ex)
            {
                OnPluginLoaded?.Invoke(new PluginLoadResultData(type.FullName, PluginType.UNKNOWN, false, ex.Message));
            }
        }
    }

}
