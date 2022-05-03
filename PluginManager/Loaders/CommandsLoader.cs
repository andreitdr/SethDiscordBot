using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using PluginManager.Interfaces;

namespace PluginManager.Loaders
{
    internal class CommandsLoader
    {
        private readonly string CMDPath;
        private readonly string CMDExtension;


        internal delegate void onCommandLoaded(string name, bool success, DBCommand? command = null, Exception? exception = null);
        internal delegate void onCommandFileLoaded(string path);

        /// <summary>
        /// Event fired when a command is loaded
        /// </summary>
        internal onCommandLoaded? OnCommandLoaded;

        /// <summary>
        /// Event fired when the file is loaded
        /// </summary>
        internal onCommandFileLoaded? OnCommandFileLoaded;

        /// <summary>
        /// Command Loader contructor
        /// </summary>
        /// <param name="CommandPath">The path to the commands</param>
        /// <param name="CommandExtension">The extension to search for in the <paramref name="CommandPath"/></param>
        internal CommandsLoader(string CommandPath, string CommandExtension)
        {
            CMDPath = CommandPath;
            CMDExtension = CommandExtension;
        }

        /// <summary>
        /// The method that loads all commands
        /// </summary>
        /// <returns></returns>
        internal List<DBCommand>? LoadCommands()
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
