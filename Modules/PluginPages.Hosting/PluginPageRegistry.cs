using System.Collections.Immutable;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace PluginPages;

public sealed class PluginPageRegistry : IPluginPageRegistry
{
    private readonly PluginPagesOptions _Options;
    private readonly IInlineConstraintResolver _ConstraintResolver;
    private readonly ILogger<PluginPageRegistry> _Logger;
    private readonly object _WriteLock = new();

    private ImmutableDictionary<string, PluginEntry> _Plugins = ImmutableDictionary.Create<string, PluginEntry>(StringComparer.OrdinalIgnoreCase);

    public PluginPageRegistry(IOptions<PluginPagesOptions> options, IInlineConstraintResolver constraintResolver, ILogger<PluginPageRegistry>? logger = null)
    {
        _Options = options.Value;
        _ConstraintResolver = constraintResolver;
        _Logger = logger ?? NullLogger<PluginPageRegistry>.Instance;
    }

    public event Action? Changed;

    public IReadOnlyList<PluginPageDescriptor> Pages =>
        _Plugins.Values
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .SelectMany(p => p.Routes.Select(r => r.Page).OrderBy(page => page.Order).ThenBy(page => page.Route, StringComparer.Ordinal))
            .ToArray();

    public IReadOnlyList<PluginPageRejection> Rejections =>
        _Plugins.Values
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .SelectMany(p => p.Rejections)
            .ToArray();

    public PluginPageRegistrationResult Register(string pluginKey, string pluginName, Assembly assembly)
    {
        var loadErrors = new List<PluginPageRejection>();
        IEnumerable<Type> types = GetLoadableTypes(assembly, pluginKey, pluginName, loadErrors);
        return Register(pluginKey, pluginName, assembly, types, loadErrors);
    }

    /// <summary>Registers only <paramref name="types"/> (the public overload scans the whole assembly).</summary>
    internal PluginPageRegistrationResult Register(string pluginKey, string pluginName, Assembly assembly, IEnumerable<Type> types,
        IEnumerable<PluginPageRejection>? loadErrors = null)
    {
        if (!IsValidKey(pluginKey))
        {
            var rejection = new PluginPageRejection(pluginKey, pluginName, null, null,
                "The plugin key must be non-empty and contain only letters, digits, '-', '_' or '.'.");
            return new PluginPageRegistrationResult(pluginKey, [], [rejection]);
        }

        string basePath = $"{_Options.BasePath.TrimEnd('/')}/{pluginKey}";
        var rejections = new List<PluginPageRejection>(loadErrors ?? []);
        var candidates = new List<(Type Type, PluginPageAttribute Attribute, PluginRoute Route)>();

        foreach (Type type in types)
        {
            List<PluginPageAttribute> attributes = GetPageAttributes(type, pluginKey, pluginName, rejections);
            if (attributes.Count == 0)
            {
                continue;
            }

            string? typeError = ValidateComponentType(type);
            foreach (PluginPageAttribute attribute in attributes)
            {
                if (typeError is not null)
                {
                    rejections.Add(new PluginPageRejection(pluginKey, pluginName, type.FullName, attribute.Route, typeError));
                    continue;
                }

                if (!PluginRoute.TryCreate(attribute.Route, _Options.AssetsSegment, type, _ConstraintResolver, out PluginRoute? route, out string error))
                {
                    rejections.Add(new PluginPageRejection(pluginKey, pluginName, type.FullName, attribute.Route, error));
                    continue;
                }

                candidates.Add((type, attribute, route!));
            }
        }

        // Deterministic winner on overlaps: whatever sorts first by type name, then by route, keeps the path.
        candidates.Sort((a, b) =>
        {
            int byType = string.CompareOrdinal(a.Type.FullName, b.Type.FullName);
            return byType != 0 ? byType : string.CompareOrdinal(a.Route.Route, b.Route.Route);
        });

        var claimedShapes = new Dictionary<string, (Type Type, PluginRoute Route)>(StringComparer.Ordinal);
        var accepted = new List<RegisteredRoute>();
        foreach ((Type type, PluginPageAttribute attribute, PluginRoute route) in candidates)
        {
            (Type Type, PluginRoute Route)? owner = route.Shapes
                .Select(shape => claimedShapes.TryGetValue(shape, out var existing) ? existing : ((Type, PluginRoute)?)null)
                .FirstOrDefault(existing => existing is not null);

            if (owner is { } conflict)
            {
                rejections.Add(new PluginPageRejection(pluginKey, pluginName, type.FullName, route.Route,
                    $"The route overlaps '{conflict.Route.Route}' of {conflict.Type.FullName}, which matches the same paths."));
                continue;
            }

            foreach (string shape in route.Shapes)
            {
                claimedShapes[shape] = (type, route);
            }

            bool showInMenu = !string.IsNullOrWhiteSpace(attribute.Title) && !attribute.HideFromMenu && !route.HasParameters;
            string href = route.Route == "/" ? basePath : basePath + route.Route;
            var page = new PluginPageDescriptor(pluginKey, pluginName, route.Route, attribute.Title, attribute.Icon, attribute.Order,
                showInMenu, type.FullName ?? type.Name, href);
            accepted.Add(new RegisteredRoute(page, type, route));
        }

        if (accepted.Count == 0 && rejections.Count == 0)
        {
            // Nothing to serve and nothing to report; drop any earlier registration under this key.
            Unregister(pluginKey);
            return new PluginPageRegistrationResult(pluginKey, [], []);
        }

        var entry = new PluginEntry(
            pluginKey,
            pluginName,
            basePath,
            assembly,
            assembly.ManifestModule.ModuleVersionId.ToString("N")[..12],
            accepted
                .OrderBy(r => r.Route.Precedence)
                .ThenBy(r => r.Page.Order)
                .ThenBy(r => r.Route.Route, StringComparer.Ordinal)
                .ToArray(),
            rejections.ToArray());

        lock (_WriteLock)
        {
            _Plugins = _Plugins.SetItem(pluginKey, entry);
        }

        RaiseChanged();
        return new PluginPageRegistrationResult(pluginKey, accepted.Select(r => r.Page).ToArray(), rejections.ToArray());
    }

