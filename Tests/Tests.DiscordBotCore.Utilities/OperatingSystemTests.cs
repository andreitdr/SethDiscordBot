using OperatingSystem = DiscordBotCore.Utilities.OperatingSystem;

namespace Tests.DiscordBotCore.Utilities;

public class OperatingSystemTests
{
    [Fact]
    public void GetOperatingSystem_MatchesTheCurrentPlatform()
    {
        OperatingSystem.OperatingSystemEnum expected =
            System.OperatingSystem.IsWindows() ? OperatingSystem.OperatingSystemEnum.Windows :
            System.OperatingSystem.IsLinux() ? OperatingSystem.OperatingSystemEnum.Linux :
            OperatingSystem.OperatingSystemEnum.MacOs;

        Assert.Equal(expected, OperatingSystem.GetOperatingSystem());
        Assert.Equal((int)expected, OperatingSystem.GetOperatingSystemInt());
    }

    [Theory]
    [InlineData(OperatingSystem.OperatingSystemEnum.Windows, "Windows", 0)]
    [InlineData(OperatingSystem.OperatingSystemEnum.Linux, "Linux", 1)]
    [InlineData(OperatingSystem.OperatingSystemEnum.MacOs, "MacOS", 2)]
    public void Names_AndNumbers_MatchTheRepositoryApi(OperatingSystem.OperatingSystemEnum os, string name, int number)
    {
        Assert.Equal(name, OperatingSystem.GetOperatingSystemString(os));
        Assert.Equal(os, OperatingSystem.GetOperatingSystemFromString(name));
        Assert.Equal(os, OperatingSystem.GetOperatingSystemFromString(name.ToUpperInvariant()));
        Assert.Equal(number, (int)os);
    }

    [Fact]
    public void UnknownValues_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OperatingSystem.GetOperatingSystemString((OperatingSystem.OperatingSystemEnum)42));
        Assert.Throws<ArgumentOutOfRangeException>(() => OperatingSystem.GetOperatingSystemFromString("BeOS"));
    }
}
