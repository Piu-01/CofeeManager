using System.Collections;
using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Products;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Modules.Catalogs;
using CafeManager.WinForms.Modules.Customers;
using CafeManager.WinForms.Modules.Loyalty;
using CafeManager.WinForms.Modules.Menu;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.Modules.Tables;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

static class Program
{
    static object? Field(object value, string name)
    {
        for (var type = value.GetType(); type is not null; type = type.BaseType) if (type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) is { } field) return field.GetValue(value);
        return null;
    }
    static IEnumerable<Control> All(Control root) { yield return root; foreach (Control child in root.Controls) foreach (var item in All(child)) yield return item; }
    [STAThread] static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize(); Application.SetColorMode(SystemColorMode.Dark);
        const string db = "CoffeeManager_Test_G03_20261019_215457_29e552a9_demo";
        var guard = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = db, IntegratedSecurity = true, TrustServerCertificate = true };
        using (var c = new SqlConnection(guard.ConnectionString)) { c.Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT RunId FROM dbo.G03TestMarker"; if (cmd.ExecuteScalar() as string != "20261019_215457_29e552a9") throw new Exception("Marker mismatch"); }
        var credentials = new SqlConnectionStringBuilder(DevelopmentConnection.Read()); if (credentials.InitialCatalog != "CoffeeManager_Development") throw new Exception("Unexpected profile"); credentials.InitialCatalog = db;
        var factory = new ConnectionFactory(credentials.ConnectionString); var auth = new AuthRepository(factory); var permissions = new PermissionService(auth);
        var results = new List<object>();
        void Save() => File.WriteAllText(args[0], JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        using var host = new Form { Text = "Orbit eight screens in-process read-only probe", ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), ClientSize = new Size(1166, 718) };
        async Task Settle(Control root)
        {
            await Task.Delay(150);
            for (var i = 0; i < 60 && (All(root).OfType<Label>().Any(label => label.Text.StartsWith("Đang tải")) || All(root).OfType<OrbitRecordListControl>().Any(c => Field(c, "busy") is true) || All(root).OfType<CatalogsControl>().Any(c => Field(c, "loading") is not null)); i++) await Task.Delay(100);
            await Task.Delay(100); root.PerformLayout();
        }
        object Snapshot(string screen, Control root)
        {
            var workspace = All(root).OfType<OrbitListWorkspace>().First(w => w.Visible);
            var grid = All(workspace).OfType<OrbitDataGridView>().Single();
            using var bitmap = new Bitmap(root.Width, root.Height); root.DrawToBitmap(bitmap, root.ClientRectangle); bitmap.Save(Path.Combine(Path.GetDirectoryName(args[0])!, "probe-" + screen + ".png"));
            var first = grid.Rows.Count > 0 ? Convert.ToString(grid.Rows[0].Cells[0].Value) : null;
            foreach (var overlay in All(workspace.Body).OfType<OrbitEmptyState>())
            {
                results.Add(new { Case = "EmptyOverlay", Screen = screen, overlay.Visible, overlay.Bounds, FrontIndex = overlay.Parent!.Controls.GetChildIndex(overlay), overlay.HistoryPrompt });
                if (overlay.Visible) results.Add(new { Case = "EmptyHeaderOffset", Screen = screen, Pass = overlay.Top == grid.ColumnHeadersHeight });
            }
            return new { Case = "Screen", Screen = screen, Dpi = root.DeviceDpi, Size = root.ClientSize, Title = workspace.Title.Text, RowHeight = grid.RowTemplate.Height, Rows = grid.Rows.Count, FirstCode = first, Count = workspace.Count.Text, Range = workspace.Pagination.Range.Text, Tabs = All(workspace.Tabs).OfType<Button>().Select(b => b.Text).ToArray(), Primary = workspace.PrimaryActions.Controls.OfType<Button>().Where(b => b.Visible).Select(b => new { b.Text, b.Enabled, b.Width, b.Height }).ToArray(), Actions = workspace.RowActions.Controls.OfType<Button>().Where(b => b.Visible).Select(b => new { b.Text, b.Enabled, b.Width, b.Height }).ToArray(), Columns = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).Select(c => new { c.DataPropertyName, c.Width, c.ReadOnly }).ToArray(), HeaderOutsideScroll = workspace.Body.Top >= workspace.RowActions.Bottom || !workspace.ShowMeta, FooterInside = workspace.Pagination.Bottom <= workspace.Height, Cards = All(root).OfType<OrbitTableCard>().Count(), State = workspace.Status.Text };
        }
        host.Shown += async (_, _) =>
        {
            try
            {
                host.ClientSize = new Size(1166 * host.DeviceDpi / 96, 718 * host.DeviceDpi / 96);
                var identity = (await auth.ReadCredentialAsync("admin"))!.Identity; using var session = new SessionContext(identity);
                var products = new ProductService(new ProductRepository(factory), permissions);
                var tables = new TableService(new TableRepository(factory), permissions);
                var customers = new CustomerService(new CustomerRepository(factory), permissions);
                var loyalty = new LoyaltyService(new LoyaltyRepository(factory), permissions);
                if (args.Contains("--shell-only"))
                {
                    using var shell = new CafeManager.WinForms.Forms.MainForm { Text = "Orbit shell layout probe — no login" };
                    var catalog = new CatalogsControl(new CatalogService(new CatalogRepository(factory), permissions), session) { Dock = DockStyle.Fill };
                    ((Panel)Field(shell, "content")!).Controls.Add(catalog);
                    shell.Show(); await Settle(shell);
                    var sidebar = (Control)Field(shell, "sidebar")!;
                    var strip = All(catalog).OfType<OrbitSectionTabs>().Single();
                    results.Add(new { Case = "ShellStartup", shell.DeviceDpi, SidebarWidth = sidebar.Width, ExpectedSidebarWidth = 200 * shell.DeviceDpi / 96, Pass = sidebar.Width == 200 * shell.DeviceDpi / 96 });
                    results.Add(new { Case = "ShellTabs", strip.ClientSize, Scroll = strip.HorizontalScroll.Visible, Pass = !strip.HorizontalScroll.Visible && strip.Controls.Cast<Control>().All(c => c.Bottom <= strip.ClientSize.Height) });
                    using var bitmap = new Bitmap(shell.Width, shell.Height); shell.DrawToBitmap(bitmap, shell.ClientRectangle); bitmap.Save(Path.Combine(Path.GetDirectoryName(args[0])!, "probe-shell-" + shell.DeviceDpi + ".png")); Save();
                    shell.Close(); return;
                }
                if (args.Contains("--overlay-only"))
                {
                    using var root = new LoyaltyControl(loyalty, customers, session) { Dock = DockStyle.Fill };
                    host.Controls.Add(root); await Settle(root);
                    var frame = All(root).OfType<OrbitManagementTabs>().Single();
                    ((OrbitSectionTabs)Field(frame, "tabs")!).SelectedIndex = 2; await Settle(root);
                    var workspace = All(root).OfType<OrbitListWorkspace>().Single(w => w.Visible);
                    var grid = All(workspace).OfType<OrbitDataGridView>().Single();
                    var overlay = All(workspace).OfType<OrbitEmptyState>().Single();
                    grid.ColumnHeadersHeight += 20 * grid.DeviceDpi / 96;
                    results.Add(new { Case = "EmptyHeaderResize", grid.DeviceDpi, grid.ColumnHeadersHeight, OverlayTop = overlay.Top, Pass = overlay.Visible && overlay.Top == grid.ColumnHeadersHeight });
                    grid.Profile = grid.Profile;
                    results.Add(Snapshot("points-header-restored", root));
                    return;
                }
                if (args.Contains("--navigation-only"))
                {
                    using var content = new Panel { Dock = DockStyle.Fill }; host.Controls.Add(content);
                    using var navigation = new CafeManager.WinForms.Navigation.NavigationController(content, session, permissions);
                    var created = 0;
                    UserControl CreatePage() { created++; return new PagedFixture(session, 2); }
                    await navigation.NavigateAsync(CafeManager.WinForms.Navigation.NavigationTarget.Products, CreatePage, host); await Settle(content);
                    var first = content.Controls.OfType<PagedFixture>().Single();
                    All(first).OfType<OrbitSearchBox>().Single().Input.Text = "Nháp tìm kiếm";
                    await navigation.NavigateAsync(CafeManager.WinForms.Navigation.NavigationTarget.Customers, CreatePage, host); await Settle(content);
                    await navigation.NavigateAsync(CafeManager.WinForms.Navigation.NavigationTarget.Products, CreatePage, host); await Settle(content);
                    results.Add(new { Case = "NavigationCache", Created = created, Pages = content.Controls.Count, VisiblePages = content.Controls.Cast<Control>().Count(c => c.Visible), Pass = created == 2 && content.Controls.Count == 2 && first.Visible && content.Controls.Cast<Control>().Count(c => c.Visible) == 1 && All(first).OfType<OrbitSearchBox>().Single().Input.Text == "Nháp tìm kiếm" });
                    using var strip = new OrbitSectionTabs { Dock = DockStyle.None, Height = 38 * host.DeviceDpi / 96 };
                    strip.Add("Khuyến mãi"); strip.Add("Hạng thành viên"); strip.Add("Điểm khách hàng"); content.Controls.Add(strip); strip.BringToFront();
                    foreach (var width in new[] { 160, 900 })
                    {
                        strip.Width = width * host.DeviceDpi / 96; strip.PerformLayout(); await Task.Delay(100);
                        results.Add(new { Case = "TabsWidth", Width = width, strip.HorizontalScroll.Visible, Pass = strip.Controls.Cast<Control>().All(c => c.Bottom <= strip.ClientSize.Height) && (width == 160 || !strip.HorizontalScroll.Visible) });
                    }
                    return;
                }
                async Task Check(string name, Control root)
                {
                    root.Dock = DockStyle.Fill; host.Controls.Add(root); await Settle(root); results.Add(Snapshot(name, root)); Save();
                    foreach (var width in new[] { 1080, 1166 })
                    {
                        host.ClientSize = new Size(width * host.DeviceDpi / 96, (width == 1080 ? 670 : 718) * host.DeviceDpi / 96); await Settle(root);
                        var workspace = All(root).OfType<OrbitListWorkspace>().First(w => w.Visible);
                        results.Add(new { Case = "Resize", Screen = name, Width = width + 200, BodyHeight = workspace.Body.Height, Pass = workspace.Body.Height > 0 && workspace.Pagination.Bottom <= workspace.Height && workspace.PrimaryActions.Left >= workspace.Title.Right });
                    }
                }
                using (var root = new MaterialsControl(new MaterialService(new MaterialRepository(factory), permissions), session)) { await Check("01-inventory", root); host.Controls.Remove(root); }
                using (var root = new ProductsControl(products, session))
                {
                    await Check("02-menu", root);
                    var category = (ComboBox)Field(root, "categoryFilter")!; category.SelectedIndex = Math.Min(2, category.Items.Count - 1); await Settle(root);
                    var grid = (OrbitDataGridView)Field(root, "Grid")!; var selected = category.SelectedValue as string;
                    results.Add(new { Case = "MenuCategoryFilter", Pass = grid.Rows.Cast<DataGridViewRow>().All(r => r.DataBoundItem is ProductRow item && item.Category == selected), Category = selected, Rows = grid.Rows.Count });
                    var search = All(root).OfType<OrbitSearchBox>().Single().Input;
                    search.Text = "ORBIT_NO_MATCH_20261021";
                    await (Task)typeof(OrbitRecordListControl).GetMethod("ResetAndReload", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(root, null)!;
                    results.Add(new { Case = "MenuEmptySearch", Pass = grid.Rows.Count == 0 && All(root).OfType<OrbitEmptyState>().Any(c => c.Visible) });
                    host.Controls.Remove(root);
                }
                using (var root = new TablesControl(tables, session))
                {
                    await Check("03-tables", root);
                    var frame = All(root).OfType<OrbitManagementTabs>().Single(); var tabs = (OrbitSectionTabs)Field(frame, "tabs")!;
                    var tablePage = All(root).OfType<OrbitRecordListControl>().First(p => p.Visible);
                    var chosen = All(tablePage).OfType<OrbitTableCard>().Single(c => c.TableCode == "B0003"); chosen.PerformClick(); await Settle(root);
                    var selectedGrid = (OrbitDataGridView)Field(tablePage, "Grid")!;
                    results.Add(new { Case = "CardSelection", Code = (selectedGrid.CurrentRow?.DataBoundItem as CafeManager.Models.Dtos.Tables.TableRow)?.Code, SelectedCards = All(tablePage).OfType<OrbitTableCard>().Where(c => c.Selected).Select(c => c.TableCode).ToArray(), Pass = chosen.Selected && (selectedGrid.CurrentRow?.DataBoundItem as CafeManager.Models.Dtos.Tables.TableRow)?.Code == "B0003" && All(tablePage).OfType<OrbitTableCard>().Count(c => c.Selected) == 1 });
                    var filter = All(tablePage).OfType<ComboBox>().Single(); filter.SelectedIndex = Math.Min(2, filter.Items.Count - 1); await Settle(root);
                    var tableGrid = (OrbitDataGridView)Field(tablePage, "Grid")!;
                    results.Add(new { Case = "TableAreaFilter", Area = filter.SelectedValue, Rows = tableGrid.Rows.Count, Pass = tableGrid.Rows.Count > 0 && tableGrid.Rows.Cast<DataGridViewRow>().All(r => r.DataBoundItem is CafeManager.Models.Dtos.Tables.TableRow item && item.Area == filter.SelectedValue as string) });
                    var listMode = All(tablePage).OfType<OrbitButton>().Single(b => b.Text == "Danh sách"); listMode.PerformClick();
                    results.Add(new { Case = "TableListMode", Pass = tableGrid.Visible });
                    All(tablePage).OfType<OrbitButton>().Single(b => b.Text == "Sơ đồ").PerformClick();
                    results.Add(new { Case = "TableMapMode", Pass = !tableGrid.Visible && All(tablePage).OfType<OrbitTableCard>().Any(c => c.Visible) });
                    tabs.SelectedIndex = 1; await Settle(root); results.Add(Snapshot("04-areas", root)); Save(); host.Controls.Remove(root);
                }
                using (var root = new CustomersControl(customers, session)) { await Check("05-customers", root); host.Controls.Remove(root); }
                using (var root = new LoyaltyControl(loyalty, customers, session))
                {
                    await Check("06-promotions", root);
                    var frame = All(root).OfType<OrbitManagementTabs>().Single(); var tabs = (OrbitSectionTabs)Field(frame, "tabs")!;
                    tabs.SelectedIndex = 1; await Settle(root); results.Add(Snapshot("07-ranks", root)); Save();
                    tabs.SelectedIndex = 2; await Settle(root); results.Add(Snapshot("08-points", root)); Save(); host.Controls.Remove(root);
                }
                using (var root = new CatalogsControl(new CatalogService(new CatalogRepository(factory), permissions), session))
                {
                    await Check("09-catalog-products", root); var tabs = (OrbitSectionTabs)Field(root, "tabs")!;
                    tabs.SelectedIndex = 2; await Settle(root); results.Add(Snapshot("09-methods", root)); Save();
                    for (var i = 0; i < 7; i++) { tabs.SelectedIndex = i; await Settle(root); var grid = (OrbitDataGridView)Field(root, "grid")!; results.Add(new { Case = "CatalogTab", Index = i, Rows = grid.Rows.Count, Columns = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).Select(c => c.DataPropertyName).ToArray() }); }
                    host.Controls.Remove(root);
                }
                foreach (var user in new[] { "thungan", "kho" })
                {
                    using var roleSession = new SessionContext((await auth.ReadCredentialAsync(user))!.Identity);
                    using var menu = new ProductsControl(products, roleSession); await Check("role-" + roleSession.Identity.Role + "-menu", menu); host.Controls.Remove(menu);
                }
                foreach (var count in new[] { 0, 1, 49, 50, 51, 100, 101 })
                {
                    using var fixture = new PagedFixture(session, count); await Check("paging-" + count, fixture);
                    var seen = new List<string>(); var grid = (OrbitDataGridView)Field(fixture, "Grid")!; var workspace = All(fixture).OfType<OrbitListWorkspace>().Single();
                    for (var p = 0; p < 5; p++) { seen.AddRange(grid.Rows.Cast<DataGridViewRow>().Select(r => Convert.ToString(r.Cells[0].Value)!)); if (!workspace.Pagination.Next.Enabled) break; workspace.Pagination.Next.PerformClick(); await Settle(fixture); }
                    results.Add(new { Case = "ServerPaging", Count = count, Seen = seen.Count, Pass = seen.Count == count && seen.Distinct().Count() == count && !workspace.Pagination.Next.Enabled }); host.Controls.Remove(fixture); Save();
                }
            }
            catch (Exception ex) { results.Add(new { Case = "Exception", Error = ex.ToString() }); }
            finally { Save(); host.Close(); }
        };
        Application.Run(host);
    }
}
sealed class PagedFixture : OrbitRecordListControl
{
    public PagedFixture(SessionContext session, int count)
    {
        var rows = Enumerable.Range(0, count).Select(i => new Row("P" + i, "Row " + i)).ToList();
        Configure("Menu và công thức", session, (_, skip, _) => Task.FromResult<IList>(rows.Skip(skip).Take(50).ToList()), ("Code", "Mã"), ("Name", "Tên"));
    }
    private sealed record Row(string Code, string Name);
}
