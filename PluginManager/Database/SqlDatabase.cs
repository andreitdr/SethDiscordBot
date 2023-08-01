using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace PluginManager.Database;

public class SqlDatabase
{
    private readonly SQLiteConnection Connection;

    /// <summary>
    ///     Initialize a SQL connection by specifing its private path
    /// </summary>
    /// <param name="fileName">The path to the database (it is starting from ./Data/Resources/)</param>
    public SqlDatabase(string fileName)
    {
        if (!fileName.StartsWith("./Data/Resources/"))
            fileName = Path.Combine("./Data/Resources", fileName);
        if (!File.Exists(fileName))
            SQLiteConnection.CreateFile(fileName);
        var connectionString = $"URI=file:{fileName}";
        Connection       = new SQLiteConnection(connectionString);
    }


    /// <summary>
    ///     Open the SQL Connection. To close use the Stop() method
    /// </summary>
    /// <returns></returns>
    public async Task Open()
    {
        await Connection.OpenAsync();
    }

    /// <summary>
    ///     <para>
    ///         Insert into a specified table some values
    ///     </para>
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="values">The values to be inserted (in the correct order and number)</param>
    /// <returns></returns>
    public async Task InsertAsync(string tableName, params string[] values)
    {
        var query = $"INSERT INTO {tableName} VALUES (";
        for (var i = 0; i < values.Length; i++)
        {
            query += $"'{values[i]}'";
            if (i != values.Length - 1)
                query += ", ";
        }

        query += ")";

        var command = new SQLiteCommand(query, Connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     <para>
    ///         Insert into a specified table some values
    ///     </para>
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="values">The values to be inserted (in the correct order and number)</param>
    /// <returns></returns>
    public void Insert(string tableName, params string[] values)
    {
        var query = $"INSERT INTO {tableName} VALUES (";
        for (var i = 0; i < values.Length; i++)
        {
            query += $"'{values[i]}'";
            if (i != values.Length - 1)
                query += ", ";
        }

        query += ")";

        var command = new SQLiteCommand(query, Connection);
        command.ExecuteNonQuery();
    }

    /// <summary>
    ///     Remove every row in a table that has a certain propery
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="KeyName">The column name that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the specified column</param>
    /// <returns></returns>
    public async Task RemoveKeyAsync(string tableName, string KeyName, string KeyValue)
    {
        var query = $"DELETE FROM {tableName} WHERE {KeyName} = '{KeyValue}'";

        var command = new SQLiteCommand(query, Connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     Remove every row in a table that has a certain propery
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="KeyName">The column name that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the specified column</param>
    /// <returns></returns>
    public void RemoveKey(string tableName, string KeyName, string KeyValue)
    {
        var query = $"DELETE FROM {tableName} WHERE {KeyName} = '{KeyValue}'";

        var command = new SQLiteCommand(query, Connection);
        command.ExecuteNonQuery();
    }

    /// <summary>
    ///     Check if the key exists in the table
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="keyName">The column that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the specified column</param>
    /// <returns></returns>
    public async Task<bool> KeyExistsAsync(string tableName, string keyName, string KeyValue)
    {
        var query = $"SELECT * FROM {tableName} where {keyName} = '{KeyValue}'";

        if (await ReadDataAsync(query) is not null)
            return true;

        return false;
    }

    /// <summary>
    ///     Check if the key exists in the table
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="keyName">The column that the search is made by</param>
    /// <param name="KeyValue">The value that is searched in the specified column</param>
    /// <returns></returns>
    public bool KeyExists(string tableName, string keyName, string KeyValue)
    {
        var query = $"SELECT * FROM {tableName} where {keyName} = '{KeyValue}'";

        if (ReadData(query) is not null)
            return true;

        return false;
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

        await ExecuteAsync(
                           $"UPDATE {tableName} SET {ResultColumnName}='{ResultColumnValue}' WHERE {keyName}='{KeyValue}'");
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

        Execute($"UPDATE {tableName} SET {ResultColumnName}='{ResultColumnValue}' WHERE {keyName}='{KeyValue}'");
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

        return await ReadDataAsync($"SELECT {ResultColumnName} FROM {tableName} WHERE {keyName}='{KeyValue}'");
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

        return ReadData($"SELECT {ResultColumnName} FROM {tableName} WHERE {keyName}='{KeyValue}'");
    }

    /// <summary>
    ///     Stop the connection to the SQL Database
    /// </summary>
    /// <returns></returns>
    public async void Stop()
    {
        await Connection.CloseAsync();
    }

    /// <summary>
    ///     Change the structure of a table by adding new columns
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="columns">The columns to be added</param>
    /// <param name="TYPE">The type of the columns (TEXT, INTEGER,  FLOAT, etc)</param>
    /// <returns></returns>
    public async Task AddColumnsToTableAsync(string tableName, string[] columns, string TYPE = "TEXT")
    {
        var command = Connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {tableName}";
        var reader       = await command.ExecuteReaderAsync();
        var tableColumns = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
            tableColumns.Add(reader.GetName(i));

        foreach (var column in columns)
            if (!tableColumns.Contains(column))
            {
                command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {column} {TYPE}";
                await command.ExecuteNonQueryAsync();
            }
    }

    /// <summary>
    ///     Change the structure of a table by adding new columns
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="columns">The columns to be added</param>
    /// <param name="TYPE">The type of the columns (TEXT, INTEGER,  FLOAT, etc)</param>
    /// <returns></returns>
    public void AddColumnsToTable(string tableName, string[] columns, string TYPE = "TEXT")
    {
        var command = Connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {tableName}";
        var reader       = command.ExecuteReader();
        var tableColumns = new List<string>();
        for (var i = 0; i < reader.FieldCount; i++)
            tableColumns.Add(reader.GetName(i));

        foreach (var column in columns)
            if (!tableColumns.Contains(column))
            {
                command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {column} {TYPE}";
                command.ExecuteNonQuery();
            }
    }

    /// <summary>
    ///     Check if a table exists
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <returns>True if the table exists, false if not</returns>
    public async Task<bool> TableExistsAsync(string tableName)
    {
        var cmd = Connection.CreateCommand();
        cmd.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
        var result = await cmd.ExecuteScalarAsync();

        if (result == null)
            return false;
        return true;
    }

    /// <summary>
    ///     Check if a table exists
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <returns>True if the table exists, false if not</returns>
    public bool TableExists(string tableName)
    {
        var cmd = Connection.CreateCommand();
        cmd.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
        var result = cmd.ExecuteScalar();

        if (result == null)
            return false;
        return true;
    }

    /// <summary>
    ///     Create a table
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="columns">The columns of the table</param>
    /// <returns></returns>
    public async Task CreateTableAsync(string tableName, params string[] columns)
    {
        var cmd = Connection.CreateCommand();
        cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)})";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     Create a table
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <param name="columns">The columns of the table</param>
    /// <returns></returns>
    public void CreateTable(string tableName, params string[] columns)
    {
        var cmd = Connection.CreateCommand();
        cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)})";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    ///     Execute a custom query
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The number of rows that the query modified</returns>
    public async Task<int> ExecuteAsync(string query)
    {
        if (!Connection.State.HasFlag(ConnectionState.Open))
            await Connection.OpenAsync();
        var command = new SQLiteCommand(query, Connection);
        var answer  = await command.ExecuteNonQueryAsync();
        return answer;
    }

