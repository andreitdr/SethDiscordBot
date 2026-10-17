using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace PluginPages;

/// <summary>A validated, compiled plugin route.</summary>
internal sealed class PluginRoute
{
    private readonly TemplateMatcher _Matcher;
    private readonly IReadOnlyDictionary<string, IRouteConstraint[]> _Constraints;
    private readonly IReadOnlyDictionary<string, PropertyInfo> _ComponentParameters;

    private PluginRoute(
        string route,
        RouteTemplate template,
        IReadOnlyList<string> shapes,
        IReadOnlyDictionary<string, IRouteConstraint[]> constraints,
        Type componentType)
    {
        Route = route;
        Template = template;
        Shapes = shapes;
        Precedence = RoutePrecedence.ComputeInbound(template);
        _Constraints = constraints;
        _Matcher = new TemplateMatcher(template, GetDefaults(template));
        _ComponentParameters = componentType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.IsDefined(typeof(ParameterAttribute), inherit: true))
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The normalized route, such as <c>/</c> or <c>/users/{id:long}</c>.</summary>
    public string Route { get; }

    public RouteTemplate Template { get; }

    /// <summary>
    /// The route with literals lower-cased and every parameter reduced to a placeholder; two routes with a shape in common
    /// match the same paths. Optional parameters produce one shape with and one without the last segment.
    /// </summary>
    public IReadOnlyList<string> Shapes { get; }

    /// <summary>Lower is more specific (ASP.NET Core inbound precedence: literals, then constrained parameters, then parameters, then catch-alls).</summary>
    public decimal Precedence { get; }

    public bool HasParameters => Template.Parameters.Count > 0;

    public static bool TryCreate(string? route, string reservedSegment, Type componentType, IInlineConstraintResolver constraintResolver,
        out PluginRoute? compiled, out string error)
    {
        compiled = null;
        if (!TryNormalize(route, reservedSegment, out string normalized, out error))
        {
            return false;
        }

        RouteTemplate template;
        try
        {
            template = TemplateParser.Parse(normalized.TrimStart('/'));
        }
        catch (ArgumentException ex)
        {
            error = $"The route template is invalid: {ex.Message}";
            return false;
        }

        var constraints = new Dictionary<string, IRouteConstraint[]>(StringComparer.OrdinalIgnoreCase);
        foreach (TemplatePart parameter in template.Parameters)
        {
            var resolved = new List<IRouteConstraint>();
            foreach (InlineConstraint inline in parameter.InlineConstraints)
            {
                IRouteConstraint? constraint;
                try
                {
                    constraint = constraintResolver.ResolveConstraint(inline.Constraint);
                }
                catch (Exception ex)
                {
                    error = $"The constraint '{inline.Constraint}' on '{parameter.Name}' is invalid: {ex.Message}";
                    return false;
                }

                if (constraint is null)
                {
                    error = $"The constraint '{inline.Constraint}' on '{parameter.Name}' is unknown.";
                    return false;
                }

                resolved.Add(constraint);
            }

            constraints[parameter.Name!] = resolved.ToArray();
        }

        compiled = new PluginRoute(normalized, template, ComputeShapes(template), constraints, componentType);
        return true;
    }

    /// <summary>Checks the route's syntax and returns it with a single leading slash and no trailing one.</summary>
    internal static bool TryNormalize(string? route, string reservedSegment, out string normalized, out string error)
    {
        normalized = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(route))
        {
            error = "The route is empty. Use \"/\" for the plugin's index page.";
            return false;
        }

        if (!route.StartsWith('/'))
        {
            error = "The route must start with '/'.";
            return false;
        }

        if (ContainsQueryOrFragment(route))
        {
            error = "The route must not contain a query ('?' outside a parameter), '#' or '\\'.";
            return false;
        }

        string trimmed = route.Length > 1 ? route.TrimEnd('/') : route;
        if (trimmed.Length == 0 || trimmed == "/")
        {
            normalized = "/";
            return true;
        }

