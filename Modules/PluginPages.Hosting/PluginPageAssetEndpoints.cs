using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace PluginPages;

public static class PluginPageAssetEndpoints
{
    private static readonly FileExtensionContentTypeProvider _ContentTypes = new();

    /// <summary>
    /// Serves files embedded in registered plugin assemblies at <c>{BasePath}/{pluginKey}/{AssetsSegment}/{path}</c>.
    /// </summary>
    public static IEndpointConventionBuilder MapPluginPageAssets(this IEndpointRouteBuilder endpoints)
    {
        PluginPagesOptions options = endpoints.ServiceProvider.GetRequiredService<IOptions<PluginPagesOptions>>().Value;
        string pattern = $"{options.BasePath.TrimEnd('/')}/{{pluginKey}}/{options.AssetsSegment}/{{**path}}";

        return endpoints.MapGet(pattern, (string pluginKey, string? path, IPluginPageRegistry registry, HttpContext http) =>
        {
            if (string.IsNullOrEmpty(path) || !registry.TryOpenAsset(pluginKey, path, out Stream stream))
            {
                return Results.NotFound();
            }

            if (!_ContentTypes.TryGetContentType(path, out string? contentType))
            {
                contentType = "application/octet-stream";
            }

            http.Response.Headers.XContentTypeOptions = "nosniff";
            // Versioned URLs (from PluginPageContext.AssetUrl) change when the plugin is rebuilt, so they can be cached.
            http.Response.Headers.CacheControl = http.Request.Query.ContainsKey("v") ? "private, max-age=86400" : "no-cache";
            return Results.Stream(stream, contentType);
        }).ExcludeFromDescription();
    }
}
