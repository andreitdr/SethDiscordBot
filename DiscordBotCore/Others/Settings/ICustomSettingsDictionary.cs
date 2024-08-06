using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBotCore.Others.Settings;

internal interface ICustomSettingsDictionary<TKey,TValue>
{
    /// <summary>
    /// Adds an element to the custom settings dictionary
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The value</param>
    void Add(TKey key, TValue value);

    /// <summary>
    /// Sets the value of a key in the custom settings dictionary
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The value</param>
    void Set(TKey key, TValue value);

    /// <summary>
    /// Gets the value of a key in the custom settings dictionary. If the T type is different then the TValue type, it will try to convert it.
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="defaultValue">The default value to be returned if the searched value is not found</param>
    /// <typeparam name="T">The type of the returned value</typeparam>
    /// <returns></returns>
    T Get<T>(TKey key, T defaultValue);

    /// <summary>
    /// Gets the value of a key in the custom settings dictionary. If the T type is different then the TValue type, it will try to convert it.
    /// </summary>
    /// <param name="key">The key</param>
    /// <typeparam name="T">The type of the returned value</typeparam>
    /// <returns></returns>
    T? Get<T>(TKey key);

    /// <summary>
    /// Get a list of values from the custom settings dictionary
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="defaultValue">The default list to be returned if nothing is found</param>
    /// <typeparam name="T">The type of the returned value</typeparam>
    /// <returns></returns>
    List<T> GetList<T>(TKey key, List<T> defaultValue);

    /// <summary>
    /// Remove a key from the custom settings dictionary
    /// </summary>
    /// <param name="key">The key</param>
    void Remove(TKey key);
    
    /// <summary>
    /// Get the enumerator of the custom settings dictionary
    /// </summary>
    /// <returns></returns>
    IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();

    /// <summary>
    /// Clear the custom settings dictionary
    /// </summary>
    void Clear();

    /// <summary>
    /// Check if the custom settings dictionary contains a key
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns></returns>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Filter the custom settings dictionary based on a predicate
    /// </summary>
    /// <param name="predicate">The predicate</param>
    /// <returns></returns>
    IEnumerable<KeyValuePair<TKey, TValue>> Where(Func<KeyValuePair<TKey, TValue>, bool> predicate);

    /// <summary>
    /// Filter the custom settings dictionary based on a predicate
    /// </summary>
    /// <param name="predicate">The predicate</param>
    IEnumerable<KeyValuePair<TKey, TValue>> Where(Func<KeyValuePair<TKey, TValue>, int, bool> predicate);

    /// <summary>
    /// Filter the custom settings dictionary based on a predicate
    /// </summary>
    /// <param name="selector">The predicate</param>
    IEnumerable<TResult> Where<TResult>(Func<KeyValuePair<TKey, TValue>, TResult> selector);

    /// <summary>
    /// Filter the custom settings dictionary based on a predicate
    /// </summary>
    /// <param name="selector">The predicate</param>
    IEnumerable<TResult> Where<TResult>(Func<KeyValuePair<TKey, TValue>, int, TResult> selector);

    /// <summary>
    /// Get the first element of the custom settings dictionary based on a predicate
    /// </summary>
    /// <param name="predicate">The predicate</param>
    KeyValuePair<TKey, TValue> FirstOrDefault(Func<KeyValuePair<TKey, TValue>, bool> predicate);

    /// <summary>
    /// Get the first element of the custom settings dictionary
    /// </summary>
    /// <returns></returns>
    KeyValuePair<TKey, TValue> FirstOrDefault();

    /// <summary>
    /// Checks if the custom settings dictionary contains all the keys
    /// </summary>
    /// <param name="keys">A list of keys</param>
    /// <returns></returns>
    bool ContainsAllKeys(params TKey[] keys);

    /// <summary>
    /// Try to get the value of a key in the custom settings dictionary
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The value</param>
    /// <returns></returns>
    bool TryGetValue(TKey key, out TValue? value);
    
    /// <summary>
    /// Save the custom settings dictionary to a file
    /// </summary>
    /// <returns></returns>
    Task SaveToFile();
    
    /// <summary>
    /// Load the custom settings dictionary from a file
    /// </summary>
    /// <returns></returns>
    Task LoadFromFile();
}
