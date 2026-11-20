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

internal static partial class Probe
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly string Db = "CoffeeManager_Test_G08_20261109_211932_790ab603_demo";
    static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
    static readonly List<object> Results = [];
    static readonly Assembly App = typeof(LoginForm).Assembly;
    static readonly Type Helper = App.GetType("CafeManager.WinForms.Configuration.DevelopmentQuickLogin")!;
    static readonly Dictionary<string, LoginRequest> Credentials = [];
    static readonly Dictionary<string, string> Employees = [];
    static readonly Dictionary<string, object> Evidence = [];
    static object? Config;
    static string? StoragePath;
    static AuthService Auth = null!;
    static PermissionService Permissions = null!;
    static AuthRepository Repository = null!;
    static SqlConnection Owner = null!;
    static Form Controller = null!;
    static string Connection = "";
    static void Assert(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    static object? Call(object? target, string method, params object?[] args)
    {
        try { return (target?.GetType() ?? Helper).GetMethod(method, Any)!.Invoke(target, args); }
        catch (TargetInvocationException ex) when (ex.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
    }
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Any)!.GetValue(target)!;
    static async Task Case(string id, Func<Task> action)
    {
        try { await action(); Results.Add(new { Id = id, Status = "PASS" }); }
        catch (Exception ex) { Results.Add(new { Id = id, Status = "FAIL", Type = ex.GetType().Name, Assertion = ex is InvalidOperationException ? ex.Message : null, Sql = ex is SqlException s ? s.Number : ex is DataAccessException d ? d.Error.Number : (int?)null }); }
        Flush();
    }
    static Task Sync(Action action) { action(); return Task.CompletedTask; }
    static void Flush() => File.WriteAllText(Path.Combine(Root, "regression-results.json"), JsonSerializer.Serialize(new { Results, Evidence }, new JsonSerializerOptions { WriteIndented = true }));
    static object NewConfig(string db) => Activator.CreateInstance(Helper, Any, null, ["localhost", db], null)!;
    static LoginForm Login() => (LoginForm)Activator.CreateInstance(typeof(LoginForm), Any, null, [Auth, null, Config], null)!;
    static async Task Sql(string text, string? employee = null)
    {
        using var cmd = Owner.CreateCommand(); cmd.CommandText = text;
        if (employee is not null) cmd.Parameters.AddWithValue("@e", employee);
        await cmd.ExecuteNonQueryAsync();
    }
    static async Task Denied(LoginRequest request)
    {
        try { using var session = await Auth.LoginAsync(request); }
        catch (UnauthorizedAccessException) { return; }
        throw new InvalidOperationException("Expected authentication refusal");
    }
    static Task Quick(LoginForm form, string role) => (Task)Call(form, "QuickLoginAsync", role)!;
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Controller = new Form { Text = "DQ test controller", ShowInTaskbar = false, Width = 240, Height = 100 };
        Controller.Shown += async (_, _) =>
        {
            try { await Run(); }
            catch (Exception ex) { Evidence["Fatal"] = new { Type = ex.GetType().Name, Sql = ex is SqlException s ? s.Number : ex is DataAccessException d ? d.Error.Number : (int?)null }; Flush(); Controller.Close(); }
        };
        Application.Run(Controller);
        Owner?.Dispose();
        if (StoragePath is not null && Path.GetFileName(StoragePath) == Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("LOCALHOST\n" + Db.ToUpperInvariant()))) + ".dpapi")
        { File.Delete(StoragePath); Evidence["StorageCleaned"] = !File.Exists(StoragePath); }
        Flush();
    }
    static async Task Run()
    {
        Evidence["SourceDll"] = new { Path = typeof(LoginForm).Assembly.Location, Hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(LoginForm).Assembly.Location))) };
        Evidence["DeviceDpi"] = Controller.DeviceDpi;
        await Case("DQ01.DebugGate", () => Sync(() =>
        {
            Environment.SetEnvironmentVariable("CAFEMANAGER_USE_DEVELOPMENT", "1");
            Environment.SetEnvironmentVariable("CAFEMANAGER_DEV_QUICK_LOGIN", "1");
            Assert(Call(null, "Create", "Server=localhost;Database=CoffeeManager_Development;Integrated Security=true") is not null, "Debug target rejected");
            foreach (var c in new[] { "Server=localhost;Database=" + Db, "Server=other;Database=CoffeeManager_Development", "broken=bad", "" }) Assert(Call(null, "Create", c) is null, "Wrong target accepted");
            foreach (var flag in new[] { "CAFEMANAGER_USE_DEVELOPMENT", "CAFEMANAGER_DEV_QUICK_LOGIN" })
            {
                Environment.SetEnvironmentVariable(flag, null);
                Assert(Call(null, "Create", "Server=localhost;Database=CoffeeManager_Development") is null, "Missing flag accepted");
                Environment.SetEnvironmentVariable(flag, "1");
            }
        }));
        Config = NewConfig(Db); StoragePath = Field<string>(Config, "path");
        Assert(!File.Exists(StoragePath), "Fixture storage exists");
        foreach (var r in new[] { "ADMIN", "CASH", "STORE" }) Credentials[r] = new(r == "ADMIN" ? "admin" : "dq." + r.ToLowerInvariant(), Guid.NewGuid().ToString("N") + "!a1");
        foreach (var r in Credentials.Keys)
            await Case("DQ02.Roundtrip." + r, () => Sync(() =>
            {
                Call(Config, "Save", r, Credentials[r]);
                var got = (LoginRequest)Call(NewConfig(Db), "Get", r)!;
                Assert(got.Username == Credentials[r].Username && got.Password == Credentials[r].Password, "Roundtrip mismatch");
                Assert(!Encoding.UTF8.GetString(File.ReadAllBytes(StoragePath)).Contains(Credentials[r].Password), "Plaintext stored");
            }));
        await Case("DQ02.ReplaceDeleteScope", () => Sync(() =>
        {
            Call(Config, "Save", "ADMIN", new LoginRequest("changed", Guid.NewGuid().ToString("N")));
            Assert(((LoginRequest)Call(Config, "Get", "ADMIN")!).Username == "changed", "Replace failed");
            Call(Config, "Delete", "ADMIN"); Assert(Call(Config, "Get", "ADMIN") is null, "Delete failed");
            Assert(Call(Config, "Get", "CASH") is not null, "Other role lost");
            Assert(Call(NewConfig(Db + "_other"), "Get", "CASH") is null, "Scope leaked");
            Call(Config, "Save", "ADMIN", Credentials["ADMIN"]);
        }));
        await Case("DQ02.CorruptAndVersion", () => Sync(() =>
        {
            var good = File.ReadAllBytes(StoragePath);
            try
            {
                foreach (var bytes in new[] { new byte[] { 1, 2, 3 }, ProtectedData.Protect(Encoding.UTF8.GetBytes("{\"Version\":2,\"Server\":\"localhost\",\"Database\":\"" + Db + "\",\"Accounts\":{}}"), Encoding.UTF8.GetBytes("CoffeeManager.Development.QuickLogin.v1"), DataProtectionScope.CurrentUser) })
                {
                    File.WriteAllBytes(StoragePath, bytes);
                    var refused = false; try { Call(Config, "Get", "ADMIN"); } catch (InvalidOperationException) { refused = true; }
                    Assert(refused, "Invalid file accepted");
                }
            }
            finally { File.WriteAllBytes(StoragePath, good); }
        }));
        await Case("DQ02.LockedWritePreservesFile", () => Sync(() =>
        {
            var before = SHA256.HashData(File.ReadAllBytes(StoragePath));
            using (var locked = new FileStream(StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var refused = false; try { Call(Config, "Save", "ADMIN", new LoginRequest("changed", Guid.NewGuid().ToString("N"))); } catch (InvalidOperationException) { refused = true; }
                Assert(refused, "Locked file unexpectedly replaced");
            }
            Assert(before.SequenceEqual(SHA256.HashData(File.ReadAllBytes(StoragePath))), "Previous file changed");
            Assert(!Directory.GetFiles(Path.GetDirectoryName(StoragePath)!, Path.GetFileName(StoragePath) + ".*.tmp").Any(), "Temporary leaked");
        }));
        var b = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = Db, IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = true, Pooling = false };
        Connection = b.ConnectionString; Owner = new SqlConnection(Connection); await Owner.OpenAsync();
        using (var marker = Owner.CreateCommand()) { marker.CommandText = "SELECT RunId FROM dbo.OperationInstallMarker"; Assert((string?)await marker.ExecuteScalarAsync() == "20261109_211954_5e6b3d68", "DB marker mismatch"); }
        var ownerFactory = new ConnectionFactory(Connection);
        using var stamp = Owner.CreateCommand(); stamp.CommandText = "SELECT SecurityStamp FROM dbo.TAIKHOAN WHERE TenDangNhap='admin'";
        var password = Credentials["ADMIN"].Password;
        await new BootstrapService(new SetupRepository(ownerFactory), new AuthRepository(ownerFactory), new PasswordService()).CompleteAsync(new BootstrapRequest { Mode = "ActivateDemo", Username = Credentials["ADMIN"].Username, Password = password, Confirmation = password, EmployeeCode = "NV001", ExpectedStamp = (byte[]) (await stamp.ExecuteScalarAsync())! });
        await Sql("CREATE USER DQRuntime WITHOUT LOGIN; ALTER ROLE CafeManagerApp ADD MEMBER DQRuntime;");
        var factory = new ConnectionFactory(Connection);
        typeof(ConnectionFactory).GetProperty("SessionInitializer", Any)!.SetValue(factory, (Func<SqlConnection, CancellationToken, Task>)(async (c, token) => { using var cmd = c.CreateCommand(); cmd.CommandText = "EXECUTE AS USER='DQRuntime'"; await cmd.ExecuteNonQueryAsync(token); }));
        Repository = new(factory); Auth = new(Repository, new PasswordService()); Permissions = new(Repository);
        using (var admin = await Auth.LoginAsync(Credentials["ADMIN"]))
        {
            Employees["ADMIN"] = admin.Identity.EmployeeCode;
            var employees = new EmployeeService(new EmployeeRepository(factory), Permissions);
            var accounts = new EmployeeAccountService(new EmployeeAccountRepository(factory), Permissions, new PasswordService());
            foreach (var r in new[] { "CASH", "STORE" })
            {
                var employee = await employees.SaveAsync(admin, new("DQ " + r, null, "Part-time", 0, 1, "CV003"), null, Guid.NewGuid());
                Employees[r] = employee;
                await accounts.WriteAsync(admin, "Create", new(employee, null, Credentials[r].Username, r, true), Credentials[r].Password, Credentials[r].Password, Guid.NewGuid());
            }
        }
        foreach (var r in Credentials.Keys)
        {
            await Case("DQ03.AuthAndPermission." + r, async () =>
            {
                using var session = await Auth.LoginAsync((LoginRequest)Call(Config, "Get", r)!);
                Assert(session.Identity.Role == r && session.Identity.EmployeeCode == Employees[r], "Identity mismatch");
                await Permissions.DemandAsync(session, r == "ADMIN" ? AppPermission.Finance : r == "CASH" ? AppPermission.Sales : AppPermission.Inventory);
                if (r != "ADMIN")
                {
                    var denied = false; try { await Permissions.DemandAsync(session, AppPermission.Finance); } catch (UnauthorizedAccessException) { denied = true; }
                    Assert(denied, "Finance allowed");
                }
                using var form = Login(); var count = 0;
                form.LoggedIn += s => { count++; Assert(s.Identity.Role == r, "Login event role mismatch"); s.Dispose(); };
                form.Show(); await Quick(form, r); Assert(count == 1, "Quick login event missing or duplicated"); form.Close();
            });
            await Case("DQ03.WrongPassword." + r, () => Denied(new(Credentials[r].Username, Guid.NewGuid().ToString("N"))));
        }
        await Case("DQ03.LockedAndRetired", async () =>
        {
            await Sql("UPDATE dbo.TAIKHOAN SET HoatDong=0 WHERE MaNV=@e", Employees["CASH"]);
            try { await Denied(Credentials["CASH"]); } finally { await Sql("UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV=@e", Employees["CASH"]); }
            await Sql("UPDATE dbo.NHANVIEN SET DangLamViec=0 WHERE MaNV=@e", Employees["STORE"]);
            try { await Denied(Credentials["STORE"]); } finally { await Sql("UPDATE dbo.NHANVIEN SET DangLamViec=1 WHERE MaNV=@e", Employees["STORE"]); }
        });
        await Case("DQ03.MismatchedRole", async () =>
        {
            Call(Config, "Save", "CASH", Credentials["STORE"]);
            try
            {
                using var form = Login(); var count = 0; form.LoggedIn += s => { count++; s.Dispose(); };
                form.Show(); await Quick(form, "CASH");
                Assert(count == 0 && Field<Label>(form, "statusLabel").Text.Contains("role"), "Wrong role entered");
                Assert(Field<Button>(form, "loginButton").Enabled, "Button not restored"); form.Close();
            }
            finally { Call(Config, "Save", "CASH", Credentials["CASH"]); }
        });
        await Case("DQ06.RevokedSession", async () =>
        {
            using var session = await Auth.LoginAsync(Credentials["CASH"]);
            await Sql("UPDATE dbo.TAIKHOAN SET HoatDong=0 WHERE MaNV=@e", Employees["CASH"]);
            try
            {
                var denied = false; try { await Permissions.DemandAsync(session, AppPermission.Sales); } catch (UnauthorizedAccessException) { denied = true; }
                Assert(denied && session.IsRevoked, "Stale session accepted");
            }
            finally { await Sql("UPDATE dbo.TAIKHOAN SET HoatDong=1 WHERE MaNV=@e", Employees["CASH"]); }
        });
        await Case("DQ06.NormalLogin", async () =>
        {
            using var form = new LoginForm(Auth, null); form.Show(); var count = 0;
            form.LoggedIn += s => { count++; s.Dispose(); };
            Field<TextBox>(form, "usernameInput").Text = Credentials["CASH"].Username;
            Field<TextBox>(form, "passwordInput").Text = Credentials["CASH"].Password;
            Field<Button>(form, "loginButton").PerformClick();
            for (var i = 0; i < 200 && Field<CancellationTokenSource?>(form, "pending") is not null; i++) await Task.Delay(25);
            Assert(count == 1 && Field<TextBox>(form, "passwordInput").Text == "", "Normal login failed"); form.Close();
        });
        await Case("DQ05.DoubleClickAndClosePending", async () =>
        {
            await Sql("BEGIN TRAN; UPDATE dbo.AppSetupState SET State=State WHERE Id=1;");
            try
            {
                using var form = Login(); form.Show(); var count = 0; form.LoggedIn += s => { count++; s.Dispose(); };
                var first = Quick(form, "CASH"); await Task.Delay(150);
                Assert(Field<CancellationTokenSource?>(form, "pending") is not null, "Not pending");
                Assert(!Field<Button>(form, "loginButton").Enabled && Field<List<Button>>(form, "devButtons").All(x => !x.Enabled), "Actions not locked");
                await Quick(form, "STORE"); Assert(!first.IsCompleted, "Duplicate affected pending request");
                form.Close(); await first.WaitAsync(TimeSpan.FromSeconds(10)); Assert(count == 0, "Late login emitted");
            }
            finally { await Sql("ROLLBACK TRAN;"); }
        });
        await Case("DQ04.ComponentDialogSave", async () =>
        {
            var type = App.GetType("CafeManager.WinForms.Forms.DevelopmentQuickLoginDialog")!;
            using var owner = Login(); owner.Show();
            using var dialog = (Form)Activator.CreateInstance(type, Any, null, [Auth, Config, "CASH"], null)!;
            dialog.Show(owner);
            Field<TextBox>(dialog, "username").Text = Credentials["CASH"].Username; Field<TextBox>(dialog, "password").Text = Credentials["CASH"].Password;
            Field<Button>(dialog, "save").PerformClick();
            for (var i = 0; i < 200 && Field<CancellationTokenSource?>(dialog, "pending") is not null; i++) await Task.Delay(25);
            Assert(Field<Label>(dialog, "status").Text.StartsWith("Đã lưu"), "Dialog save failed");
            Assert(Field<TextBox>(dialog, "password").Text == "" && dialog.AcceptButton is null && !dialog.ShowInTaskbar, "Dialog properties failed");
            dialog.Close(); owner.Close();
        });
        await RegressionEdges();
        Evidence["NativeStage"] = "NOT RUN; prior modal geometry/focus limitation remains; component API assertions kept separate";
        Controller.Close();
    }
}
