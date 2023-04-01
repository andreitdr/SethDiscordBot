using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PluginManager.WindowManagement.Controls
{
    public class ConsoleOption
    {
        public string Text { get; set; }
        public byte Index { get; set; }

        public Action Action { get; set; }
    }
}