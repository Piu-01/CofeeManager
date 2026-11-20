using System.Reflection;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;

internal static class OperationProbe
{
    internal static ConnectionFactory RuntimeFactory(string connection)
    {
        var factory = new ConnectionFactory(connection);
        typeof(ConnectionFactory).GetProperty("SessionInitializer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(factory, (Func<SqlConnection, CancellationToken, Task>)(async (c, t) =>
            {
                await using var command = c.CreateCommand(); command.CommandText = "EXECUTE AS USER='G08Runtime'";
                await command.ExecuteNonQueryAsync(t);
            }));
        return factory;
    }

    internal static async Task Run(SqlConnection owner, string connection, string mode, Dictionary<string, object?> evidence)
    {
        // Hash thật qua BLL; password ngẫu nhiên chỉ sống trong bộ nhớ của probe.
        var password = Guid.NewGuid().ToString("N") + "!a1";
        var factory = new ConnectionFactory(connection);
        var bootstrap = new BootstrapService(new SetupRepository(factory), new AuthRepository(factory), new PasswordService());
        byte[]? stamp = null;
        if (mode == "Demo")
        {
            await using var read = owner.CreateCommand();
            read.CommandText = "SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE TenDangNhap='admin'";
            stamp = (byte[]?)await read.ExecuteScalarAsync();
        }
        await bootstrap.CompleteAsync(new BootstrapRequest { Mode = mode == "Demo" ? "ActivateDemo" : "NewEmployee",
            Username = "admin", Password = password, Confirmation = password, EmployeeName = "Quản trị kiểm kỹ thuật G08",
            EmployeeCode = mode == "Demo" ? "NV001" : null, ExpectedStamp = stamp });
        await using (var create = owner.CreateCommand())
        {
            create.CommandText = "CREATE USER G08Runtime WITHOUT LOGIN;ALTER ROLE CafeManagerApp ADD MEMBER G08Runtime;";
            await create.ExecuteNonQueryAsync();
        }
        var runtime = RuntimeFactory(connection);
        var auth = new AuthRepository(runtime);
        using var session = await new AuthService(auth, new PasswordService()).LoginAsync(new LoginRequest("admin", password));
        await new PermissionService(auth).DemandAsync(session, AppPermission.FinancialReports);
        evidence["Bootstrap"] = new { State = (await bootstrap.ReadStateAsync()).State, Role = session.Identity.Role,
            Runtime = "DB user WITHOUT LOGIN/CafeManagerApp; không thay kiểm identity kết nối thật" };
        await using var check = owner.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM dbo.SchemaMigrations WHERE Version BETWEEN 1 AND 27";
        if (Convert.ToInt32(await check.ExecuteScalarAsync()) != 27) throw new InvalidOperationException("Schema incomplete.");
        evidence["SchemaVersion"] = 27;
    }
}
