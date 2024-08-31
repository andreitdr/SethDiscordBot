using System.Reflection;
using DiscordBotWebUI.Components;
using DiscordBotWebUI.StartupActions;
using Radzen;


string logo = 
    @"

   _____      _   _        _____  _                       _    ____        _   
  / ____|    | | | |      |  __ \(_)                     | |  |  _ \      | |  
 | (___   ___| |_| |__    | |  | |_ ___  ___ ___  _ __ __| |  | |_) | ___ | |_ 
  \___ \ / _ \ __| '_ \   | |  | | / __|/ __/ _ \| '__/ _` |  |  _ < / _ \| __|
  ____) |  __/ |_| | | |  | |__| | \__ \ (_| (_) | | | (_| |  | |_) | (_) | |_ 
 |_____/ \___|\__|_| |_|  |_____/|_|___/\___\___/|_|  \__,_|  |____/ \___/ \__| 
                                                                                (WEB Edition) 
                                                                                                                                                                                          
";

var currentDomain = AppDomain.CurrentDomain;
currentDomain.AssemblyResolve += LoadFromSameFolder;

static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
{
    Directory.CreateDirectory("./Libraries");
    string requestingAssembly = args.RequestingAssembly?.GetName().Name;
    var    folderPath         = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $"Libraries/{requestingAssembly}");
    var    assemblyName       = new AssemblyName(args.Name).Name + ".dll";
    var    assemblyPath       = Path.Combine(folderPath, assemblyName);
            
    if (File.Exists(assemblyPath))
    {
        var fileAssembly = Assembly.LoadFrom(assemblyPath);
        return fileAssembly;
    }
            
    return null;
}


// Start startup actions if any
if (args.Length > 0)
{
    // get all classes that derive from IStartupAction
    var startupActions = Assembly.GetExecutingAssembly()
                               .GetTypes()
                               .Where(t => t.IsSubclassOf(typeof(IStartupAction)))
                               .Select(t => Activator.CreateInstance(t) as IStartupAction)
                               .ToList();
    
    foreach(var argument in args)
    {
        startupActions.FirstOrDefault(action => action?.Command == argument)?.RunAction(argument.Split(' ')[1..]);
    }
}

Console.Clear();

Console.ForegroundColor = ConsoleColor.DarkYellow;
Console.WriteLine(logo);
Console.ResetColor();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();

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
