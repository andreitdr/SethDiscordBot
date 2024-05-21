using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal record StartupAction(string Command, Action<string[]> RunAction) : IStartupAction;
}
