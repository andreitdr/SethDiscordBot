using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace PluginManager.Database
{
    public class SqlDatabase
    {
        private string ConnectionString;
        private SQLiteConnection Connection;

        public SqlDatabase(string fileName)
        {
            if (!fileName.StartsWith("./Data/Resources/"))
                fileName = Path.Combine("./Data/Resources", fileName);
            if (!File.Exists(fileName))
                SQLiteConnection.CreateFile(fileName);
            ConnectionString = $"URI=file:{fileName}";
            Connection = new SQLiteConnection(ConnectionString);
        }


        public async Task Open()
        {
            await Connection.OpenAsync();

            //Console.WriteLine("Opened database successfully");
        }

        public async Task InsertAsync(string tableName, params string[] values)
        {
            string query = $"INSERT INTO {tableName} VALUES (";
            for (int i = 0; i < values.Length; i++)
            {
                query += $"'{values[i]}'";
                if (i != values.Length - 1)
                    query += ", ";
            }

            query += ")";

            SQLiteCommand command = new SQLiteCommand(query, Connection);
            await command.ExecuteNonQueryAsync();
        }

        public void Insert(string tableName, params string[] values)
        {
            string query = $"INSERT INTO {tableName} VALUES (";
            for (int i = 0; i < values.Length; i++)
            {
                query += $"'{values[i]}'";
                if (i != values.Length - 1)
                    query += ", ";
            }

            query += ")";

            SQLiteCommand command = new SQLiteCommand(query, Connection);
            command.ExecuteNonQuery();
        }

        public async Task RemoveKeyAsync(string tableName, string KeyName, string KeyValue)
        {
            string query = $"DELETE FROM {tableName} WHERE {KeyName} = '{KeyValue}'";

            SQLiteCommand command = new SQLiteCommand(query, Connection);
            await command.ExecuteNonQueryAsync();
        }

        public void RemoveKey(string tableName, string KeyName, string KeyValue)
        {
            string query = $"DELETE FROM {tableName} WHERE {KeyName} = '{KeyValue}'";

            SQLiteCommand command = new SQLiteCommand(query, Connection);
            command.ExecuteNonQuery();
        }


        public async Task<bool> KeyExistsAsync(string tableName, string keyName, string KeyValue)
        {
            string query = $"SELECT * FROM {tableName} where {keyName} = '{KeyValue}'";

            if (await ReadDataAsync(query) is not null)
                return true;

            return false;
        }

        public bool KeyExists(string tableName, string keyName, string KeyValue)
        {
            string query = $"SELECT * FROM {tableName} where {keyName} = '{KeyValue}'";

            if (ReadData(query) is not null)
                return true;

            return false;
        }


        public async Task SetValueAsync(string tableName, string keyName, string KeyValue, string ResultColumnName,
                                        string ResultColumnValue)
        {
            if (!await TableExistsAsync(tableName))
                throw new System.Exception($"Table {tableName} does not exist");

            await ExecuteAsync(
                $"UPDATE {tableName} SET {ResultColumnName}='{ResultColumnValue}' WHERE {keyName}='{KeyValue}'");
        }

        public void SetValue(string tableName, string keyName, string KeyValue, string ResultColumnName,
                             string ResultColumnValue)
        {
            if (!TableExists(tableName))
                throw new System.Exception($"Table {tableName} does not exist");

            Execute($"UPDATE {tableName} SET {ResultColumnName}='{ResultColumnValue}' WHERE {keyName}='{KeyValue}'");
        }


        public async Task<string?> GetValueAsync(string tableName, string keyName, string KeyValue,
                                                string ResultColumnName)
        {
            if (!await TableExistsAsync(tableName))
                throw new System.Exception($"Table {tableName} does not exist");

            return await ReadDataAsync($"SELECT {ResultColumnName} FROM {tableName} WHERE {keyName}='{KeyValue}'");
        }

        public string? GetValue(string tableName, string keyName, string KeyValue, string ResultColumnName)
        {
            if (!TableExists(tableName))
                throw new System.Exception($"Table {tableName} does not exist");

            return ReadData($"SELECT {ResultColumnName} FROM {tableName} WHERE {keyName}='{KeyValue}'");
        }

        public async void Stop()
        {
            await Connection.CloseAsync();
        }

        public async Task AddColumnsToTableAsync(string tableName, string[] columns)
        {
            var command = Connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {tableName}";
            var reader = await command.ExecuteReaderAsync();
            var tableColumns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
                tableColumns.Add(reader.GetName(i));

            foreach (var column in columns)
            {
                if (!tableColumns.Contains(column))
                {
                    command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {column} TEXT";
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public void AddColumnsToTable(string tableName, string[] columns)
        {
            var command = Connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {tableName}";
            var reader = command.ExecuteReader();
            var tableColumns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
                tableColumns.Add(reader.GetName(i));

            foreach (var column in columns)
            {
                if (!tableColumns.Contains(column))
                {
                    command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {column} TEXT";
                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task<bool> TableExistsAsync(string tableName)
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            var result = await cmd.ExecuteScalarAsync();

            if (result == null)
                return false;
            return true;
        }

        public bool TableExists(string tableName)
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            var result = cmd.ExecuteScalar();

            if (result == null)
                return false;
            return true;
        }

        public async Task CreateTableAsync(string tableName, params string[] columns)
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)})";
            await cmd.ExecuteNonQueryAsync();
        }

        public void CreateTable(string tableName, params string[] columns)
        {
            var cmd = Connection.CreateCommand();
            cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)})";
            cmd.ExecuteNonQuery();
        }

        public async Task<int> ExecuteAsync(string query)
        {
            if (!Connection.State.HasFlag(System.Data.ConnectionState.Open))
                await Connection.OpenAsync();
            var command = new SQLiteCommand(query, Connection);
            int answer = await command.ExecuteNonQueryAsync();
            return answer;
        }

        public int Execute(string query)
        {
            if (!Connection.State.HasFlag(System.Data.ConnectionState.Open))
                Connection.Open();
            var command = new SQLiteCommand(query, Connection);
            int r = command.ExecuteNonQuery();

            return r;
        }

        public async Task<string?> ReadDataAsync(string query)
        {
            if (!Connection.State.HasFlag(System.Data.ConnectionState.Open))
                await Connection.OpenAsync();
            var command = new SQLiteCommand(query, Connection);
            var reader = await command.ExecuteReaderAsync();

            object[] values = new object[reader.FieldCount];
            if (reader.Read())
            {
                reader.GetValues(values);
                return string.Join<object>(" ", values);
            }

            return null;
        }

        public string? ReadData(string query)
        {
            if (!Connection.State.HasFlag(System.Data.ConnectionState.Open))
                Connection.Open();
            var command = new SQLiteCommand(query, Connection);
            var reader = command.ExecuteReader();

            object[] values = new object[reader.FieldCount];
            if (reader.Read())
            {
                reader.GetValues(values);
                return string.Join<object>(" ", values);
            }

            return null;
        }

        public async Task<object[]?> ReadDataArrayAsync(string query)
        {
            if (!Connection.State.HasFlag(System.Data.ConnectionState.Open))
                await Connection.OpenAsync();
            var command = new SQLiteCommand(query, Connection);
            var reader = await command.ExecuteReaderAsync();

            object[] values = new object[reader.FieldCount];
            if (reader.Read())
            {
                reader.GetValues(values);
                return values;
            }

            return null;
        }

        public async Task<List<string[]>?> ReadAllRowsAsync(string query)
        {
            if (!Connection.State.HasFlag(System.Data.ConnectionState.Open))
                await Connection.OpenAsync();
            var command = new SQLiteCommand(query, Connection);
            var reader = await command.ExecuteReaderAsync();

            if (!reader.HasRows)
                return null;

            List<string[]> rows = new();
            while (await reader.ReadAsync())
            {
                string[] values = new string[reader.FieldCount];
                reader.GetValues(values);
                rows.Add(values);
            }

            if (rows.Count == 0) return null;

            return rows;
        }

        public object[]? ReadDataArray(string query)
        {
            if (!Connection.State.HasFlag(System.Data.ConnectionState.Open))
                Connection.Open();
            var command = new SQLiteCommand(query, Connection);
            var reader = command.ExecuteReader();

            object[] values = new object[reader.FieldCount];
            if (reader.Read())
            {
                reader.GetValues(values);
                return values;
            }

            return null;
        }
    }
}