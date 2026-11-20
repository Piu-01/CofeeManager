using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Customers;
using CafeManager.WinForms.Modules.Menu;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

static partial class Program
{
    static readonly List<object> results = [];
    static string output = "";
    static Form host = null!;
    static readonly List<System.Windows.Forms.Timer> answers = [];
    static string? caseFilter;
    static object? Field(object value, string name)
    {
        for (var type = value.GetType(); type != null; type = type.BaseType)
            if (type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is { } f) return f.GetValue(value);
        throw new Exception("Missing field " + name);
    }
    static T F<T>(object value, string name) => (T)Field(value, name)!;
    static IEnumerable<Control> All(Control root) { yield return root; foreach (Control c in root.Controls) foreach (var n in All(c)) yield return n; }
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static async Task Until(Func<bool> predicate) { for (int i = 0; i < 160; i++) { if (predicate()) return; await Task.Delay(50); } throw new TimeoutException("Expected state not reached"); }
    static void Log(string id, bool pass, string actual) { results.Add(new { id, status = pass ? "PASS" : "FAIL", actual }); File.WriteAllText(output, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true })); Console.WriteLine($"{id}: {(pass ? "PASS" : "FAIL")} {actual}"); }
    static async Task Case(string id, Func<Task> action) { if (caseFilter != null && !id.StartsWith(caseFilter)) return; try { await action(); Log(id, true, "Expected assertions satisfied"); } catch (Exception ex) { Log(id, false, ex.ToString()); } finally { foreach (var timer in answers) { timer.Stop(); timer.Dispose(); } answers.Clear(); } }
    static void Click(Control root, string text) => All(root).OfType<Button>().Single(b => b.Text.Replace("&", "") == text).PerformClick();
    // Chỉ điều khiển các form do probe sở hữu; không đăng nhập, không tự động hóa app bên ngoài.
    static void AnswerNext(string text)
    {
        var timer = new System.Windows.Forms.Timer { Interval = 30 };
        answers.Add(timer);
        int ticks = 0;
        timer.Tick += (_, _) => { var form = Application.OpenForms.OfType<OrbitConfirmationDialog>().LastOrDefault(); if (form != null) { timer.Stop(); timer.Dispose(); Click(form, text); } else if (++ticks > 200) { timer.Stop(); timer.Dispose(); } };
        timer.Start();
    }
    static async Task CloseDirty(OperationEditorDialog d) { if (d.IsDisposed) return; AnswerNext("Đồng ý"); d.Close(); await Task.Delay(30); }
    static OperationEditorDialog Editor() { var d = new OperationEditorDialog("Functional fixture", _ => Task.FromResult<OperationReceipt?>(null)); d.Show(host); return d; }
    [STAThread] static void Main(string[] args)
    {
        output = Path.GetFullPath(args[0]); ApplicationConfiguration.Initialize();
        using var form = new Form { Text = "Orbit dialog functional fixture", ShowInTaskbar = false, ClientSize = new Size(1000, 700) }; host = form;
        form.Shown += async (_, _) => { try { if (args.Contains("--audit")) await AuditCustomer(); else if (args.Contains("--policy-only")) { caseFilter = "R-D03-policy"; await PreviouslyBlocked(); } else if (args.Contains("--remaining")) await Remaining(); else if (args.Contains("--regression")) { await Run(); await Followup(); await PreviouslyBlocked(); } else if (args.Contains("--followup")) await Followup(); else await Run(); } catch (Exception e) { Log("HARNESS", false, e.ToString()); } finally { form.Close(); } };
        Application.Run(form);
    }
    static async Task Run()
    {
        await Case("D05-validation-retains-input", async () => {
            using var d = Editor(); var input = d.AddText("Tên khách", "Nháp tiếng Việt"); int writes = 0;
            d.CaptureWrite = () => throw new ArgumentException("Tên không hợp lệ", "Tên khách");
            Click(d, "Lưu"); await Task.Delay(50);
            Assert(input.Text == "Nháp tiếng Việt" && !d.IsDisposed && writes == 0, "Draft lost or dialog closed");
            Assert(F<ErrorProvider>(d, "errors").GetError(input).Contains("không hợp lệ"), "Error not attached to wrapped input");
            Assert(input.Focused, "Validation focus not on input"); await CloseDirty(d);
        });
        await Case("D06-reject-confirmation-no-write", async () => {
            using var d = Editor(); int writes = 0; d.Confirmation = "Ghi fixture?";
            d.CaptureWrite = () => _ => { writes++; return Task.FromResult(new OperationResult("fixture", null)); };
            AnswerNext("Hủy"); Click(d, "Lưu"); await Task.Delay(50);
            Assert(writes == 0 && !d.IsDisposed && d.ResultCode == null, "Cancel wrote data"); d.Close();
        });
        await Case("D06-busy-blocks-double-save-close", async () => {
            using var d = Editor(); var pending = new TaskCompletionSource<OperationResult>(); int writes = 0;
            d.CaptureWrite = () => _ => { writes++; return pending.Task; };
            Click(d, "Lưu"); Click(d, "Lưu"); d.Close();
            Assert(writes == 1 && !d.IsDisposed && !F<Button>(d, "save").Enabled && !F<Control>(d, "fields").Enabled, "Busy state did not block");
            pending.SetResult(new OperationResult("done", null, 1)); await Until(() => d.IsDisposed);
            Assert(d.ResultCode == "done" && d.AffectedCount == 1, "Result lost");
        });
        await Case("D06-unknown-reconcile-original-id", async () => {
            Guid written = default, reconciled = default;
            using var d = new OperationEditorDialog("Unknown fixture", id => { reconciled = id; return Task.FromResult<OperationReceipt?>(new("found", null, "fixture", DateTime.Now)); });
            d.CaptureWrite = () => id => { written = id; throw new OperationCanceledException(); }; d.Show(host); Click(d, "Lưu"); await Task.Delay(30); d.Close();
            Assert(!d.IsDisposed && !F<Button>(d, "save").Enabled && F<Button>(d, "check").Enabled && !F<Button>(d, "cancel").Enabled, "Unknown state not locked");
            Click(d, "Đối chiếu"); await Until(() => d.IsDisposed); Assert(written != Guid.Empty && written == reconciled && d.ResultCode == "found", "Reconciliation lost request identity");
        });
        await Case("D06-dirty-close-cancel-then-discard", async () => {
            using var d = Editor(); var input = d.AddText("Tên"); input.Text = "nháp";
            AnswerNext("Hủy"); d.Close(); Assert(!d.IsDisposed && input.Text == "nháp", "Cancel discarded draft");
            await CloseDirty(d); Assert(d.IsDisposed, "Discard did not close");
        });
        foreach (var buttons in new[] { MessageBoxButtons.YesNo, MessageBoxButtons.OKCancel })
        foreach (var action in new[] { "positive", "negative", "escape", "close", "enter" })
            await Case($"D06-confirm-{buttons}-{action}", () => {
                var constructor = typeof(OrbitConfirmationDialog).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
                using var d = (OrbitConfirmationDialog)constructor.Invoke(["Fixture only", "Confirm", buttons]);
                var negative = buttons == MessageBoxButtons.YesNo ? DialogResult.No : DialogResult.Cancel;
                using var timer = new System.Windows.Forms.Timer { Interval = 60 };
                timer.Tick += (_, _) => { timer.Stop();
                    if (action == "positive") Click(d, buttons == MessageBoxButtons.YesNo ? "Có" : "Đồng ý");
                    else if (action == "negative") Click(d, buttons == MessageBoxButtons.YesNo ? "Không" : "Hủy");
                    else if (action == "close") d.Close();
                    else { var key = action == "escape" ? Keys.Escape : Keys.Enter; typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(d, [key]); if (d.Visible) d.Close(); }
                };
                timer.Start(); var result = d.ShowDialog(host);
                var expected = action == "positive" ? (buttons == MessageBoxButtons.YesNo ? DialogResult.Yes : DialogResult.OK) : negative;
                Assert(result == expected, $"Expected {expected}, actual {result}"); return Task.CompletedTask;
            });
        const string db = "CoffeeManager_Test_G03_20261019_215457_29e552a9_demo";
        var guard = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = db, IntegratedSecurity = true, TrustServerCertificate = true };
        using (var connection = new SqlConnection(guard.ConnectionString)) { await connection.OpenAsync(); using var command = connection.CreateCommand(); command.CommandText = "SELECT RunId FROM dbo.G03TestMarker"; Assert(await command.ExecuteScalarAsync() as string == "20261019_215457_29e552a9", "Test marker mismatch"); }
        Log("DB-marker", true, db + " marker verified; no production target");
        var config = new SqlConnectionStringBuilder(DevelopmentConnection.Read()); config.InitialCatalog = db;
        var factory = new ConnectionFactory(config.ConnectionString); var auth = new AuthRepository(factory); var permissions = new PermissionService(auth);
        using var session = new SessionContext((await auth.ReadCredentialAsync("admin"))!.Identity);
        var customers = new CustomerService(new CustomerRepository(factory), permissions);
        var catalog = new CatalogService(new CatalogRepository(factory), permissions);
        var materials = new MaterialService(new MaterialRepository(factory), permissions);
        var products = new ProductService(new ProductRepository(factory), permissions);
        var tables = new TableService(new TableRepository(factory), permissions);
        var loyalty = new LoyaltyService(new LoyaltyRepository(factory), permissions);
        foreach (var kind in Enum.GetValues<CatalogKind>())
            await Case("D02-catalog-" + kind, async () => {
                var rows = await catalog.ListAsync(session, kind, "", 0); var row = rows.First();
                using var d = new CatalogEditorDialog(catalog, session, kind, row); d.Show(host);
                Assert(F<TextBox>(d, "codeInput").ReadOnly && F<TextBox>(d, "nameInput").Text == row.Name, "Binding/read-only changed");
                Assert(F<Control>(d, "phoneInput").Visible == (kind == CatalogKind.Supplier), "Supplier visibility changed");
                Assert(F<Control>(d, "startInput").Visible == (kind == CatalogKind.Shift), "Shift visibility changed");
                if (row is SupplierRow supplier) Assert(F<TextBox>(d, "phoneInput").Text == supplier.Phone, "Phone lost");
                if (row is PaymentMethodRow payment) Assert(F<CheckBox>(d, "supportedInput").Checked == payment.Supported, "Support lost");
                if (row is ShiftRow shift) Assert(F<OrbitTimeInput>(d, "startInput").Value.TimeOfDay == shift.StartTime, "Time lost");
                Assert(!d.HasUnsavedChanges, "Populate marked dirty"); d.Close();
            });
        await Case("D04-picker-search-select", async () => {
            using var d = new CustomerPickerDialog(customers, session); d.Show(host);
            var control = All(d).OfType<CustomersControl>().Single(); await Until(() => !F<bool>(control, "busy"));
            var actions = All(d).OfType<Button>().Where(b => b.Visible).Select(b => b.Text).ToArray();
            Assert(!actions.Any(t => t is "Sửa" or "Xóa" or "Mở sổ điểm" or "Hóa đơn" or "Khách vãng lai" || t.StartsWith("Thêm")), "Management leaked into picker");
            var search = All(control).OfType<TextBox>().Single(); search.Text = "0911111111";
            await (Task)typeof(OrbitRecordListControl).GetMethod("ResetAndReload", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, null)!;
            var grid = All(control).OfType<DataGridView>().Single(); Assert(grid.Rows.Count == 1 && grid.Rows[0].Cells["Code"].Value as string == "KH001", "Search returned wrong rows");
            Click(d, "Chọn khách"); await Until(() => d.IsDisposed); Assert(d.SelectedCustomer?.Code == "KH001", "Wrong selected customer");
        });
        await Case("D04-picker-guest-and-cancel", async () => {
            using var d = new CustomerPickerDialog(customers, session, true); d.Show(host);
            var control = All(d).OfType<CustomersControl>().Single(); await Until(() => !F<bool>(control, "busy"));
            Click(d, "Khách vãng lai"); await Until(() => d.IsDisposed); Assert(d.SelectedCustomer == null && d.DialogResult == DialogResult.OK, "Guest mapping wrong");
            using var canceled = new CustomerPickerDialog(customers, session); canceled.Show(host); await Until(() => !F<bool>(All(canceled).OfType<CustomersControl>().Single(), "busy")); Click(canceled, "Đóng"); Assert(canceled.IsDisposed && canceled.DialogResult != DialogResult.OK, "Close selected a customer");
        });
        await Case("D03-recipe-unit-delete-cancel", async () => {
            using var c = new ProductsControl(products, session) { Dock = DockStyle.Fill }; host.Controls.Add(c); await Until(() => !F<bool>(c, "busy"));
            var master = All(c).OfType<DataGridView>().Single(); master.CurrentCell = master.Rows[0].Cells[0]; var row = (CafeManager.Models.Dtos.Products.ProductRow)master.CurrentRow!.DataBoundItem!;
            var before = await products.RecipeAsync(session, row.Code); Exception? failure = null;
            using var timer = new System.Windows.Forms.Timer { Interval = 60 };
            timer.Tick += (_, _) => { var d = Application.OpenForms.OfType<OperationEditorDialog>().LastOrDefault(); if (d == null) return; timer.Stop();
                try { var grid = All(d).OfType<DataGridView>().Single(); Assert(!grid.ReadOnly && grid.Columns["Đơn vị"]!.ReadOnly, "Recipe edit permissions changed");
                    var source = (System.Data.DataTable)grid.DataSource!; var material = ((System.Collections.Generic.List<CafeManager.Models.Dtos.Inventory.MaterialRow>)((DataGridViewComboBoxColumn)grid.Columns[0]).DataSource!).First();
                    source.Rows.Add("", "0,0123", ""); grid.CurrentCell = grid.Rows[grid.Rows.Count - 2].Cells[0]; grid.CurrentRow!.Cells[0].Value = material.Code;
                    Assert(Convert.ToString(grid.CurrentRow.Cells[2].Value) == material.Unit, "Unit not updated after ingredient selection");
                    var count = grid.Rows.Count; Click(d, "Xóa dòng đang chọn"); Assert(grid.Rows.Count == count - 1, "Delete did not remove draft row");
                } catch (Exception ex) { failure = ex; } finally { AnswerNext("Đồng ý"); d.Close(); }
            }; timer.Start();
            await (Task)typeof(ProductsControl).GetMethod("Recipe", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(c, null)!;
            if (failure != null) throw failure;
            var after = await products.RecipeAsync(session, row.Code); Assert(before.SequenceEqual(after), "Cancel changed recipe in DB"); host.Controls.Remove(c);
        });
        var tag = "Dlg" + DateTime.Now.ToString("MMddHHmmss");
        await Case("D09-customer-editor-write-readback-cleanup", async () => {
            using var d = Editor(); var name = d.AddText("Tên khách", tag); var phone = d.AddText("Số điện thoại");
            d.CaptureWrite = () => { var request = CustomerService.Normalize(new(name.Text, phone.Text)); return id => customers.SaveAsync(session, request, null, id); };
            Click(d, "Lưu"); await Until(() => d.IsDisposed || F<Label>(d, "status").Text.Length > 0);
            Assert(d.ResultCode != null, "Customer save failed: " + F<Label>(d, "status").Text);
            var row = await customers.GetAsync(session, d.ResultCode!); Assert(row?.Name == tag && row.Phone == null && row.Points == 0, "Customer payload/readback wrong");
            await customers.DeleteAsync(session, row!, Guid.NewGuid()); Assert(await customers.GetAsync(session, row!.Code) == null, "Fixture cleanup failed");
        });
        await Case("D09-catalog-editor-write-readback-cleanup", async () => {
            using var d = new CatalogEditorDialog(catalog, session, CatalogKind.ProductCategory, null); d.Show(host); F<TextBox>(d, "nameInput").Text = tag;
            Click(d, "Lưu"); await Until(() => d.DialogResult == DialogResult.OK || F<Label>(d, "statusLabel").Text.StartsWith("Không"));
            Assert(d.DialogResult == DialogResult.OK, "Catalog save failed");
            var rows = await catalog.ListAsync(session, CatalogKind.ProductCategory, tag, 0); var row = rows.Single(r => r.Name == tag);
            await catalog.DeleteAsync(session, CatalogKind.ProductCategory, row); Assert(await catalog.GetAsync(session, CatalogKind.ProductCategory, row.Code) == null, "Catalog cleanup failed"); d.DiscardChanges(); d.Close();
        });
    }
}
