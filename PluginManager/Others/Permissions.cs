using Discord;
using Discord.WebSocket;

using System.Linq;

namespace PluginManager.Others
{
    public static class Permissions
    {
        public static bool hasPermission(this IRole role, GuildPermission permission) => role.Permissions.Has(permission);

        public static bool hasRole(this SocketGuildUser user, IRole role) => user.Roles.Contains(role);

        public static bool hasPermission(this SocketGuildUser user, GuildPermission permission)
                                => user.Roles.Where(role => role.hasPermission(permission)).Any();

    }


}