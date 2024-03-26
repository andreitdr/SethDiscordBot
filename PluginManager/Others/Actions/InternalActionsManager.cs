using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PluginManager.Interfaces;
using PluginManager.Loaders;

namespace PluginManager.Others.Actions;

public class InternalActionManager
{
    public Dictionary<string, ICommandAction> Actions = new();
    private readonly ActionsLoader _loader;

    public InternalActionManager(string path, string extension)
    {
        _loader = new ActionsLoader(path, extension);
    }

    public async Task Initialize()
    {
        var loadedActions = await _loader.Load();
        if (loadedActions == null)
            return;
        foreach (var action in loadedActions)
            Actions.TryAdd(action.ActionName, action);
        
    }

    public async Task Refresh()
    {
        Actions.Clear();
        await Initialize();
    }

    public async Task<bool> Execute(string actionName, params string[]? args)
    {
        if (!Actions.ContainsKey(actionName))
        {
            Config.Logger.Log($"Action {actionName} not found", type: LogType.ERROR, source: typeof(InternalActionManager));
            return false;
        }

        try
        {
            await Actions[actionName].Execute(args);
            return true;
        }
        catch (Exception e)
        {
            Config.Logger.Log(e.Message, type: LogType.ERROR, source: typeof(InternalActionManager));
            return false;
        }
    }
}
