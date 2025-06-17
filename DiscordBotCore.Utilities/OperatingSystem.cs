namespace DiscordBotCore.Utilities;

public class OperatingSystem
{
    public enum OperatingSystemEnum : int
    {
        Windows = 0,
        Linux = 1,
        MacOs = 2
    }
    
    public static OperatingSystemEnum GetOperatingSystem()
    {
        if(System.OperatingSystem.IsLinux()) return OperatingSystemEnum.Linux;
        if(System.OperatingSystem.IsWindows()) return OperatingSystemEnum.Windows;
        if(System.OperatingSystem.IsMacOS()) return OperatingSystemEnum.MacOs;
        throw new PlatformNotSupportedException();
    }
    
    public static string GetOperatingSystemString(OperatingSystemEnum os)
    {
        return os switch
        {
            OperatingSystemEnum.Windows => "Windows",
            OperatingSystemEnum.Linux => "Linux",
            OperatingSystemEnum.MacOs => "MacOS",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public static OperatingSystemEnum GetOperatingSystemFromString(string os)
    {
        return os.ToLower() switch
        {
            "windows" => OperatingSystemEnum.Windows,
            "linux" => OperatingSystemEnum.Linux,
            "macos" => OperatingSystemEnum.MacOs,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public static int GetOperatingSystemInt()
    {
        return (int) GetOperatingSystem();
    }
}