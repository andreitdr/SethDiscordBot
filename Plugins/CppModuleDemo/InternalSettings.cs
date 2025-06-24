using CppCompatibilityModule;
using CppCompatibilityModule.Extern;

namespace CppModuleDemo;

internal static class InternalSettings
{
    internal static ExternalApplicationHandler? ExternalApplicationHandler { get; set; } = null;
    internal static Guid DemoModuleInternalId { get; set; } = Guid.Empty;
}