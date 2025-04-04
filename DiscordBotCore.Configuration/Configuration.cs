using DiscordBotCore.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordBotCore.Configuration;

public class Configuration : ConfigurationBase
{
    private readonly bool _EnableAutoAddOnGetWithDefault;
    private Configuration(ILogger logger, string diskLocation, bool enableAutoAddOnGetWithDefault): base(logger, diskLocation)
    {
        _EnableAutoAddOnGetWithDefault = enableAutoAddOnGetWithDefault;
    }

    public override async Task SaveToFile()
    {
        var json = JsonConvert.SerializeObject(_InternalDictionary, Formatting.Indented);
        await File.WriteAllTextAsync(_DiskLocation, json);
    }

    public override T Get<T>(string key, T defaultValue)
    {
        T value = base.Get(key, defaultValue);
        
        if (_EnableAutoAddOnGetWithDefault && value.Equals(defaultValue))
        {
            Add(key, defaultValue);
        }
        
        return value;
    }

    public override List<T> GetList<T>(string key, List<T> defaultValue)
    {
        List<T> value = base.GetList(key, defaultValue);
        
        if (_EnableAutoAddOnGetWithDefault && value.All(defaultValue.Contains))
        {
            Add(key, defaultValue);
        }
        
        return value;
    }

    public override async void LoadFromFile()
    {
        if (!File.Exists(_DiskLocation))
        {
            await SaveToFile();
            return;
        }
        
        string jsonContent = await File.ReadAllTextAsync(_DiskLocation);
        var    jObject     = JsonConvert.DeserializeObject<JObject>(jsonContent);

        if (jObject is null)
        {
            await SaveToFile();
            return;
        }
        
        _InternalDictionary.Clear();
        
        foreach (var kvp in jObject)
        {
            AddPairToDictionary(kvp, _InternalDictionary);
        }
    }

    private void AddPairToDictionary(KeyValuePair<string, JToken> kvp, IDictionary<string, object> dict)
    {
        if (kvp.Value is JObject nestedJObject)
        {
            dict[kvp.Key] = nestedJObject.ToObject<Dictionary<string, object>>();
            
            foreach (var nestedKvp in nestedJObject)
            {
                AddPairToDictionary(nestedKvp, dict[kvp.Key] as Dictionary<string, object>);
            }
        }
        else if (kvp.Value is JArray nestedJArray)
        {
            dict[kvp.Key] = nestedJArray.ToObject<List<object>>();
        }
        else
        {
            if (kvp.Value.Type == JTokenType.Integer)
                dict[kvp.Key] = kvp.Value.Value<int>();
            else if (kvp.Value.Type == JTokenType.Float)
                dict[kvp.Key] = kvp.Value.Value<float>();
            else if (kvp.Value.Type == JTokenType.Boolean)
                dict[kvp.Key] = kvp.Value.Value<bool>();
            else if (kvp.Value.Type == JTokenType.String)
                dict[kvp.Key] = kvp.Value.Value<string>();
            else if (kvp.Value.Type == JTokenType.Date)
                dict[kvp.Key] = kvp.Value.Value<DateTime>();
            else
                dict[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Create a new Settings Dictionary from a file
    /// </summary>
    /// <param name="baseFile">The file location</param>
    /// <param name="enableAutoAddOnGetWithDefault">Set this to true if you want to update the dictionary with default values on get</param>
    public static Configuration CreateFromFile(ILogger logger, string baseFile, bool enableAutoAddOnGetWithDefault)
    {
        var settings = new Configuration(logger, baseFile, enableAutoAddOnGetWithDefault);
        settings.LoadFromFile();
        return settings;
    }
}