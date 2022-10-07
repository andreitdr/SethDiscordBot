using PluginManager.Online.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Interfaces
{
    public interface AppExtension
    {
        public string Name { get; }
        public VersionString Version { get; }

    }
}