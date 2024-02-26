using System.Diagnostics;
using PluginManager;
using PluginManager.Online.Helpers;
using PluginManager.Others;
using PluginManager.Plugin;

if (args.Length == 1)
{
    PluginOnlineInfo? result = await JsonManager.ConvertFromJson<PluginOnlineInfo>(args[0]);
    // print all rows
    Console.WriteLine($"Name: {result.Name}");
    Console.WriteLine($"Version: {result.Version.ToShortString()}");
    Console.WriteLine($"Description: {result.Description}");
    Console.WriteLine($"Download link: {result.DownLoadLink}");
    Console.WriteLine($"Supported OS: {((result.SupportedOS & OSType.WINDOWS) != 0 ? "Windows" : "")} {((result.SupportedOS & OSType.LINUX) != 0 ? "Linux" : "")} {((result.SupportedOS & OSType.MACOSX) != 0 ? "MacOSX" : "")}");
    Console.WriteLine($"Has dependencies: {result.HasDependencies}");
    Console.WriteLine($"Dependencies: {result.Dependencies.Count}");

    return;
}


var _levelingSystem = new PluginOnlineInfo(
    "Leveling System",
    new PluginVersion(0, 0, 1),
    "A simple leveling system for your server",
    "https://github.com/andreitdr/SethPlugins/raw/releases/LevelingSystem/LevelingSystem.dll",
    OSType.WINDOWS | OSType.LINUX
);

var _musicPlayerWindows = new PluginOnlineInfo(
    "Music Player (Windows)", new PluginVersion(0, 0, 1),
    "A simple music player for your server",
    "https://github.com/andreitdr/SethPlugins/raw/releases/MusicPlayer/MusicPlayer.dll",
    OSType.WINDOWS,
    [
        new OnlineDependencyInfo("https://github.com/andreitdr/SethPlugins/raw/releases/MusicPlayer/libs/windows/ffmpeg.exe", "ffmpeg.exe"),
        new OnlineDependencyInfo("https://github.com/andreitdr/SethPlugins/raw/releases/MusicPlayer/libs/windows/libopus.dll", "libopus.dll"),
        new OnlineDependencyInfo("https://github.com/andreitdr/SethPlugins/raw/releases/MusicPlayer/libs/windows/libsodium.dll", "libsodium.dll"),
        new OnlineDependencyInfo("https://github.com/andreitdr/SethPlugins/raw/releases/MusicPlayer/libs/windows/opus.dll", "opus.dll"),
        new OnlineDependencyInfo("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_x86.exe", "yt-dlp.exe")
    ]
);

var _musicPlayerLINUX = new PluginOnlineInfo(
    "Music Player (Linux)", new PluginVersion(0, 0, 1),
    "A simple music player for your server",
    "https://github.com/andreitdr/SethPlugins/raw/releases/MusicPlayer/MusicPlayer.dll",
    OSType.LINUX,
    [
        new OnlineDependencyInfo("https://github.com/andreitdr/SethPlugins/raw/releases/MusicPlayer/libs/linux/ffmpeg", "ffmpeg"),
        new OnlineDependencyInfo("https://github.com/andreitdr/SethPlugins/raw/releases/MusicPlayer/libs/linux/libopus.so", "libopus.so"),
        new OnlineDependencyInfo("https://github.com/andreitdr/SethPlugins/raw/releases/MusicPlayer/libs/linux/libsodium.so", "libsodium.so"),
        new OnlineDependencyInfo("https://github.com/yt-dlp/yt-dlp/releases/download/2023.10.13/yt-dlp", "yt-dlp")
    ]
);

List<PluginOnlineInfo> plugins =
[
    _levelingSystem,
    _musicPlayerWindows,
    _musicPlayerLINUX
];

Directory.CreateDirectory("output");
await JsonManager.SaveToJsonFile("./output/PluginsList.json", plugins);

Process.Start("notepad.exe", "./output/PluginsList.json");