using Discord.Commands;

using PluginManager.Interfaces;

namespace FreeGamesModule
{
    public class Free : DBCommand
    {
        public string Command => "free";

        public List<string>? Aliases => null;

        public string Description => "Check out any free game";

        public string Usage => "free [platform]";

        public bool requireAdmin => false;

        public void ExecuteServer(SocketCommandContext context)
        {

        }
    }
}