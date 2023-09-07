using System.IO;

namespace PluginManager.Others.Logger;

public class LogFile
{
    public FileInfo File { get; set; }
    public LogFile(string path)
    {
        File = new FileInfo(path);
    }
    
    public void Write(string message)
    {
        using var sw = File.AppendText();
        sw.WriteLine(message);
    }
    
    public void Write(string message, LogLevel type)
    {
        using var sw = File.AppendText();
        sw.WriteLine($"[{type}] {message}");
    }
    
    public void Write(string message, string sender, LogLevel type)
    {
        using var sw = File.AppendText();
        sw.WriteLine($"[{type}] [{sender}] {message}");
    }

    public void Write(LogMessage logMessage)
    {
        using var sw = File.AppendText();
        sw.WriteLine(logMessage.ToString());
    }
}