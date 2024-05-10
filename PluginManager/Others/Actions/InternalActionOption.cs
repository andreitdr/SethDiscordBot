using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Others.Actions
{
    public class InternalActionOption
    {
        public string OptionName { get; set; }
        public string OptionDescription { get; set; }

        public InternalActionOption(string optionName, string optionDescription)
        {
            OptionName = optionName;
            OptionDescription = optionDescription;
        }
    }
}
