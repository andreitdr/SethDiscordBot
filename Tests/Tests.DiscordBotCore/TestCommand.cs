using DiscordBotCore.PluginCore.Interfaces;

namespace Tests.DiscordBotCore;

internal sealed class TestCommand(string command, bool requireAdmin = false, params string[] aliases) : IDbCommand
{
    public string Command => command;
    public List<string> Aliases => aliases.ToList();
    public string Description => $"Does {command}";
    public string Usage => $"{command} <x>";
    public bool RequireAdmin => requireAdmin;
}
