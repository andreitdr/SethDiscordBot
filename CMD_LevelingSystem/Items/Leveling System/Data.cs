using Discord.WebSocket;

using System;
using System.IO;
public static class Data
{
    private static readonly string folder = @".\Data\Resources\LevelingSystem\";
    public static void registerPlayer(SocketGuildUser user)
    {
        ulong id = user.Id;
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, id.ToString() + ".data"), "Level=0,EXP=0,REXP=100");
    }

    public static int GetLevel(ulong id) => int.Parse(File.ReadAllText(Path.Combine(folder, id.ToString() + ".data")).Split(',')[0].Split('=')[1]);


    public static Int64 GetExp(ulong id) => Int64.Parse(File.ReadAllText(Path.Combine(folder, id.ToString() + ".data")).Split(',')[1].Split('=')[1]);


    public static Int64 GetReqEXP(ulong id) => Int64.Parse(File.ReadAllText(Path.Combine(folder, id.ToString() + ".data")).Split(',')[2].Split('=')[1]);


}
