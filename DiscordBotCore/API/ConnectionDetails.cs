using DiscordBotCore.Interfaces.API;

namespace DiscordBotCore.API;

public class ConnectionDetails : IConnectionDetails
{
    public string Host { get; }
    public int Port { get; }

    public ConnectionDetails(string host, int port)
    {
        Host = host;
        Port = port;
    }

}
