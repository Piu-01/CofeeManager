using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Sales;
using Microsoft.Data.SqlClient;

internal static class Program
{
    static readonly List<object> Results = [];
    static int failed;
    static void Check(string name, bool ok, object? actual = null) { Results.Add(new { Name = name, Outcome = ok ? "PASS" : "FAIL", Actual = actual }); if (!ok) failed++; }
    static FieldInfo Info(object o, string n) { for (var t = o.GetType(); t != null; t = t.BaseType) { var f = t.GetField(n, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); if (f != null) return f; } throw new MissingFieldException(n); }
    static T F<T>(object o, string n) => (T)Info(o, n).GetValue(o)!;
    static Task Call(object o, string n, params object?[] args) { for (var t = o.GetType(); t != null; t = t.BaseType) { var m = t.GetMethod(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly); if (m != null) { var result = m.Invoke(o, args); return result as Task ?? Task.CompletedTask; } } throw new MissingMethodException(n); }
    static IEnumerable<Control> Children(Control c) { foreach (Control x in c.Controls) { yield return x; foreach (var y in Children(x)) yield return y; } }
    static async Task Wait(Func<bool> p) { for (int i = 0; i < 600; i++) { if (p()) return; await Task.Delay(25); } throw new TimeoutException("UI readiness deadline"); }
    static async Task Idle(object p) { await Task.Delay(100); await Wait(() => !F<bool>(p, "busy")); }
    static Form? Open(string type) => Application.OpenForms.Cast<Form>().LastOrDefault(f => f.GetType().Name == type && f.Visible);
    static async Task<Form> Dialog(string type) { await Wait(() => Open(type) != null); return Open(type)!; }
    static Button[] Buttons(Control c) => Children(c).OfType<Button>().Where(b => b.Visible).ToArray();
    static Button Button(Control c, string text) => Buttons(c).Single(b => b.Text == text);
    static SalesMenuItem Item => new("SP001", "Ca phe den", 25000m, "Dang ban", true);

