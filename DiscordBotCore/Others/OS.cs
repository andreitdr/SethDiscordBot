using System;

namespace DiscordBotCore.Others;

public class OS
{
    public enum OperatingSystem : int
    {
        Windows = 0,
        Linux = 1,
        MacOS = 2
    }
    
    public static OperatingSystem GetOperatingSystem()
    {
        if(System.OperatingSystem.IsLinux()) return OperatingSystem.Linux;
        if(System.OperatingSystem.IsWindows()) return OperatingSystem.Windows;
        if(System.OperatingSystem.IsMacOS()) return OperatingSystem.MacOS;
        throw new PlatformNotSupportedException();
    }
    
    public static string GetOperatingSystemString(OperatingSystem os)
    {
        return os switch
        {
            OperatingSystem.Windows => "Windows",
            OperatingSystem.Linux => "Linux",
            OperatingSystem.MacOS => "MacOS",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public static OperatingSystem GetOperatingSystemFromString(string os)
    {
        return os.ToLower() switch
        {
            "windows" => OperatingSystem.Windows,
            "linux" => OperatingSystem.Linux,
            "macos" => OperatingSystem.MacOS,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public static int GetOperatingSystemInt()
    {
        return (int) GetOperatingSystem();
    }
}