    /// <summary>
    ///     Execute a custom query
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The number of rows that the query modified</returns>
    public int Execute(string query)
    {
        if (!Connection.State.HasFlag(ConnectionState.Open))
            Connection.Open();
        var command = new SQLiteCommand(query, Connection);
        var r       = command.ExecuteNonQuery();

        return r;
    }

    /// <summary>
    ///     Read data from the result table and return the first row
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The result is a string that has all values separated by space character</returns>
    public async Task<string?> ReadDataAsync(string query)
    {
        if (!Connection.State.HasFlag(ConnectionState.Open))
            await Connection.OpenAsync();
        var command = new SQLiteCommand(query, Connection);
        var reader  = await command.ExecuteReaderAsync();

        var values = new object[reader.FieldCount];
        if (reader.Read())
        {
            reader.GetValues(values);
            return string.Join<object>(" ", values);
        }

        return null;
    }

    /// <summary>
    ///     Read data from the result table and return the first row
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The result is a string that has all values separated by space character</returns>
    public string? ReadData(string query)
    {
        if (!Connection.State.HasFlag(ConnectionState.Open))
            Connection.Open();
        var command = new SQLiteCommand(query, Connection);
        var reader  = command.ExecuteReader();

        var values = new object[reader.FieldCount];
        if (reader.Read())
        {
            reader.GetValues(values);
            return string.Join<object>(" ", values);
        }

        return null;
    }

    /// <summary>
    ///     Read data from the result table and return the first row
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The first row as separated items</returns>
    public async Task<object[]?> ReadDataArrayAsync(string query)
    {
        if (!Connection.State.HasFlag(ConnectionState.Open))
            await Connection.OpenAsync();
        var command = new SQLiteCommand(query, Connection);
        var reader  = await command.ExecuteReaderAsync();

        var values = new object[reader.FieldCount];
        if (reader.Read())
        {
            reader.GetValues(values);
            return values;
        }

        return null;
    }


    /// <summary>
    ///     Read data from the result table and return the first row
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The first row as separated items</returns>
    public object[]? ReadDataArray(string query)
    {
        if (!Connection.State.HasFlag(ConnectionState.Open))
            Connection.Open();
        var command = new SQLiteCommand(query, Connection);
        var reader  = command.ExecuteReader();

        var values = new object[reader.FieldCount];
        if (reader.Read())
        {
            reader.GetValues(values);
            return values;
        }

        return null;
    }

    /// <summary>
    ///     Read all rows from the result table and return them as a list of string arrays. The string arrays contain the
    ///     values of each row
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>A list of string arrays representing the values that the query returns</returns>
    public async Task<List<string[]>?> ReadAllRowsAsync(string query)
    {
        if (!Connection.State.HasFlag(ConnectionState.Open))
            await Connection.OpenAsync();
        var command = new SQLiteCommand(query, Connection);
        var reader  = await command.ExecuteReaderAsync();

        if (!reader.HasRows)
            return null;

        List<string[]> rows = new();
        while (await reader.ReadAsync())
        {
            var values = new string[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values);
        }

        if (rows.Count == 0) return null;

        return rows;
    }
}
