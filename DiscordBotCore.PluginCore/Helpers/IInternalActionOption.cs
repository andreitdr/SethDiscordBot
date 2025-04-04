namespace DiscordBotCore.PluginCore.Helpers;

public interface IInternalActionOption
{
    string OptionName { get; set; }
    string OptionDescription { get; set; }
    List<InternalActionOption> SubOptions { get; set; }
}
