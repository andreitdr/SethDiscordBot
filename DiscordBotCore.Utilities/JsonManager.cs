using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscordBotCore.Utilities;

public static class JsonManager
{

    public static async Task<string> ConvertToJson<T>(List<T> data, string[] propertyNamesToUse)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (propertyNamesToUse == null) throw new ArgumentNullException(nameof(propertyNamesToUse));

        // Use reflection to filter properties dynamically
        var filteredData = data.Select(item =>
        {
            if (item == null) return null;

            var type          = typeof(T);
            var propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Create a dictionary with specified properties and their values
            var selectedProperties = propertyInfos
                                     .Where(p => propertyNamesToUse.Contains(p.Name))
                                     .ToDictionary(p => p.Name, p => p.GetValue(item));

            return selectedProperties;
        }).ToList();

        // Serialize the filtered data to JSON
        var options = new JsonSerializerOptions
        {
            WriteIndented = true, // For pretty-print JSON; remove if not needed
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return await Task.FromResult(JsonSerializer.Serialize(filteredData, options));
    }
    
    public static async Task<string> ConvertToJsonString<T>(T Data)
    {
        var str = new MemoryStream();
        await JsonSerializer.SerializeAsync(str, Data, typeof(T), new JsonSerializerOptions
        {
            WriteIndented = false,
        });
        var result = Encoding.ASCII.GetString(str.ToArray());
        await str.FlushAsync();
        str.Close();
        return result;
    }
    
    /// <summary>
    ///     Save to JSON file
    /// </summary>
    /// <typeparam name="T">The class type</typeparam>
    /// <param name="file">The file path</param>
    /// <param name="Data">The values</param>
    /// <returns></returns>
    public static async Task SaveToJsonFile<T>(string file, T Data)
    {
        var str = new MemoryStream();
        await JsonSerializer.SerializeAsync(str, Data, typeof(T), new JsonSerializerOptions
            {
                WriteIndented = true,
        }
        );
        await File.WriteAllBytesAsync(file, str.ToArray());
        await str.FlushAsync();
        str.Close();
    }

    /// <summary>
    ///     Convert json text or file to some kind of data
    /// </summary>
    /// <typeparam name="T">The data type</typeparam>
    /// <param name="input">The file or json text</param>
    /// <returns></returns>
    public static async Task<T> ConvertFromJson<T>(string input)
    {
        Stream text;
        if (File.Exists(input))
            text = new MemoryStream(await File.ReadAllBytesAsync(input));
        else
            text = new MemoryStream(Encoding.ASCII.GetBytes(input));

        text.Position = 0;

        JsonSerializerOptions options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };

        var obj = await JsonSerializer.DeserializeAsync<T>(text, options);
        await text.FlushAsync();
        text.Close();
        
        return (obj ?? default)!;
    }
}
