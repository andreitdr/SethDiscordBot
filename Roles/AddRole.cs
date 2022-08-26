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

        public bool canUseDM => false;

        public bool canUseServer => true;

        public bool requireAdmin => true;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            if (message.MentionedUsers.Count == 0 || message.MentionedRoles.Count == 0)
            {
                await context.Channel.SendMessageAsync($"Invalid invocation\nUsage:{Usage}");
                return;
            }

            try
            {
                var users = message.MentionedUsers;
                var roles = message.MentionedRoles as IEnumerable<IRole>;

                foreach (var user in users)
                {
                    SocketGuildUser? usr = context.Client.GetUser(user.Username, user.Discriminator) as SocketGuildUser;
                    if (usr is null)
                        throw new Exception("User is null");
                    await usr.AddRolesAsync(roles);
                }
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync(ex.Message);
            }
        }
    }
}
