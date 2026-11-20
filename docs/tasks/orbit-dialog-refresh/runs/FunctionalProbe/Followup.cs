using System.Reflection;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Customers;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

static partial class Program
{
    static async Task Followup()
    {
        // Sửa thao tác của harness (mnemonic & và đợi tải), không đổi source hoặc expected nghiệp vụ.
        await Case("D06-retry-keeps-id-and-captured-payload", async () => {
            int captures = 0, writes = 0; var ids = new List<Guid>();
            using var d = Editor(); d.CaptureWrite = () => { captures++; return id => { writes++; ids.Add(id); if (writes == 1) throw new OperationCanceledException(); return Task.FromResult(new OperationResult("retried", null)); }; };
            Click(d, "Lưu"); Assert(F<Button>(d, "check").Enabled, "Unknown not entered");
            AnswerNext("Đồng ý"); Click(d, "Đối chiếu"); await Until(() => d.IsDisposed);
            Assert(captures == 1 && writes == 2 && ids[0] == ids[1], "Retry changed request/payload");
        });
        await Case("D06-readonly-no-write-actions", () => { using var d = Editor(); d.SetReadOnly(); Assert(!F<Button>(d, "save").Visible && !F<Button>(d, "check").Visible && F<Button>(d, "cancel").Text == "Đóng", "Readonly actions exposed"); d.Close(); return Task.CompletedTask; });
        await Case("D05-date-typed-and-invalid-input", () => {
            using var date = new OrbitDateField(); host.Controls.Add(date); date.Value = new DateTime(2026, 10, 22); var input = All(date).OfType<TextBox>().Single();
            Assert(input.Text == "22/10/2026", "Date format changed"); input.Text = "31/02/2026"; bool rejected = false; try { _ = date.Value; } catch (ArgumentException) { rejected = true; }
            Assert(rejected && input.Text == "31/02/2026", "Invalid input not rejected/retained"); host.Controls.Remove(date); return Task.CompletedTask;
        });
        const string db = "CoffeeManager_Test_G03_20261019_215457_29e552a9_demo";
        var guard = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = db, IntegratedSecurity = true, TrustServerCertificate = true };
        using (var c = new SqlConnection(guard.ConnectionString)) { await c.OpenAsync(); using var q = c.CreateCommand(); q.CommandText = "SELECT RunId FROM dbo.G03TestMarker"; Assert(await q.ExecuteScalarAsync() as string == "20261019_215457_29e552a9", "Marker mismatch"); }
        var config = new SqlConnectionStringBuilder(DevelopmentConnection.Read()); config.InitialCatalog = db; var factory = new ConnectionFactory(config.ConnectionString);
        var auth = new AuthRepository(factory); var permissions = new PermissionService(auth); using var session = new SessionContext((await auth.ReadCredentialAsync("admin"))!.Identity);
        var catalog = new CatalogService(new CatalogRepository(factory), permissions); var customers = new CustomerService(new CustomerRepository(factory), permissions);
        await Case("D04-picker-close-after-load", async () => {
            using var d = new CustomerPickerDialog(customers, session); d.Show(host); var control = All(d).OfType<CustomersControl>().Single();
            await Until(() => !F<bool>(control, "busy")); Click(d, "Đóng");
            Assert(d.IsDisposed && d.DialogResult != DialogResult.OK && d.SelectedCustomer == null, "Cancel selected customer");
        });
        await Case("D04-picker-guest", async () => {
            using var d = new CustomerPickerDialog(customers, session, true); d.Show(host); var control = All(d).OfType<CustomersControl>().Single();
            await Until(() => !F<bool>(control, "busy")); Click(d, "Khách vãng lai"); await Until(() => d.IsDisposed);
            Assert(d.DialogResult == DialogResult.OK && d.SelectedCustomer == null, "Guest result wrong");
        });
        await Case("D09-catalog-real-editor-write-readback-cleanup", async () => {
            var name = "Dlg" + DateTime.Now.ToString("MMddHHmmss");
            using var d = new CatalogEditorDialog(catalog, session, CatalogKind.ProductCategory, null); d.Show(host); F<TextBox>(d, "nameInput").Text = name;
            F<Button>(d, "saveButton").PerformClick(); await Until(() => !F<bool>(d, "writing"));
            Assert(d.DialogResult == DialogResult.OK, "Save failed: " + F<Label>(d, "statusLabel").Text);
            var row = (await catalog.ListAsync(session, CatalogKind.ProductCategory, name, 0)).Single(r => r.Name == name);
            Log("FIXTURE-catalog-code", true, row.Code + "; " + name + "; created in guarded DB test");
            await catalog.DeleteAsync(session, CatalogKind.ProductCategory, row); Assert(await catalog.GetAsync(session, CatalogKind.ProductCategory, row.Code) == null, "Fixture not removed");
            d.DiscardChanges(); d.Close();
        });
        await Case("D05-catalog-invalid-retains-input-and-focus", async () => {
            using var d = new CatalogEditorDialog(catalog, session, CatalogKind.DeliveryPartner, null); d.Show(host);
            F<TextBox>(d, "codeInput").Text = "DT987"; F<TextBox>(d, "nameInput").Text = "Nháp đối tác"; var ratio = F<TextBox>(d, "ratioInput"); ratio.Text = "abc";
            F<Button>(d, "saveButton").PerformClick(); await Task.Delay(30);
            Assert(ratio.Text == "abc" && ratio.Focused && F<ErrorProvider>(d, "errors").GetError(ratio).Length > 0 && !d.IsWriting, "Invalid ratio handling regressed"); d.DiscardChanges(); d.Close();
        });
        await Case("D02-PT010-protected-support", async () => {
            var row = await catalog.GetAsync(session, CatalogKind.PaymentMethod, "PT010"); Assert(row != null, "PT010 fixture missing");
            using var d = new CatalogEditorDialog(catalog, session, CatalogKind.PaymentMethod, row); d.Show(host);
            Assert(!F<CheckBox>(d, "supportedInput").Enabled, "PT010 support editable"); d.Close();
        });
        await Case("D04-customer-orders-data-empty-and-page50", async () => {
            var type = typeof(CustomersControl).Assembly.GetType("CafeManager.WinForms.Modules.Customers.CustomerOrderList")!;
            foreach (var code in new[] { "KH001", "KH999" }) {
                using var list = (Control)Activator.CreateInstance(type, customers, session, code)!; host.Controls.Add(list);
                await Until(() => !F<bool>(list, "busy")); var grid = All(list).OfType<DataGridView>().Single();
                var expected = await customers.OrdersAsync(session, code, 0);
                Assert(grid.Rows.Count == expected.Count && grid.Rows.Count <= 50 && grid.Columns.Count == 3, "Orders binding/columns/paging changed");
                Assert(grid.ReadOnly, "History editable"); host.Controls.Remove(list);
            }
        });
    }
}
