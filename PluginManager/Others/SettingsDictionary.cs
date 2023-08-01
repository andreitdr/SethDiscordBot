using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PluginManager.Others;

public class SettingsDictionary<TKey, TValue> : IDictionary<TKey, TValue>
{
    public string _file { get; set; }
    private IDictionary<TKey, TValue>? _dictionary;

    public SettingsDictionary(string file)
    {
        if (file is null) 
            throw new FileLoadException("The file can not be null");
        
        if (!File.Exists(file))
            File.Create(file).Close();
        
        _file = file;
        LoadFromFile();
    }

    public async Task SaveToFile()
    {
        if (!string.IsNullOrEmpty(_file))
            await JsonManager.SaveToJsonFile(_file, _dictionary);
    }

    private void LoadFromFile()
    {
        if (!string.IsNullOrEmpty(_file))
            _dictionary = JsonManager.ConvertFromJson<IDictionary<TKey, TValue>>(_file).Result;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _dictionary!.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable) _dictionary!).GetEnumerator();
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        this._dictionary!.Add(item);
    }

    public void Clear()
    {
        this._dictionary!.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return this._dictionary!.Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        this._dictionary!.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return this._dictionary!.Remove(item);
    }

    public int Count  => _dictionary!.Count;
    public bool IsReadOnly => _dictionary!.IsReadOnly;
    public void Add(TKey key, TValue value)
    {
        this._dictionary!.Add(key, value);
    }

    public bool ContainsKey(TKey key)
    {
        return this._dictionary!.ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        return this._dictionary!.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        return this._dictionary!.TryGetValue(key, out value);
    }

    public TValue this[TKey key]
    {
        get => this._dictionary![key];
        set => this._dictionary![key] = value;
    }

    public ICollection<TKey> Keys => _dictionary!.Keys;
    public ICollection<TValue> Values => _dictionary!.Values;
}