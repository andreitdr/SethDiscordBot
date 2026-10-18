using DiscordBotCore.Database.Sqlite;
using Microsoft.Data.Sqlite;

namespace Tests.DiscordBotCore.Database.Sqlite;

public class SqlDatabaseTests : IAsyncLifetime
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-sqlite-tests", Guid.NewGuid().ToString("N"));
    private SqlDatabase _db = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_folder);
        _db = new SqlDatabase(Path.Combine(_folder, "test.db"));
        await _db.Open();
        await _db.CreateTableAsync("Users", "UserID TEXT", "Name TEXT", "Level INT");
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, true);
    }

    private static KeyValuePair<string, object> P(string name, object value) => new(name, value);

    #region Tables and columns

    [Fact]
    public async Task CreateTable_IsIdempotentAndDetectable()
    {
        await _db.CreateTableAsync("Users", "UserID TEXT");
        _db.CreateTable("Other", "A TEXT");

        Assert.True(await _db.TableExistsAsync("Users"));
        Assert.True(_db.TableExists("Other"));
        Assert.False(await _db.TableExistsAsync("Missing"));
        Assert.False(_db.TableExists("Missing"));
    }

    [Fact]
    public async Task TableExists_TreatsTheNameAsAValue()
    {
        Assert.False(await _db.TableExistsAsync("x' OR '1'='1"));
        Assert.False(_db.TableExists("x' OR '1'='1"));
    }

    [Fact]
    public async Task AddColumnsToTableAsync_AddsOnlyMissingColumns()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");

        await _db.AddColumnsToTableAsync("Users", ["Name", "Email", "Score"], "TEXT");
        await _db.AddColumnsToTableAsync("Users", ["Email"]);

        await _db.SetValueAsync("Users", "UserID", "1", "Email", "ann@example.com");
        Assert.Equal("ann@example.com", await _db.GetValueAsync("Users", "UserID", "1", "Email"));
        // UserID, Name, Level + Email, Score
        Assert.Equal(5, (await _db.ReadDataArrayAsync("SELECT * FROM Users"))!.Length);
    }

    [Fact]
    public void AddColumnsToTable_AddsOnlyMissingColumns()
    {
        _db.Insert("Users", "1", "Ann", "3");

        _db.AddColumnsToTable("Users", ["Name", "Email"]);
        _db.AddColumnsToTable("Users", ["Email"]);

        _db.SetValue("Users", "UserID", "1", "Email", "ann@example.com");
        Assert.Equal("ann@example.com", _db.GetValue("Users", "UserID", "1", "Email"));
    }

    #endregion

    #region Insert, read, update, delete (string helpers)

    [Fact]
    public async Task InsertAsync_ThenReadData_ReturnsTheRowJoinedBySpaces()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");

        Assert.Equal("1 Ann 3", await _db.ReadDataAsync("SELECT * FROM Users WHERE UserID = '1'"));
        Assert.Equal("1 Ann 3", _db.ReadData("SELECT * FROM Users WHERE UserID = '1'"));
    }

    [Fact]
    public void Insert_ThenKeyExists()
    {
        _db.Insert("Users", "1", "Ann", "3");

        Assert.True(_db.KeyExists("Users", "UserID", "1"));
        Assert.False(_db.KeyExists("Users", "UserID", "2"));
    }

    [Fact]
    public async Task KeyExistsAsync_FindsInsertedKeys()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");

        Assert.True(await _db.KeyExistsAsync("Users", "UserID", "1"));
        Assert.False(await _db.KeyExistsAsync("Users", "UserID", "2"));
    }

    [Fact]
    public async Task StringHelpers_AcceptValuesWithQuotes()
    {
        await _db.InsertAsync("Users", "o'brien", "Conan O'Brien", "1");
        _db.Insert("Users", "d'arcy", "D'Arcy", "2");

        Assert.True(await _db.KeyExistsAsync("Users", "UserID", "o'brien"));
        Assert.True(_db.KeyExists("Users", "UserID", "d'arcy"));
        Assert.Equal("Conan O'Brien", await _db.GetValueAsync("Users", "UserID", "o'brien", "Name"));

        await _db.SetValueAsync("Users", "UserID", "o'brien", "Name", "It's me");
        _db.SetValue("Users", "UserID", "d'arcy", "Name", "Mr. D'Arcy");
        Assert.Equal("It's me", _db.GetValue("Users", "UserID", "o'brien", "Name"));
        Assert.Equal("Mr. D'Arcy", await _db.GetValueAsync("Users", "UserID", "d'arcy", "Name"));

        await _db.RemoveKeyAsync("Users", "UserID", "o'brien");
        _db.RemoveKey("Users", "UserID", "d'arcy");
        Assert.Null(await _db.ReadDataAsync("SELECT * FROM Users"));
    }

    [Fact]
    public async Task StringHelpers_DoNotRunInjectedSql()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");

        await _db.RemoveKeyAsync("Users", "UserID", "x' OR '1'='1");
        Assert.False(await _db.KeyExistsAsync("Users", "UserID", "x' OR '1'='1"));

        Assert.True(await _db.KeyExistsAsync("Users", "UserID", "1"));
    }

    [Fact]
    public async Task SetValueAsync_UpdatesOnlyTheMatchingRow()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");
        await _db.InsertAsync("Users", "2", "Bob", "5");

        await _db.SetValueAsync("Users", "UserID", "1", "Level", "4");

        Assert.Equal("4", await _db.GetValueAsync("Users", "UserID", "1", "Level"));
        Assert.Equal("5", _db.GetValue("Users", "UserID", "2", "Level"));
    }

    [Fact]
    public async Task GetValueAsync_ReturnsNullForAMissingKey()
    {
        Assert.Null(await _db.GetValueAsync("Users", "UserID", "missing", "Name"));
        Assert.Null(_db.GetValue("Users", "UserID", "missing", "Name"));
    }

    [Fact]
    public async Task SetAndGetValue_FailForAMissingTable()
    {
        await Assert.ThrowsAsync<Exception>(() => _db.SetValueAsync("Missing", "a", "b", "c", "d"));
        await Assert.ThrowsAsync<Exception>(() => _db.GetValueAsync("Missing", "a", "b", "c"));
        Assert.Throws<Exception>(() => _db.SetValue("Missing", "a", "b", "c", "d"));
        Assert.Throws<Exception>(() => _db.GetValue("Missing", "a", "b", "c"));
    }

    [Fact]
    public async Task RemoveKey_RemovesEveryMatchingRow()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");
        await _db.InsertAsync("Users", "1", "Ann again", "3");
        await _db.InsertAsync("Users", "2", "Bob", "5");

        await _db.RemoveKeyAsync("Users", "UserID", "1");

        Assert.Equal("2", Assert.Single((await _db.ReadAllRowsAsync("SELECT UserID FROM Users"))!)[0]);
    }

    #endregion

    #region Raw queries

    [Fact]
    public async Task Execute_ReturnsTheNumberOfChangedRows()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");
        await _db.InsertAsync("Users", "2", "Bob", "5");

        Assert.Equal(2, await _db.ExecuteAsync("UPDATE Users SET Level = 1"));
        Assert.Equal(1, _db.Execute("DELETE FROM Users WHERE UserID = '1'"));
    }

    [Fact]
    public async Task ReadDataArray_ReturnsTypedValuesOrNull()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");

        object[]? row = await _db.ReadDataArrayAsync("SELECT UserID, Level FROM Users");
        Assert.NotNull(row);
        Assert.Equal("1", row[0]);
        Assert.Equal(3L, row[1]);

        Assert.Equal(row, _db.ReadDataArray("SELECT UserID, Level FROM Users"));
        Assert.Null(await _db.ReadDataArrayAsync("SELECT * FROM Users WHERE UserID = 'x'"));
        Assert.Null(_db.ReadDataArray("SELECT * FROM Users WHERE UserID = 'x'"));
    }

    [Fact]
    public async Task ReadAllRowsAsync_ReturnsEveryRowAsText()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");
        await _db.ExecuteAsync("INSERT INTO Users (UserID, Name, Level) VALUES ('2', NULL, 5)");

        List<string[]>? rows = await _db.ReadAllRowsAsync("SELECT UserID, Name, Level FROM Users ORDER BY UserID");

        Assert.NotNull(rows);
        Assert.Equal(["1", "Ann", "3"], rows[0]);
        Assert.Equal(["2", "", "5"], rows[1]);
    }

    [Fact]
    public async Task ReadAllRowsAsync_ReturnsNullWithoutRows()
    {
        Assert.Null(await _db.ReadAllRowsAsync("SELECT * FROM Users"));
    }

    [Fact]
    public async Task ManyQueries_DoNotLeaveTheDatabaseLocked()
    {
        for (int i = 0; i < 50; i++)
        {
            await _db.InsertAsync("Users", i.ToString(), "user", "1");
            await _db.ReadDataAsync($"SELECT * FROM Users WHERE UserID = '{i}'");
            await _db.ReadDataArrayAsync("SELECT * FROM Users");
            await _db.ReadAllRowsAsync("SELECT * FROM Users");
        }

        // Schema changes need no open statement on the connection
        await _db.ExecuteAsync("CREATE TABLE Later (A TEXT)");
        Assert.Equal(1, await _db.ExecuteAsync("INSERT INTO Later VALUES ('x')"));
        await _db.ExecuteAsync("DROP TABLE Later");
        Assert.False(await _db.TableExistsAsync("Later"));
    }

    #endregion

    #region Parameterized queries

    [Fact]
    public async Task ExecuteNonQueryAsync_BindsEveryParameterType()
    {
        await _db.ExecuteAsync("CREATE TABLE Types (S TEXT, I INT, L INT, D REAL, B INT, G TEXT, Bytes BLOB, N TEXT)");
        Guid guid = Guid.NewGuid();

        int changed = await _db.ExecuteNonQueryAsync(
            "INSERT INTO Types VALUES (@s, @i, @l, @d, @b, @g, @bytes, @n)",
            P("s", "it's"), P("i", 42), P("l", 1234567890123456789L), P("d", 1.5), P("b", true),
            P("g", guid.ToString()), P("bytes", new byte[] { 1, 2, 3 }), P("n", DBNull.Value));

        Assert.Equal(1, changed);
        object[]? row = await _db.ReadDataArrayAsync("SELECT * FROM Types");
        Assert.NotNull(row);
        Assert.Equal("it's", row[0]);
        Assert.Equal(42L, row[1]);
        Assert.Equal(1234567890123456789L, row[2]);
        Assert.Equal(1.5, row[3]);
        Assert.Equal(1L, row[4]);
        Assert.Equal(guid.ToString(), row[5]);
        Assert.Equal(new byte[] { 1, 2, 3 }, row[6]);
        Assert.Equal(DBNull.Value, row[7]);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_BindsNullAsDbNull()
    {
        await _db.ExecuteNonQueryAsync("INSERT INTO Users VALUES (@id, @name, @level)", P("id", "1"), P("name", null!), P("level", 1));

        Assert.Equal(DBNull.Value, (await _db.ReadDataArrayAsync("SELECT Name FROM Users"))![0]);
    }

    [Fact]
    public async Task ReadDataAsync_WithParameters_FiltersRows()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");
        await _db.InsertAsync("Users", "2", "Bob", "5");

        Assert.Equal("2 Bob 5", await _db.ReadDataAsync("SELECT * FROM Users WHERE UserID = @id", P("id", "2")));
        Assert.Null(await _db.ReadDataAsync("SELECT * FROM Users WHERE UserID = @id", P("id", "3")));
    }

    [Fact]
    public async Task ReadDataArrayAsync_WithParameters_FiltersRows()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");

        object[]? row = await _db.ReadDataArrayAsync("SELECT Name FROM Users WHERE Level = @level", P("level", 3));

        Assert.Equal("Ann", row![0]);
    }

    [Fact]
    public async Task ReadObjectOfTypeAsync_ConvertsTheFirstRow()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");

        string? name = await _db.ReadObjectOfTypeAsync("SELECT Name FROM Users WHERE UserID = @id", row => (string)row[0], P("id", "1"));
        string? missing = await _db.ReadObjectOfTypeAsync("SELECT Name FROM Users WHERE UserID = @id", row => (string)row[0], P("id", "9"));

        Assert.Equal("Ann", name);
        Assert.Null(missing);
    }

    [Fact]
    public async Task ReadListOfTypeAsync_ConvertsEveryRowOrReturnsEmpty()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");
        await _db.InsertAsync("Users", "2", "Bob", "5");

        List<long> levels = await _db.ReadListOfTypeAsync("SELECT Level FROM Users WHERE Level > @min ORDER BY Level", row => (long)row[0], P("min", 0));
        List<long> none = await _db.ReadListOfTypeAsync("SELECT Level FROM Users WHERE Level > @min", row => (long)row[0], P("min", 10));

        Assert.Equal([3L, 5L], levels);
        Assert.Empty(none);
    }

    #endregion

    #region Connection

    [Fact]
    public async Task Queries_ReopenAClosedConnection()
    {
        await _db.CloseAsync();

        Assert.Equal(0, await _db.ExecuteAsync("DELETE FROM Users"));
        Assert.Null(_db.ReadData("SELECT * FROM Users"));
    }

    [Fact]
    public async Task Data_SurvivesReopeningTheFile()
    {
        await _db.InsertAsync("Users", "1", "Ann", "3");
        await _db.CloseAsync();

        await using var reopened = new SqlDatabase(Path.Combine(_folder, "test.db"));
        await reopened.Open();

        Assert.Equal("Ann", await reopened.GetValueAsync("Users", "UserID", "1", "Name"));
    }

    #endregion
}
