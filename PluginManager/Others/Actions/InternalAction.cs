using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Others.Actions
{
    public class InternalAction
    {
        public string? Name { get; init; }
        public Action<string[]> Action { get; init; }

        public InternalAction(string name, Action<string[]> action)
        {
            Name = name;
            Action = action;
        }

        public InternalAction(string name, Action action)
        {
            Name = name;
            Action = (o) =>
            {
                action();
                return;
            };
        }

        public void Invoke(string[] args)
        {
            Action(args);
        }

        public async Task InvokeAsync(string[] args)
        {
            await Task.Run(() => Action(args));
        }
    }
}
