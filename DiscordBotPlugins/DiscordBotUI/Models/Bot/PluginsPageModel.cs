using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PluginManager.Interfaces;

namespace DiscordBotUI.Models.Bot
{
    public class PluginsPageModel
    {
        public List<string> InstalledCommands {get;set;}
        public List<string> InstalledEvents {get;set;}
        public List<string> InstalledSlashCommands {get;set;}
        public List<string[]> Plugins {get;set;}

        public PluginManager.Online.PluginsManager PluginsManager {get;set;}
    }
}