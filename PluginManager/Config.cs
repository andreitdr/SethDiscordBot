using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PluginManager.Bot;
using PluginManager.Others;
using PluginManager.Others.Logger;

namespace PluginManager;

public class Config
{
    private static bool                  IsLoaded;
    public static  DBLogger              Logger;
    public static  Json<string, string>? Data;
    public static  Json<string, string>? Plugins;

    internal static Boot? _DiscordBotClient;

    public static Boot? DiscordBot => _DiscordBotClient;

    public static async Task Initialize()
    {
        if (IsLoaded)
            return;

        Directory.CreateDirectory("./Data/Resources");
        Directory.CreateDirectory("./Data/Plugins");
        Directory.CreateDirectory("./Data/PAKS");
        Directory.CreateDirectory("./Data/Logs/Logs");
        Directory.CreateDirectory("./Data/Logs/Errors");

        Data    = new Json<string, string>("./Data/Resources/config.json");
        Plugins = new Json<string, string>("./Data/Resources/Plugins.json");

        Data["LogFolder"]   = "./Data/Logs/Logs";
        Data["ErrorFolder"] = "./Data/Logs/Errors";

        Logger = new DBLogger();

        ArchiveManager.Initialize();

        IsLoaded = true;

        Logger.Log("Config initialized", LogLevel.INFO);
    }

    public class Json<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly string                     _file = "";
        private readonly IDictionary<TKey, TValue>? _dictionary;

        /// <summary>
        ///     Empty constructor
        /// </summary>
        public Json()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        public Json(string file)
        {
            _dictionary = PrivateReadConfig(file).GetAwaiter().GetResult();
            _file       = file;
        }

        public virtual void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
        }

        public void Clear() { _dictionary.Clear(); }

        public bool ContainsKey(TKey key)
        {
            if (_dictionary == null)
                throw new Exception("Dictionary is null");

            return _dictionary.ContainsKey(key);
        }

        public virtual ICollection<TKey> Keys => _dictionary.Keys;

        public virtual ICollection<TValue> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => _dictionary.IsReadOnly;

        public virtual TValue this[TKey key]
        {
            get
            {
                if (_dictionary.TryGetValue(key, out var value)) return value;
                throw new Exception("Key not found in dictionary " + key + " (Json )" + GetType().Name +
                                    ")");
            }
            set
            {
                if (_dictionary.ContainsKey(key))
                    _dictionary[key] = value;
                else _dictionary.Add(key, value);
            }
        }

        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public bool Remove(TKey key)
        {
            return _dictionary.Remove(key);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionary.Add(item);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_dictionary).GetEnumerator();
        }

        public async void Save()
        {
            if (!string.IsNullOrEmpty(_file))
                await Functions.SaveToJsonFile(_file, _dictionary);
        }

        private async Task<Dictionary<TKey, TValue>> PrivateReadConfig(string file)
        {
            if (!File.Exists(file))
            {
                var dictionary = new Dictionary<TKey, TValue>();
                await Functions.SaveToJsonFile(file, _dictionary);
                return dictionary;
            }

            try
            {
                var d = await Functions.ConvertFromJson<Dictionary<TKey, TValue>>(file);
                if (d is null)
                    throw new Exception("Failed to read config file");

                return d;
            }
            catch (Exception ex)
            {
                File.Delete(file);
                return new Dictionary<TKey, TValue>();
            }
        }
    }
}
