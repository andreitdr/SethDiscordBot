using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordBotCore.Others.Settings;

public class CustomSettingsDictionary : CustomSettingsDictionaryBase<string, object>
{
    private bool _EnableAutoAddOnGetWithDefault;
    private CustomSettingsDictionary(string diskLocation, bool enableAutoAddOnGetWithDefault): base(diskLocation)
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
        List<T> result = base.GetList(key, defaultValue);

        if (_EnableAutoAddOnGetWithDefault && defaultValue.All(result.Contains))
        {
            Add(key,defaultValue);
        }
        
        return result;
    }

    public override async Task LoadFromFile()
    {
        string jsonContent = await File.ReadAllTextAsync(_DiskLocation);
        var    jObject     = JsonConvert.DeserializeObject<JObject>(jsonContent);
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
            dict[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Create a new Settings Dictionary from a file
    /// </summary>
    /// <param name="baseFile">The file location</param>
    /// <param name="enableAutoAddOnGetWithDefault">Set this to true if you want to update the dictionary with default values on get</param>
    internal static async Task<CustomSettingsDictionary> CreateFromFile(string baseFile, bool enableAutoAddOnGetWithDefault)
    {
        var settings = new CustomSettingsDictionary(baseFile, enableAutoAddOnGetWithDefault);
        await settings.LoadFromFile();
        return settings;
    }
}
