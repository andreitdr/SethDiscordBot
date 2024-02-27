using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PluginManager.Others;

public class SettingsDictionary<TKey, TValue>: IDictionary<TKey, TValue>
{
    public string? _file { get; }
    private IDictionary<TKey, TValue>? _dictionary;

    public SettingsDictionary(string? file)
    {
        _file = file;
        if (!LoadFromFile())
        {
            _dictionary = new Dictionary<TKey, TValue>();
            SaveToFile();
        }
    }

    public async Task SaveToFile()
    {
        if (!string.IsNullOrEmpty(_file))
            await JsonManager.SaveToJsonFile(_file, _dictionary);
    }

    private bool LoadFromFile()
    {
        if (!string.IsNullOrEmpty(_file))
            try
            {
                if (File.Exists(_file))
                {
                    var FileContent = File.ReadAllText(_file);
                    if (string.IsNullOrEmpty(FileContent))
                        File.WriteAllText(_file, "{}");

                    if (!FileContent.Contains("{") || !FileContent.Contains("}"))
                        File.WriteAllText(_file, "{}");
                }
                else
                    File.WriteAllText(_file, "{}");
                _dictionary = JsonManager.ConvertFromJson<IDictionary<TKey, TValue>>(_file).Result;
                return true;
            }
            catch
            {
                return false;
            }

        return false;

    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _dictionary!.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_dictionary!).GetEnumerator();
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        _dictionary!.Add(item);
    }

    public void Clear()
    {
        _dictionary!.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return _dictionary!.Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        _dictionary!.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return _dictionary!.Remove(item);
    }

    public int Count => _dictionary!.Count;
    public bool IsReadOnly => _dictionary!.IsReadOnly;
    public void Add(TKey key, TValue value)
    {
        _dictionary!.Add(key, value);
    }

    public bool ContainsKey(TKey key)
    {
        return _dictionary!.ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        return _dictionary!.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        return _dictionary!.TryGetValue(key, out value);
    }

    public TValue this[TKey key]
    {
        get
        {
            if (_dictionary!.ContainsKey(key))
                if (_dictionary[key] is string s && !string.IsNullOrEmpty(s) && !string.IsNullOrWhiteSpace(s))
                    return _dictionary[key];

            return default!;
        }
        set => _dictionary![key] = value;
    }

    public ICollection<TKey> Keys => _dictionary!.Keys;
    public ICollection<TValue> Values => _dictionary!.Values;
}
