using DiscordBotCore.Bot;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Loading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscordBotCore.WebApplication;

public static class Initializer
{
    private static readonly string DefaultLogFormat = "{ThrowTime} {SenderName} {Message}";
    private static readonly string DefaultLogFolder = "./Data/Logs";
    private static readonly string DefaultResourcesFolder = "./Data/Resources";
    private static readonly string DefaultConfigFile = "./Data/Resources/config.json";
    private static readonly string DefaultPluginFolder = "./Data/Plugins";
    private static readonly string DefaultPluginDatabaseFile = "./Data/Resources/plugins.json";
    private static readonly string DefaultMaxHistorySize = "1000";
    
    public static void AddDiscordBotComponents(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ILogger>(sp =>
        {
            string logFormat = builder.Configuration["Logger:LogFormat"] ?? DefaultLogFormat;
            string logFolder = builder.Configuration["Logger:LogFolder"] ?? DefaultLogFolder;
            string maxHistorySize = builder.Configuration["Logger:MaxHistorySize"] ?? DefaultMaxHistorySize;
            Directory.CreateDirectory(logFolder);
            if (!int.TryParse(maxHistorySize, out int maxHistorySizeInt))
            {
                maxHistorySizeInt = int.Parse(DefaultMaxHistorySize);
            }
            
            ILogger logger = new Logger(logFolder, logFormat, maxHistorySizeInt);
            logger.OnLogReceived += (logMessage) =>
            {
                Console.WriteLine(logMessage.Message);
            };

            return logger;
        });

        builder.Services.AddSingleton<IConfiguration>(sp =>
        {
            ILogger logger = sp.GetRequiredService<ILogger>();
            string configFile = builder.Configuration["ConfigFile"] ?? DefaultConfigFile;
            Directory.CreateDirectory(new FileInfo(configFile).DirectoryName);
            IConfiguration configuration = Configuration.Configuration.CreateFromFile(logger, configFile, true);
            return configuration;
        });

        builder.Services.AddSingleton<IPluginRepositoryConfiguration>(sp =>
        {
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            Dictionary<string, string>? remotePluginConnectionConfigurationDetails =
                configuration.Get<Dictionary<string, string>>("RemotePluginConnectionConfigurationDetails");

            if (remotePluginConnectionConfigurationDetails is null)
            {
                return PluginRepositoryConfiguration.Default;
            }

            return new PluginRepositoryConfiguration(
                remotePluginConnectionConfigurationDetails["Baseurl"],
                remotePluginConnectionConfigurationDetails["PluginsEndpoint"],
                remotePluginConnectionConfigurationDetails["DependenciesEndpoint"]
            );
        });

        builder.Services.AddSingleton<IPluginRepository>(sp =>
        {
            IPluginRepositoryConfiguration pluginRepositoryConfiguration =
                sp.GetRequiredService<IPluginRepositoryConfiguration>();
            ILogger logger = sp.GetRequiredService<ILogger>();
            IPluginRepository pluginRepository = new PluginRepository(pluginRepositoryConfiguration, logger);
            return pluginRepository;
        });

        builder.Services.AddSingleton<IPluginManager>(sp =>
        {
            IPluginRepository pluginRepository = sp.GetRequiredService<IPluginRepository>();
            ILogger logger = sp.GetRequiredService<ILogger>();
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();

            string pluginFolder = configuration.Get<string>("PluginFolder", DefaultPluginFolder);
            Directory.CreateDirectory(pluginFolder);

            string resourcesFolder = configuration.Get<string>("ResourcesFolder", DefaultResourcesFolder);
            Directory.CreateDirectory(resourcesFolder);

            string pluginDatabaseFile = configuration.Get<string>("PluginDatabase", DefaultPluginDatabaseFile);
            Directory.CreateDirectory(new FileInfo(pluginDatabaseFile).DirectoryName);

            IPluginManager pluginManager = new PluginManager(pluginRepository, logger, configuration);
            return pluginManager;
        });

        builder.Services.AddSingleton<IPluginLoader>(sp =>
        {
            IPluginManager pluginManager = sp.GetRequiredService<IPluginManager>();
            ILogger logger = sp.GetRequiredService<ILogger>();
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            return new PluginLoader(pluginManager, logger, configuration);
        });

        builder.Services.AddSingleton<IDiscordBotApplication>(sp =>
        {
            ILogger logger = sp.GetRequiredService<ILogger>();
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            IPluginLoader pluginLoader = sp.GetRequiredService<IPluginLoader>();
            return new DiscordBotApplication(logger, configuration, pluginLoader);
        });
    }
}