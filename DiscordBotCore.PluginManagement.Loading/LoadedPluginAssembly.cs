using System.Reflection;
using DiscordBotCore.PluginManagement.Models;

namespace DiscordBotCore.PluginManagement.Loading;

/// <summary>
/// A plugin's main assembly, as loaded by <see cref="PluginLoader"/> into its collectible load context.
/// </summary>
public sealed record LoadedPluginAssembly(LocalPlugin Plugin, Assembly Assembly);
