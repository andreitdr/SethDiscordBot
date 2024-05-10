using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PluginManager.Interfaces;
using PluginManager.Others;

namespace PluginManager.Loaders;

public class ActionsLoader
{
    public delegate void ActionLoaded(string name, string typeName, bool success, Exception? e = null);

    private readonly string _actionExtension = "dll";

    private readonly string _actionFolder = @"./Data/Plugins/";

    public ActionsLoader(string path, string extension)
    {
        _actionFolder    = path;
        _actionExtension = extension;
    }

    public event ActionLoaded? ActionLoadedEvent;

    public async Task<List<ICommandAction>?> Load()
    {
        Directory.CreateDirectory(_actionFolder);
        var files = Directory.GetFiles(_actionFolder, $"*.{_actionExtension}", SearchOption.AllDirectories);

        var actions = new List<ICommandAction>();

        foreach (var file in files)
            try
            {
                Assembly.LoadFrom(file);
            }
            catch (Exception e)
            {
                ActionLoadedEvent?.Invoke(file, "", false, e);
            }

        var types = AppDomain.CurrentDomain.GetAssemblies()
                             .SelectMany(s => s.GetTypes())
                             .Where(p => typeof(ICommandAction).IsAssignableFrom(p) && !p.IsInterface);

        foreach (var type in types)
            try
            {
                var action = (ICommandAction)Activator.CreateInstance(type);
                if (action.ActionName == null)
                {
                    ActionLoadedEvent?.Invoke(action.ActionName, type.Name, false);
                    continue;
                }

                if (action.RunType == InternalActionRunType.ON_STARTUP)
                    await action.Execute(null);

                ActionLoadedEvent?.Invoke(action.ActionName, type.Name, true);
                actions.Add(action);
            }
            catch (Exception e)
            {
                ActionLoadedEvent?.Invoke(type.Name, type.Name, false, e);
            }

        return actions;
    }
}
