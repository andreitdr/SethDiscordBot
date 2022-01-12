using Discord;
using Discord.WebSocket;

using System.Linq;

namespace PluginManager.Others.Permissions
{
    public static class DiscordPermissions
    {
        public static bool hasPermission(this IRole role, GuildPermission permission) => role.Permissions.Has(permission);

        public static bool hasRole(this SocketGuildUser user, IRole role) => user.Roles.Contains(role);

        public static bool hasPermission(this SocketGuildUser user, GuildPermission permission)
                                => user.Roles.Where(role => role.hasPermission(permission)).Any() || user.Guild.Owner == user;
        public static bool isAdmin(this SocketGuildUser user) => user.hasPermission(GuildPermission.Administrator);
        public static bool isAdmin(this SocketUser user) => isAdmin((SocketGuildUser)user);
    }




}