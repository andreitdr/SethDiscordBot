
namespace DiscordBotCore.API.Sockets;

internal class SocketResponse
{
    public byte[] Data { get;}
    public bool EndOfMessage { get; }
    public bool Success { get; }
    public bool CloseConnectionAfterResponse { get; set; }

    private SocketResponse(byte[] data, bool endOfMessage, bool success, bool closeConnectionAfterResponse)
    {
        Data = data;
        EndOfMessage = endOfMessage;
        Success = success;
        CloseConnectionAfterResponse = closeConnectionAfterResponse;
    }

    internal static SocketResponse From(byte[] data, bool endOfMessage, bool success, bool closeConnectionAfterResponse)
    {
        return new SocketResponse(data, endOfMessage, success, closeConnectionAfterResponse);
    }

    internal static SocketResponse From(byte[] data, bool endOfMessage)
    {
        return new SocketResponse(data, endOfMessage, true, false);
    }

    internal static SocketResponse From(byte[] data)
    {
        return new SocketResponse(data, true, true, false);
    }

    internal static SocketResponse Fail(bool closeConnection)
    {
        return new SocketResponse(new byte[0], true, false, closeConnection);
    }
}
