using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace PluginPages;

/// <summary>
/// Renders the plugin page that matches <see cref="PluginPath"/>, with its <see cref="PluginPageContext"/> cascaded.
/// Re-resolves whenever the registry changes, so a page disappears (and its component is disposed) as soon as its plugin
/// is unregistered, and comes back after a reload.
/// </summary>
public sealed class PluginPageOutlet : ComponentBase, IDisposable
{
    private PluginPageMatch? _Match;
    private bool _IsRegistered;

    [Inject] private IPluginPageRegistry Registry { get; set; } = default!;

    [Parameter, EditorRequired] public string PluginKey { get; set; } = default!;

    /// <summary>The path relative to the plugin's prefix, typically the host page's <c>{*PluginPath}</c> value.</summary>
    [Parameter] public string? PluginPath { get; set; }

    /// <summary>Shown when no plugin is registered under <see cref="PluginKey"/>.</summary>
    [Parameter] public RenderFragment? NotLoaded { get; set; }

    /// <summary>Shown when the plugin is registered but none of its pages matches the path.</summary>
    [Parameter] public RenderFragment? NotFound { get; set; }

    /// <summary>Shown when the plugin page throws while rendering.</summary>
    [Parameter] public RenderFragment<Exception>? Error { get; set; }

    /// <summary>Called with the resolved match (or null) every time it is resolved, so the host can show a title or breadcrumb.</summary>
    [Parameter] public EventCallback<PluginPageMatch?> MatchChanged { get; set; }

    protected override void OnInitialized()
    {
        Registry.Changed += OnRegistryChanged;
    }

    protected override Task OnParametersSetAsync() => ResolveAsync();

    private void OnRegistryChanged()
    {
        _ = InvokeAsync(async () =>
        {
            await ResolveAsync();
            StateHasChanged();
        });
    }

    private async Task ResolveAsync()
    {
        PluginPageMatch? previous = _Match;
        _IsRegistered = Registry.IsRegistered(PluginKey);
        _Match = Registry.TryMatch(PluginKey, PluginPath, out PluginPageMatch match) ? match : null;

        // Only report real changes: the callback re-renders the parent, which sets our parameters again.
        bool changed = previous is null || _Match is null
            ? !ReferenceEquals(previous, _Match)
            : !ReferenceEquals(previous.Page, _Match.Page) || previous.Context.RelativePath != _Match.Context.RelativePath;
        if (changed)
        {
            await MatchChanged.InvokeAsync(_Match);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_Match is null)
        {
            if (_IsRegistered)
            {
                builder.AddContent(0, NotFound ?? DefaultMessage("There is no page at this address in this plugin."));
            }
            else
            {
                builder.AddContent(1, NotLoaded ?? DefaultMessage("This plugin is not loaded."));
            }

            return;
        }

        PluginPageMatch match = _Match;

        // Keyed by type and path: a different page, a reloaded plugin (new Type) or a different address gets a fresh
        // error boundary and component instead of reusing one that may have failed.
        builder.OpenComponent<ErrorBoundary>(2);
        builder.SetKey((match.ComponentType, match.Context.RelativePath));
        builder.AddComponentParameter(3, nameof(ErrorBoundary.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<CascadingValue<PluginPageContext>>(0);
            content.AddComponentParameter(1, nameof(CascadingValue<PluginPageContext>.Value), match.Context);
            content.AddComponentParameter(2, nameof(CascadingValue<PluginPageContext>.ChildContent), (RenderFragment)(page =>
            {
                page.OpenComponent<DynamicComponent>(0);
                page.AddComponentParameter(1, nameof(DynamicComponent.Type), match.ComponentType);
                page.AddComponentParameter(2, nameof(DynamicComponent.Parameters), match.Parameters.ToDictionary(p => p.Key, p => p.Value));
                page.CloseComponent();
            }));
            content.CloseComponent();
        }));
        if (Error is not null)
        {
            builder.AddComponentParameter(4, nameof(ErrorBoundary.ErrorContent), Error);
        }

        builder.CloseComponent();
    }

    private static RenderFragment DefaultMessage(string text) => builder =>
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, text);
        builder.CloseElement();
    };

    public void Dispose()
    {
        Registry.Changed -= OnRegistryChanged;
    }
}
