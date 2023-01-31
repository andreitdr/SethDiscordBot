using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal class StartupArguments
    {
        public string runArgs { get; } = "";
        public bool loadPluginsAtStartup { get; } = true;
    }
}
