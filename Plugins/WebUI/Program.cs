using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;
using WebUI.Components;

public class Entry: ICommandAction
{

    public string ActionName => "WebUI";
    public string? Description => "Starts the WebUI";
    public string? Usage => "webui";
    public IEnumerable<InternalActionOption> ListOfOptions => [];
    public InternalActionRunType RunType => InternalActionRunType.OnStartup;
    
    public bool RequireOtherThread => true;

    public Task Execute(string[]? args)
    {
        var builder = WebApplication.CreateBuilder();

        // Add services to the container.
        builder.Services.AddRazorComponents()
               .AddInteractiveServerComponents();

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
        return Task.CompletedTask;
        
    }
}
