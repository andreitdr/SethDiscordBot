using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using PluginManager.Others.Actions;
using PluginManager.Interfaces;
using System.Collections;

namespace PluginManager.Loaders
{
    public class ActionsLoader
    {
        public delegate void ActionLoaded(string name, string typeName, bool success, Exception? e = null);
        public event ActionLoaded? ActionLoadedEvent;

        private string actionFolder = @"./Data/Actions/";
        private string actionExtension = "dll";

        public ActionsLoader(string path, string extension)
        {
            actionFolder = path;
            actionExtension = extension;
        }

        public async Task<List<ICommandAction>?> Load()
        {
            Directory.CreateDirectory(actionFolder);
            var files = Directory.GetFiles(actionFolder, $"*.{actionExtension}", SearchOption.AllDirectories);

            List<ICommandAction> actions = new List<ICommandAction>();

            foreach (var file in files)
            {
                try
                {
                    Assembly.LoadFrom(file);
                }
                catch (Exception e)
                {
                    ActionLoadedEvent?.Invoke(file, "", false, e);
                }
            }

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ICommandAction).IsAssignableFrom(p) && !p.IsInterface);

            foreach (var type in types)
            {
                try
                {
                    var action = (ICommandAction) Activator.CreateInstance(type);
                    if (action.ActionName == null)
                    {
                        ActionLoadedEvent?.Invoke(action.ActionName, type.Name, false);
                        continue;
                    }

                    if(action.RunType == PluginManager.Others.InternalActionRunType.ON_STARTUP)
                        await action.Execute(null);

                    ActionLoadedEvent?.Invoke(action.ActionName, type.Name, true);
                    actions.Add(action);
                }
                catch (Exception e)
                {
                    ActionLoadedEvent?.Invoke(type.Name, type.Name, false, e);
                }
            }

            return actions;
        }
    }
}