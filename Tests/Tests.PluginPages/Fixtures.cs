using Microsoft.AspNetCore.Components;
using PluginPages;

namespace Tests.PluginPages.Fixtures;

[PluginPage("/", Title = "Home")]
public sealed class HomePage : ComponentBase;

[PluginPage("/settings", Title = "Settings", Order = 1)]
public sealed class SettingsPage : ComponentBase;

[PluginPage("/items/new", Title = "New item")]
public sealed class NewItemPage : ComponentBase;

[PluginPage("/items/{id:int}")]
public sealed class ItemPage : ComponentBase
{
    [Parameter] public int Id { get; set; }
}

[PluginPage("/items/{number}")]
public sealed class ConflictingItemPage : ComponentBase;

[PluginPage("/files/{*path}")]
public sealed class FilesPage : ComponentBase
{
    [Parameter] public string? Path { get; set; }
}

[PluginPage("/files/readme")]
public sealed class ReadmePage : ComponentBase;

[PluginPage("/reports/{year?}")]
public sealed class ReportsPage : ComponentBase
{
    [Parameter] public int? Year { get; set; }
}

[PluginPage("/reports")]
public sealed class ReportsIndexPage : ComponentBase;

[PluginPage("/users/{id:guid}")]
public sealed class UserPage : ComponentBase
{
    [Parameter] public Guid Id { get; set; }
}

[PluginPage("/a")]
[PluginPage("/b", Title = "B")]
public sealed class TwoRoutesPage : ComponentBase;

[PluginPage("/bad/{id:nope}")]
public sealed class UnknownConstraintPage : ComponentBase;

[PluginPage("/_assets/x")]
public sealed class ReservedSegmentPage : ComponentBase;

[PluginPage("/not-a-component")]
public sealed class NotAComponent;

[PluginPage("/abstract")]
public abstract class AbstractPage : ComponentBase;

[PluginPage("/ctor")]
public sealed class NoDefaultConstructorPage : ComponentBase
{
    public NoDefaultConstructorPage(int value)
    {
    }
}

[PluginPage("/hidden")]
internal sealed class InternalPage : ComponentBase;