    public bool Unregister(string pluginKey)
    {
        bool removed;
        lock (_WriteLock)
        {
            ImmutableDictionary<string, PluginEntry> before = _Plugins;
            _Plugins = before.Remove(pluginKey);
            removed = !ReferenceEquals(before, _Plugins);
        }

        if (removed)
        {
            RaiseChanged();
            PurgeCaches();
        }

        return removed;
    }

    public void Clear()
    {
        bool hadAny;
        lock (_WriteLock)
        {
            hadAny = !_Plugins.IsEmpty;
            _Plugins = _Plugins.Clear();
        }

        if (hadAny)
        {
            RaiseChanged();
            PurgeCaches();
        }
    }

    public bool IsRegistered(string pluginKey) => _Plugins.ContainsKey(pluginKey);

    public bool TryGetPluginName(string pluginKey, out string pluginName)
    {
        if (_Plugins.TryGetValue(pluginKey, out PluginEntry? entry))
        {
            pluginName = entry.Name;
            return true;
        }

        pluginName = string.Empty;
        return false;
    }

    public bool TryMatch(string pluginKey, string? relativePath, out PluginPageMatch match)
    {
        match = default!;
        if (!_Plugins.TryGetValue(pluginKey, out PluginEntry? entry))
        {
            return false;
        }

        string path = NormalizeRequestPath(relativePath);
        foreach (RegisteredRoute registered in entry.Routes)
        {
            if (!registered.Route.TryMatch(path, out var routeValues, out var parameters))
            {
                continue;
            }

            var context = new PluginPageContext(entry.Key, entry.Name, entry.BasePath, _Options.AssetsSegment, entry.AssetVersion,
                path, registered.Page.Title, routeValues);
            match = new PluginPageMatch(registered.Page, registered.ComponentType, routeValues, parameters, context);
            return true;
        }

        return false;
    }

