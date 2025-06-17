using WebUI.Models;
namespace WebUI.Services;

public class NotificationService
{
    public event Action<Notification> OnNotify;

    public void Notify(string message, NotificationType type = NotificationType.Info)
    {
        OnNotify?.Invoke(new Notification { Message = message, Type = type });
    }
}
