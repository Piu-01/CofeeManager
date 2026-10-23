using System.Data;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Errors;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CafeManager.Tests.Integration;

[Collection("SqlIntegration")]
[Trait("Category", "Integration")]
public sealed class DataAccessTests(TestDatabaseFixture fixture)
{
    [Theory]
    [Trait("Case", "T05-K01")]
    [Trait("Case", "T05-K02")]
    [InlineData("Cà phê sữa")]
    [InlineData("O'Brien")]
    [InlineData("'; DROP TABLE dbo.T05_Sentinel;--")]
    public async Task ParametersRoundTripWithoutExecutingInput(string text)
    {
        var at = new DateTime(2025, 4, 1, 9, 15, 0);
        var parameters = new[]
        {
            SqlParameters.Text("@Text", SqlDbType.NVarChar, 100, text, true),
            SqlParameters.Text("@Optional", SqlDbType.NVarChar, 100, "  "),
            SqlParameters.Text("@Code", SqlDbType.Char, 5, "A", true),
            SqlParameters.Decimal("@Money", 999999999999999999m),
            SqlParameters.Decimal("@Quantity", 123.4567m, 4),
            SqlParameters.DateTime("@At", at)
        };
        await new SqlProcedureExecutor(fixture.AppFactory()).ExecuteAsync("dbo.T05_SaveValues", parameters);
        await using var connection = await fixture.OpenAdminAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.T05_ReadValues"; command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(SqlParameters.Text("@Text", SqlDbType.NVarChar, 100, text, true));
            await using (var reader = await command.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync());
                Assert.Equal(text, reader.GetString(0)); Assert.True(reader.IsDBNull(1));
                Assert.Equal("A", reader.GetString(2).TrimEnd());
                Assert.Equal(999999999999999999m, reader.GetDecimal(3));
                Assert.Equal(123.4567m, reader.GetDecimal(4)); Assert.Equal(at, reader.GetDateTime(5));
            }
            Assert.Equal(1, Convert.ToInt32(await TestDatabaseFixture.ScalarAsync(connection, "SELECT COUNT(*) FROM dbo.T05_Sentinel;")));
        }
        finally
        {
            await using var cleanup = connection.CreateCommand();
            cleanup.CommandText = "DELETE dbo.T05_Values WHERE TextValue=@text;";
            cleanup.Parameters.Add(SqlParameters.Text("@text", SqlDbType.NVarChar, 100, text, true));
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    [Trait("Case", "T05-K03")]
    public async Task EmptyResultsAreDifferentFromConnectionFailure()
    {
        // T05 fixture chỉ có migration001; kiểm DAL, quyền phiên được kiểm ở G03/T11.
        var repository = new TableStatusRepository(fixture.AppFactory());
        var empty = await repository.GetAsync("ZZ999", default);
        Assert.Empty(empty);
        var broken = new SqlConnectionStringBuilder(fixture.ConnectionString)
        { DataSource = "tcp:127.0.0.1,1", ConnectTimeout = 1, Pooling = false };
        var failure = await Assert.ThrowsAsync<DataAccessException>(() => new TableStatusRepository(new ConnectionFactory(broken.ConnectionString)).GetAsync(null, default));
        Assert.NotNull(failure.Error);
    }

    [Theory]
    [Trait("Case", "T05-K04")]
    [InlineData(0)] [InlineData(1)] [InlineData(3)]
    public async Task TvpPreservesColumnsTypesAndNull(int count)
    {
        var rows = Enumerable.Range(1, count).Select(i => new PaymentAllocation($"PT00{i}", i * 1000m, i == 1 ? "  " : "Cà phê"));
        using var table = PaymentTableBuilder.Build(rows);
        await using var connection = await fixture.OpenAdminAsync();
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure; command.CommandText = "dbo.T05_Tvp";
        command.Parameters.Add(PaymentTableBuilder.Parameter(table));
        await using var reader = await command.ExecuteReaderAsync();
        var actual = 0;
        while (await reader.ReadAsync())
        {
            ++actual; Assert.Equal($"PT00{actual}", reader.GetString(0).TrimEnd());
            Assert.Equal(actual * 1000m, reader.GetDecimal(1));
            if (actual == 1) Assert.True(reader.IsDBNull(2)); else Assert.Equal("Cà phê", reader.GetString(2));
        }
        Assert.Equal(count, actual);
    }

    [Fact]
    [Trait("Case", "T05-K04")]
    public async Task InvalidTvpTypeProducesDiagnosticError()
    {
        using var table = PaymentTableBuilder.Build([new("PT001", 1000m)]);
        var parameter = PaymentTableBuilder.Parameter(table); parameter.TypeName = "dbo.NotAnExistingType";
        var error = await Assert.ThrowsAsync<DataAccessException>(() => new SqlProcedureExecutor(fixture.AppFactory())
            .ExecuteAsync("dbo.T05_Tvp", [parameter]));
        Assert.NotEqual(0, error.Error.Number);
        Assert.NotEqual(DataErrorKind.BusinessRule, error.Error.Kind);
    }

    [Theory]
    [Trait("Case", "T05-K05")]
    [Trait("Case", "T05-K06")]
    [InlineData("Z5PAY00001", "ZZ999", 547)]
    [InlineData("Z5PAY00003", "PT001", 52906)]
    public async Task LateProcedureErrorRollsBackRowsAndSession(string invoice, string method, int expectedNumber)
    {
        await using var admin = await fixture.OpenAdminAsync();
        var before = await TestDatabaseFixture.SnapshotAsync(admin);
        string? sessionState = null, after = null;
        using var table = PaymentTableBuilder.Build([new(method, 30000m)]);
        var factory = new ConnectionFactory(fixture.ConnectionString);
        SqlConnection? observedConnection = null;
        var error = await Assert.ThrowsAsync<DataAccessException>(() => new SqlProcedureExecutor(factory).ExecuteAsync(
            "dbo.sp_ThanhToanHoaDon",
            [SqlParameters.Text("@MaHD", SqlDbType.Char, 10, invoice, true), PaymentTableBuilder.Parameter(table), SqlParameters.DateTime("@NgayTT", new DateTime(2025, 4, 1))],
            invoice, inspectSession: async connection =>
            {
                observedConnection = connection;
                sessionState = (string?)await TestDatabaseFixture.ScalarAsync(connection,
                    "DECLARE @t INT=@@TRANCOUNT,@s INT=XACT_STATE(); SELECT CONCAT(@t,':',@s);");
                after = await TestDatabaseFixture.SnapshotAsync(connection);
            }));
        Assert.Equal(expectedNumber, error.Error.Number);
        Assert.Equal("0:0", sessionState); Assert.Equal(before, after);
        Assert.NotNull(observedConnection); Assert.Equal(ConnectionState.Closed, observedConnection.State);
        Assert.Equal(1, Convert.ToInt32(await TestDatabaseFixture.ScalarAsync(admin, "SELECT 1;")));
    }

    [Fact]
    [Trait("Case", "T05-K06")]
    public async Task SuccessfulProcedureHasNoOuterTransactionAndAppCanExecute()
    {
        using var table = PaymentTableBuilder.Build([new("PT001", 30000m)]);
        string? state = null;
        await new SqlProcedureExecutor(fixture.AppFactory()).ExecuteAsync("dbo.sp_ThanhToanHoaDon",
            [SqlParameters.Text("@MaHD", SqlDbType.Char, 10, "Z5PAY00002", true), PaymentTableBuilder.Parameter(table), SqlParameters.DateTime("@NgayTT", new DateTime(2025, 4, 1))],
            "Z5PAY00002", inspectSession: async connection => state = (string?)await TestDatabaseFixture.ScalarAsync(connection,
                "DECLARE @t INT=@@TRANCOUNT,@s INT=XACT_STATE(); SELECT CONCAT(@t,':',@s);"));
        Assert.Equal("0:0", state);
        await using var admin = await fixture.OpenAdminAsync();
        Assert.Equal("Da thanh toan", await TestDatabaseFixture.ScalarAsync(admin, "SELECT TrangThai FROM dbo.HOADON WHERE MaHD='Z5PAY00002';"));
        Assert.Equal(30000m, await TestDatabaseFixture.ScalarAsync(admin, "SELECT SUM(SoTien) FROM dbo.CHITIET_THANHTOAN WHERE MaHD='Z5PAY00002';"));
    }

    [Fact]
    [Trait("Case", "T05-K08")]
    [Trait("Case", "T05-K09")]
    public async Task UnknownAndTimeoutErrorsDoNotLeakSensitiveTextOrReportSuccess()
    {
        var path = Path.Combine(Path.GetTempPath(), $"CoffeeManager_{Guid.NewGuid():N}.log");
        try
        {
            var executor = new SqlProcedureExecutor(fixture.AppFactory(1), new DataOperationLog(path));
            var unknown = await Assert.ThrowsAsync<DataAccessException>(() => executor.ExecuteAsync("dbo.T05_Unknown", [], "Z5UNKNOWN01"));
            Assert.Equal(DataErrorKind.Unknown, unknown.Error.Kind);
            Assert.DoesNotContain("sensitive", unknown.Message);
            var timeout = await Assert.ThrowsAsync<DataAccessException>(() => executor.ExecuteAsync("dbo.T05_Slow", [], "Z5TIMEOUT01"));
            Assert.Equal(DataErrorKind.Timeout, timeout.Error.Kind); Assert.True(timeout.Error.RequiresReconciliation);
            var known = await Assert.ThrowsAsync<DataAccessException>(() => executor.ExecuteAsync("dbo.sp_HuyHoaDon",
                [SqlParameters.Text("@MaHD", SqlDbType.Char, 10, "ZZMISSING1", true)], "ZZMISSING1"));
            Assert.Equal(DataErrorKind.BusinessRule, known.Error.Kind); Assert.Equal(51109, known.Error.Number);
            var log = await File.ReadAllTextAsync(path);
            Assert.Contains("Z5UNKNOWN01", log); Assert.Contains("T05_Unknown", log); Assert.Contains("CorrelationId", log);
            Assert.DoesNotContain("sensitive", log); Assert.DoesNotContain(fixture.ConnectionString, log);
            Assert.DoesNotContain("Password", log, StringComparison.OrdinalIgnoreCase);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    [Trait("Case", "T05-K10")]
    public async Task FixtureCleanupPreservesSentinelAndSeed()
    {
        await using var admin = await fixture.OpenAdminAsync();
        Assert.Equal(0, Convert.ToInt32(await TestDatabaseFixture.ScalarAsync(admin, "SELECT COUNT(*) FROM dbo.T05_Values;")));
        Assert.Equal(1, Convert.ToInt32(await TestDatabaseFixture.ScalarAsync(admin, "SELECT COUNT(*) FROM dbo.T05_Sentinel;")));
        Assert.Equal(10, Convert.ToInt32(await TestDatabaseFixture.ScalarAsync(admin, "SELECT COUNT(*) FROM dbo.HOADON WHERE MaHD LIKE 'HD%';")));
    }
}
