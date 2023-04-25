using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PluginManager.Others.Actions
{
    public class ActionManager
    {
        public List<InternalAction> Actions { get; private set; }

        private bool _isInitialized = false;

        public ActionManager()
        {
            if(_isInitialized) return;

            Actions = new List<InternalAction>();

            _isInitialized = true;
        }

        public bool ActionExists(string name)
        {
            if(!_isInitialized) throw new Exception("ActionManager is not initialized");
            return Actions.Any(x => x.Name == name);
        }

        public void AddAction(InternalAction action)
        {
            if(!_isInitialized) throw new Exception("ActionManager is not initialized");
            Actions.Add(action);
        }

        public void ExecuteAction(string name, string[] args)
        {
            if(!_isInitialized) throw new Exception("ActionManager is not initialized");
            var action = Actions.FirstOrDefault(x => x.Name == name);
            if(action == null) throw new Exception($"Action {name} not found");
            action.Invoke(args);
        }

        public async Task ExecuteActionAsync(string name, string[] args)
        {
            if(!_isInitialized) throw new Exception("ActionManager is not initialized");
            var action = Actions.FirstOrDefault(x => x.Name == name);
            if(action == null) throw new Exception($"Action {name} not found");
            await action.InvokeAsync(args);
        }
    }
}
