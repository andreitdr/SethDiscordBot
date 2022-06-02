using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using PluginManager.Interfaces;
using PluginManager.Others;

namespace PluginManager.Loaders
{
    internal class LoaderArgs : EventArgs
    {
        internal string?    PluginName { get; init; }
        internal string?    TypeName   { get; init; }
        internal bool       IsLoaded   { get; init; }
        internal Exception? Exception  { get; init; }
        internal object?    Plugin     { get; init; }
    }

    internal class Loader<T>
    {
        internal delegate void FileLoadedEventHandler(LoaderArgs args);

        internal event FileLoadedEventHandler? FileLoaded;

        internal delegate void PluginLoadedEventHandler(LoaderArgs args);

        internal event PluginLoadedEventHandler? PluginLoaded;


        private string path      { get; }
        private string extension { get; }


        internal Loader(string path, string extension)
        {
            this.path      = path;
            this.extension = extension;
        }

        internal List<T>? Load()
        {
            List<T>  list  = new List<T>();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return null;
            }

            string[] files = Directory.GetFiles(path, $"*.{extension}", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                Assembly.LoadFrom(file);
                if (FileLoaded != null)
                {
                    LoaderArgs args = new LoaderArgs() { Exception = null, TypeName = nameof(T), IsLoaded = false, PluginName = file, Plugin = null };
                    FileLoaded.Invoke(args);
                }
            }

            try
            {
                Type interfaceType = typeof(T);
                Type[] types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(p => interfaceType.IsAssignableFrom(p) && p.IsClass)
                    .ToArray();


                list.Clear();
                foreach (Type type in types)
                {
                    try
                    {
                        T plugin = (T)(Activator.CreateInstance(type)!);
                        list.Add(plugin);


                        if (PluginLoaded != null) { PluginLoaded.Invoke(new() { Exception = null, IsLoaded = true, PluginName = type.FullName, TypeName = nameof(T), Plugin = plugin }); }
                    }
                    catch (Exception ex)
                    {
                        if (PluginLoaded != null) { PluginLoaded.Invoke(new() { Exception = ex, IsLoaded = false, PluginName = type.FullName, TypeName = nameof(T) }); }
                    }
                }
            }
            catch (Exception ex) { Functions.WriteErrFile(ex.ToString()); }


            return list;
        }
    }
}