        string[] segments = trimmed[1..].Split('/');
        if (segments.Any(segment => segment.Length == 0))
        {
            error = "The route must not contain empty segments ('//').";
            return false;
        }

        if (segments.Any(segment => segment is "." or ".."))
        {
            error = "The route must not contain '.' or '..' segments.";
            return false;
        }

        if (string.Equals(segments[0], reservedSegment, StringComparison.OrdinalIgnoreCase))
        {
            error = $"The first segment '{reservedSegment}' is reserved for the plugin's embedded files.";
            return false;
        }

        normalized = "/" + string.Join('/', segments);
        return true;
    }

    /// <summary>'?' is only allowed inside a parameter, where it marks it optional (<c>{year?}</c>).</summary>
    private static bool ContainsQueryOrFragment(string route)
    {
        int depth = 0;
        foreach (char c in route)
        {
            switch (c)
            {
                case '{': depth++; break;
                case '}': depth = Math.Max(0, depth - 1); break;
                case '#' or '\\': return true;
                case '?' when depth == 0: return true;
            }
        }

        return false;
    }

    /// <summary>Matches a normalized path and binds the captured values to the component's parameters.</summary>
    public bool TryMatch(string path, out Dictionary<string, string?> routeValues, out Dictionary<string, object> parameters)
    {
        routeValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        parameters = new Dictionary<string, object>(StringComparer.Ordinal);

        var values = new RouteValueDictionary();
        if (!_Matcher.TryMatch(path, values))
        {
            return false;
        }

        foreach (TemplatePart parameter in Template.Parameters)
        {
            string name = parameter.Name!;
            values.TryGetValue(name, out object? raw);
            string? text = raw is null ? null : Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture);

            if (string.IsNullOrEmpty(text))
            {
                if (!parameter.IsOptional && !parameter.IsCatchAll && parameter.DefaultValue is null)
                {
                    return false;
                }
            }
            else
            {
                foreach (IRouteConstraint constraint in _Constraints[name])
                {
                    if (!constraint.Match(null, null, name, values, RouteDirection.IncomingRequest))
                    {
                        return false;
                    }
                }
            }

            routeValues[name] = text;

            if (text is not null && _ComponentParameters.TryGetValue(name, out PropertyInfo? property))
            {
                if (!RouteValueConverter.TryConvert(text, property.PropertyType, out object? converted) || converted is null)
                {
                    return false;
                }

                parameters[property.Name] = converted;
            }
        }

        return true;
    }

    private static RouteValueDictionary GetDefaults(RouteTemplate template)
    {
        var defaults = new RouteValueDictionary();
        foreach (TemplatePart parameter in template.Parameters)
        {
            if (parameter.DefaultValue is not null)
            {
                defaults[parameter.Name!] = parameter.DefaultValue;
            }
        }

        return defaults;
    }

    private static IReadOnlyList<string> ComputeShapes(RouteTemplate template)
    {
        var segments = new List<string>();
        bool lastIsOptional = false;

        for (int i = 0; i < template.Segments.Count; i++)
        {
            TemplateSegment segment = template.Segments[i];
            bool isLast = i == template.Segments.Count - 1;

            if (segment.Parts.Count == 1 && segment.Parts[0].IsParameter)
            {
                TemplatePart part = segment.Parts[0];
                if (part.IsCatchAll)
                {
                    segments.Add("{*}");
                }
                else
                {
                    segments.Add("{}");
                    lastIsOptional = isLast && (part.IsOptional || part.DefaultValue is not null);
                }

                continue;
            }

            segments.Add(string.Concat(segment.Parts.Select(part =>
                part.IsParameter ? (part.IsOptional ? "{?}" : "{}") : part.Text!.ToLowerInvariant())));
        }

        string full = "/" + string.Join('/', segments);
        if (!lastIsOptional)
        {
            return [full];
        }

        return [full, "/" + string.Join('/', segments.Take(segments.Count - 1))];
    }
}