    [STAThread]
    static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize(); Application.SetColorMode(SystemColorMode.Dark);
        var dir = args[0];
        var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json"))).RootElement;
        var db = manifest.GetProperty("Database").GetString()!;
        var runId = manifest.GetProperty("RunId").GetString()!;
        using var owner = new SqlConnection($"Server=localhost;Initial Catalog={db};Integrated Security=True;TrustServerCertificate=True;Pooling=False"); owner.Open();
        object? Scalar(string sql) { using var c = owner.CreateCommand(); c.CommandText = sql; return c.ExecuteScalar(); }
        void Sql(string sql, string? hash = null) { using var c = owner.CreateCommand(); c.CommandText = sql; if (hash != null) c.Parameters.AddWithValue("@hash", hash); c.ExecuteNonQuery(); }
        int Count(string sql) => Convert.ToInt32(Scalar(sql));
        string? LatestOpen(string table) => (string?)Scalar($"SELECT TOP(1) MaHD FROM dbo.HOADON WHERE MaBan='{table}' AND TrangThai=N'Chua thanh toan' ORDER BY NgayLap DESC,MaHD DESC");
        int OpenCount(string table) => Count($"SELECT COUNT(*) FROM dbo.HOADON WHERE MaBan='{table}' AND TrangThai=N'Chua thanh toan'");
        if ((string?)Scalar("SELECT RunId FROM dbo.G04TestMarker") != runId || Convert.ToInt32(Scalar("SELECT MAX(Version) FROM dbo.SchemaMigrations")) != 16) throw new Exception("DB marker/schema mismatch");
        var cs = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("POSNEW_SQL_RUNTIME")!);
        if (cs.DataSource != "localhost" || cs.InitialCatalog != db || cs.IntegratedSecurity) throw new Exception("Runtime target mismatch");
        var factory = new ConnectionFactory(cs.ConnectionString); var auth = new AuthRepository(factory); var permission = new PermissionService(auth);
        var sales = new SalesService(new SalesRepository(factory), permission); var tables = new TableService(new TableRepository(factory), permission);
        var customers = new CustomerService(new CustomerRepository(factory), permission); var loyalty = new LoyaltyService(new LoyaltyRepository(factory), permission);
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        bool provisioned = false;
        try
        {
            if (Count("SELECT (SELECT COUNT(*) FROM dbo.NHANVIEN WHERE MaNV IN('SCA30','SCC30'))+(SELECT COUNT(*) FROM dbo.TAIKHOAN WHERE MaNV IN('SCA30','SCC30'))") != 0) throw new Exception("Fixture collision");
            Sql("BEGIN TRAN;INSERT dbo.NHANVIEN(MaNV,TenNV,LoaiHopDong,MaCV) SELECT v.code,N'POS new order fixture','Full-time',(SELECT MIN(MaCV) FROM dbo.CHUCVU) FROM(VALUES('SCA30'),('SCC30'))v(code);INSERT dbo.TAIKHOAN(MaNV,TenDangNhap,MatKhauHash,MaVT,HoatDong) VALUES('SCA30','posnew.admin',@hash,'ADMIN',1),('SCC30','posnew.cash',@hash,'CASH',1);COMMIT;", new PasswordService().Hash(password));
            provisioned = true;
            using var cash = new AuthService(auth, new PasswordService()).LoginAsync(new("posnew.cash", password)).GetAwaiter().GetResult();
            using var admin = new AuthService(auth, new PasswordService()).LoginAsync(new("posnew.admin", password)).GetAwaiter().GetResult();
            async Task<string> Occupy(string table, SessionContext actor, string note)
            {
                var code = await sales.AllocateAsync(actor);
                await sales.CreateAsync(actor, new(code, Guid.NewGuid(), new("Tai quan", table), "SP001", 1, note));
                return code;
            }
            var fixtureB1 = Occupy("B0001", admin, "Fixture ca khác giữ bàn 01").GetAwaiter().GetResult();
            Check("F00 B0001 occupied fixture is open", OpenCount("B0001") == 1 && (string?)Scalar($"SELECT TrangThai FROM dbo.HOADON WHERE MaHD='{fixtureB1}'") == "Chua thanh toan", fixtureB1);
            using var main = new MainForm { Text = "G04-POS-01 frozen functional test" };
            var page = new SalesControl(sales, tables, customers, loyalty, cash); F<Panel>(main, "content").Controls.Add(page);
            using var deadline = new System.Windows.Forms.Timer { Interval = 240000 };
            deadline.Tick += (_, _) => { Check("Runner deadline", false); main.Dialogs.AbortAll(); main.Dispose(); Application.ExitThread(); };
            main.Shown += async (_, _) =>
            {
                deadline.Start();
                var combo = F<ComboBox>(page, "table"); var channel = F<ComboBox>(page, "channel");
                var state = F<Label>(page, "state"); var status = F<Label>(page, "tableStatus");
                string Table() => combo.SelectedValue as string ?? "(null)";
                async Task Add() { await Call(page, "AddProduct", Item); await Idle(page); }
                async Task Reload() { await Call(page, "ReloadAll"); await Idle(page); }
                try
                {
                    await Idle(page);
                    Check("P00 default selection is occupied B0001 as in bug report", Table() == "B0001" && status.Text.Contains("Đang phục vụ"), new { Table = Table(), Status = status.Text });
                    // P1: Đơn mới trên bàn bận tự chuyển sang bàn trống đầu tiên, thêm món tạo đơn tại bàn mới.
                    await Call(page, "NewOrder"); await Idle(page);
                    var p1Table = Table();
                    Check("P01 New Order switches off occupied B0001", p1Table is "B0002" && OpenCount(p1Table) == 0, new { Selected = p1Table });
                    Check("P01 status announces free table", state.Text.Contains("Đã chuyển sang") && state.Text.Contains("trống") && status.Text.Contains("Trống"), new { State = state.Text, Status = status.Text });
                    await Add();
                    var p1Order = F<SalesOrderDto?>(page, "order");
                    Check("P01 first item creates order on switched table", p1Order is not null && p1Order.Header.TableCode == p1Table && p1Order.Lines.Count == 1, p1Order?.Header);
                    Check("P01 SQL order/line on switched table only", OpenCount(p1Table) == 1 && Count($"SELECT COUNT(*) FROM dbo.CHITIET_HD d JOIN dbo.HOADON h ON h.MaHD=d.MaHD WHERE h.MaBan='{p1Table}' AND h.MaNV='SCC30'") == 1 && OpenCount("B0001") == 1, new { New = LatestOpen(p1Table) });
                    // P3: Đơn mới lặp lại khi bàn vừa tạo đã bận -> chọn bàn trống kế tiếp.
                    await Call(page, "NewOrder"); await Idle(page);
                    var p3Table = Table();
                    Check("P03 repeat New Order picks next free table", p3Table is "B0004" && p3Table != p1Table && OpenCount(p3Table) == 0, new { Selected = p3Table });
                    Check("P03 repeat New Order created no header", F<SalesOrderDto?>(page, "order") is null && Count("SELECT COUNT(*) FROM dbo.HOADON WHERE MaNV='SCC30'") == 1);
                    // P2: bàn trống đang chọn được giữ nguyên.
                    combo.SelectedValue = "B0005"; await Task.Delay(50);
                    await Call(page, "NewOrder"); await Idle(page);
                    Check("P02 current free table is kept", Table() == "B0005" && state.Text.Contains("Đang chọn") && state.Text.Contains("trống"), new { Selected = Table(), State = state.Text });
                    // P6a: chọn bàn trống qua danh sách vẫn giữ đúng bàn và tạo đơn tại đó.
                    await page.OpenTableAsync("B0004"); await Idle(page);
                    Check("P06 table pick keeps chosen free table draft", Table() == "B0004" && page.HasUnsavedChanges && state.Text.Contains("Đã chọn") && status.Text.Contains("Trống"), new { Selected = Table(), State = state.Text });
                    await Add();
                    var p6Order = F<SalesOrderDto?>(page, "order");
                    Check("P06 table pick creates order at picked table", p6Order is not null && p6Order.Header.TableCode == "B0004" && OpenCount("B0004") == 1, p6Order?.Header);
                    // P6c: dialog Có/Không và Đồng ý/Hủy giữ nguyên hành vi sau khi thêm nhánh OK-only.
                    var ynTask = OrbitConfirmationDialog.ShowAsync(page, "Nội dung kiểm tra", "Kiểm tra Có Không", MessageBoxButtons.YesNo);
                    var yn = await Dialog(nameof(OrbitConfirmationDialog));
                    Check("P06 YesNo dialog has two actions", yn.Text == "Kiểm tra Có Không" && Buttons(yn).Length == 2 && Buttons(yn).Any(b => b.Text == "Có") && Buttons(yn).Any(b => b.Text == "Không"), Buttons(yn).Select(b => b.Text));
                    Button(yn, "Không").PerformClick(); await Wait(() => yn.IsDisposed || !yn.Visible);
                    Check("P06 YesNo cancel returns No", await ynTask == DialogResult.No, await ynTask);
                    var ocTask = OrbitConfirmationDialog.ShowAsync(page, "Nội dung kiểm tra", "Kiểm tra Đồng ý Hủy", MessageBoxButtons.OKCancel);
                    var oc = await Dialog(nameof(OrbitConfirmationDialog));
                    Check("P06 OKCancel dialog has two actions", oc.Text == "Kiểm tra Đồng ý Hủy" && Buttons(oc).Length == 2 && Buttons(oc).Any(b => b.Text == "Đồng ý") && Buttons(oc).Any(b => b.Text == "Hủy"), Buttons(oc).Select(b => b.Text));
                    Button(oc, "Hủy").PerformClick(); await Wait(() => oc.IsDisposed || !oc.Visible);
                    Check("P06 OKCancel cancel returns Cancel", await ocTask == DialogResult.Cancel, await ocTask);
                    // P4: toàn bộ bàn bận -> hộp thoại một nút, không tạo header, thêm món bị chặn.
                    var free = new List<string>();
                    using (var c = owner.CreateCommand()) { c.CommandText = "SELECT b.MaBan FROM dbo.BAN b WHERE NOT EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.MaBan=b.MaBan AND h.TrangThai=N'Chua thanh toan') ORDER BY b.MaBan"; using var r = c.ExecuteReader(); while (r.Read()) free.Add(r.GetString(0)); }
                    Check("P04 fixture frees at least one table before fill", free.Count > 0, free);
                    foreach (var t in free) await Occupy(t, admin, "Fixture đầy bàn " + t);
                    Check("P04 all tables occupied in SQL", Count("SELECT COUNT(*) FROM dbo.BAN b WHERE NOT EXISTS(SELECT 1 FROM dbo.HOADON h WHERE h.MaBan=b.MaBan AND h.TrangThai=N'Chua thanh toan')") == 0);
                    await Reload();
                    combo.SelectedValue = "B0001"; await Task.Delay(50);
                    var ordersBeforeFull = Count("SELECT COUNT(*) FROM dbo.HOADON");
                    var fullTask = Call(page, "NewOrder");
                    var full = await Dialog(nameof(OrbitConfirmationDialog));
                    Check("P04 no free table dialog title", full.Text == "Hết bàn trống", full.Text);
                    Check("P04 no free table dialog has one action only", Buttons(full).Length == 1 && Buttons(full)[0].Text == "Đã hiểu", Buttons(full).Select(b => b.Text));
                    Button(full, "Đã hiểu").PerformClick(); await Wait(() => full.IsDisposed || !full.Visible);
                    await fullTask; await Idle(page);
                    Check("P04 table cleared and state explains full house", combo.SelectedIndex == -1 && state.Text.Contains("Không còn bàn trống"), new { Selected = Table(), State = state.Text });
                    var countBeforeAdd = Count("SELECT COUNT(*) FROM dbo.HOADON");
                    await Add();
                    Check("P04 add blocked with no header written", F<SalesOrderDto?>(page, "order") is null && Count("SELECT COUNT(*) FROM dbo.HOADON") == countBeforeAdd && state.Text.Contains("Chọn bàn trước khi thêm món"), new { State = state.Text, Orders = countBeforeAdd });
                    Check("P04 no header on occupied table from full-house path", ordersBeforeFull == Count("SELECT COUNT(*) FROM dbo.HOADON"));
                    // P5: sau khi hết bàn, chuyển Mang đi vẫn bán được.
                    channel.SelectedValue = "Mang di"; await Task.Delay(50); await Add();
                    var takeAway = F<SalesOrderDto?>(page, "order");
                    Check("P05 take-away creates order after full house", takeAway is not null && takeAway.Header.Channel == "Mang di" && takeAway.Header.TableCode is null, takeAway?.Header);
                    Check("P05 take-away SQL row has no table", Count("SELECT COUNT(*) FROM dbo.HOADON WHERE MaNV='SCC30' AND MaBan IS NULL AND LoaiDon='Mang di' AND TrangThai=N'Chua thanh toan'") == 1);
                    // P6b: F4 mở đơn có sẵn và thêm món vẫn chạy (đường dùng được trong báo lỗi).
                    var beforeLines = Count("SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD='HD20250410'");
                    await page.OpenOrderAsync("HD20250410"); await Idle(page);
                    var opened = F<SalesOrderDto?>(page, "order");
                    Check("P06 open existing order loads table/context", opened is not null && opened.Header.TableCode == "B0003" && opened.Header.Channel == "Tai quan", opened?.Header);
                    await Add();
                    Check("P06 add line to existing order succeeds", Count($"SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD='HD20250410'") == beforeLines + 1, new { Before = beforeLines, After = Count("SELECT COUNT(*) FROM dbo.CHITIET_HD WHERE MaHD='HD20250410'") });
                }
                catch (Exception e) { Check("Runner exception", false, (e.InnerException ?? e).ToString()); }
                finally { deadline.Stop(); main.Dialogs.AbortAll(); page.Dispose(); main.Dispose(); Application.ExitThread(); }
            };
            Application.Run(main);
            Check("F99 no order was created on occupied B0001 by cashier", Count("SELECT COUNT(*) FROM dbo.HOADON WHERE MaBan='B0001' AND MaNV='SCC30' AND TrangThai=N'Chua thanh toan'") == 0);
        }
        catch (Exception e) { Check("Harness exception", false, (e.InnerException ?? e).ToString()); }
        finally
        {
            if (provisioned) Sql("UPDATE dbo.TAIKHOAN SET HoatDong=0 WHERE MaNV IN('SCA30','SCC30');");
            File.WriteAllText(Path.Combine(dir, "results.json"), JsonSerializer.Serialize(new { Failed = failed, Database = db, Scope = "Frozen source; real Auth/runtime SQL and in-process native UI actions; dialog clicks via WinForms controls. No physical keyboard/visual acceptance.", Results }, new JsonSerializerOptions { WriteIndented = true }));
        }
        return failed == 0 ? 0 : 1;
    }
}
