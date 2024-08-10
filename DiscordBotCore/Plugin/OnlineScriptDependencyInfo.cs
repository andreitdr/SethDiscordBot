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
