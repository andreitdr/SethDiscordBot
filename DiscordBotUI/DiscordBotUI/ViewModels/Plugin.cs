using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBotUI.ViewModels
{
    public class Plugin
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public bool IsMarkedToUninstall { get; set; }

        public Plugin(string Name, string Version, bool isMarkedToUninstall)
        {
            this.Name = Name;
            this.Version = Version;
            IsMarkedToUninstall = isMarkedToUninstall;
        }
    }
}
