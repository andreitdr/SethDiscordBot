namespace WebUI.Models;

/// <summary>
/// Notification for the Notification System
/// </summary>
public class Notification
{
    /// <summary>
    /// The message of the notification.
    /// </summary>
    public string Message { get; set; }
    /// <summary>
    /// The type of the notification
    /// </summary>
    public NotificationType Type { get; set; } 
    /// <summary>
    /// The time when the notification was being sent
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    /// <summary>
    /// The time in milliseconds while the notification should be displayed.
    /// The notification should disappear after this time
    /// </summary>
    public int DelayMs { get; set; } = 1500;
}
