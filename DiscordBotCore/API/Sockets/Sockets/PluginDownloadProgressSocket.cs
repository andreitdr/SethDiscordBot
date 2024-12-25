using System.Text;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces.API;

namespace DiscordBotCore.API.Sockets.Sockets;

internal class PluginDownloadProgressSocket : ISocket
{
    public string Path => "/plugin/download/progress";
    public Task<SocketResponse> HandleRequest(byte[] request, int count)
    {
        if (!Application.CurrentApplication.PluginManager.InstallingPluginInformation.IsInstalling)
        {
            return Task.FromResult(SocketResponse.Fail(true));
        }
        
        float value = Application.CurrentApplication.PluginManager.InstallingPluginInformation.InstallationProgress;
        SocketResponse response = SocketResponse.From(Encoding.UTF8.GetBytes(value.ToString()));
        response.CloseConnectionAfterResponse = false;
        return Task.FromResult(response);
    }
}
