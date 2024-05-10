using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PluginManager;

public class JsonManager
{
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
                WriteIndented = true
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

        var obj = await JsonSerializer.DeserializeAsync<T>(text);
        await text.FlushAsync();
        text.Close();
        
        return (obj ?? default)!;
    }
}
