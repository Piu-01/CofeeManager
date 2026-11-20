using System.Text.RegularExpressions;
using System.Security.Cryptography;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CafeManager.Tests.G02;

[CollectionDefinition("G02Sql", DisableParallelization = true)]
public sealed class G02Collection : ICollectionFixture<G02DatabaseFixture> { }
public sealed class G02DatabaseFixture : IAsyncLifetime
{
    internal string Connection { get; } = Environment.GetEnvironmentVariable("CAFEMANAGER_G02_CONNECTION")
        ?? throw new InvalidOperationException("Use Test-G02.ps1 with an isolated marked DB.");
    internal string Password { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
    internal AuthRepository Auth => new(AppFactory());
    internal async Task<SessionContext> Login(string role = "ADMIN") => await new AuthService(Auth, new PasswordService()).LoginAsync(new LoginRequest("g02." + role.ToLowerInvariant(), Password));
    public async Task InitializeAsync()
    {
        await using var owner = await OpenOwner(Connection);
        await new SetupRepository(Factory(Connection, true)).CheckAccessAsync();
        await new BootstrapService(new SetupRepository(Factory(Connection, true)), new AuthRepository(Factory(Connection, true)), new PasswordService())
            .CompleteAsync(new BootstrapRequest { Mode = "NewEmployee", Username = "g02.admin", Password = Password, Confirmation = Password, EmployeeName = "G02 kiểm thử" });
        await Execute(owner, """
            INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) VALUES
            ('NV900',N'G02 CASH','Full-time','CVADM'),('NV901',N'G02 STORE','Full-time','CVADM');
            INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES
            ('NV900','g02.cash',@hash,'CASH',1),('NV901','g02.store',@hash,'STORE',1);
            """, new SqlParameter("@hash", new PasswordService().Hash(Password)));
        var root = Environment.GetEnvironmentVariable("CAFEMANAGER_G02_ROOT") ?? throw new InvalidOperationException("Fixture source root required.");
        await Execute(owner, await File.ReadAllTextAsync(Path.Combine(root, "database", "tests", "t08", "history-fixtures.sql")));
    }
    public Task DisposeAsync() => Task.CompletedTask;
    internal ConnectionFactory AppFactory() => Factory(Connection, false);
    internal static string FixtureConnection(string suffix) => Environment.GetEnvironmentVariable("CAFEMANAGER_G02_" + suffix + "_CONNECTION")
        ?? throw new InvalidOperationException("Required fixture connection absent.");
    internal static ConnectionFactory Factory(string connection, bool setup)
    {
        Validate(connection);
        return new(new SqlConnectionStringBuilder(connection) { Pooling = false }.ConnectionString)
        {
            SessionInitializer = async (c, ct) =>
            {
                await Guard(c);
                await using var cmd = c.CreateCommand(); cmd.CommandText = setup ? "EXECUTE AS USER=N'G02_Setup';" : "EXECUTE AS USER=N'G02_Runtime';";
                await cmd.ExecuteNonQueryAsync(ct);
            }
        };
    }
    internal static void Validate(string connection)
    {
        var b = new SqlConnectionStringBuilder(connection);
        if (b.DataSource != "localhost" || !b.IntegratedSecurity || !Regex.IsMatch(b.InitialCatalog, @"^CoffeeManager_Test_G02_\d{8}_\d{6}_[a-f0-9]{8}$"))
            throw new InvalidOperationException("Refusing non-G02 target.");
    }
    internal static async Task Guard(SqlConnection c)
    {
        Validate(c.ConnectionString);
        await using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT CASE WHEN DB_NAME()=@db AND OBJECT_ID(N'dbo.G02TestMarker',N'U') IS NOT NULL THEN 1 ELSE 0 END";
        cmd.Parameters.AddWithValue("@db", c.Database);
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 1) throw new InvalidOperationException("G02 marker required.");
    }
    internal static async Task<SqlConnection> OpenOwner(string connection)
    {
        Validate(connection); var c = new SqlConnection(connection);
        try { await c.OpenAsync(); await Guard(c); return c; } catch { await c.DisposeAsync(); throw; }
    }
    internal static async Task Execute(SqlConnection c, string sql, params SqlParameter[] parameters)
    {
        await Guard(c); await using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.Parameters.AddRange(parameters); await cmd.ExecuteNonQueryAsync();
    }
}
