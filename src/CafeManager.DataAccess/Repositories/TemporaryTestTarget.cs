#if DEBUG
using System.Data;
using CafeManager.Models.Dtos;
using Microsoft.Data.SqlClient;

namespace CafeManager.DataAccess.Repositories;

// Tiện ích Debug tạm: chỉ tạo identity test sau khi kiểm đúng DB và marker.
public sealed class TemporaryTestTarget
{
    private readonly ConnectionFactory factory;
    private readonly string database;
    private readonly string marker;

    public TemporaryTestTarget(string connectionString, string database, string marker)
    {
        var target = new SqlConnectionStringBuilder(connectionString);
        if (target.DataSource != "localhost" || !database.StartsWith("CoffeeManager_Test_", StringComparison.Ordinal)
            || target.InitialCatalog != database || string.IsNullOrWhiteSpace(marker) || marker.Length > 40)
            throw new ArgumentException("Cấu hình bỏ qua đăng nhập chỉ dùng cho DB test local đã chỉ định.");
        factory = new ConnectionFactory(connectionString); this.database = database; this.marker = marker;
    }

    public async Task<AccountIdentity> ReadAdminIdentityAsync(CancellationToken token)
    {
        await using var connection = factory.CreateConnection();
        try
        {
            await factory.OpenAsync(connection, token);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = factory.CommandTimeoutSeconds;
            command.CommandText = "SELECT CASE WHEN DB_NAME()=@Database AND COUNT_BIG(*)=1 AND MIN(RunId)=@Marker THEN 1 ELSE 0 END FROM dbo.G03TestMarker;";
            command.Parameters.Add("@Database", SqlDbType.NVarChar, 128).Value = database;
            command.Parameters.Add("@Marker", SqlDbType.VarChar, 40).Value = marker;
            if (Convert.ToInt32(await command.ExecuteScalarAsync(token)) != 1)
                throw new InvalidOperationException("DB hoặc marker test không khớp; không thể bỏ qua đăng nhập.");
        }
        catch (SqlException ex) { throw new DataAccessException(SqlErrorMapper.Map(ex, false)); }

        var auth = new AuthRepository(factory);
        var state = await auth.ReadStateAsync(token);
        if (state.State != "Completed" || state.AdminAccountId is not int accountId)
            throw new InvalidOperationException("DB test chưa hoàn tất thiết lập quản trị.");
        var identity = await auth.ReadIdentityAsync(accountId, token);
        if (identity is null || !identity.Active || !identity.Employed || identity.Role != "ADMIN")
            throw new UnauthorizedAccessException("Tài khoản ADMIN test không còn hợp lệ.");
        token.ThrowIfCancellationRequested();
        return identity;
    }
}
#endif
