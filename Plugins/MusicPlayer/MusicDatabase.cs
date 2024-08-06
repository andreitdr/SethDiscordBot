using DiscordBotCore.Others;
using DiscordBotCore.Others.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MusicPlayer;

public class MusicDatabase: CustomSettingsDictionaryBase<string, MusicInfo>
{
    public MusicDatabase(string diskLocation): base(diskLocation)
    {
    }

    public List<MusicInfo> GetMusicInfoWithTitleOrAlias(string searchQuery)
    {
        List<MusicInfo> musicInfos = new List<MusicInfo>();
        foreach (var (title, musicInfo) in _InternalDictionary)
        {
            
            if(title.StartsWith(searchQuery))
            {
                musicInfos.Add(musicInfo);
            }
            
            if (musicInfo.Aliases is null)
            {
                continue;
            }

            if (musicInfo.Aliases.Contains(searchQuery) || musicInfo.Aliases.Any(x => x.StartsWith(searchQuery)))
            {
                musicInfos.Add(musicInfo);
            }
        }

        return musicInfos;
    }

    public override async Task SaveToFile()
    {
        var json = JsonConvert.SerializeObject(_InternalDictionary, Formatting.Indented);
        await File.WriteAllTextAsync(_DiskLocation, json);
    }

    public override async Task LoadFromFile()
    {
        string jsonContent = await File.ReadAllTextAsync(_DiskLocation);
        var    jObject     = JsonConvert.DeserializeObject<JObject>(jsonContent);
        _InternalDictionary.Clear();

        foreach (var (key,value) in jObject)
        {
            if (value is null || value.Type == JTokenType.Null)
            {
                continue;
            }
            
            MusicInfo? info = value.ToObject<MusicInfo>();
            if (info is null)
            {
                continue;
            }
            
            _InternalDictionary[key] = info;
        }
    }
}
