using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Requests;
using CafeManager.WinForms.Forms;
using Microsoft.Data.SqlClient;
internal static class Native
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const string Db = "CoffeeManager_Test_G08_20261109_204755_debbbe06_demo";
    static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
    static readonly Dictionary<string, object?> Evidence = [];
    static void Write() => File.WriteAllText(Path.Combine(Root, "native-fixture.json"), JsonSerializer.Serialize(Evidence, new JsonSerializerOptions { WriteIndented = true }));
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        var assembly = typeof(LoginForm).Assembly;
        var helper = assembly.GetType("CafeManager.WinForms.Configuration.DevelopmentQuickLogin")!;
        var config = Activator.CreateInstance(helper, Any, null, ["localhost", Db], null)!;
        var path = (string)helper.GetField("path", Any)!.GetValue(config)!;
        if (File.Exists(path)) throw new InvalidOperationException("Fixture file already exists");
        var connection = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = Db, IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = true, Pooling = false }.ConnectionString;
        using var owner = new SqlConnection(connection); owner.Open();
        using var marker = owner.CreateCommand(); marker.CommandText = "SELECT RunId FROM dbo.OperationInstallMarker";
        if ((string?)marker.ExecuteScalar() != "20261109_204907_48b49f99") throw new InvalidOperationException("Marker mismatch");
        var passwords = new PasswordService();
        foreach (var role in new[] { "ADMIN", "CASH", "STORE" })
        {
            var username = role == "ADMIN" ? "admin" : "dq." + role.ToLowerInvariant();
            var password = Guid.NewGuid().ToString("N") + "!a1";
            using var update = owner.CreateCommand(); update.CommandText = "UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash WHERE TenDangNhap=@user";
            update.Parameters.AddWithValue("@hash", passwords.Hash(password)); update.Parameters.AddWithValue("@user", username);
            if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Fixture account missing");
            helper.GetMethod("Save", Any)!.Invoke(config, [role, new LoginRequest(username, password)]);
        }
        var factory = new ConnectionFactory(connection);
        typeof(ConnectionFactory).GetProperty("SessionInitializer", Any)!.SetValue(factory, (Func<SqlConnection, CancellationToken, Task>)(async (c, token) => { using var cmd = c.CreateCommand(); cmd.CommandText = "EXECUTE AS USER='DQRuntime'"; await cmd.ExecuteNonQueryAsync(token); }));
        var repository = new AuthRepository(factory); var auth = new AuthService(repository, passwords); var permissions = new PermissionService(repository);
        using var context = (ApplicationContext)Activator.CreateInstance(assembly.GetType("CafeManager.WinForms.ApplicationSessionContext")!, Any, null, [auth, permissions, repository, null, null, null, config], null)!;
        context.GetType().GetMethod("Start", Any)!.Invoke(context, null);
        Evidence["Started"] = DateTimeOffset.Now; Evidence["Scope"] = Db; Evidence["ModuleFactories"] = "Default shell only; full production modules not certified";
        var timer = new System.Windows.Forms.Timer { Interval = 500 }; var started = DateTime.UtcNow;
        timer.Tick += (_, _) =>
        {
            Evidence["Forms"] = Application.OpenForms.Cast<Form>().Select(f => new { Type = f.GetType().Name, f.Text, f.Visible, f.Enabled, f.DeviceDpi }).ToArray(); Write();
            if (File.Exists(Path.Combine(Root, "stop-native-only")) || DateTime.UtcNow - started > TimeSpan.FromMinutes(15))
            { context.Dispose(); Application.ExitThread(); }
        };
        timer.Start();
        try { Application.Run(); }
        finally
        {
            timer.Stop(); timer.Dispose();
            if (Path.GetFileName(path) != Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("LOCALHOST\n" + Db.ToUpperInvariant()))) + ".dpapi") throw new InvalidOperationException("Cleanup target mismatch");
            File.Delete(path); Evidence["StorageCleaned"] = !File.Exists(path); Evidence["Ended"] = DateTimeOffset.Now; Write();
        }
    }
}
