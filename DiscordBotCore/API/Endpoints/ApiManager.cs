using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore.API.Endpoints;
using DiscordBotCore.API.Endpoints.PluginManagement;
using DiscordBotCore.API.Endpoints.SettingsManagement;
using DiscordBotCore.Interfaces.API;
using DiscordBotCore.Others;
using Microsoft.AspNetCore.Builder;

namespace DiscordBotCore.API.Endpoints;

public class ApiManager
{
    private bool IsRunning { get; set; }
    private List<IEndpoint> ApiEndpoints { get; }
    
    public ApiManager()
    {
        ApiEndpoints = new List<IEndpoint>();
    }
    
    internal void AddBaseEndpoints()
    {
        AddEndpoint(new HomeEndpoint());
        AddEndpoint(new PluginListEndpoint());
        AddEndpoint(new PluginListInstalledEndpoint());
        AddEndpoint(new PluginInstallEndpoint());

        AddEndpoint(new SettingsChangeEndpoint());
        AddEndpoint(new SettingsGetEndpoint());
    }
    
    public Result AddEndpoint(IEndpoint endpoint)
    {
        if (ApiEndpoints.Contains(endpoint) || ApiEndpoints.Exists(x => x.Path == endpoint.Path))
        {
            return Result.Failure("Endpoint already exists");
        }
        
        ApiEndpoints.Add(endpoint);
        return Result.Success();
    }

    public void RemoveEndpoint(string endpointPath)
    {
        this.ApiEndpoints.RemoveAll(endpoint => endpoint.Path == endpointPath);
    }
    
    public bool EndpointExists(string endpointPath)
    {
        return this.ApiEndpoints.Exists(endpoint => endpoint.Path == endpointPath);
    }
    
    public async void InitializeApi()
    {
        if (IsRunning)
            return;

        IsRunning = true;

        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        app.UseRouting();

        EndpointManager manager = new EndpointManager(app);
        foreach(IEndpoint endpoint in this.ApiEndpoints)
        {
            manager.MapEndpoint(endpoint);
        }
        
        await app.RunAsync();
    }
}
