using Discord;
using Discord.WebSocket;

using PluginManager.Interfaces;
namespace New_User
{
    public class Greeting : DBEvent
    {
        public string name =>"Greeting";

        public string description => "Greets new users";

        public void Start(DiscordSocketClient client)
        {
            client.UserJoined += async (arg) =>
            {
                IGuild? guild = client.Guilds.FirstOrDefault();
                ITextChannel chn = await guild.GetDefaultChannelAsync();
                await chn.SendMessageAsync($"A wild {arg.Username} has apperead!");
                IRole? role = guild.Roles.FirstOrDefault(x => x.Name == "New User");
                if (role == null)
                    await arg.Guild.CreateRoleAsync("New User", GuildPermissions.None, Color.DarkBlue);
                await arg.AddRoleAsync(role);
            };
        }
    }
}