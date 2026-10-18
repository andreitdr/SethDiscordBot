using System.Net;

namespace Tests.DiscordBotCore.PluginManagement;

/// <summary>Answers requests from a table of URLs, and records how many ran at the same time.</summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, byte[] Body, bool SendLength)> _responses = new();
    private int _inFlight;

    public TimeSpan Delay { get; set; } = TimeSpan.Zero;
    public int MaxConcurrentRequests { get; private set; }
    public List<string> RequestedUrls { get; } = new();

    public FakeHttpHandler Add(string url, byte[] body, HttpStatusCode status = HttpStatusCode.OK, bool sendLength = true)
    {
        _responses[url] = (status, body, sendLength);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string url = request.RequestUri!.AbsoluteUri;
        lock (RequestedUrls)
        {
            RequestedUrls.Add(url);
            _inFlight++;
            MaxConcurrentRequests = Math.Max(MaxConcurrentRequests, _inFlight);
        }

        try
        {
            await Task.Delay(Delay, cancellationToken);

            if (!_responses.TryGetValue(url, out var response))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("<html>not found</html>") };
            }

            HttpContent content = response.SendLength
                ? new ByteArrayContent(response.Body)
                : new StreamContent(new UnknownLengthStream(response.Body));
            return new HttpResponseMessage(response.Status) { Content = content };
        }
        finally
        {
            lock (RequestedUrls)
            {
                _inFlight--;
            }
        }
    }

    /// <summary>A stream that can't report its length, so the response has no Content-Length.</summary>
    private sealed class UnknownLengthStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }
}

internal sealed class SynchronousProgress(Action<float> report) : IProgress<float>
{
    public void Report(float value) => report(value);
}
