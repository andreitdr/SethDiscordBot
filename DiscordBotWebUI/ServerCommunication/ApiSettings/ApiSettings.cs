namespace DiscordBotWebUI.ServerCommunication.ApiSettings;

public class ApiSettings : IApiSettings
{
    public string BaseUrl { get; }
    public string BasePort { get; }

    public ApiSettings(string baseUrl, string basePort)
    {
        BaseUrl = baseUrl;
        BasePort = basePort;
    }
}
