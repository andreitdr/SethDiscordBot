using Discord;
using DiscordBotCore.Others;
using Moq;

namespace Tests.DiscordBotCore;

public class DiscordPermissionsTests
{
    private static IRole Role(params GuildPermission[] permissions)
    {
        ulong raw = permissions.Aggregate(0UL, (all, permission) => all | (ulong)permission);
        var role = new Mock<IRole>();
        role.Setup(r => r.Permissions).Returns(new GuildPermissions(raw));
        return role.Object;
    }

    [Fact]
    public void Role_HasTheGrantedPermissions()
    {
        IRole role = Role(GuildPermission.KickMembers, GuildPermission.BanMembers);

        Assert.True(role.HasPermission(GuildPermission.KickMembers));
        Assert.True(role.HasPermission(GuildPermission.BanMembers));
        Assert.False(role.HasPermission(GuildPermission.Administrator));
    }

    [Fact]
    public void Role_WithoutPermissions_HasNone()
    {
        Assert.False(Role().HasPermission(GuildPermission.SendMessages));
    }
}
