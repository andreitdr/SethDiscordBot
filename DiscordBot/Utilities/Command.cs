using System;
using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot.Utilities;

public class ConsoleCommand
{
    public string? CommandName { get; init; }
    public string? Description { get; init; }
    public string? Usage { get; init; }
    public Action<string[]>? Action { get; init; }
}