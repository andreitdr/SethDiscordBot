using Discord;
using Discord.WebSocket;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Interfaces;

namespace CppModuleDemo;

public class StopSlashCommand : IDbSlashCommand
{
    public string Name => "stop-cpp-module-demo";
    public string Description => "Stops the C++ module demo and cleans up resources.";
    public bool CanUseDm => false;
    public bool HasInteraction => false;
    public List<SlashCommandOptionBuilder> Options => [];

    public async void ExecuteServer(ILogger logger, SocketSlashCommand context)
    {
        if (InternalSettings.ExternalApplicationHandler == null)
        {
            logger.Log("No C++ module is currently running.", this);
            return;
        }

        Guid id = InternalSettings.DemoModuleInternalId;
        if (id == Guid.Empty)
        {
            logger.Log("No valid C++ module ID found. Cannot stop the module.", this);
            return;
        }

        InternalSettings.ExternalApplicationHandler.StopApplication(id);
        InternalSettings.DemoModuleInternalId = Guid.Empty;
        logger.Log("CppModuleDemo stopped successfully.", this);

        await context.Channel.SendMessageAsync("CppModuleDemo has been stopped and resources cleaned up.");
    }
}