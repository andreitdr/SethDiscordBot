using System.IO;
using System.Text;
using DiscordBotCore.Interfaces.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DiscordBotCore.API.Endpoints;

internal sealed class EndpointManager
{
    private readonly WebApplication _AppBuilder;
    internal EndpointManager(WebApplication appBuilder)
    {
        _AppBuilder = appBuilder;
    }
    
    internal void MapEndpoint(IEndpoint endpoint)
    {
        switch (endpoint.HttpMethod)
        {
            case EndpointType.Get:
                _AppBuilder.MapGet(endpoint.Path, async context =>
                {
                    //convert the context to a string
                    string jsonRequest = string.Empty;
                    if (context.Request.Body.CanRead)
                    {
                        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                        jsonRequest = await reader.ReadToEndAsync();
                    }
                    
                    var response = await endpoint.HandleRequest(jsonRequest);
                    await context.Response.WriteAsync(await response.ToJson());
                });
                break;
            case EndpointType.Put:
                _AppBuilder.MapPut(endpoint.Path, async context =>
                {
                    string jsonRequest = string.Empty;
                    if (context.Request.Body.CanRead)
                    {
                        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                        jsonRequest = await reader.ReadToEndAsync();
                    }
                    
                    var response = await endpoint.HandleRequest(jsonRequest);
                    await context.Response.WriteAsync(await response.ToJson());
                });
                break;
            case EndpointType.Post:
                _AppBuilder.MapPost(endpoint.Path, async context =>
                {
                    string jsonRequest = string.Empty;
                    if (context.Request.Body.CanRead)
                    {
                        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                        jsonRequest = await reader.ReadToEndAsync();
                    }
                    
                    var response = await endpoint.HandleRequest(jsonRequest);
                    await context.Response.WriteAsync(await response.ToJson());
                });
                break;
            case EndpointType.Delete:
                _AppBuilder.MapDelete(endpoint.Path, async context =>
                {
                    string jsonRequest = string.Empty;
                    if (context.Request.Body.CanRead)
                    {
                        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                        jsonRequest = await reader.ReadToEndAsync();
                    }
                    
                    var response = await endpoint.HandleRequest(jsonRequest);
                    await context.Response.WriteAsync(await response.ToJson());
                });
                break;
        }
    }
}
