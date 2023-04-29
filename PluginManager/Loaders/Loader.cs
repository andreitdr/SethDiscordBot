using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using PluginManager.Interfaces;

namespace PluginManager.Loaders
{
    internal class LoaderArgs : EventArgs
    {
        internal string? PluginName { get; init; }
        internal string? TypeName { get; init; }
        internal bool IsLoaded { get; init; }
        internal Exception? Exception { get; init; }
        internal object? Plugin { get; init; }
    }
    internal class Loader
    {
        internal Loader(string path, string extension)
        {
            this.path = path;
            this.extension = extension;
        }


        private string path { get; }
        private string extension { get; }

        internal event FileLoadedEventHandler? FileLoaded;

        internal event PluginLoadedEventHandler? PluginLoaded;


        internal delegate void FileLoadedEventHandler(LoaderArgs args);

        internal delegate void PluginLoadedEventHandler(LoaderArgs args);


        internal (List<DBEvent>?, List<DBCommand>?, List<DBSlashCommand>?) Load()
        {

            List<DBEvent> events = new();
            List<DBSlashCommand> slashCommands = new();
            List<DBCommand> commands = new();

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return (null, null, null);
            }

            var files = Directory.GetFiles(path, $"*.{extension}", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    Assembly.LoadFrom(file);
                }
                catch (Exception ex)
                {
                    Config.Logger.Log("PluginName: " + new FileInfo(file).Name.Split('.')[0] + " not loaded", this, Others.TextType.ERROR);
                    continue;
                }
                if (FileLoaded != null)
                {
                    var args = new LoaderArgs
                    {
                        Exception = null,
                        TypeName = null,
                        IsLoaded = false,
                        PluginName = new FileInfo(file).Name.Split('.')[0],
                        Plugin = null
                    };
                    FileLoaded.Invoke(args);
                }
            }



            return (LoadItems<DBEvent>(), LoadItems<DBCommand>(), LoadItems<DBSlashCommand>());
        }

        internal List<T> LoadItems<T>()
        {
            List<T> list = new();


            try
            {
                var interfaceType = typeof(T);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                                     .SelectMany(a => a.GetTypes())
                                     .Where(p => interfaceType.IsAssignableFrom(p) && p.IsClass)
                                     .ToArray();


                list.Clear();
                foreach (var type in types)
                    try
                    {
                        var plugin = (T)Activator.CreateInstance(type)!;
                        list.Add(plugin);


                        if (PluginLoaded != null)
                            PluginLoaded.Invoke(new LoaderArgs
                            {
                                Exception = null,
                                IsLoaded = true,
                                PluginName = type.FullName,
                                TypeName = typeof(T) == typeof(DBCommand) ? "DBCommand" : typeof(T) == typeof(DBEvent) ? "DBEvent" : "DBSlashCommand",
                                Plugin = plugin
                            }
                            );
                    }
                    catch (Exception ex)
                    {
                        if (PluginLoaded != null)
                            PluginLoaded.Invoke(new LoaderArgs
                            {
                                Exception = ex,
                                IsLoaded = false,
                                PluginName = type.FullName,
                                TypeName = nameof(T)
                            });
                    }

                return list;
            }
            catch (Exception ex)
            {
                Config.Logger.Log(ex.Message, this, Others.TextType.ERROR);

                return null;
            }
            return null;
        }

    }
}
