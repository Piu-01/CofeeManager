using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Navigation;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

internal static class Program
{
    private const string Database = "CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout";
    private static readonly List<object> Results = [];
    private static string directory = "";
    private static int failed;
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr key, IntPtr data);
    [STAThread]
    private static int Main(string[] args)
    {
        directory = Path.GetFullPath(args.Single());
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.SetCompatibleTextRenderingDefault(false);
        using var host = new Form { ShowInTaskbar = false, Opacity = 0 };
        using var watchdog = new System.Windows.Forms.Timer { Interval = 120000 };
        watchdog.Tick += (_, _) => { Check("Runner timeout", false); Save(); Environment.Exit(2); };
        host.Shown += async (_, _) =>
        {
            watchdog.Start();
            try { await Run(); } catch (Exception ex) { Check("Runner exception", false, ex.ToString()); }
            finally { watchdog.Stop(); Save(); host.Close(); }
        };
        Application.Run(host);
        Console.WriteLine($"Sidebar regression: {Results.Count - failed}/{Results.Count} PASS; failed={failed}");
        return failed == 0 ? 0 : 1;
    }

    private static async Task Run()
    {
        var cs = $"Server=localhost;Initial Catalog={Database};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False;Connect Timeout=5";
        using var db = new SqlConnection(cs); await db.OpenAsync();
        using var schema = db.CreateCommand(); schema.CommandText = "SELECT MAX(Version) FROM dbo.SchemaMigrations";
        Check("Test schema016", Convert.ToInt32(await schema.ExecuteScalarAsync()) == 16);
        using var marker = db.CreateCommand(); marker.CommandText = "SELECT COUNT(*) FROM dbo.G04TestMarker";
        if (Convert.ToInt32(await marker.ExecuteScalarAsync()) != 1) throw new InvalidOperationException("Expected test marker.");
        var auth = new AuthRepository(new ConnectionFactory(cs)); var permissions = new PermissionService(auth);
        foreach (var role in new[] { "ADMIN", "CASH", "STORE" })
        {
            using var id = db.CreateCommand();
            id.CommandText = "SELECT MIN(t.MaTK) FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV WHERE t.MaVT=@role AND t.HoatDong=1 AND n.DangLamViec=1";
            id.Parameters.AddWithValue("@role", role);
            var identity = await auth.ReadIdentityAsync(Convert.ToInt32(await id.ExecuteScalarAsync())) ?? throw new InvalidOperationException("Missing test identity.");
            using var session = new SessionContext(identity);
            var pages = new Dictionary<NavigationTarget, ProbePage>(); var created = new Dictionary<NavigationTarget, int>();
            var factories = Enum.GetValues<NavigationTarget>().Where(t => t is not (NavigationTarget.Home or NavigationTarget.Catalogs or NavigationTarget.SystemInfo))
                .ToDictionary(t => t, t => (Func<UserControl>)(() => { created[t] = created.GetValueOrDefault(t) + 1; return pages[t] = new ProbePage(); }));
            using var form = new MainForm(session, permissions, auth.ReadStateAsync, () => new ProbePage(), factories);
            try
            {
                form.Show(); await Wait(() => Field<Label>(form, "statusLabel").Text == "Phiên đang hoạt động.");
                var side = Field<OrbitSidebar>(form, "sidebar");
                Check(role + " native DPI125", form.DeviceDpi == 120, new { form.DeviceDpi, Screen = Screen.FromControl(form).Bounds, WorkingArea = Screen.FromControl(form).WorkingArea });
                if (form.DeviceDpi != 120) throw new InvalidOperationException("Actual desktop not125%; no scale change allowed.");
                var expected = role switch
                {
                    "ADMIN" => new[] { NavigationTarget.Sales, NavigationTarget.Invoices, NavigationTarget.Delivery, NavigationTarget.Tables, NavigationTarget.Customers, NavigationTarget.Loyalty, NavigationTarget.Products, NavigationTarget.Materials, NavigationTarget.Inventory, NavigationTarget.Finance, NavigationTarget.Debt, NavigationTarget.Assets, NavigationTarget.Reports, NavigationTarget.HumanResources },
                    "CASH" => new[] { NavigationTarget.Sales, NavigationTarget.Invoices, NavigationTarget.Delivery, NavigationTarget.Customers, NavigationTarget.Materials },
                    _ => new[] { NavigationTarget.Products, NavigationTarget.Materials, NavigationTarget.Inventory, NavigationTarget.Debt }
                };
                Check(role + " complete ordered menu", side.NavigationButtons.Where(b => b.Tag is NavigationTarget).Select(b => (NavigationTarget)b.Tag!).SequenceEqual(expected));
                Check(role + " catalog ownership/visibility", side.NavigationButtons.Contains(Field<Button>(form, "catalogsButton")) == (role == "ADMIN"));
                Check(role + " initial target", role == "CASH" ? pages.ContainsKey(NavigationTarget.Sales) : Current(form)?.GetType().Name == "SessionHomeControl");
                Check(role + " group count (empty groups hidden)", Headings(side).Length == (role == "ADMIN" ? 5 : role == "CASH" ? 4 : 3), Headings(side).Select(h => h.Text).ToArray());
                Check(role + " all authorized tabs visible initially", side.NavigationButtons.All(b => b.Visible));
                Check(role + " headings are noninteractive labels", Headings(side).All(h => !h.TabStop && !h.CanSelect) && !Descendants(side).OfType<Button>().Any(b => b.AccessibleDescription == "Nhóm điều hướng"));
                var current = Current(form);
                foreach (var h in Headings(side))
                {
                    PostMessage(h.Handle, 0x201, (IntPtr)1, (IntPtr)0x00050005);
                    PostMessage(h.Handle, 0x202, IntPtr.Zero, (IntPtr)0x00050005); await Task.Delay(20);
                    Check(role + " label click never hides tabs or navigates: " + h.Text, Current(form) == current && side.NavigationButtons.All(b => b.Visible));
                }
                var first = Field<Button>(form, "homeButton");
                form.Activate(); first.Focus();
                form.SelectNextControl(first, true, true, true, true); var forward = Focused(form);
                Check(role + " Tab skips headings and enters navigation", forward is Button forwardButton && side.NavigationButtons.Contains(forwardButton) && forwardButton.Visible && forwardButton.Enabled && forwardButton != first);
                form.SelectNextControl(forward, false, true, true, true);
                Check(role + " ShiftTab traversal returns", first.Focused);

                foreach (var size in new[] { new Size(1280, 720), new Size(1366, 768), new Size(800, 640), new Size(1366, 768) })
                {
                    form.ClientSize = size; await Task.Delay(80);
                    var brand = Field<Label>(form, "brandLabel"); var caption = Field<Label>(form, "brandCaption"); var logout = Field<Button>(form, "logoutButton");
                    var viewport = Descendants(side).OfType<Panel>().Single(p => p.AutoScroll);
                    var head = brand.Parent!; var foot = logout.Parent!;
                    Check(role + " disjoint fixed regions " + size, head.Bottom <= viewport.Top && viewport.Bottom <= foot.Top && foot.Bottom <= side.ClientSize.Height,
                        new { form.ClientSize, side.Compact, Head = head.Bounds, Viewport = viewport.Bounds, Footer = foot.Bounds });
                    Check(role + " header bounds/order " + size, side.Compact ? !brand.Visible && !caption.Visible : brand.Visible && brand.Bottom <= caption.Top && caption.Bottom <= head.Height && head.ClientRectangle.Contains(brand.Bounds));
                    Check(role + " logout always inside footer " + size, logout.Visible && foot.ClientRectangle.Contains(logout.Bounds));
                    var position = head.Bounds; var footerPosition = foot.Bounds;
                    viewport.AutoScrollPosition = new Point(0, int.MaxValue); await Task.Delay(20);
                    Check(role + " scroll moves menu only " + size, head.Bounds == position && foot.Bounds == footerPosition);
                    Check(role + " all tabs stay visible across resize/scroll " + size, side.NavigationButtons.All(b => b.Visible));
                    if (side.Compact)
                    {
                        Check(role + " compact exposes all targets", side.NavigationButtons.All(b => b.Visible && !string.IsNullOrWhiteSpace(b.AccessibleName)) && Headings(side).All(h => !h.Visible));
                        var hints = Field<ToolTip>(side, "hints");
                        Check(role + " compact tooltips preserve names", side.NavigationButtons.All(b => hints.GetToolTip(b) == b.Text));
                        Check(role + " compact icon fits despite scrollbar", side.NavigationButtons.All(b => b.Width >= 39), side.NavigationButtons.Select(b => new { b.Text, b.Width, b.Height }).ToArray());
                    }
                    else
                    {
                        Check(role + " plain labels visible in full sidebar " + size, Headings(side).All(h => h.Visible && !h.TabStop));
                        foreach (var button in side.NavigationButtons.Where(b => b.Visible))
                            Check(role + " row bounds " + size + " " + button.Text, button.Parent!.ClientRectangle.Contains(button.Bounds), new { button.Bounds, Parent = button.Parent.ClientRectangle });
                    }
                }
                foreach (var target in expected)
                {
                    await form.OpenWorkspaceAsync(target.ToString());
                    var selected = side.NavigationButtons.OfType<OrbitButton>().Where(b => b.NavigationSelected).ToArray();
                    Check(role + " external navigation reveals " + target, selected.Length == 1 && Equals(selected[0].Tag, target) && selected[0].Visible && Current(form) == pages[target]);
                    var viewport = Descendants(side).OfType<Panel>().Single(p => p.AutoScroll);
                    Check(role + " selected target scrolled into viewport " + target, selected.Length == 1 && viewport.ClientRectangle.Contains(viewport.RectangleToClient(selected[0].RectangleToScreen(selected[0].ClientRectangle))));
                    Check(role + " navigating never hides other tabs " + target, side.NavigationButtons.All(b => b.Visible));
                }
                var clickTarget = expected[0];
                var clickButton = side.NavigationButtons.Single(b => Equals(b.Tag, clickTarget));
                side.Reveal(clickButton); clickButton.PerformClick(); clickButton.PerformClick();
                await Wait(() => !Field<bool>(form, "navigating"));
                Check(role + " navigation click handler and repeat guard", Current(form) == pages[clickTarget] && created[clickTarget] == 1);
                var firstTarget = expected[0]; await form.OpenWorkspaceAsync(firstTarget.ToString());
                var page = pages[firstTarget]; page.Input.Text = "draft-filter-keep";
                await form.OpenWorkspaceAsync(expected[1].ToString()); await form.OpenWorkspaceAsync(firstTarget.ToString());
                Check(role + " module cached and input retained", created[firstTarget] == 1 && pages[firstTarget] == page && page.Input.Text == "draft-filter-keep");
                var navBefore = Current(form); page.Writing = true;
                using (var dismiss = Dismiss("Đồng ý")) await form.OpenWorkspaceAsync(expected[1].ToString());
                Check(role + " busy guard preserves page", Current(form) == navBefore); page.Writing = false;
                page.Dirty = true;
                using (var dismiss = Dismiss("Không")) await form.OpenWorkspaceAsync(expected[1].ToString());
                Check(role + " refuse discard retains draft/selection", Current(form) == page && page.Dirty && Equals(side.NavigationButtons.OfType<OrbitButton>().Single(b => b.NavigationSelected).Tag, firstTarget));
                using (var dismiss = Dismiss("Có")) await form.OpenWorkspaceAsync(expected[1].ToString());
                Check(role + " approve discard navigates once", !page.Dirty && page.DiscardCount == 1 && Current(form) == pages[expected[1]]);
                var modalCurrent = Current(form);
                using (var dialog = new OrbitDialogForm { Text = "Sidebar modal guard", ClientSize = new Size(300, 180) })
                {
                    dialog.Shown += async (_, _) => { await form.OpenWorkspaceAsync(firstTarget.ToString()); Check(role + " modal blocks navigation", Current(form) == modalCurrent); dialog.Close(); };
                    await OrbitDialogHost.ShowAsync(dialog, form);
                }
                var logoutEvents = 0; form.LoggedOut += () => logoutEvents++;
                Field<Button>(form, "logoutButton").PerformClick();
                await Wait(() => form.IsDisposed);
                Check(role + " native logout handler once and session revoked", logoutEvents == 1 && session.IsRevoked);
            }
            catch (Exception ex) { Check(role + " exception", false, ex.ToString()); }
            finally { if (!form.IsDisposed) { FieldInfo f = typeof(MainForm).GetField("closeApproved", BindingFlags.Instance | BindingFlags.NonPublic)!; f.SetValue(form, true); form.Close(); } }
        }
        using var transaction = db.CreateCommand(); transaction.CommandText = "DECLARE @depth int=@@TRANCOUNT,@state int=XACT_STATE(); SELECT CONCAT(@depth,':',@state)";
        var transactionState = Convert.ToString(await transaction.ExecuteScalarAsync());
        Check("Read-only fixture transaction clean", transactionState == "0:0", transactionState);
    }
    private static System.Windows.Forms.Timer Dismiss(string buttonText)
    {
        var timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (_, _) => { var dialog = Application.OpenForms.OfType<OrbitConfirmationDialog>().FirstOrDefault(); if (dialog is null) return; var button = Descendants(dialog).OfType<Button>().FirstOrDefault(b => b.Text == buttonText); if (button is null) return; timer.Stop(); button.PerformClick(); };
        timer.Start(); return timer;
    }
    private static Label[] Headings(Control side) => Descendants(side).OfType<Label>().Where(label => label.AccessibleDescription == "Nhóm điều hướng").ToArray();
    private static Control? Current(MainForm form) => Field<Panel>(form, "content").Controls.Cast<Control>().FirstOrDefault(c => c.Visible);
    private static Control? Focused(Control root) => Descendants(root).FirstOrDefault(c => c.Focused);
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static IEnumerable<Control> Descendants(Control root) { foreach (Control child in root.Controls) { yield return child; foreach (var c in Descendants(child)) yield return c; } }
    private static async Task Wait(Func<bool> predicate) { for (var i = 0; i < 200 && !predicate(); i++) await Task.Delay(25); if (!predicate()) throw new TimeoutException("UI condition not reached."); }
    private static void Check(string name, bool ok, object? actual = null) { if (!ok) failed++; Results.Add(new { Name = name, Outcome = ok ? "PASS" : "FAIL", Actual = actual }); }
    private static void Save() => File.WriteAllText(Path.Combine(directory, "results.json"), JsonSerializer.Serialize(new { Database, Schema = 16, Mode = "Read-only SQL identity/permission checks; real MainForm/native handlers; stub module bodies; no business CRUD/login or visual acceptance", Results, Failed = failed }, new JsonSerializerOptions { WriteIndented = true }));
}

internal sealed class ProbePage : UserControl, IUnsavedChanges
{
    internal readonly TextBox Input = new() { Dock = DockStyle.Top };
    internal bool Dirty, Writing; internal int DiscardCount;
    public bool HasUnsavedChanges => Dirty;
    public bool IsWriting => Writing;
    public void DiscardChanges() { Dirty = false; DiscardCount++; }
    public ProbePage() => Controls.Add(Input);
}
