using System.Data;
using Microsoft.Data.Sqlite;

namespace DiscordBotCore.Database.Sqlite;

/// <summary>
///     A small helper around one SQLite connection. Table and column names are written into the SQL as given, so they
///     must come from your code; values are always sent as parameters.
/// </summary>
public class SqlDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _Connection;

    /// <summary>
    ///     Initialize a SQL connection by specifying its private path
    /// </summary>
    /// <param name="fileName">The path to the database file</param>
    public SqlDatabase(string fileName)
    {
        var connectionString = $"Data Source={fileName}";
        _Connection = new SqliteConnection(connectionString);
    }


    /// <summary>
    ///     Open the SQL Connection. To close it use <see cref="CloseAsync"/> (or <see cref="Stop"/>).
    /// </summary>
    public async Task Open()
    {
        await _Connection.OpenAsync();
    }

    /// <summary>
    ///     Insert into a specified table some values
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="values">The values to be inserted (in the correct order and number)</param>
    public async Task InsertAsync(string tableName, params string[] values)
    {
        await using var command = await CreateCommandAsync(BuildInsert(tableName, values), ValueParameters(values));
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     Insert into a specified table some values
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="values">The values to be inserted (in the correct order and number)</param>
    public void Insert(string tableName, params string[] values)
    {
        using var command = CreateCommand(BuildInsert(tableName, values), ValueParameters(values));
        command.ExecuteNonQuery();
    }

    /// <summary>
    ///     Remove every row in a table that has a certain property
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="KeyName">The column name that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the specified column</param>
    public async Task RemoveKeyAsync(string tableName, string KeyName, string KeyValue)
    {
        await ExecuteNonQueryAsync($"DELETE FROM {tableName} WHERE {KeyName} = @key", Parameter("key", KeyValue));
    }

    /// <summary>
    ///     Remove every row in a table that has a certain property
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="KeyName">The column name that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the specified column</param>
    public void RemoveKey(string tableName, string KeyName, string KeyValue)
    {
        using var command = CreateCommand($"DELETE FROM {tableName} WHERE {KeyName} = @key", Parameter("key", KeyValue));
        command.ExecuteNonQuery();
    }

    /// <summary>
    ///     Check if the key exists in the table
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="keyName">The column that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the specified column</param>
    public async Task<bool> KeyExistsAsync(string tableName, string keyName, string KeyValue)
    {
        return await ReadDataAsync($"SELECT * FROM {tableName} WHERE {keyName} = @key", Parameter("key", KeyValue)) is not null;
    }

    /// <summary>
    ///     Check if the key exists in the table
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="keyName">The column that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the specified column</param>
    public bool KeyExists(string tableName, string keyName, string KeyValue)
    {
        using var command = CreateCommand($"SELECT * FROM {tableName} WHERE {keyName} = @key", Parameter("key", KeyValue));
        using var reader = command.ExecuteReader();
        return reader.Read();
    }

    /// <summary>
    ///     Set value of a column in a table
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="keyName">The column that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the column specified</param>
    /// <param name="ResultColumnName">The column that has to be modified</param>
    /// <param name="ResultColumnValue">The new value that will replace the old value from the column specified</param>
    public async Task SetValueAsync(
        string tableName, string keyName, string KeyValue, string ResultColumnName,
        string ResultColumnValue)
    {
        if (!await TableExistsAsync(tableName))
            throw new Exception($"Table {tableName} does not exist");

        await ExecuteNonQueryAsync($"UPDATE {tableName} SET {ResultColumnName} = @value WHERE {keyName} = @key",
            Parameter("value", ResultColumnValue), Parameter("key", KeyValue));
    }

    /// <summary>
    ///     Set value of a column in a table
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="keyName">The column that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the column specified</param>
    /// <param name="ResultColumnName">The column that has to be modified</param>
    /// <param name="ResultColumnValue">The new value that will replace the old value from the column specified</param>
    public void SetValue(
        string tableName, string keyName, string KeyValue, string ResultColumnName,
        string ResultColumnValue)
    {
        if (!TableExists(tableName))
            throw new Exception($"Table {tableName} does not exist");

        using var command = CreateCommand($"UPDATE {tableName} SET {ResultColumnName} = @value WHERE {keyName} = @key",
            Parameter("value", ResultColumnValue), Parameter("key", KeyValue));
        command.ExecuteNonQuery();
    }

    /// <summary>
    ///     Get value from a column in a table
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="keyName">The column that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the specified column</param>
    /// <param name="ResultColumnName">The column that has the result</param>
    /// <returns>A string that has the requested value (can be null if nothing found)</returns>
    public async Task<string?> GetValueAsync(
        string tableName, string keyName, string KeyValue,
        string ResultColumnName)
    {
        if (!await TableExistsAsync(tableName))
            throw new Exception($"Table {tableName} does not exist");

        return await ReadDataAsync($"SELECT {ResultColumnName} FROM {tableName} WHERE {keyName} = @key", Parameter("key", KeyValue));
    }

    /// <summary>
    ///     Get value from a column in a table
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="keyName">The column that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the specified column</param>
    /// <param name="ResultColumnName">The column that has the result</param>
    /// <returns>A string that has the requested value (can be null if nothing found)</returns>
    public string? GetValue(string tableName, string keyName, string KeyValue, string ResultColumnName)
    {
        if (!TableExists(tableName))
            throw new Exception($"Table {tableName} does not exist");

        using var command = CreateCommand($"SELECT {ResultColumnName} FROM {tableName} WHERE {keyName} = @key", Parameter("key", KeyValue));
        using var reader = command.ExecuteReader();
        return ReadFirstRowAsText(reader);
    }

    /// <summary>
    ///     Stop the connection to the SQL Database. Prefer <see cref="CloseAsync"/>, which can be awaited.
    /// </summary>
    public async void Stop()
    {
        await CloseAsync();
    }

    /// <summary>
    ///     Close the connection. Later queries open it again.
    /// </summary>
    public async Task CloseAsync()
    {
        await _Connection.CloseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _Connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Change the structure of a table by adding new columns
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="columns">The columns to be added</param>
    /// <param name="TYPE">The type of the columns (TEXT, INTEGER,  FLOAT, etc)</param>
    public async Task AddColumnsToTableAsync(string tableName, string[] columns, string TYPE = "TEXT")
    {
        List<string> tableColumns = await GetColumnNamesAsync(tableName);

        foreach (var column in columns.Where(column => !tableColumns.Contains(column)))
        {
            await ExecuteAsync($"ALTER TABLE {tableName} ADD COLUMN {column} {TYPE}");
        }
    }

    /// <summary>
    ///     Change the structure of a table by adding new columns
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="columns">The columns to be added</param>
    /// <param name="TYPE">The type of the columns (TEXT, INTEGER,  FLOAT, etc)</param>
    public void AddColumnsToTable(string tableName, string[] columns, string TYPE = "TEXT")
    {
        var tableColumns = new List<string>();
        using (var command = CreateCommand($"SELECT * FROM {tableName}"))
        using (var reader = command.ExecuteReader())
        {
            for (var i = 0; i < reader.FieldCount; i++)
                tableColumns.Add(reader.GetName(i));
        }

        foreach (var column in columns.Where(column => !tableColumns.Contains(column)))
        {
            Execute($"ALTER TABLE {tableName} ADD COLUMN {column} {TYPE}");
        }
    }

    /// <summary>
    ///     Check if a table exists
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <returns>True if the table exists, false if not</returns>
    public async Task<bool> TableExistsAsync(string tableName)
    {
        await using var command = await CreateCommandAsync("SELECT name FROM sqlite_master WHERE type='table' AND name=@name", Parameter("name", tableName));
        return await command.ExecuteScalarAsync() is not null;
    }

    /// <summary>
    ///     Check if a table exists
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <returns>True if the table exists, false if not</returns>
    public bool TableExists(string tableName)
    {
        using var command = CreateCommand("SELECT name FROM sqlite_master WHERE type='table' AND name=@name", Parameter("name", tableName));
        return command.ExecuteScalar() is not null;
    }

    /// <summary>
    ///     Create a table if it doesn't exist
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="columns">The columns of the table, with their types</param>
    public async Task CreateTableAsync(string tableName, params string[] columns)
    {
        await ExecuteAsync($"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)})");
    }

    /// <summary>
    ///     Create a table if it doesn't exist
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="columns">The columns of the table, with their types</param>
    public void CreateTable(string tableName, params string[] columns)
    {
        Execute($"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)})");
    }

    /// <summary>
    ///     Execute a custom query
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The number of rows that the query modified</returns>
    public async Task<int> ExecuteAsync(string query)
    {
        return await ExecuteNonQueryAsync(query);
    }

    /// <summary>
    ///     Execute a custom query
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The number of rows that the query modified</returns>
    public int Execute(string query)
    {
        using var command = CreateCommand(query);
        return command.ExecuteNonQuery();
    }

    /// <summary>
    ///     Read data from the result table and return the first row, its values separated by spaces
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The result or null if there are no rows</returns>
    public Task<string?> ReadDataAsync(string query) => ReadDataAsync(query, []);

    /// <summary>
    ///     Read data from the result table and return the first row, its values separated by spaces
    /// </summary>
    /// <param name="query">The query</param>
    /// <param name="parameters">The parameters of the query</param>
    /// <returns>The result or null if there are no rows</returns>
    public async Task<string?> ReadDataAsync(string query, params KeyValuePair<string, object>[] parameters)
    {
        await using var command = await CreateCommandAsync(query, parameters);
        await using var reader = await command.ExecuteReaderAsync();
        return ReadFirstRowAsText(reader);
    }

    /// <summary>
    ///     Read data from the result table and return the first row, its values separated by spaces
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The result or null if there are no rows</returns>
    public string? ReadData(string query)
    {
        using var command = CreateCommand(query);
        using var reader = command.ExecuteReader();
        return ReadFirstRowAsText(reader);
    }

    /// <summary>
    ///     Read data from the result table and return the first row
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The values of the first row, or null if there are no rows</returns>
    public Task<object[]?> ReadDataArrayAsync(string query) => ReadDataArrayAsync(query, []);

    /// <summary>
    ///     Read data from the result table and return the first row
    /// </summary>
    /// <param name="query">The query</param>
    /// <param name="parameters">The parameters of the query</param>
    /// <returns>The values of the first row, or null if there are no rows</returns>
    public async Task<object[]?> ReadDataArrayAsync(string query, params KeyValuePair<string, object>[] parameters)
    {
        await using var command = await CreateCommandAsync(query, parameters);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? GetValues(reader) : null;
    }

    /// <summary>
    ///     Read data from the result table and return the first row
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The values of the first row, or null if there are no rows</returns>
    public object[]? ReadDataArray(string query)
    {
        using var command = CreateCommand(query);
        using var reader = command.ExecuteReader();
        return reader.Read() ? GetValues(reader) : null;
    }

    /// <summary>
    ///     Read every row of the result, each value as text (NULL becomes an empty string)
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The rows, or null if there are none</returns>
    public async Task<List<string[]>?> ReadAllRowsAsync(string query)
    {
        await using var command = await CreateCommandAsync(query);
        await using var reader = await command.ExecuteReaderAsync();

        List<string[]> rows = new();
        while (await reader.ReadAsync())
        {
            rows.Add(GetValues(reader).Select(value => Convert.ToString(value) ?? string.Empty).ToArray());
        }

        return rows.Count == 0 ? null : rows;
    }

    /// <summary>
    ///     Execute a query with parameters
    /// </summary>
    /// <param name="query">The query, with parameters such as <c>@name</c></param>
    /// <param name="parameters">The parameter names (without <c>@</c>) and values</param>
    /// <returns>The number of rows that the query modified</returns>
    public async Task<int> ExecuteNonQueryAsync(string query, params KeyValuePair<string, object>[] parameters)
    {
        await using var command = await CreateCommandAsync(query, parameters);
        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     Read the first row of a query and convert it
    /// </summary>
    /// <param name="query">The query</param>
    /// <param name="convertor">Turns the row's values into the result</param>
    /// <param name="parameters">The parameters of the query</param>
    /// <returns>The converted row, or the default value if there are no rows</returns>
    public async Task<T?> ReadObjectOfTypeAsync<T>(string query, Func<object[], T> convertor, params KeyValuePair<string, object>[] parameters)
    {
        await using var command = await CreateCommandAsync(query, parameters);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? convertor(GetValues(reader)) : default;
    }

    /// <summary>
    ///     Read every row of a query and convert each one
    /// </summary>
    /// <param name="query">The query</param>
    /// <param name="convertor">Turns a row's values into an item</param>
    /// <param name="parameters">The parameters of the query</param>
    /// <returns>The converted rows (empty if there are none)</returns>
    public async Task<List<T>> ReadListOfTypeAsync<T>(string query, Func<object[], T> convertor,
        params KeyValuePair<string, object>[] parameters)
    {
        await using var command = await CreateCommandAsync(query, parameters);
        await using var reader = await command.ExecuteReaderAsync();

        List<T> rows = new();
        while (await reader.ReadAsync())
        {
            rows.Add(convertor(GetValues(reader)));
        }

        return rows;
    }

    private async Task<List<string>> GetColumnNamesAsync(string tableName)
    {
        await using var command = await CreateCommandAsync($"SELECT * FROM {tableName}");
        await using var reader = await command.ExecuteReaderAsync();

        var columns = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));
        return columns;
    }

    private static string BuildInsert(string tableName, string[] values) =>
        $"INSERT INTO {tableName} VALUES ({string.Join(", ", values.Select((_, i) => $"@value{i}"))})";

    private static KeyValuePair<string, object>[] ValueParameters(string[] values) =>
        values.Select((value, i) => Parameter($"value{i}", value)).ToArray();

    private static KeyValuePair<string, object> Parameter(string name, object value) => new(name, value);

    private static object[] GetValues(SqliteDataReader reader)
    {
        var values = new object[reader.FieldCount];
        reader.GetValues(values);
        return values;
    }

    private static string? ReadFirstRowAsText(SqliteDataReader reader) =>
        reader.Read() ? string.Join<object>(" ", GetValues(reader)) : null;

    private async Task<SqliteCommand> CreateCommandAsync(string query, params KeyValuePair<string, object>[] parameters)
    {
        if (_Connection.State != ConnectionState.Open)
            await _Connection.OpenAsync();

        return BuildCommand(query, parameters);
    }

    private SqliteCommand CreateCommand(string query, params KeyValuePair<string, object>[] parameters)
    {
        if (_Connection.State != ConnectionState.Open)
            _Connection.Open();

        return BuildCommand(query, parameters);
    }

    private SqliteCommand BuildCommand(string query, KeyValuePair<string, object>[] parameters)
    {
        var command = new SqliteCommand(query, _Connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(CreateParameter(parameter.Key, parameter.Value));
        }

        return command;
    }

    private static SqliteParameter CreateParameter(string name, object? value)
    {
        // Microsoft.Data.Sqlite picks the storage class from the value's type; NULL needs DBNull
        return new SqliteParameter(name, value ?? DBNull.Value);
    }
}
