using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PluginPages;

/// <summary>
/// Blazor keeps static per-component-type caches (factories, parameter writers, cascading parameter info). An entry for a
/// plugin type keeps the plugin's collectible load context alive after it is unloaded. This removes those entries: every
/// static <see cref="Type"/>-keyed dictionary in the Components assemblies loses its keys from collectible assemblies, and
/// the static <c>ClearCache</c> methods the framework uses for hot reload are called for caches of any other shape.
/// </summary>
public static class PluginComponentCaches
{
    private const string ClearCacheMethodName = "ClearCache";

    /// <summary>Drops plugin types from the static caches of the loaded ASP.NET Core Components assemblies. Never throws.</summary>
    /// <returns>How many caches were cleared or had plugin entries removed.</returns>
    public static int TryPurge(ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        int cleared = 0;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string? name = assembly.GetName().Name;
            if (name is null || !name.StartsWith("Microsoft.AspNetCore.Components", StringComparison.Ordinal))
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }

            foreach (Type type in types)
            {
                if (type.ContainsGenericParameters)
                {
                    continue;
                }

                cleared += RemoveCollectibleTypeKeys(type, logger);

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (method.Name != ClearCacheMethodName || method.ContainsGenericParameters)
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    object?[]? arguments = parameters.Length switch
                    {
                        0 => [],
                        1 when parameters[0].ParameterType == typeof(Type[]) => [null],
                        _ => null
                    };

                    if (arguments is null)
                    {
                        continue;
                    }

                    try
                    {
                        method.Invoke(null, arguments);
                        cleared++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Clearing {Type}.{Method} failed.", type.FullName, method.Name);
                    }
                }
            }
        }

        return cleared;
    }

    private static int RemoveCollectibleTypeKeys(Type type, ILogger logger)
    {
        int cleaned = 0;
        foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (!IsTypeKeyedDictionary(field.FieldType))
            {
                continue;
            }

            try
            {
                if (field.GetValue(null) is not System.Collections.IDictionary dictionary)
                {
                    continue;
                }

                List<Type> pluginKeys = dictionary.Keys.OfType<Type>().Where(key => key.Assembly.IsCollectible).ToList();
                foreach (Type key in pluginKeys)
                {
                    dictionary.Remove(key);
                }

                if (pluginKeys.Count > 0)
                {
                    cleaned++;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Cleaning {Type}.{Field} failed.", type.FullName, field.Name);
            }
        }

        return cleaned;
    }

    private static bool IsTypeKeyedDictionary(Type fieldType)
    {
        for (Type? current = fieldType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericArguments() is [var key, _] && key == typeof(Type)
                && typeof(System.Collections.IDictionary).IsAssignableFrom(current))
            {
                return true;
            }
        }

        return false;
    }
}
