using System.Net.WebSockets;
using System.Threading.Tasks;
using DiscordBotCore.API.Sockets;

namespace DiscordBotCore.Interfaces.API;

internal interface ISocket
{
    public string Path { get; }
    public Task<SocketResponse> HandleRequest(byte[] request, int count);
}
