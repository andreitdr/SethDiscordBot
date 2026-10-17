using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace PluginPages;

/// <summary>
/// Adds a stylesheet embedded in the plugin (under <c>wwwroot/</c>) to the document head while the page is shown.
/// </summary>
public sealed class PluginStylesheet : ComponentBase
{
    [CascadingParameter] public PluginPageContext? Context { get; set; }

    /// <summary>The path relative to the plugin's <c>wwwroot/</c>, such as <c>css/site.css</c>.</summary>
    [Parameter, EditorRequired] public string Href { get; set; } = default!;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
        {
            return;
        }

        builder.OpenComponent<HeadContent>(0);
        builder.AddComponentParameter(1, nameof(HeadContent.ChildContent), (RenderFragment)(head =>
        {
            head.OpenElement(0, "link");
            head.AddAttribute(1, "rel", "stylesheet");
            head.AddAttribute(2, "href", Context.AssetUrl(Href));
            head.CloseElement();
        }));
        builder.CloseComponent();
    }
}
