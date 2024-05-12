namespace DiscordBotCore.Others.Actions
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
