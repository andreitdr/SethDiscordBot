using System.Reflection;
using DiscordBotCore.PluginCore;
using DiscordBotCore.PluginCore.Interfaces;
using DiscordBotCore.PluginManagement.Loading.Exceptions;

namespace DiscordBotCore.PluginManagement.Loading;

internal class Loader
{
    internal delegate void FileLoadedHandler(string fileName, Exception exception);
    internal delegate void PluginLoadedHandler(PluginLoaderResult result);

    internal event FileLoadedHandler? OnFileLoadedException;
    internal event PluginLoadedHandler? OnPluginLoaded;
    
    private readonly IPluginManager _pluginManager;

    internal Loader(IPluginManager manager)
    {
        _pluginManager = manager;
    }

    internal async Task Load()
    {
        var installedPlugins = await _pluginManager.GetInstalledPlugins();
        var files            = installedPlugins.Where(plugin => plugin.IsEnabled).Select(plugin => plugin.FilePath).ToArray();

        foreach (var file in files)
        {
            try
            {
                Assembly.LoadFrom(file);
            }
            catch
            {
                OnFileLoadedException?.Invoke(file, new Exception($"Failed to load plugin from file {file}"));
            }
        }

        await LoadEverythingOfType<IDbEvent>();
        await LoadEverythingOfType<IDbCommand>();
        await LoadEverythingOfType<IDbSlashCommand>();
    }

    private Task LoadEverythingOfType<T>()
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

                PluginLoaderResult result = plugin switch
                {
                    IDbEvent @event         => PluginLoaderResult.FromIDbEvent(@event),
                    IDbCommand command      => PluginLoaderResult.FromIDbCommand(command),
                    IDbSlashCommand command => PluginLoaderResult.FromIDbSlashCommand(command),
                    _                       => PluginLoaderResult.FromException(new PluginNotFoundException($"Unknown plugin type {plugin.GetType().FullName}"))
                };

                OnPluginLoaded?.Invoke(result);
            }
            catch (Exception ex)
            {
                OnPluginLoaded?.Invoke(PluginLoaderResult.FromException(ex));
            }
        }
        
        return Task.CompletedTask;
    }

}
