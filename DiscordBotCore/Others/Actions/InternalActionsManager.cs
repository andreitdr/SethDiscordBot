using System;
using System.Collections.Generic;
using System.Threading;
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
            if (action.RunType == InternalActionRunType.OnCall || action.RunType == InternalActionRunType.OnStartupAndCall)
            {
                if (this.Actions.ContainsKey(action.ActionName))
                {
                    // This should never happen. If it does, log it and return
                    Application.Logger.Log($"Action {action.ActionName} already exists", this, LogType.Error);
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
            Application.Logger.Log($"Action {actionName} not found", this, LogType.Error);
            return false;
        }

        try
        {
            if (Actions[actionName].RunType == InternalActionRunType.OnStartup)
            {
                Application.Logger.Log($"Action {actionName} is not executable", this, LogType.Error);
                return false;
            }

            await StartAction(Actions[actionName], args);
            return true;
        }
        catch (Exception e)
        {
            Application.Logger.Log(e.Message, type: LogType.Error, sender: this);
            return false;
        }
    }

    public async Task StartAction(ICommandAction action, params string[]? args)
    {
        if (action.RequireOtherThread)
        {
            async void Start() => await action.Execute(args);

            Thread thread = new(Start);
            thread.Start();
        }
        else
        {
            await action.Execute(args);
        }
    }
}
