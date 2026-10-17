using System.Globalization;

namespace PluginPages;

/// <summary>Converts captured route text to a component parameter's type, like Blazor's router does for <c>@page</c> routes.</summary>
internal static class RouteValueConverter
{
    public static bool TryConvert(string text, Type targetType, out object? value)
    {
        Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        CultureInfo invariant = CultureInfo.InvariantCulture;
        value = null;

        if (type == typeof(string) || type == typeof(object))
        {
            value = text;
            return true;
        }

        bool ok;
        if (type == typeof(int)) { ok = int.TryParse(text, NumberStyles.Integer, invariant, out int v); value = v; }
        else if (type == typeof(long)) { ok = long.TryParse(text, NumberStyles.Integer, invariant, out long v); value = v; }
        else if (type == typeof(ulong)) { ok = ulong.TryParse(text, NumberStyles.Integer, invariant, out ulong v); value = v; }
        else if (type == typeof(bool)) { ok = bool.TryParse(text, out bool v); value = v; }
        else if (type == typeof(Guid)) { ok = Guid.TryParse(text, out Guid v); value = v; }
        else if (type == typeof(decimal)) { ok = decimal.TryParse(text, NumberStyles.Number, invariant, out decimal v); value = v; }
        else if (type == typeof(double)) { ok = double.TryParse(text, NumberStyles.Float, invariant, out double v); value = v; }
        else if (type == typeof(float)) { ok = float.TryParse(text, NumberStyles.Float, invariant, out float v); value = v; }
        else if (type == typeof(DateTime)) { ok = DateTime.TryParse(text, invariant, DateTimeStyles.None, out DateTime v); value = v; }
        else if (type.IsEnum) { ok = Enum.TryParse(type, text, ignoreCase: true, out object? v); value = v; }
        else { ok = false; }

        if (!ok)
        {
            value = null;
        }

        return ok;
    }
}
