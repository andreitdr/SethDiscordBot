namespace DiscordBotCore.Configuration;

public interface IConfiguration
{
    /// <summary>
    /// Adds an element to the custom settings dictionary
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The value</param>
    void Add(string key, object value);

    /// <summary>
    /// Sets the value of a key in the custom settings dictionary
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The value</param>
    void Set(string key, object value);

    /// <summary>
    /// Gets the value of a key in the custom settings dictionary. If the T type is different then the object type, it will try to convert it.
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="defaultObject">The default value to be returned if the searched value is not found</param>
    /// <typeparam name="T">The type of the returned value</typeparam>
    /// <returns></returns>
    T Get<T>(string key, T defaultObject);

    /// <summary>
    /// Gets the value of a key in the custom settings dictionary. If the T type is different then the object type, it will try to convert it.
    /// </summary>
    /// <param name="key">The key</param>
    /// <typeparam name="T">The type of the returned value</typeparam>
    /// <returns></returns>
    T? Get<T>(string key);

    /// <summary>
    /// Get a list of values from the custom settings dictionary
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="defaultObject">The default list to be returned if nothing is found</param>
    /// <typeparam name="T">The type of the returned value</typeparam>
    /// <returns></returns>
    List<T> GetList<T>(string key, List<T> defaultObject);

    /// <summary>
    /// Remove a key from the custom settings dictionary
    /// </summary>
    /// <param name="key">The key</param>
    void Remove(string key);
    
    /// <summary>
    /// Get the enumerator of the custom settings dictionary
    /// </summary>
    /// <returns></returns>
    IEnumerator<KeyValuePair<string, object>> GetEnumerator();

    /// <summary>
    /// Clear the custom settings dictionary
    /// </summary>
    void Clear();

    /// <summary>
    /// Check if the custom settings dictionary contains a key
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns></returns>
    bool ContainsKey(string key);

    /// <summary>
    /// Filter the custom settings dictionary based on a predicate
    /// </summary>
    /// <param name="predicate">The predicate</param>
    /// <returns></returns>
    IEnumerable<KeyValuePair<string, object>> Where(Func<KeyValuePair<string, object>, bool> predicate);

    /// <summary>
    /// Filter the custom settings dictionary based on a predicate
    /// </summary>
    /// <param name="predicate">The predicate</param>
    IEnumerable<KeyValuePair<string, object>> Where(Func<KeyValuePair<string, object>, int, bool> predicate);

    /// <summary>
    /// Filter the custom settings dictionary based on a predicate
    /// </summary>
    /// <param name="selector">The predicate</param>
    IEnumerable<TResult> Where<TResult>(Func<KeyValuePair<string, object>, TResult> selector);

    /// <summary>
    /// Filter the custom settings dictionary based on a predicate
    /// </summary>
    /// <param name="selector">The predicate</param>
    IEnumerable<TResult> Where<TResult>(Func<KeyValuePair<string, object>, int, TResult> selector);

    /// <summary>
    /// Get the first element of the custom settings dictionary based on a predicate
    /// </summary>
    /// <param name="predicate">The predicate</param>
    KeyValuePair<string, object> FirstOrDefault(Func<KeyValuePair<string, object>, bool> predicate);

    /// <summary>
    /// Get the first element of the custom settings dictionary
    /// </summary>
    /// <returns></returns>
    KeyValuePair<string, object> FirstOrDefault();

    /// <summary>
    /// Checks if the custom settings dictionary contains all the keys
    /// </summary>
    /// <param name="keys">A list of keys</param>
    /// <returns></returns>
    bool ContainsAllKeys(params string[] keys);

    /// <summary>
    /// Try to get the value of a key in the custom settings dictionary
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="value">The value</param>
    /// <returns></returns>
    bool TryGetValue(string key, out object? value);
    
    /// <summary>
    /// Save the custom settings dictionary to a file
    /// </summary>
    /// <returns></returns>
    Task SaveToFile();
    
    /// <summary>
    /// Load the custom settings dictionary from a file
    /// </summary>
    /// <returns></returns>
    void LoadFromFile();
}