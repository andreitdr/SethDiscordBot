using System.Text;
using System.Text.Json;

namespace DiscordBotWebUI.ServerCommunication;

public class JsonManager
{
    public static async Task<T?> ConvertFromJson<T>(string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            throw new ArgumentException("JSON string cannot be null or empty.", nameof(jsonString));

        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to deserialize JSON.", ex);
        }
    }

    public static async Task<string> ConvertToJsonString<T>(T data)
    {
        using MemoryStream stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, data);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

}
