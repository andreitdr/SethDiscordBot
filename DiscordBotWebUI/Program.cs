using DiscordBotWebUI.Components;
using DiscordBotWebUI.ServerCommunication;
using DiscordBotWebUI.ServerCommunication.ApiSettings;
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

Console.Clear();

Console.ForegroundColor = ConsoleColor.DarkYellow;
Console.WriteLine(logo);
Console.ResetColor();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();

builder.Services.AddRadzenCookieThemeService(options =>
{
    options.Name = "RadzenTheme";       // The name of the cookie
    options.Duration = TimeSpan.FromDays(365); // The duration of the cookie
});

builder.Services.AddSingleton<IApiSettings>(new ApiSettings("http://localhost", "5000"));
builder.Services.AddSingleton<ApiHandler>();

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