    public bool TryOpenAsset(string pluginKey, string path, out Stream stream)
    {
        stream = Stream.Null;
        if (!_Plugins.TryGetValue(pluginKey, out PluginEntry? entry))
        {
            return false;
        }

        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || path.Contains('\\') || segments.Any(s => s is "." or ".."))
        {
            return false;
        }

        string wanted = $"{_Options.EmbeddedAssetsRoot.Trim('/')}/{string.Join('/', segments)}";
        string? resourceName = entry.Assembly.GetManifestResourceNames()
            .Where(name => string.Equals(name.Replace('\\', '/'), wanted, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => string.Equals(name, wanted, StringComparison.Ordinal) ? 0 : 1)
            .FirstOrDefault();

        Stream? opened = resourceName is null ? null : entry.Assembly.GetManifestResourceStream(resourceName);
        if (opened is null)
        {
            return false;
        }

        stream = opened;
        return true;
    }

    internal static string NormalizeRequestPath(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return "/";
        }

        string path = "/" + relativePath.Trim('/');
        return path;
    }

    private static bool IsValidKey(string? key) =>
        !string.IsNullOrEmpty(key) && key.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    private static string? ValidateComponentType(Type type)
    {
        if (!type.IsVisible)
        {
            return "The component must be public.";
        }

        if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
        {
            return "The component must be a non-abstract, non-generic class.";
        }

        if (!typeof(IComponent).IsAssignableFrom(type))
        {
            return $"The type must implement {nameof(IComponent)} (a Razor component).";
        }

        if (type.GetConstructor(Type.EmptyTypes) is null)
        {
            return "The component must have a public parameterless constructor.";
        }

        return null;
    }

    private List<PluginPageAttribute> GetPageAttributes(Type type, string pluginKey, string pluginName, List<PluginPageRejection> rejections)
    {
        try
        {
            var attributes = type.GetCustomAttributes<PluginPageAttribute>(inherit: false).ToList();
            if (attributes.Count == 0 && type.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(PluginPageAttribute).FullName))
            {
                // Same name, different type: the plugin loaded its own copy of the contract assembly.
                rejections.Add(new PluginPageRejection(pluginKey, pluginName, type.FullName, null,
                    $"The page uses a different copy of {typeof(PluginPageAttribute).Assembly.GetName().Name} than the host. " +
                    "Reference it with Private=\"false\" and don't ship it with the plugin."));
            }

            return attributes;
        }
        catch (Exception ex)
        {
            rejections.Add(new PluginPageRejection(pluginKey, pluginName, type.FullName, null, $"Its attributes could not be read: {ex.Message}"));
            return [];
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly, string pluginKey, string pluginName, List<PluginPageRejection> rejections)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            string reasons = string.Join("; ", ex.LoaderExceptions.Where(e => e is not null).Select(e => e!.Message).Distinct().Take(3));
            rejections.Add(new PluginPageRejection(pluginKey, pluginName, null, null, $"Some types could not be loaded and were skipped: {reasons}"));
            return ex.Types.Where(type => type is not null).Cast<Type>();
        }
    }

    private void RaiseChanged()
    {
        Action? handlers = Changed;
        if (handlers is null)
        {
            return;
        }

        foreach (Action handler in handlers.GetInvocationList().Cast<Action>())
        {
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "A plugin page registry change handler failed.");
            }
        }
    }

    private void PurgeCaches()
    {
        if (_Options.PurgeComponentCachesOnUnregister)
        {
            PluginComponentCaches.TryPurge(_Logger);
        }
    }

    private sealed record RegisteredRoute(PluginPageDescriptor Page, Type ComponentType, PluginRoute Route);

    private sealed record PluginEntry(
        string Key,
        string Name,
        string BasePath,
        Assembly Assembly,
        string AssetVersion,
        IReadOnlyList<RegisteredRoute> Routes,
        IReadOnlyList<PluginPageRejection> Rejections);
}
