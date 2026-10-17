namespace WebUI.Services;

/// <summary>
/// A service whose state is rendered by the dashboard. <see cref="Changed"/> may be raised from any thread.
/// </summary>
public interface IStateNotifier
{
    event Action? Changed;
}
