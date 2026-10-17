using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CafeManager.Tests.G02;

[Collection("G02Sql"), Trait("Group", "G02"), Trait("Task", "T06")]
public sealed class AuthBoundaryRegressionTests(G02DatabaseFixture db)
{
    public static IEnumerable<object[]> InvalidKeys()
    {
        foreach (var legacy in new[] { false, true })
        foreach (var value in new[] { "", " ", " a", "a ", "a b", "a\t", "a/b", "tên", "Ａ", "a🙂", new string('a', 51), new string('a', 1001) })
            yield return [legacy, value];
    }

    [Theory, MemberData(nameof(InvalidKeys))]
    public async Task B03_AuthSqlRejectsOriginalInvalidKeyWithoutTruncationOrBestFit(bool legacy, string value)
    {
        var factory = G02DatabaseFixture.Factory(db.Connection, legacy);
        await using var connection = factory.CreateConnection();
        await factory.OpenAsync(connection, default);
        await using var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = legacy ? "dbo.sp_SetupReadLegacyAccount" : "dbo.sp_AuthReadAccount";
        cmd.Parameters.Add(new SqlParameter("@LoginKey", SqlDbType.NVarChar, -1) { Value = value });
        var error = await Assert.ThrowsAsync<SqlException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Contains(error.Errors.Cast<SqlError>(), x => x.Number == 51404);
        Assert.Equal("0:0", await TestDatabaseFixture.ScalarAsync(connection,
            "DECLARE @t int=@@TRANCOUNT,@s int=XACT_STATE(); SELECT CONCAT(@t,':',@s)"));
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task B03_ValidCanonicalKeyStillReadsTheSameAccount(bool legacy)
    {
        var factory = G02DatabaseFixture.Factory(db.Connection, legacy);
        await using var connection = factory.CreateConnection();
        await factory.OpenAsync(connection, default);
        await using var cmd = connection.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = legacy ? "dbo.sp_SetupReadLegacyAccount" : "dbo.sp_AuthReadAccount";
        cmd.Parameters.Add(new SqlParameter("@LoginKey", SqlDbType.NVarChar, -1) { Value = "g02.admin" });
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("g02.admin", reader.GetString(3));
        Assert.False(await reader.ReadAsync());
    }
}
