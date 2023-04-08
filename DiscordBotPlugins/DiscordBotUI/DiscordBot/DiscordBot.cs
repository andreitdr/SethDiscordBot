using PluginManager.Bot;

namespace DiscordBotUI.DiscordBot
{
    public class DiscordBot
    {
        public static DiscordBot Instance { get; private set; }
        public Boot _boot { get; private set; }

        public DiscordBot(string token, string prefix)
        {
            if(Instance is not null)
                throw new Exception("DiscordBot is already initialized");
            Instance = this;
            _boot = new Boot(token,prefix);
        }

        public async Task Start()
        {
            await _boot.Awake();
        }

        public async Task LoadPlugins()
        {
            var loader = new PluginManager.Loaders.PluginLoader(_boot.client);
            loader.onCMDLoad += (name, type, success, e) =>
            {
                if (success)
                    PluginManager.Logger.WriteLine($"Loaded command {name} from {type}");
                else
                    PluginManager.Logger.WriteLine($"Failed to load command {name} from {type} with error {e}");
            };

            loader.onEVELoad += (name, type, success, e) =>
            {
                if (success)
                    PluginManager.Logger.WriteLine($"Loaded event {name} from {type}");
                else
                    PluginManager.Logger.WriteLine($"Failed to load event {name} from {type} with error {e}");
            };

            loader.onSLSHLoad += (name, type, success, e) =>
            {
                if (success)
                    PluginManager.Logger.WriteLine($"Loaded slash command {name} from {type}");
                else
                    PluginManager.Logger.WriteLine($"Failed to load slash command {name} from {type} with error {e}");
            };
            loader.LoadPlugins();
        }
    }
}
