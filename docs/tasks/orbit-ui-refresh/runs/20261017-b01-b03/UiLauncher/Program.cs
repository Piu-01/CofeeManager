using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Requests;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Catalogs;
using Microsoft.Data.SqlClient;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length == 0) args = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "launch-arguments.txt"));
        // Test arrangement only: displays the unchanged production Forms. All UI input uses sky.
        // No login-dialog automation, PerformClick, SendKeys, UIA, or application-source changes.
        var output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(output);
        try
        {
            var db = args[1]; var mode = args[2];
            if (!Regex.IsMatch(db, @"^CoffeeManager_Test_G02_\d{8}_\d{6}_[a-f0-9]{8}$")) throw new Exception("Isolated G02 DB required");
            var cs = $"Server=localhost;Initial Catalog={db};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False";
            using (var owner = new SqlConnection(cs))
            {
                owner.Open();
                using var marker = new SqlCommand("SELECT COUNT(*) FROM dbo.G02TestMarker", owner);
                if (Convert.ToInt32(marker.ExecuteScalar()) != 1) throw new Exception("Missing test marker");
            }
            ConnectionFactory Factory(bool setup = false)
            {
                var factory = new ConnectionFactory(cs);
                Func<SqlConnection, CancellationToken, Task> initialize = async (connection, token) =>
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = setup ? "EXECUTE AS USER=N'G02_Setup'" : "EXECUTE AS USER=N'G02_Runtime'";
                    await cmd.ExecuteNonQueryAsync(token);
                };
                typeof(ConnectionFactory).GetProperty("SessionInitializer", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(factory, initialize);
                return factory;
            }
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.SetDefaultFont(new Font("Segoe UI", 10F));
            Application.SetColorMode(SystemColorMode.Dark);
            Application.ThreadException += (_, e) => { File.WriteAllText(Path.Combine(output, "thread-exception.txt"), e.Exception.ToString()); Application.Exit(); };
            var passwords = new PasswordService(); var authRepo = new AuthRepository(Factory());
            Form form; SessionContext? session = null;
            if (mode == "bootstrap") form = new BootstrapForm(new BootstrapService(new SetupRepository(Factory(true)), new AuthRepository(Factory(true)), passwords));
            else if (mode == "startup") form = new StartupForm(new TableStatusService(new TableStatusRepository(Factory())), null);
            else
            {
                var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
                var setupRepo = new SetupRepository(Factory(true));
                if (setupRepo.ReadStateAsync().GetAwaiter().GetResult().State == "Pending")
                    new BootstrapService(setupRepo, new AuthRepository(Factory(true)), passwords).CompleteAsync(new BootstrapRequest { Mode="NewEmployee", Username="orbit.admin", Password=password, Confirmation=password, EmployeeName="Kiểm thử Orbit" }).GetAwaiter().GetResult();
                var role = args.Length > 4 ? args[4] : "ADMIN";
                using (var owner = new SqlConnection(cs))
                {
                    owner.Open();
                    using var command = new SqlCommand("""
                        IF NOT EXISTS(SELECT 1 FROM dbo.NHANVIEN WHERE MaNV='NV900')
                          INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) VALUES('NV900',N'Orbit CASH','Full-time','CVADM');
                        IF NOT EXISTS(SELECT 1 FROM dbo.NHANVIEN WHERE MaNV='NV901')
                          INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) VALUES('NV901',N'Orbit STORE','Full-time','CVADM');
                        IF NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE TenDangNhap='orbit.cash')
                          INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES('NV900','orbit.cash',@hash,'CASH',1);
                        IF NOT EXISTS(SELECT 1 FROM dbo.TAIKHOAN WHERE TenDangNhap='orbit.store')
                          INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES('NV901','orbit.store',@hash,'STORE',1);
                        UPDATE dbo.TAIKHOAN SET MatKhauHash=@hash WHERE TenDangNhap IN('orbit.admin','orbit.cash','orbit.store');
                        """, owner);
                    command.Parameters.AddWithValue("@hash", passwords.Hash(password)); command.ExecuteNonQuery();
                }
                session = new AuthService(authRepo,passwords).LoginAsync(new LoginRequest("orbit." + role.ToLowerInvariant(), password)).GetAwaiter().GetResult();
                var permissions = new PermissionService(authRepo);
                var catalogService = new CatalogService(new CatalogRepository(Factory()), permissions);
                form = mode == "editor" ? new CatalogEditorDialog(catalogService, session, Enum.Parse<CatalogKind>(args[3]), null)
                    : new MainForm(session,permissions,ct => authRepo.ReadStateAsync(ct),() => new CatalogsControl(catalogService,session));
            }
            if (mode != "editor" && args.Length > 3)
            {
                var size = args[3].Split('x'); form.Size = new Size(int.Parse(size[0]),int.Parse(size[1]));
            }
            var label = mode + (args.Length > 4 ? "-" + args[4] : "") + "-" + args[3];
            void Record()
            {
                var geometry = Descendants(form).Select(c => new { Type=c.GetType().Name,c.Name,c.Bounds,c.Visible,c.Enabled,Font=c.Font.SizeInPoints, c.TabIndex }).ToArray();
                File.WriteAllText(Path.Combine(output,label+"-geometry.json"),JsonSerializer.Serialize(new { Mode=mode,Database=db,form.DeviceDpi,form.Size,form.ClientSize,Controls=geometry,FormCount=Application.OpenForms.Count,Handles=System.Diagnostics.Process.GetCurrentProcess().HandleCount }, new JsonSerializerOptions{WriteIndented=true}));
            }
            form.Shown += (_, _) => form.BeginInvoke((Action)Record);
            form.ResizeEnd += (_, _) => Record();
            Application.Run(form); session?.Dispose();
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(output,"launch-error.txt"),error.ToString()); Environment.ExitCode=1; }
    }
    static IEnumerable<Control> Descendants(Control root)
    {
        foreach(Control c in root.Controls) { yield return c; foreach(var child in Descendants(c)) yield return child; }
    }
}



