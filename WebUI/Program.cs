using System.Reflection;
using DiscordBotCore.Bot;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Loading;
using IConfiguration = DiscordBotCore.Configuration.IConfiguration;
using ILogger = DiscordBotCore.Logging.ILogger;

#region Load External (Unmanaged) Assemblies
// This code is used to load external (unmanaged) assemblies from the same folder as the executing assembly.
// It handles the AssemblyResolve event to search for the requested assembly in a specific folder structure.
// The folder structure is expected to be: <ExecutingAssemblyDirectory>/Libraries/<RequestingAssemblyName>/<AssemblyName>.<Extension>
// The extensions to search for are specified in the 'extensions' parameter.

var currentDomain = AppDomain.CurrentDomain;
currentDomain.AssemblyResolve += (sender, args) => LoadFromSameFolder(sender,args, [".dll", ".so", ".dylib"]);

static Assembly? LoadFromSameFolder(object? sender, ResolveEventArgs args, string[] extensions)
{
    string? requestingAssemblyName = args.RequestingAssembly?.GetName().Name;
    string executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;
    string? executingAssemblyDirectory = Path.GetDirectoryName(executingAssemblyLocation);

    if (string.IsNullOrEmpty(executingAssemblyDirectory))
    {
        Console.WriteLine($"Error: Could not determine the directory of the executing assembly.");
        return null;
    }

    string librariesFolder = Path.Combine(executingAssemblyDirectory, "Libraries", requestingAssemblyName ?? "");
    string requestedAssemblyNameWithoutExtension = new AssemblyName(args.Name).Name;

    Console.WriteLine($"Requesting Assembly: {requestingAssemblyName}");
    Console.WriteLine($"Requested Assembly Name (without extension): {requestedAssemblyNameWithoutExtension}");
    Console.WriteLine($"Searching in folder: {librariesFolder}");
    Console.WriteLine($"Searching for extensions: {string.Join(", ", extensions)}");

    foreach (string extension in extensions)
    {
        string assemblyFileName = requestedAssemblyNameWithoutExtension + extension;
        string assemblyPath = Path.Combine(librariesFolder, assemblyFileName);

        Console.WriteLine($"Attempting to load from: {assemblyPath}");

        if (File.Exists(assemblyPath))
        {
            try
            {
                var fileAssembly = Assembly.LoadFrom(assemblyPath);
                Console.WriteLine($"Successfully loaded Assembly: {fileAssembly.FullName}");
                return fileAssembly;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading assembly from '{assemblyPath}': {ex.Message}");
                // Optionally log the full exception for debugging
            }
        }
        else
        {
            Console.WriteLine($"File not found: {assemblyPath}");
        }
    }

    Console.WriteLine($"Failed to load assembly '{args.Name}' from the specified locations.");
    return null;
}
#endregion

var builder = WebApplication.CreateBuilder(args);

string defaultLogFormat = "{ThrowTime} {SenderName} {Message}";
string defaultLogFolder = "./Data/Logs";
string defaultConfigFile = "./Data/Resources/config.json";
string defaultPluginFolder = "./Data/Plugins";
string defaultPluginDatabaseFile = "./Data/Resources/plugins.json";

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ILogger>(sp =>
{
    string logFormat = builder.Configuration["Logger:LogFormat"] ?? defaultLogFormat;
    string logFolder = builder.Configuration["Logger:LogFolder"] ?? defaultLogFolder;
    
    Directory.CreateDirectory(logFolder);

    ILogger logger = new Logger(logFolder, logFormat);
    logger.SetOutFunction((s, type) =>
    {
        Console.WriteLine($"[{type}] {s}");
    });
    
    return logger;
});

builder.Services.AddSingleton<IConfiguration>(sp =>
{
    ILogger logger = sp.GetRequiredService<ILogger>();
    string configFile = builder.Configuration["ConfigFile"] ?? defaultConfigFile;
    Directory.CreateDirectory(new FileInfo(configFile).DirectoryName);
    IConfiguration configuration = Configuration.CreateFromFile(logger, configFile, true);
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
        remotePluginConnectionConfigurationDetails["Baseurl"], remotePluginConnectionConfigurationDetails["PluginsEndpoint"],
        remotePluginConnectionConfigurationDetails["DependenciesEndpoint"]
    );
});

builder.Services.AddSingleton<IPluginRepository>(sp =>
{
    IPluginRepositoryConfiguration pluginRepositoryConfiguration = sp.GetRequiredService<IPluginRepositoryConfiguration>();
    ILogger logger = sp.GetRequiredService<ILogger>();
    IPluginRepository pluginRepository = new PluginRepository(pluginRepositoryConfiguration, logger);
    return pluginRepository;
});

builder.Services.AddSingleton<IPluginManager>(sp =>
{
    IPluginRepository pluginRepository = sp.GetRequiredService<IPluginRepository>();
    ILogger logger = sp.GetRequiredService<ILogger>();
    IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
    
    Directory.CreateDirectory(configuration.Get<string>("PluginFolder", defaultPluginFolder));
    string pluginDatabaseFile = configuration.Get<string>("PluginDatabase", defaultPluginDatabaseFile);
    Directory.CreateDirectory(new FileInfo(pluginDatabaseFile).DirectoryName);
    
    IPluginManager pluginManager = new PluginManager(pluginRepository, logger, configuration);
    return pluginManager;
});
builder.Services.AddSingleton<IDiscordBotApplication>(sp =>
{
    ILogger logger = sp.GetRequiredService<ILogger>();
    IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
    IPluginLoader pluginLoader = sp.GetRequiredService<IPluginLoader>();
    return new DiscordBotApplication(logger, configuration, pluginLoader);
});

builder.Services.AddSingleton<IPluginLoader>(sp =>
{
    IPluginManager pluginManager = sp.GetRequiredService<IPluginManager>();
    ILogger logger = sp.GetRequiredService<ILogger>();
    IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
    IDiscordBotApplication discordBotApplication = sp.GetRequiredService<IDiscordBotApplication>();
    return new PluginLoader(pluginManager, logger, configuration, discordBotApplication.Client);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();