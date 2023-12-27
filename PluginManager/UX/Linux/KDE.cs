using System.Diagnostics;
using System.Threading.Tasks;

namespace PluginManager.UX.Linux;

internal class KDE : IOutputModel
{

    public async Task ShowMessageBox(string title, string message, MessageBoxType type)
    {
        var process = new Process();
        process.StartInfo.FileName = "kdialog";
        
        string typeStr = type switch
        {
            MessageBoxType.Info => "msgbox",
            MessageBoxType.Warning => "sorry",
            MessageBoxType.Error => "error",
            _ => "info"
        };
        
        process.StartInfo.Arguments = $"--title \"{title}\" --{typeStr} \"{message}\"";
        process.Start();
        await process.WaitForExitAsync();
    }

    public async Task<string> ShowInputBox(string title, string message)
    {
        var process = new Process();
        process.StartInfo.FileName = "kdialog";
        process.StartInfo.Arguments = $"--title \"{title}\" --inputbox \"{message}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        
        await process.WaitForExitAsync();
        return await process.StandardOutput.ReadToEndAsync();
    }
    
    public async Task ShowMessageBox(string message)
    {
        var process = new Process();
        process.StartInfo.FileName = "kdialog";
        process.StartInfo.Arguments = $"--msgbox \"{message}\"";
        process.Start();
        await process.WaitForExitAsync();
    }
    
    public async Task<int> ShowMessageBox(string title, string message, MessageBoxButtons buttons, bool isWarning)
    {
        var process = new Process();
        process.StartInfo.FileName = "kdialog";
        
        string buttonsStr = buttons switch
        {
            MessageBoxButtons.YesNo => "yesno",
            MessageBoxButtons.YesNoCancel => "yesnocancel",
            MessageBoxButtons.ContinueCancel => "continuecancel",
            _ => "yesno"
        };
        string typeStr = isWarning ? "warning" : "";
        process.StartInfo.Arguments = $"--title \"{title}\" --{typeStr}{buttonsStr} \"{message}\"";
        process.Start();
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
    
    public async Task ShowNotification(string title, string message, int timeout_seconds = 5)
    {
        var process = new Process();
        process.StartInfo.FileName = "kdialog";
        process.StartInfo.Arguments = $"--title \"{title}\" --passivepopup \"{message}\" {timeout_seconds}";
        process.Start();
        await process.WaitForExitAsync();
    }
}
