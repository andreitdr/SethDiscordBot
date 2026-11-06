using Humanizer;

namespace Tests.VersionPluginB;

public static class VersionReader
{
    /// <summary>Uses the package, so its assembly really is loaded for this plugin.</summary>
    public static string Read() => typeof(StringHumanizeExtensions).Assembly.GetName().Version + " " + "some_text".Humanize();
}
