using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DiscordBotCore.API.Sockets.Sockets;
using DiscordBotCore.Interfaces.API;

namespace DiscordBotCore.API.Sockets;

internal class SocketManager
{
    private readonly IConnectionDetails _ConnectionDetails;
    private List<ISocket> _Sockets = new List<ISocket>();

    public SocketManager(IConnectionDetails connectionDetails)
    {
        _ConnectionDetails = connectionDetails;
    }

    public void RegisterBaseSockets()
    {
        Register(new PluginDownloadProgressSocket());
    }

    public bool Register(ISocket socket)
    {
        if (_Sockets.Any(s => s.Path == socket.Path))
        {
            return false;
        }

        _Sockets.Add(socket);
        return true;
    }

    public void Start()
    {
        Console.WriteLine("Starting sockets ...");
        foreach (var socket in _Sockets)
        {
            Thread thread = new Thread(() => StartSocket(socket));
            thread.Start();
        }
    }

    private async void StartSocket(ISocket socket)
    {

        if (!socket.Path.StartsWith("/"))
        {
            throw new ArgumentException($"Socket path '{socket.Path}' must start with '/'.");
        }

        string prefix = $"http://{_ConnectionDetails.Host}:{_ConnectionDetails.Port}{socket.Path}/";
        Console.WriteLine($"Starting socket with prefix: {prefix}");

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        await ConnectionHandler(listener, socket.HandleRequest);
    }


    private async Task ConnectionHandler(HttpListener listener, Func<byte[], int, Task<SocketResponse>> handler)
    {
        while (true)
        {
            var context = await listener.GetContextAsync();

            if (context.Request.IsWebSocketRequest)
            {
                WebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                Application.CurrentApplication.Logger.Log("WebSocket connection established.");
                await HandleSocket(webSocketContext.WebSocket, handler);
            }
            else { break; }
        }
    }

    private async Task HandleSocket(WebSocket socket, Func<byte[], int, Task<SocketResponse>> handler)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        byte[]                 buffer       = new byte[1024 * 4];
        var                    receivedData = new List<byte>();
        WebSocketReceiveResult result;

        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            receivedData.AddRange(buffer.Take(result.Count));
        } while (!result.EndOfMessage);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection ...", CancellationToken.None);
            Application.CurrentApplication.Logger.Log("WebSocket connection closed.");
            return;
        }

        Application.CurrentApplication.Logger.Log("WebSocket message received. Length: " + receivedData.Count);
        SocketResponse     socketResponse = await handler(receivedData.ToArray(), receivedData.Count);
        ArraySegment<byte> response       = new ArraySegment<byte>(socketResponse.Data, 0, socketResponse.Data.Length);
        byte[]?            lastResponse   = null;

        while (!socketResponse.CloseConnectionAfterResponse)
        {
            if (lastResponse == null || !socketResponse.Data.SequenceEqual(lastResponse))
            {
                await socket.SendAsync(response, WebSocketMessageType.Text, socketResponse.EndOfMessage, CancellationToken.None);
                lastResponse = socketResponse.Data;
            }

            socketResponse = await handler(receivedData.ToArray(), receivedData.Count);
            response = new ArraySegment<byte>(socketResponse.Data, 0, socketResponse.Data.Length);
        }

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection ...", CancellationToken.None);


    }
}
