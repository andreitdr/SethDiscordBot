using System.Threading.Tasks;

namespace PluginManager.UX;

public static class UxHandler 
{
    private static IOutputModel? _model;
    
    public static void Init()
    {
        _model = Config.AppSettings["UI"] switch
        {
            "KDE"     => new Linux.KDE(),
            "CONSOLE" => new Other.Console(),
            _         => null
        };
        
    }
    
    public static async Task ShowMessageBox(string title, string message, MessageBoxType type = MessageBoxType.Info)
    {
        await _model.ShowMessageBox(title, message, type);
    }
    
    public static async Task<string> ShowInputBox(string title, string message)
    {
        return await _model.ShowInputBox(title, message);
    }
    
    public static async Task ShowMessageBox(string message)
    {
        await _model.ShowMessageBox(message);
    }
    
    public static async Task<int> ShowMessageBox(string title, string message, MessageBoxButtons buttons, bool isWarning)
    {
        return await _model.ShowMessageBox(title, message, buttons, isWarning);
    }
    
    public static async Task ShowNotification(string title, string message, int timeout_seconds = 5)
    {
        await _model.ShowNotification(title, message, timeout_seconds);
    }
    
}
