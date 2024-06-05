using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBotCore.Plugin
{
    public class OnlineScriptDependencyInfo
    {
        public string DependencyName { get; private set; }
        public string ScriptContent { get; private set; }

        public OnlineScriptDependencyInfo(string dependencyName, string scriptContent)
        {
            DependencyName = dependencyName;
            ScriptContent = scriptContent;
        }
    }
}
