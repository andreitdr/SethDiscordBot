using System;

namespace DiscordBotCore.Others.Exceptions
{
    public class PluginNotFoundException : Exception
    {
        public PluginNotFoundException(string pluginName) : base($"Plugin {pluginName} was not found") { }

        public PluginNotFoundException(string pluginName, string url, string branch) :
            base ($"Plugin {pluginName} was not found on {url} (branch: {branch}") { }
    }
}
