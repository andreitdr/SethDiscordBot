using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Loaders;

namespace DiscordBotCore.Others.Actions;

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
            Application.CurrentApplication.Logger.Log($"Action {actionName} not found", this, LogType.ERROR);
            return false;
        }

        try
        {
            await Actions[actionName].Execute(args);
            return true;
        }
        catch (Exception e)
        {
            Application.CurrentApplication.Logger.Log(e.Message, type: LogType.ERROR, Sender: this);
            return false;
        }
    }
}
