using System.Threading.Tasks;

namespace PluginManager.UX;

public enum MessageBoxType
{
    Info,
    Warning,
    Error
}

public enum MessageBoxButtons
{
    YesNo,
    YesNoCancel,
    ContinueCancel,
}

internal interface IOutputModel
{
    
    internal Task ShowMessageBox(string title, string message, MessageBoxType type);
    internal Task<string> ShowInputBox(string title, string message);

    internal Task ShowMessageBox(string message);
    internal Task<int> ShowMessageBox(string title, string message, MessageBoxButtons buttons, bool isWarning);
    
    internal Task ShowNotification(string title, string message, int timeout_seconds = 5);
}
