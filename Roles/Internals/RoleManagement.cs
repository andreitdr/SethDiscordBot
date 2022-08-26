using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using PluginManager.Others;

namespace Roles.Internals
{
    internal static class RoleManagement
    {
        internal static async void AddRole(this SocketGuildUser user, string roleName)
        {
            string role = roleName;
            IRole? r    = user.Guild.Roles.FirstOrDefault(rl => rl.Name == role || rl.Mention == role);
            if (r is null)
                throw new Exception("The role does not exist");

            try
            {
                await user.AddRoleAsync(r);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Permission", StringComparison.CurrentCultureIgnoreCase))
                    throw new Exception("Insufficient permissions");
            }
        }

        internal static async void AddRole(this SocketGuildUser user, IRole role)
        {
            try
            {
                await user.AddRoleAsync(role);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Permission", StringComparison.CurrentCultureIgnoreCase))
                    throw new Exception("Insufficient permissions");
            }
        }

        internal static async void AddRoles(this SocketGuildUser user, string[] roleNames)
        {
            foreach (string rolename in roleNames)
            {
                string roleName = rolename;
                IRole? r        = user.Guild.Roles.FirstOrDefault(rl => rl.Name == roleName || rl.Mention == roleName);
                if (r is null)
                    throw new Exception("The role does not exist");

                try
                {
                    await user.AddRoleAsync(r);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Permission", StringComparison.CurrentCultureIgnoreCase))
                        throw new Exception("Insufficient permissions");
                }
            }
        }

        internal static async void AddRoles(this SocketGuildUser user, IEnumerable<IRole> roles)
        {
            try
            {
                await user.AddRolesAsync(roles);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Permission", StringComparison.CurrentCultureIgnoreCase))
                    throw new Exception("Insufficient permissions");
            }
        }
    }
}
