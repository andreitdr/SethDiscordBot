using DiscordBotCore;
using DiscordBotCore.Interfaces.Logger;
using DiscordBotCore.Interfaces.Modules;

namespace LoggerModule
{
    public class Entry : IModule<ILogger>
    {
        public string Name => "Logger Module";
        const string _LogFolder = "./Data/Logs/";
        const string _LogFormat = "{ThrowTime} {SenderName} {Message}";

        public ILogger Module { get; private set; }

        public Task Initialize()
        {
            ILogger logger = new Logger(_LogFolder, _LogFormat);
            Module = logger;
            return Task.CompletedTask;
        }
    }
}
