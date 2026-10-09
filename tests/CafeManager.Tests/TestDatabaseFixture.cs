using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeManager.DataAccess;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CafeManager.Tests;

[CollectionDefinition("SqlIntegration", DisableParallelization = true)]
public sealed class SqlIntegrationCollection : ICollectionFixture<TestDatabaseFixture> { }

public sealed class TestDatabaseFixture
{
    internal string ConnectionString { get; }

    public TestDatabaseFixture()
    {
        ConnectionString = Environment.GetEnvironmentVariable("CAFEMANAGER_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Integration needs Test-DataAccess.ps1 and an isolated T05 database.");
        ValidateTarget(ConnectionString);
    }

    internal static void ValidateTarget(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (builder.DataSource != "localhost" || !Regex.IsMatch(builder.InitialCatalog,
                @"^CoffeeManager_Test_T05_\d{8}_\d{6}_[a-f0-9]{8}$") || !builder.IntegratedSecurity)
            throw new InvalidOperationException("Refusing non-T05 SQL test target.");
    }

    // EXECUTE AS USER sessions cannot be reset safely by SqlClient pooling.
    internal ConnectionFactory AppFactory(int timeout = 30) => new(
        new SqlConnectionStringBuilder(ConnectionString) { Pooling = false }.ConnectionString, timeout)
    {
        SessionInitializer = async (connection, cancellation) =>
        {
            await AssertTargetAsync(connection);
            await using var command = connection.CreateCommand();
            command.CommandText = "EXECUTE AS USER=N'T05_App';";
            await command.ExecuteNonQueryAsync(cancellation);
        }
    };

    internal async Task<SqlConnection> OpenAdminAsync()
    {
        var connection = new SqlConnection(ConnectionString);
        try { await connection.OpenAsync(); await AssertTargetAsync(connection); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }

    internal static async Task AssertTargetAsync(SqlConnection connection)
    {
        ValidateTarget(connection.ConnectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DB_NAME();";
        if ((string?)await command.ExecuteScalarAsync() != connection.Database)
            throw new InvalidOperationException("SQL database target changed.");
    }

    internal static async Task<object?> ScalarAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand(); command.CommandText = sql;
        return await command.ExecuteScalarAsync();
    }

    internal static async Task<string> SnapshotAsync(SqlConnection connection)
    {
        // All persistent rows/columns ordered by PK; identity next value deliberately excluded.
        var tables = new List<(string Name, string Keys)>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT t.name,STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(c.name)),N',')
                    WITHIN GROUP(ORDER BY ic.key_ordinal)
                FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id AND i.is_primary_key=1
                JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
                JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                WHERE t.is_ms_shipped=0 GROUP BY t.name ORDER BY t.name;
                """;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add((reader.GetString(0), reader.GetString(1)));
        }
        var snapshot = new SortedDictionary<string, string>();
        foreach (var (name, keys) in tables)
        {
            var value = await ScalarAsync(connection,
                $"SELECT (SELECT * FROM dbo.[{name.Replace("]", "]]")}] ORDER BY {keys} FOR JSON PATH, INCLUDE_NULL_VALUES);");
            snapshot[name] = value is null or DBNull ? "[]" : (string)value;
        }
        return JsonSerializer.Serialize(snapshot);
    }
}
