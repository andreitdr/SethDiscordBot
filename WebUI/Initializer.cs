using DiscordBotCore.Bot;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Loading;
using DiscordBotCore.Resources;
using IConfiguration = DiscordBotCore.Configuration.IConfiguration;
using ILogger = DiscordBotCore.Logging.ILogger;

namespace WebUI;


public static class Initializer
{
    private static readonly string ConfigurationLoggerLogFormatStringPattern = "Logger:LogFormat";
    private static readonly string ConfigurationLoggerLogFolderStringPattern = "Logger:LogFolder";
    private static readonly string ConfigurationLoggerMaxHistorySizeStringPattern = "Logger:LogFolder";
    private static readonly string ConfigurationConfigFileStringPattern = "ConfigFile";
    
    public static void AddDiscordBotComponents(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ILogger>(sp =>
        {
            string logFormat = builder.Configuration[ConfigurationLoggerLogFormatStringPattern] ?? DefaultConfigurationValues.DefaultLogFormat;
            string logFolder = builder.Configuration[ConfigurationLoggerLogFolderStringPattern] ?? DefaultConfigurationValues.DefaultLogFolder;
            string maxHistorySize = builder.Configuration[ConfigurationLoggerMaxHistorySizeStringPattern] ?? DefaultConfigurationValues.DefaultMaxHistorySize;
            Directory.CreateDirectory(logFolder);
            if (!int.TryParse(maxHistorySize, out int maxHistorySizeInt))
            {
                maxHistorySizeInt = int.Parse(DefaultConfigurationValues.DefaultMaxHistorySize);
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
            string configFile = builder.Configuration[ConfigurationConfigFileStringPattern] ?? DefaultConfigurationValues.DefaultConfigFile;
            Directory.CreateDirectory(new FileInfo(configFile).DirectoryName);
            IConfiguration configuration = Configuration.CreateFromFile(logger, configFile, true);
            return configuration;
        });

        builder.Services.AddSingleton<IPluginRepositoryConfiguration>(sp =>
        {
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            Dictionary<string, string>? remotePluginConnectionConfigurationDetails =
                configuration.Get<Dictionary<string, string>>(DefaultConfigurationKeys.RemotePluginConnectionConfiguration.Details);

            if (remotePluginConnectionConfigurationDetails is null)
            {
                return PluginRepositoryConfiguration.Default;
            }

            return new PluginRepositoryConfiguration(
                remotePluginConnectionConfigurationDetails[DefaultConfigurationKeys.RemotePluginConnectionConfiguration.BaseUrl],
                remotePluginConnectionConfigurationDetails[DefaultConfigurationKeys.RemotePluginConnectionConfiguration.PluginsEndpoint],
                remotePluginConnectionConfigurationDetails[DefaultConfigurationKeys.RemotePluginConnectionConfiguration.DependenciesEndpoint]
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

        builder.Services.AddSingleton<ILocalPluginRepository>(sp =>
        {
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            string pluginDatabaseFile = configuration.Get<string>(DefaultConfigurationKeys.PluginsDatabaseFile, DefaultConfigurationValues.DefaultPluginDatabaseFile);
            Directory.CreateDirectory(new FileInfo(pluginDatabaseFile).DirectoryName);
            return new LocalPluginRepository(pluginDatabaseFile);
        });

        builder.Services.AddSingleton<IPluginManager>(sp =>
        {
            IPluginRepository pluginRepository = sp.GetRequiredService<IPluginRepository>();
            ILocalPluginRepository localPluginRepository = sp.GetRequiredService<ILocalPluginRepository>();
            ILogger logger = sp.GetRequiredService<ILogger>();
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            

            string pluginFolder = configuration.Get<string>(DefaultConfigurationKeys.PluginFolder, DefaultConfigurationValues.DefaultPluginFolder);
            Directory.CreateDirectory(pluginFolder);

            string resourcesFolder = configuration.Get<string>(DefaultConfigurationKeys.ResourcesFolder, DefaultConfigurationValues.DefaultResourcesFolder);
            Directory.CreateDirectory(resourcesFolder);
            
            IPluginManager pluginManager = new PluginManager(pluginRepository, localPluginRepository, logger, configuration);
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