using DiscordBotCore.Utilities.Responses;
using Microsoft.AspNetCore.Components;
using Radzen;
using WebUI.Components.Dashboard;
using WebUI.Services;

namespace WebUI.Components.Panels;

/// <summary>
/// Base for dashboard panels: re-renders when an observed service changes and stops listening when the panel goes away.
/// </summary>
public abstract class PanelBase : ComponentBase, IDisposable
{
    private readonly List<IStateNotifier> _Observed = new();

    [Inject] protected NotificationService Toasts { get; set; } = default!;
    [Inject] protected DialogService Dialogs { get; set; } = default!;

    protected void Observe(params IStateNotifier[] notifiers)
    {
        foreach (var notifier in notifiers)
        {
            notifier.Changed += OnObservedChanged;
            _Observed.Add(notifier);
        }
    }

    protected virtual void OnObservedChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>Shows a toast for the outcome of an action. Failures always show; success only when given a message.</summary>
    protected void Report(IResponse<bool> result, string? successMessage = null)
    {
        if (!result.IsSuccess)
        {
            ShowError(string.IsNullOrWhiteSpace(result.Message) ? "Something went wrong. See the console for details." : result.Message);
        }
        else if (successMessage is not null)
        {
            Toasts.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = successMessage, Duration = 3000 });
        }
    }

    protected void ShowError(string message) =>
        Toasts.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = message, Duration = 6000 });

    protected async Task<bool> ConfirmAsync(string message, string title, string okText, bool destructive = true) =>
        await Dialogs.OpenAsync<ConfirmDialog>(title,
            new Dictionary<string, object?>
            {
                [nameof(ConfirmDialog.Message)] = message,
                [nameof(ConfirmDialog.OkText)] = okText,
                [nameof(ConfirmDialog.Destructive)] = destructive
            },
            new DialogOptions { Width = "420px", CloseDialogOnEsc = true }) is true;

    public virtual void Dispose()
    {
        foreach (var notifier in _Observed)
        {
            notifier.Changed -= OnObservedChanged;
        }

        _Observed.Clear();
    }
}
