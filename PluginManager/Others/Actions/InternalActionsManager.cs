using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PluginManager.Interfaces;
using PluginManager.Loaders;

namespace PluginManager.Others.Actions
{
    public class InternalActionManager
    {
        public ActionsLoader loader;
        public Dictionary<string, ICommandAction> Actions = new Dictionary<string, ICommandAction>();

        public InternalActionManager(string path, string extension)
        {
            loader = new ActionsLoader(path, extension);
        }

        public async Task Initialize()
        {
            loader.ActionLoadedEvent += OnActionLoaded;
            var m_actions = await loader.Load();
            if(m_actions == null) return;
            foreach(var action in m_actions)
                Actions.Add(action.ActionName, action);
        }

        private void OnActionLoaded(string name, string typeName, bool success, Exception? e)
        {
            if (!success)
            {
                Config.Logger.Error(e);
                return;
            }

            Config.Logger.Log($"Action {name} loaded successfully", typeName, LogLevel.INFO);
        }

        public async Task<string> Execute(string actionName, params string[]? args)
        {
            if (!Actions.ContainsKey(actionName))
            {
                Config.Logger.Log($"Action {actionName} not found", "InternalActionManager", LogLevel.WARNING);
                return "Action not found";
            }

            try{
                await Actions[actionName].Execute(args);
                return "Action executed";
            }catch(Exception e){
                Config.Logger.Log(e.Message, "InternalActionManager", LogLevel.ERROR);
                return e.Message;
            }
        }
    }
}
