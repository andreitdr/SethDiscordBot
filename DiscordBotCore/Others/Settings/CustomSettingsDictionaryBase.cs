using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBotCore.Others.Settings;

public abstract class CustomSettingsDictionaryBase<TKey,TValue> : ICustomSettingsDictionary<TKey,TValue>
{
    protected readonly IDictionary<TKey,TValue> _InternalDictionary = new Dictionary<TKey, TValue>();
    protected readonly string                   _DiskLocation;

    protected CustomSettingsDictionaryBase(string diskLocation)
    {
        this._DiskLocation = diskLocation;
    }
    
    public virtual void Add(TKey key, TValue value)
    {
        if (_InternalDictionary.ContainsKey(key))
            return;

        if (value is null)
            return;

        _InternalDictionary.Add(key, value);
    }

    public virtual void Set(TKey key, TValue value)
    {
        _InternalDictionary[key] = value;
    }
    
    public virtual TValue Get(TKey key)
    {
        return _InternalDictionary[key];
    }

    public virtual T Get<T>(TKey key, T defaultValue)
    {
        if (_InternalDictionary.TryGetValue(key, out var value))
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        
        return defaultValue;
    }

    public virtual T? Get<T>(TKey key)
    {
        if (_InternalDictionary.TryGetValue(key, out var value))
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        
        return default;
    }

    public virtual List<T> GetList<T>(TKey key, List<T> defaultValue)
    {
        if(_InternalDictionary.TryGetValue(key, out var value))
        {
            if (value is not IList)
            {
                throw new Exception("The value is not a list");
            }
            
            var list = new List<T>();
            foreach (var item in (IList)value)
            {
                list.Add(ConvertValue<T>(item));
            }
            
            return list;
        }
        
        return defaultValue;
    }

    public virtual void Remove(TKey key)
    {
        _InternalDictionary.Remove(key);
    }

    public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _InternalDictionary.GetEnumerator();
    }

    public virtual void Clear()
    {
        _InternalDictionary.Clear();
    }

    public virtual bool ContainsKey(TKey key)
    {
        return _InternalDictionary.ContainsKey(key);
    }

    public virtual IEnumerable<KeyValuePair<TKey, TValue>> Where(Func<KeyValuePair<TKey, TValue>, bool> predicate)
    {
        return _InternalDictionary.Where(predicate);
    }

    public virtual IEnumerable<KeyValuePair<TKey, TValue>> Where(Func<KeyValuePair<TKey, TValue>, int, bool> predicate)
    {
        return _InternalDictionary.Where(predicate);
    }

    public virtual IEnumerable<TResult> Where<TResult>(Func<KeyValuePair<TKey, TValue>, TResult> selector)
    {
        return _InternalDictionary.Select(selector);
    }

    public virtual IEnumerable<TResult> Where<TResult>(Func<KeyValuePair<TKey, TValue>, int, TResult> selector)
    {
        return _InternalDictionary.Select(selector);
    }

    public virtual KeyValuePair<TKey, TValue> FirstOrDefault(Func<KeyValuePair<TKey, TValue>, bool> predicate)
    {
        return _InternalDictionary.FirstOrDefault(predicate);
    }

    public virtual KeyValuePair<TKey, TValue> FirstOrDefault()
    {
        return _InternalDictionary.FirstOrDefault();
    }

    public virtual bool ContainsAllKeys(params TKey[] keys)
    {
        return keys.All(ContainsKey);
    }

    public virtual bool TryGetValue(TKey key, out TValue? value)
    {
        return _InternalDictionary.TryGetValue(key, out value);
    }

    public abstract Task SaveToFile();

    public abstract Task LoadFromFile();

    protected virtual T? ConvertValue<T>(object value)
    {

        if (typeof(T) == typeof(ulong) && value is long longValue)
        {
            return (T)(object)Convert.ToUInt64(longValue);
        }

        if (typeof(T).IsEnum && value is string stringValue)
        {
            return (T)Enum.Parse(typeof(T), stringValue);
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }
}
