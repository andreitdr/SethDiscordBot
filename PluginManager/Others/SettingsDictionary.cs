using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PluginManager.Others;

public class SettingsDictionary<TKey, TValue>
{
    private string _File { get; }
    private IDictionary<TKey, TValue> _Dictionary;

    public SettingsDictionary(string file)
    {
        this._File = file;
        _Dictionary = null!;
    }

    public async Task SaveToFile()
    {
        if (!string.IsNullOrEmpty(_File))
            await JsonManager.SaveToJsonFile(_File, _Dictionary);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _Dictionary.GetEnumerator();
    }   

    public async Task<bool> LoadFromFile()
    {
        if (string.IsNullOrEmpty(_File))
            return false;

        if(!File.Exists(_File))
        {
            _Dictionary = new Dictionary<TKey, TValue>();
            return true;
        }

        string fileAsText = await File.ReadAllTextAsync(_File);
        if(string.IsNullOrEmpty(fileAsText) || string.IsNullOrWhiteSpace(fileAsText))
        {
            _Dictionary = new Dictionary<TKey, TValue>();
            return true;
        }

        _Dictionary = await JsonManager.ConvertFromJson<IDictionary<TKey,TValue>>(fileAsText);

        return true;
    }

    public void Add(TKey key, TValue value)
    {
        _Dictionary.Add(key, value);
    }

    public bool ContainsAllKeys(params TKey[] keys) 
    {
        return keys.All(key => _Dictionary.ContainsKey(key));
    }

    public bool ContainsKey(TKey key)
    {
        return _Dictionary.ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        return _Dictionary.Remove(key);
    }

    public TValue this[TKey key]
    {
        get
        {
            if(!_Dictionary.ContainsKey(key))
                throw new System.Exception($"The key {key} ({typeof(TKey)}) was not present in the dictionary");

            if(_Dictionary[key] is not TValue)
                throw new System.Exception("The dictionary is corrupted. This error is critical !");

            return _Dictionary[key];
        }
        set => _Dictionary[key] = value;
    }
}
