using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using PluginManager.Interfaces;

namespace PluginManager.Loaders
{
    public class CommandsLoader
    {
        private readonly string CMDPath;
        private readonly string CMDExtension;

        public delegate void onCommandLoaded(string name, bool success, DBCommand? command = null, Exception? exception = null);

        public delegate void onCommandFileLoaded(string path);

        public onCommandLoaded? OnCommandLoaded;
        public onCommandFileLoaded? OnCommandFileLoaded;

        public CommandsLoader(string CommandPath, string CommandExtension)
        {
            CMDPath = CommandPath;
            CMDExtension = CommandExtension;
        }

        public List<DBCommand>? LoadCommands()
        {
            if (!Directory.Exists(CMDPath))
            {
                Directory.CreateDirectory(CMDPath);
                return null;
            }
            string[] files = Directory.GetFiles(CMDPath, $"*{CMDExtension}", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                Assembly.LoadFile(Path.GetFullPath(file));
                if (OnCommandFileLoaded != null)
                    OnCommandFileLoaded.Invoke(file);
            }

            List<DBCommand> plugins = new List<DBCommand>();

            try
            {
                Type interfaceType = typeof(DBCommand);
                Type[] types = AppDomain.CurrentDomain.GetAssemblies()
                                        .SelectMany(a => a.GetTypes())
                                        .Where(p => interfaceType.IsAssignableFrom(p) && p.IsClass)
                                        .ToArray();
                foreach (Type type in types)
                {
                    try
                    {
                        DBCommand plugin = (DBCommand)Activator.CreateInstance(type)!;
                        plugins.Add(plugin);

                        if (OnCommandLoaded != null)
                            OnCommandLoaded.Invoke(type.FullName!, true, plugin);
                    }
                    catch (Exception e)
                    {
                        if (OnCommandLoaded != null)
                            OnCommandLoaded.Invoke(type.FullName!, false, null, e);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return plugins;

        }
    }
}
