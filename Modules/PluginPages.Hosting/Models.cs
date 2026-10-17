namespace PluginPages;

/// <summary>A page the registry accepted.</summary>
public sealed record PluginPageDescriptor(
    string PluginKey,
    string PluginName,
    string Route,
    string? Title,
    string? Icon,
    int Order,
    bool ShowInMenu,
    string ComponentTypeName,
    string Href);

/// <summary>A page the registry refused, and why.</summary>
public sealed record PluginPageRejection(
    string PluginKey,
    string PluginName,
    string? ComponentTypeName,
    string? Route,
    string Reason);

public sealed record PluginPageRegistrationResult(
    string PluginKey,
    IReadOnlyList<PluginPageDescriptor> Accepted,
    IReadOnlyList<PluginPageRejection> Rejected);

/// <summary>The outcome of resolving a request path against a plugin's pages.</summary>
public sealed record PluginPageMatch(
    PluginPageDescriptor Page,
    Type ComponentType,
    IReadOnlyDictionary<string, string?> RouteValues,
    IReadOnlyDictionary<string, object> Parameters,
    PluginPageContext Context);
