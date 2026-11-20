using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CafeManager.Tests.G02;

[Collection("G02Sql"), Trait("Group", "G02"), Trait("Task", "T06")]
public sealed class UsernameCharsetRegressionTests(G02DatabaseFixture db)
{
    public static IEnumerable<object[]> Characters()
    {
        for (var code = 0; code < 128; code++)
            yield return [code, code is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-'];
        foreach (var code in new[] { 0x00A0, 0x00E9, 0x200B, 0xFF21, 0xFF3F, 0xFF0D })
            yield return [code, false];
    }

    public static IEnumerable<object[]> AsciiCharacters() => Characters().Where(row => (int)row[0] < 128);

    [Theory, MemberData(nameof(Characters))]
    public async Task B07_AuthAndLegacyReadValidateEveryAsciiCharacter(int code, bool allowed)
    {
        foreach (var setup in new[] { false, true })
        {
            var factory = G02DatabaseFixture.Factory(db.Connection, setup);
            await using var c = factory.CreateConnection();
            await factory.OpenAsync(c, default);
            await using var cmd = c.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = setup ? "dbo.sp_SetupReadLegacyAccount" : "dbo.sp_AuthReadAccount";
            cmd.Parameters.Add(new("@LoginKey", SqlDbType.NVarChar, -1) { Value = "b07" + (char)code + "key" });
            if (allowed)
            {
                await using var reader = await cmd.ExecuteReaderAsync();
                Assert.False(await reader.ReadAsync());
            }
            else
            {
                var error = await Assert.ThrowsAsync<SqlException>(() => cmd.ExecuteNonQueryAsync());
                Assert.Contains(error.Errors.Cast<SqlError>(), e => e.Number == 51404);
            }
        }
    }

    [Theory, MemberData(nameof(AsciiCharacters))]
    public async Task B07_StoredUsernameConstraintValidatesEveryAsciiCharacter(int code, bool allowed)
    {
        await using var c = await G02DatabaseFixture.OpenOwner(db.Connection);
        await using var transaction = (SqlTransaction)await c.BeginTransactionAsync();
        await using var cmd = c.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "UPDATE dbo.TAIKHOAN SET TenDangNhap=@name WHERE TenDangNhap='g02.admin'";
        cmd.Parameters.Add(new("@name", SqlDbType.NVarChar, -1) { Value = "b07" + (char)code + "key" });
        try
        {
            if (allowed) Assert.Equal(1, await cmd.ExecuteNonQueryAsync());
            else
            {
                var error = await Assert.ThrowsAsync<SqlException>(() => cmd.ExecuteNonQueryAsync());
                Assert.Contains(error.Errors.Cast<SqlError>(), e => e.Number == 547);
            }
        }
        finally { await transaction.RollbackAsync(); }
    }
}
