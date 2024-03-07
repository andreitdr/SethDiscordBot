using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBotUI.ViewModels
{
    public class OnlinePlugin
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }

        public OnlinePlugin(string name, string description, string version) {
            Name = name;
            Description = description;
            Version = version;

        }
    }
}
