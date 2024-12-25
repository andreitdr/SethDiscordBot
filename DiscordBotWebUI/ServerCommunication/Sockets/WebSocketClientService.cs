using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBotWebUI.ServerCommunication.Sockets;

public class WebSocketClientService
{
    private ClientWebSocket _webSocket;
    public event Action<string> OnMessageReceived;

    private readonly string _BaseUrl = "ws://localhost:5055";

    public async Task ConnectAsync(string uri)
    {
        _webSocket = new ClientWebSocket();
        await _webSocket.ConnectAsync(new Uri(_BaseUrl + uri), CancellationToken.None);
        Console.WriteLine("Connected to WebSocket server.");
    }

    public async Task SendMessageAsync(string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task ReceiveMessages()
    {
        byte[] buffer = new byte[102400];
        while (_webSocket.State == WebSocketState.Open)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Console.WriteLine("WebSocket connection closed.");
            }
            else
            {
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                OnMessageReceived?.Invoke(receivedMessage);
            }
        }
    }
}
