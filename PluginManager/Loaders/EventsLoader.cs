using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using PluginManager.Interfaces;

namespace PluginManager.Loaders
{
    internal class EventsLoader
    {

        private readonly string EVPath;
        private readonly string EVExtension;

        internal delegate void onEventLoad(string name, bool success, DBEvent? ev = null, Exception? e = null);
        internal delegate void onEventFileLoaded(string path);

        /// <summary>
        /// An event that is fired whenever a <see cref="DBEvent"/> event is loaded in memory
        /// </summary>
        internal onEventLoad? EventLoad;

        /// <summary>
        /// An event that is fired whenever a <see cref="DBEvent"/> event file is loaded
        /// </summary>
        internal onEventFileLoaded? EventFileLoaded;

        /// <summary>
        /// The Event Loader constructor
        /// </summary>
        /// <param name="path">The path to all events</param>
        /// <param name="ext">The extension for events</param>
        internal EventsLoader(string path, string ext)
        {
            EVPath = path;
            EVExtension = ext;
        }

        /// <summary>
        /// The method that loads all events
        /// </summary>
        /// <returns></returns>
        internal List<DBEvent>? LoadEvents()
        {

            if (!Directory.Exists(EVPath))
            {
                Directory.CreateDirectory(EVPath);
                return null;
            }

            string[] files = Directory.GetFiles(EVPath, $"*{EVExtension}", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                Assembly.LoadFile(Path.GetFullPath(file));
                if (EventFileLoaded != null)
                    EventFileLoaded.Invoke(file);
            }

            List<DBEvent> events = new List<DBEvent>();

            try
            {
                Type interfaceType = typeof(DBEvent);
                Type[] types = AppDomain.CurrentDomain.GetAssemblies()
                                        .SelectMany(a => a.GetTypes())
                                        .Where(p => interfaceType.IsAssignableFrom(p) && p.IsClass)
                                        .ToArray();
                foreach (Type type in types)
                {
                    try
                    {
                        DBEvent ev = (DBEvent)Activator.CreateInstance(type)!;
                        events.Add(ev);

                        if (EventLoad != null)
                            EventLoad.Invoke(type.FullName!, true, ev, null);
                    }
                    catch (Exception e)
                    {
                        if (EventLoad != null)
                            EventLoad.Invoke(type.FullName!, false, null, e);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return events;

        }
    }
}