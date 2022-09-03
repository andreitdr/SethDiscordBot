using System.IO.Compression;
using System.Runtime.CompilerServices;

using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

using Microsoft.Win32.SafeHandles;

using PluginManager.Interfaces;
using PluginManager.Others;

using Roles.Internals;

namespace Roles
{
    public class AddRole : DBCommand
    {
        public string Command => "addrole";

        public List<string> Aliases => new() { "ar", "addr", "roleadd" };

        public string Description => "Role options";

        public string Usage => "addrole [user1] [user2] ... [role1] [role2] ...";

        public bool requireAdmin => true;

        public async void ExecuteServer(SocketCommandContext context)
        {
            if (context.Message.MentionedUsers.Count == 0 || context.Message.MentionedRoles.Count == 0)
            {
                await context.Channel.SendMessageAsync($"Invalid invocation\nUsage:{Usage}");
                return;
            }

            try
            {
                var users = context.Message.MentionedUsers;
                var roles = context.Message.MentionedRoles;

                foreach (var user in users)
                {
                    foreach (var role in roles)
                    {
                        try
                        {
                            await ((SocketGuildUser)context.Guild.GetUser(user.Id)).AddRoleAsync(role);
                            await context.Channel.SendMessageAsync($"User {user.Mention} got role : {role.Name}");
                        }
                        catch (Exception ex) { ex.WriteErrFile(); }
                    }
                }
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync(ex.Message);
            }
        }
    }
}
