namespace WebUI.Models;

public class Notification
{
    public string Message { get; set; }
    public NotificationType Type { get; set; } 
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public int DelayMs { get; set; } = 5000;
}
