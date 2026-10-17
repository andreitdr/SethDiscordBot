using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebUI.Components.Dashboard;

namespace WebUI.Services;

/// <summary>
/// The dashboard layout of one browser: where each panel sits, whether it is shown or collapsed, and whether the grid
/// is locked. Persisted in localStorage. Blazor renders the panels; gridstack (wwwroot/js/dashboard.js) only moves and
/// resizes them, and reports the result back through <see cref="OnLayoutChanged"/>.
/// </summary>
public sealed class DashboardLayoutService : IStateNotifier, IAsyncDisposable
{
    private const string StorageKey = "seth.dashboard.layout.v1";
    private const int CollapsedHeight = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime _Js;
    private readonly Dictionary<string, PanelState> _Panels = new();
    private readonly List<string> _PendingWidgets = new();

    private IJSObjectReference? _Module;
    private DotNetObjectReference<DashboardLayoutService>? _SelfReference;
    private ElementReference _GridElement;
    private bool _PendingInit;

    public event Action? Changed;

    public DashboardLayoutService(IJSRuntime js)
    {
        _Js = js;
        ResetState();
    }

    public bool IsLoaded { get; private set; }
    public bool Locked { get; private set; }

    public IEnumerable<PanelDefinition> VisiblePanels => PanelRegistry.All.Where(p => _Panels[p.Id].Visible);

    public PanelState this[string id] => _Panels[id];

    /// <summary>Loads the saved layout and schedules the grid to start on the next render.</summary>
    public async Task InitializeAsync(ElementReference gridElement)
    {
        _GridElement = gridElement;
        _Module = await _Js.InvokeAsync<IJSObjectReference>("import", "./js/dashboard.js");
        _SelfReference = DotNetObjectReference.Create(this);

        string? saved = await _Module.InvokeAsync<string?>("loadLayout", StorageKey);
        ApplySaved(saved);

        IsLoaded = true;
        _PendingInit = true;
        Changed?.Invoke();
    }

    /// <summary>Called by the dashboard after each render, once Blazor has put the panel elements in the DOM.</summary>
    public async Task AfterRenderAsync()
    {
        if (_Module is null)
        {
            return;
        }

        if (_PendingInit)
        {
            _PendingInit = false;
            _PendingWidgets.Clear();
            await _Module.InvokeVoidAsync("init", _GridElement, _SelfReference, new { locked = Locked });
            return;
        }

        foreach (string id in _PendingWidgets.ToArray())
        {
            await _Module.InvokeVoidAsync("adoptWidget", id);
        }

        _PendingWidgets.Clear();
    }

    public async Task HideAsync(string id)
    {
        if (!_Panels[id].Visible)
        {
            return;
        }

        // gridstack has to let go of the element before Blazor removes it.
        if (_Module is not null)
        {
            await _Module.InvokeVoidAsync("releaseWidget", id);
        }

        _Panels[id].Visible = false;
        await SaveAsync();
        Changed?.Invoke();
    }

    public async Task ShowAsync(string id)
    {
        var state = _Panels[id];
        if (state.Visible)
        {
            return;
        }

        state.Visible = true;
        // Let gridstack find a free spot instead of overlapping whatever is at the saved position now.
        state.AutoPosition = true;
        _PendingWidgets.Add(id);
        await SaveAsync();
        Changed?.Invoke();
    }

    public Task ToggleVisibilityAsync(string id) => _Panels[id].Visible ? HideAsync(id) : ShowAsync(id);

    public async Task ToggleCollapsedAsync(string id)
    {
        var definition = PanelRegistry.Find(id)!;
        var state = _Panels[id];

        if (state.Collapsed)
        {
            state.Collapsed = false;
            state.H = Math.Max(state.ExpandedH, definition.MinH);
        }
        else
        {
            state.Collapsed = true;
            state.ExpandedH = state.H;
            state.H = CollapsedHeight;
        }

        if (_Module is not null)
        {
            await _Module.InvokeVoidAsync("updateWidget", id, new
            {
                h = state.H,
                minH = state.Collapsed ? CollapsedHeight : definition.MinH,
                noResize = state.Collapsed
            });
        }

        await SaveAsync();
        Changed?.Invoke();
    }

    public async Task SetLockedAsync(bool locked)
    {
        Locked = locked;
        if (_Module is not null)
        {
            await _Module.InvokeVoidAsync("setLocked", locked);
        }

        await SaveAsync();
        Changed?.Invoke();
    }

    public async Task ResetAsync()
    {
        if (_Module is not null)
        {
            await _Module.InvokeVoidAsync("destroy");
        }

        ResetState();
        Locked = false;
        _PendingInit = true;
        await SaveAsync();
        Changed?.Invoke();
    }

    [JSInvokable]
    public async Task OnLayoutChanged(PanelPosition[] positions)
    {
        foreach (var position in positions)
        {
            if (!_Panels.TryGetValue(position.Id, out var state))
            {
                continue;
            }

            state.X = position.X;
            state.Y = position.Y;
            state.H = position.H;
            state.W = position.W;
            state.AutoPosition = false;
        }

        await SaveAsync();
    }

    private void ResetState()
    {
        _Panels.Clear();
        foreach (var definition in PanelRegistry.All)
        {
            _Panels[definition.Id] = PanelState.FromDefinition(definition);
        }
    }

    private void ApplySaved(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            var saved = JsonSerializer.Deserialize<SavedLayout>(json, JsonOptions);
            if (saved?.Panels is null)
            {
                return;
            }

            Locked = saved.Locked;
            foreach (var (id, state) in saved.Panels)
            {
                // Panels that were removed from the registry are dropped; new ones keep their defaults.
                var definition = PanelRegistry.Find(id);
                if (definition is null)
                {
                    continue;
                }

                state.W = Math.Max(state.W, definition.MinW);
                state.H = state.Collapsed ? CollapsedHeight : Math.Max(state.H, definition.MinH);
                state.AutoPosition = false;
                _Panels[id] = state;
            }
        }
        catch (JsonException)
        {
            // A corrupt or outdated layout falls back to the defaults.
        }
    }

    private async Task SaveAsync()
    {
        if (_Module is null)
        {
            return;
        }

        string json = JsonSerializer.Serialize(new SavedLayout { Locked = Locked, Panels = _Panels }, JsonOptions);
        try
        {
            await _Module.InvokeVoidAsync("saveLayout", StorageKey, json);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_Module is not null)
        {
            try
            {
                await _Module.InvokeVoidAsync("destroy");
                await _Module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _SelfReference?.Dispose();
    }

    private sealed class SavedLayout
    {
        public bool Locked { get; set; }
        public Dictionary<string, PanelState>? Panels { get; set; }
    }
}

public sealed class PanelState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
    public int ExpandedH { get; set; }
    public bool Visible { get; set; } = true;
    public bool Collapsed { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool AutoPosition { get; set; }

    public static PanelState FromDefinition(PanelDefinition d) =>
        new() { X = d.X, Y = d.Y, W = d.W, H = d.H, ExpandedH = d.H };
}

public sealed record PanelPosition(string Id, int X, int Y, int W, int H);
