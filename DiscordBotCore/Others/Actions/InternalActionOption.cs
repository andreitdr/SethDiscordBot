using System.Collections.Generic;

namespace DiscordBotCore.Others.Actions
{
    public class InternalActionOption
    {
        public string OptionName { get; set; }
        public string OptionDescription { get; set; }

        public List<InternalActionOption> SubOptions { get; set; }

        public InternalActionOption(string optionName, string optionDescription, List<InternalActionOption> subOptions)
        {
            OptionName = optionName;
            OptionDescription = optionDescription;
            SubOptions = subOptions;
        }

        public InternalActionOption(string optionName, string optionDescription)
        {
            OptionName = optionName;
            OptionDescription = optionDescription;
            SubOptions = new List<InternalActionOption>();
        }
    }
}
