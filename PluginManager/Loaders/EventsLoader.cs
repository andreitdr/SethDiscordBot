using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using PluginManager.Interfaces;

namespace PluginManager.Loaders
{
    public class EventsLoader
    {

        private readonly string EVPath;
        private readonly string EVExtension;

        public delegate void onEventLoad(string name, bool success, DBEvent? ev = null, Exception? e = null);
        public delegate void onEventFileLoaded(string path);

        public onEventLoad? EventLoad;
        public onEventFileLoaded? EventFileLoaded;

        public EventsLoader(string path, string ext)
        {
            EVPath = path;
            EVExtension = ext;
        }

        public List<DBEvent>? LoadEvents()
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
                            EventLoad.Invoke(type.FullName!, true, ev);
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