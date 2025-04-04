using System.Collections;
using System.Net.Mime;
using DiscordBotCore.Logging;

namespace DiscordBotCore.Configuration;

public abstract class ConfigurationBase : IConfiguration
{
    protected readonly IDictionary<string, object> _InternalDictionary = new Dictionary<string, object>();
    protected readonly string                   _DiskLocation;
    protected readonly ILogger _Logger;

    protected ConfigurationBase(ILogger logger, string diskLocation)
    {
        this._DiskLocation = diskLocation;
        this._Logger      = logger;
    }
    
    public virtual void Add(string key, object value)
    {
        if (_InternalDictionary.ContainsKey(key))
            return;

        if (value is null)
            return;

        _InternalDictionary.Add(key, value);
    }

    public virtual void Set(string key, object value)
    {
        _InternalDictionary[key] = value;
    }
    
    public virtual object Get(string key)
    {
        return _InternalDictionary[key];
    }

    public virtual T Get<T>(string key, T defaulobject)
    {
        if (_InternalDictionary.TryGetValue(key, out var value))
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        
        return defaulobject;
    }

    public virtual T? Get<T>(string key)
    {
        if (_InternalDictionary.TryGetValue(key, out var value))
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        
        return default;
    }
    
    public virtual IDictionary<TSubKey, TSubValue> GetDictionary<TSubKey, TSubValue>(string key)
    {
        if (_InternalDictionary.TryGetValue(key, out var value))
        {
            if (value is not IDictionary)
            {
                throw new Exception("The value is not a dictionary");
            }
            
            var dictionary = new Dictionary<TSubKey, TSubValue>();
            foreach (DictionaryEntry item in (IDictionary)value)
            {
                dictionary.Add((TSubKey)Convert.ChangeType(item.Key, typeof(TSubKey)), (TSubValue)Convert.ChangeType(item.Value, typeof(TSubValue)));
            }
            
            return dictionary;
        }
        
        return new Dictionary<TSubKey, TSubValue>();
    }

    public virtual List<T> GetList<T>(string key, List<T> defaulobject)
    {
        if(_InternalDictionary.TryGetValue(key, out var value))
        {
            if (value is not IList)
            {
                throw new Exception("The value is not a list");
            }
            
            var list = new List<T>();
            foreach (object? item in (IList)value)
            {
                list.Add((T)Convert.ChangeType(item, typeof(T)));
            }
            
            return list;
        }
        
        _Logger.Log($"Key '{key}' not found in settings dictionary. Adding default value.", LogType.Warning);
        
        return defaulobject;
    }

    public virtual void Remove(string key)
    {
        _InternalDictionary.Remove(key);
    }

    public virtual IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return _InternalDictionary.GetEnumerator();
    }

    public virtual void Clear()
    {
        _InternalDictionary.Clear();
    }

    public virtual bool ContainsKey(string key)
    {
        return _InternalDictionary.ContainsKey(key);
    }

    public virtual IEnumerable<KeyValuePair<string, object>> Where(Func<KeyValuePair<string, object>, bool> predicate)
    {
        return _InternalDictionary.Where(predicate);
    }

    public virtual IEnumerable<KeyValuePair<string, object>> Where(Func<KeyValuePair<string, object>, int, bool> predicate)
    {
        return _InternalDictionary.Where(predicate);
    }

    public virtual IEnumerable<TResult> Where<TResult>(Func<KeyValuePair<string, object>, TResult> selector)
    {
        return _InternalDictionary.Select(selector);
    }

    public virtual IEnumerable<TResult> Where<TResult>(Func<KeyValuePair<string, object>, int, TResult> selector)
    {
        return _InternalDictionary.Select(selector);
    }

    public virtual KeyValuePair<string, object> FirstOrDefault(Func<KeyValuePair<string, object>, bool> predicate)
    {
        return _InternalDictionary.FirstOrDefault(predicate);
    }

    public virtual KeyValuePair<string, object> FirstOrDefault()
    {
        return _InternalDictionary.FirstOrDefault();
    }

    public virtual bool ContainsAllKeys(params string[] keys)
    {
        return keys.All(ContainsKey);
    }

    public virtual bool TryGetValue(string key, out object? value)
    {
        return _InternalDictionary.TryGetValue(key, out value);
    }

    public abstract Task SaveToFile();

    public abstract void LoadFromFile();
}