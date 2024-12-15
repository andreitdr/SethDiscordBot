using System.Net.Http.Headers;
using System.Text;
using DiscordBotWebUI.ServerCommunication.ApiSettings;

namespace DiscordBotWebUI.ServerCommunication;

public class ApiHandler
{
    private IApiSettings ApiSettings { get; }
    public ApiHandler(IApiSettings apiSettings)
    {
        ApiSettings = apiSettings;
    }

    public async Task<ApiResponse> GetAsync(string endpoint)
    {
        using HttpClient client = new HttpClient();
        client.BaseAddress = new Uri($"{ApiSettings.BaseUrl}:{ApiSettings.BasePort}");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpResponseMessage response = await client.GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync();
            return await JsonManager.ConvertFromJson<ApiResponse>(content);
        }
        return new ApiResponse("Failed to get response", false);
    }

    public async Task<ApiResponse> PostAsync(string endpoint, Dictionary<string, string> jsonValues)
    {
        if (jsonValues.Count <= 0)
        {
            return new ApiResponse("No values to post", false);
        }

        string jsonString = await JsonManager.ConvertToJsonString(jsonValues);
        return await PostAsync(endpoint, jsonString);
    }

    public async Task<ApiResponse> PostAsync(string endpoint, string json)
    {
        using HttpClient client = new HttpClient();
        client.BaseAddress = new Uri($"{ApiSettings.BaseUrl}:{ApiSettings.BasePort}");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(endpoint, content);
        if (response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            return await JsonManager.ConvertFromJson<ApiResponse>(responseContent);
        }
        return  new ApiResponse("Failed to get response", false);
    }

}
