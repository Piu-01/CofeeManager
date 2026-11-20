using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using Microsoft.Data.SqlClient;

internal static class SessionProbe
{
    internal static async Task<object> Run(SqlConnection owner, ConnectionFactory factory, SessionContext admin)
    {
        var auth = new AuthRepository(factory); var permissions = new PermissionService(auth);
        var employees = new EmployeeService(new EmployeeRepository(factory), permissions);
        var accounts = new EmployeeAccountService(new EmployeeAccountRepository(factory), permissions, new PasswordService());
        var outcomes = new List<string>();
        foreach (var role in new[] { "CASH", "STORE" })
        {
            var employee = await employees.SaveAsync(admin, new("Phiên G08 " + role, null, "Part-time", 0, 1, "CV003"), null, Guid.NewGuid());
            var password = Guid.NewGuid().ToString("N") + "!1"; var username = "g08." + role.ToLowerInvariant();
            await accounts.WriteAsync(admin, "Create", new(employee, null, username, role, true), password, password, Guid.NewGuid());
            using var session = await new AuthService(auth, new PasswordService()).LoginAsync(new LoginRequest(username, password));
            await permissions.DemandAsync(session, role == "CASH" ? AppPermission.Sales : AppPermission.Inventory);
            try { await permissions.DemandAsync(session, AppPermission.Finance); throw new InvalidOperationException("Finance allowed."); }
            catch (UnauthorizedAccessException) { outcomes.Add(role + ":finance denied"); }
            // Thay đổi trạng thái bằng operator chỉ trên fixture test; không giữ password/hash trong evidence.
            await using var change = owner.CreateCommand();
            change.CommandText = "UPDATE dbo.TAIKHOAN SET HoatDong=0 WHERE MaNV=@employee";
            change.Parameters.AddWithValue("@employee", employee); await change.ExecuteNonQueryAsync();
            try { await permissions.DemandAsync(session, AppPermission.SystemInfo); throw new InvalidOperationException("Stale session allowed."); }
            catch (UnauthorizedAccessException) { if (!session.IsRevoked) throw; outcomes.Add(role + ":revoked"); }
        }
        return new { Outcomes = outcomes, NativeLogout = "NOT RUN; operator lock fixture không thay handler logout/đổi role/nghỉ việc" };
    }
}
