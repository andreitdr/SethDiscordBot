using System;

namespace DiscordBot
{
    internal interface IStartupAction
    {
        string Command { get; init; }
        Action<string[]> RunAction { get; init; }
    }
}