using System.Linq;
using Discord;
using Discord.WebSocket;

namespace PluginManager.Others.Permissions;

/// <summary>
///     A class whith all discord permissions
/// </summary>
public static class DiscordPermissions
{
    /// <summary>
    ///     Checks if the role has the specified permission
    /// </summary>
    /// <param name="role">The role</param>
    /// <param name="permission">The permission</param>
    /// <returns></returns>
    public static bool hasPermission(this IRole role, GuildPermission permission)
    {
        return role.Permissions.Has(permission);
    }

    /// <summary>
    ///     Check if user has the specified role
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="role">The role</param>
    /// <returns></returns>
    public static bool hasRole(this SocketGuildUser user, IRole role)
    {
        return user.Roles.Contains(role);
    }

    /// <summary>
    ///     Check if user has the specified permission
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="permission">The permission</param>
    /// <returns></returns>
    public static bool hasPermission(this SocketGuildUser user, GuildPermission permission)
    {
        return user.Roles.Where(role => role.hasPermission(permission)).Any() || user.Guild.Owner == user;
    }

    /// <summary>
    ///     Check if user is administrator of server
    /// </summary>
    /// <param name="user">The user</param>
    /// <returns></returns>
    public static bool isAdmin(this SocketGuildUser user)
    {
        return user.hasPermission(GuildPermission.Administrator);
    }

    /// <summary>
    ///     Check if user is administrator of server
    /// </summary>
    /// <param name="user">The user</param>
    /// <returns></returns>
    public static bool isAdmin(this SocketUser user)
    {
        return isAdmin((SocketGuildUser)user);
    }
}
