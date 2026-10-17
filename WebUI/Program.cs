using System.Reflection;
using PluginPages;
using WebUI;
using Radzen;
using WebUI.Services;
using WebUI.Components;

#region Load External (Unmanaged) Assemblies

// This code is used to load external (unmanaged) assemblies from the same folder as the executing assembly.
// It handles the AssemblyResolve event to search for the requested assembly in a specific folder structure.
// The folder structure is expected to be: <ExecutingAssemblyDirectory>/Libraries/<RequestingAssemblyName>/<AssemblyName>.<Extension>
// The extensions to search for are specified in the 'extensions' parameter.

var currentDomain = AppDomain.CurrentDomain;
currentDomain.AssemblyResolve += (sender, args) => LoadFromSameFolder(sender,args, [".dll", ".so", ".dylib"]);

static Assembly? LoadFromSameFolder(object? sender, ResolveEventArgs args, string[] extensions)
{
    string? requestedAssemblyName = new AssemblyName(args.Name).Name;

    // Satellite resource assemblies are probed for every culture and missing ones are normal; the runtime falls back
    // to the neutral resources on its own.
    if (string.IsNullOrEmpty(requestedAssemblyName) || requestedAssemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    string? requestingAssemblyName = args.RequestingAssembly?.GetName().Name;
    string? executingAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    if (string.IsNullOrEmpty(executingAssemblyDirectory) || string.IsNullOrEmpty(requestingAssemblyName))
    {
        return null;
    }

    string librariesFolder = Path.Combine(executingAssemblyDirectory, "Libraries", requestingAssemblyName);
    if (!Directory.Exists(librariesFolder))
    {
        return null;
    }

    foreach (string extension in extensions)
    {
        string assemblyPath = Path.Combine(librariesFolder, requestedAssemblyName + extension);
        if (!File.Exists(assemblyPath))
        {
            continue;
        }

        try
        {
            return Assembly.LoadFrom(assemblyPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load {assemblyPath} for {requestingAssemblyName}: {ex.Message}");
        }
    }

    return null;
}
#endregion


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.    
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.AddDiscordBotComponents();

builder.Services.AddRadzenComponents();
builder.Services.AddRadzenCookieThemeService(options =>
{
    options.Name = "SethDiscordBot.Theme";
    options.Duration = TimeSpan.FromDays(365);
});

// Shared across every browser tab, so all open dashboards see the same bot and plugin state.
builder.Services.AddSingleton<BotStateService>();
builder.Services.AddSingleton<PluginCatalogService>();
builder.Services.AddSingleton<LogStreamService>();
// One per circuit: the panel layout belongs to the browser that arranged it.
builder.Services.AddScoped<DashboardLayoutService>();

// Plugin web pages: registered from the loaded plugin assemblies, served under /plugin/<plugin id>/.
builder.Services.AddPluginPages();
builder.Services.AddHostedService<PluginPagesBridge>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

// Fingerprinted static assets: the browser never keeps a stale app.css or JS module after an update.
app.MapStaticAssets();
app.MapPluginPageAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();