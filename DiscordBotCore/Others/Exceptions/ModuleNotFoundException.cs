using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBotCore.Others.Exceptions
{
    internal class ModuleNotFoundException<T> : Exception
    {
        private Type _type = typeof(T);
        public ModuleNotFoundException() : base($"No module loaded with this signature: {typeof(T)}")
        {
        }
    }
}
