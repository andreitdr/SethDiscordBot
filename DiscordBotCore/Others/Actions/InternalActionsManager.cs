using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Loaders;

namespace DiscordBotCore.Others.Actions;

public class InternalActionManager
{
    private Dictionary<string, ICommandAction> Actions = new();

    public async Task Initialize()
    {
        Actions.Clear();

        PluginLoader.Actions.ForEach(action =>
        {
            if (action.RunType == InternalActionRunType.ON_CALL || action.RunType == InternalActionRunType.BOTH)
            {
                if (this.Actions.ContainsKey(action.ActionName))
                {
                    // This should never happen. If it does, log it and return
                    Application.CurrentApplication.Logger.Log($"Action {action.ActionName} already exists", this, LogType.ERROR);
                    return;
                }

                this.Actions.Add(action.ActionName, action);
            }
        });
    }

    public IReadOnlyCollection<ICommandAction> GetActions()
    {
        return Actions.Values;
    }

    public bool Exists(string actionName)
    {
        return Actions.ContainsKey(actionName);
    }

    public ICommandAction GetAction(string actionName)
    {
        return Actions[actionName];
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
            if (Actions[actionName].RunType == InternalActionRunType.ON_STARTUP)
            {
                Application.CurrentApplication.Logger.Log($"Action {actionName} is not executable", this, LogType.ERROR);
                return false;
            }    

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
