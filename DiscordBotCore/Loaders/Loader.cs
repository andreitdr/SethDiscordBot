using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others.Exceptions;

namespace DiscordBotCore.Loaders;

internal class Loader
{

    internal delegate void FileLoadedHandler(FileLoaderResult result);

    internal delegate void PluginLoadedHandler(PluginLoaderResult result);

    internal event FileLoadedHandler? OnFileLoadedException;
    internal event PluginLoadedHandler? OnPluginLoaded;

    internal async Task Load()
    {
        var installedPlugins = await Application.CurrentApplication.PluginManager.GetInstalledPlugins();
        var files            = installedPlugins.Where(plugin => plugin.IsEnabled).Select(plugin => plugin.FilePath).ToArray();

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

        await LoadEverythingOfType<IDbEvent>();
        await LoadEverythingOfType<IDbCommand>();
        await LoadEverythingOfType<IDbSlashCommand>();
        await LoadEverythingOfType<ICommandAction>();
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
                    ICommandAction action   => PluginLoaderResult.FromICommandAction(action),
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
