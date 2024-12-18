using System.Text;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces.API;

namespace DiscordBotCore.API.Sockets.Sockets;

internal class PluginDownloadProgressSocket : ISocket
{
    private float value = 0.0f;
    public string Path => "/plugin/download/progress";
    public Task<SocketResponse> HandleRequest(byte[] request, int count)
    {
        value += 0.1f;
        string pluginName = Encoding.UTF8.GetString(request, 0, count);
        Application.CurrentApplication.Logger.Log($"Received plugin download progress for {pluginName}.");
        SocketResponse response = SocketResponse.From(Encoding.UTF8.GetBytes(value.ToString()));
        response.CloseConnectionAfterResponse = value > 1.0f;
        return Task.FromResult(response);
    }
}
