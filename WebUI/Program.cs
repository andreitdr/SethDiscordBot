using System.Reflection;
using WebUI;
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

// Add services to the container.    
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.AddDiscordBotComponents();
builder.Services.AddSingleton<NotificationService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();