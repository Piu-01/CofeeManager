using System.Reflection;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Tables;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Customers;
using CafeManager.WinForms.Modules.Inventory;
using CafeManager.WinForms.Modules.Loyalty;
using CafeManager.WinForms.Modules.Menu;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

static partial class Program
{
    static Task InvokeTask(object target, string method, params object[] args) => (Task)target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args)!;
    static TextBox Input(Control root, string caption) => All(root).OfType<TextBox>().Single(t => t.AccessibleName == caption);
    static Control InternalModule(string type, params object[] args) => (Control)Activator.CreateInstance(typeof(CustomersControl).Assembly.GetType(type)!, args)!;
    static async Task Ready(Control c)
    {
        host.Controls.Add(c); c.Dock = DockStyle.Fill; c.BringToFront(); await Task.Delay(100);
        if (c is OrbitRecordListControl) await Until(() => !F<bool>(c, "busy"));
        else if (c is MaterialsControl) await Until(() => F<DataGridView>(c, "grid").Enabled);
    }
    static async Task InDialog(Func<Task> open, Func<OperationEditorDialog, Task> action)
    {
        Exception? error = null; bool inspected = false;
        var completed = new TaskCompletionSource();
        foreach (var pending in answers) { pending.Stop(); pending.Dispose(); } answers.Clear();
        using var timer = new System.Windows.Forms.Timer { Interval = 50 };
        timer.Tick += async (_, _) => {
            var d = Application.OpenForms.OfType<OperationEditorDialog>().LastOrDefault(); if (d is null) return;
            timer.Stop(); inspected = true;
            try { await action(d); } catch (Exception e) { error = e; }
            finally { if (!d.IsDisposed && d.Visible && d.DialogResult != DialogResult.OK) await CloseDirty(d); completed.SetResult(); }
        };
        timer.Start(); await open(); Assert(inspected, "Dialog did not open"); await completed.Task;
        foreach (var pending in answers) { pending.Stop(); pending.Dispose(); } answers.Clear();
        if (error != null) throw error;
    }
    static async Task SaveAndWait(OperationEditorDialog d)
    {
        Click(d, "Lưu"); await Until(() => !F<bool>(d, "writing"));
        Assert(d.DialogResult == DialogResult.OK, "Save failed: " + F<Label>(d, "status").Text);
    }
    static async Task PreviouslyBlocked()
    {
        const string db = "CoffeeManager_Test_G03_20261019_215457_29e552a9_demo";
        var guard = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = db, IntegratedSecurity = true, TrustServerCertificate = true };
        using (var c = new SqlConnection(guard.ConnectionString)) { await c.OpenAsync(); using var q = c.CreateCommand(); q.CommandText = "SELECT RunId FROM dbo.G03TestMarker"; Assert(await q.ExecuteScalarAsync() as string == "20261019_215457_29e552a9", "Marker mismatch"); }
        var config = new SqlConnectionStringBuilder(DevelopmentConnection.Read()); config.InitialCatalog = db; var factory = new ConnectionFactory(config.ConnectionString);
        var auth = new AuthRepository(factory); var permissions = new PermissionService(auth); using var session = new SessionContext((await auth.ReadCredentialAsync("admin"))!.Identity);
        var customers = new CustomerService(new CustomerRepository(factory), permissions); var materials = new MaterialService(new MaterialRepository(factory), permissions);
        var products = new ProductService(new ProductRepository(factory), permissions); var tables = new TableService(new TableRepository(factory), permissions); var loyalty = new LoyaltyService(new LoyaltyRepository(factory), permissions);
        var tag = "DlgR" + DateTime.Now.ToString("MMddHHmmss");
        await Case("R-D09-material-actual-handler-create-open-cancel-cleanup", async () => {
            string code = ""; for (int n = 99; n > 0; n--) { var candidate = $"NVL{n:00}"; if (await materials.GetAsync(session, candidate) == null) { code = candidate; break; } } Assert(code != "", "No free material code");
            using var c = new MaterialsControl(materials, session); await Ready(c);
            await InDialog(() => InvokeTask(c, "Edit", true), async d => { Input(d, "Mã NVL01–NVL99").Text = code; Input(d, "Tên nguyên liệu").Text = tag; Input(d, "Đơn vị chuẩn").Text = "Kg"; await SaveAndWait(d); });
            var row = await materials.GetAsync(session, code); Assert(row?.Name == tag && row.Unit == "Kg" && row.Quantity == 0, "Material readback wrong"); Log("FIXTURE-material", true, code + "; " + tag);
            var grid = F<DataGridView>(c, "grid"); grid.CurrentCell = grid.Rows.Cast<DataGridViewRow>().Single(r => ((MaterialRow)r.DataBoundItem!).Code == code).Cells[0];
            await InDialog(() => InvokeTask(c, "OpenBalance"), async d => { Input(d, "Lượng theo đơn vị chuẩn").Text = "1,2345"; Input(d, "Nguồn / lý do").Text = tag; AnswerNext("Hủy"); Click(d, "Lưu"); await Task.Delay(50); Assert(d.ResultCode == null, "Opening committed on cancel"); });
            var after = await materials.GetAsync(session, code); Assert(after!.Quantity == 0 && after.OpeningKind == null && after.Revision.SequenceEqual(row!.Revision), "Cancel altered stock/opening/revision");
            await materials.DeleteAsync(session, after, Guid.NewGuid()); Assert(await materials.GetAsync(session, code) == null, "Material cleanup failed"); host.Controls.Remove(c);
        });
        await Case("R-D09-product-actual-handler-create-cleanup", async () => {
            string code = ""; for (int n = 999; n > 0; n--) { var candidate = $"SP{n:000}"; if ((await products.ListAsync(session, candidate, 0)).All(r => r.Code != candidate)) { code = candidate; break; } } Assert(code != "", "No free product code");
            using var c = new ProductsControl(products, session); await Ready(c);
            await InDialog(() => InvokeTask(c, "Edit", true), async d => { Input(d, "Mã SP001–SP999").Text = code; Input(d, "Tên sản phẩm").Text = tag; Input(d, "Giá bán nguyên VND").Text = "35000"; await SaveAndWait(d); });
            var row = (await products.ListAsync(session, code, 0)).Single(r => r.Code == code); Assert(row.Name == tag && row.Price == 35000 && row.Status == "Ngung ban", "Product payload changed"); Log("FIXTURE-product", true, code + "; " + tag);
            await products.DeleteAsync(session, row, Guid.NewGuid()); Assert((await products.ListAsync(session, code, 0)).All(r => r.Code != code), "Product cleanup failed"); host.Controls.Remove(c);
        });
        await Case("R-D09-area-table-actual-handlers-create-cleanup", async () => {
            string areaCode = "", tableCode = "";
            for (int n = 999; n > 0; n--) { var candidate = $"KV{n:000}"; if ((await tables.AreasAsync(session, candidate, 0)).All(r => r.Code != candidate)) { areaCode = candidate; break; } }
            for (int n = 9999; n > 0; n--) { var candidate = $"B{n:0000}"; if ((await tables.ListAsync(session, candidate, 0)).All(r => r.Code != candidate)) { tableCode = candidate; break; } }
            Assert(areaCode != "" && tableCode != "", "No free area/table code");
            using var area = InternalModule("CafeManager.WinForms.Modules.Tables.AreaList", tables, session); await Ready(area);
            await InDialog(() => InvokeTask(area, "Edit", true), async d => { Input(d, "Mã KV001–KV999").Text = areaCode; Input(d, "Tên khu vực").Text = tag; await SaveAndWait(d); });
            using var table = InternalModule("CafeManager.WinForms.Modules.Tables.TableList", tables, session); await Ready(table);
            await InDialog(() => InvokeTask(table, "Edit", true), async d => { Input(d, "Mã B0001–B9999").Text = tableCode; Input(d, "Tên bàn").Text = tag; All(d).OfType<ComboBox>().Single(t => t.AccessibleName == "Khu vực").SelectedValue = areaCode; await SaveAndWait(d); });
            var row = await tables.SelectAsync(session, tableCode); Assert(row?.Name == tag && row.Area == areaCode && row.Status == "Trong", "Table area/status wrong"); Log("FIXTURE-table-area", true, tableCode + "; " + areaCode + "; " + tag);
            await tables.DeleteAsync(session, false, new TableRequest(row!.Code, row.Name, row.Area), row.Revision, Guid.NewGuid());
            var areaRow = (await tables.AreasAsync(session, areaCode, 0)).Single(r => r.Code == areaCode);
            await tables.DeleteAsync(session, true, new TableRequest(areaRow.Code, areaRow.Name, null), areaRow.Revision, Guid.NewGuid());
            Assert((await tables.ListAsync(session, tableCode, 0)).All(r => r.Code != tableCode) && (await tables.AreasAsync(session, areaCode, 0)).All(r => r.Code != areaCode), "Table/area cleanup failed"); host.Controls.Remove(area); host.Controls.Remove(table);
        });
        await Case("R-D09-promotion-actual-handler-create-cleanup", async () => {
            var code = "KM" + DateTime.Now.ToString("MMddHHmm"); Assert((await loyalty.PromotionsAsync(session, code, 0)).All(r => r.Code != code), "Promotion code occupied");
            using var c = InternalModule("CafeManager.WinForms.Modules.Loyalty.PromotionList", loyalty, customers, session); await Ready(c);
            await InDialog(() => InvokeTask(c, "Edit", true), async d => { Input(d, "Mã KM và8chữ số").Text = code; Input(d, "Tên chương trình").Text = tag; var dates = All(d).OfType<OrbitDateField>().ToArray(); dates[0].Value = new DateTime(2026, 10, 22); dates[1].Value = new DateTime(2026, 10, 23); Input(d, "Tỷ lệ giảm0–100%").Text = "17"; await SaveAndWait(d); });
            var row = (await loyalty.PromotionsAsync(session, code, 0)).Single(r => r.Code == code); Assert(row.Name == tag && row.Discount == 17 && row.StartDate == new DateOnly(2026, 10, 22) && row.EndDate == new DateOnly(2026, 10, 23), "Promotion payload changed"); Log("FIXTURE-promotion", true, code + "; " + tag);
            await loyalty.DeletePromotionAsync(session, row, Guid.NewGuid()); Assert((await loyalty.PromotionsAsync(session, code, 0)).All(r => r.Code != code), "Promotion cleanup failed"); host.Controls.Remove(c);
        });
        await Case("R-D03-policy-preview-invalidate-cancel", async () => {
            var before = await loyalty.PolicyAsync(session); using var c = InternalModule("CafeManager.WinForms.Modules.Loyalty.PolicyList", loyalty, session); await Ready(c);
            await InDialog(() => InvokeTask(c, "Edit"), async d => {
                Click(d, "Lưu"); Assert(F<Label>(d, "status").Text.Contains("preview"), "Policy saved without preview");
                Click(d, "Xem lại từ SQL"); await Until(() => All(d).OfType<Label>().Any(l => l.Text.StartsWith("Hạng khách sẽ")));
                AnswerNext("Hủy"); Click(d, "Lưu"); Assert(d.ResultCode == null, "Policy committed on cancel");
                var grid = All(d).OfType<DataGridView>().Single(); grid.Rows[0].Cells[2].Value = "1"; Click(d, "Lưu");
                Assert(F<Label>(d, "status").Text.Contains("preview"), "Changed policy reused old preview");
            });
            var after = await loyalty.PolicyAsync(session);
            var beforeSnapshot = before.Select(r => (r.Code, r.MinimumPoints, r.Discount, Revision: Convert.ToHexString(r.Revision))).ToArray();
            var afterSnapshot = after.Select(r => (r.Code, r.MinimumPoints, r.Discount, Revision: Convert.ToHexString(r.Revision))).ToArray();
            Log("POLICY-snapshots", beforeSnapshot.SequenceEqual(afterSnapshot), System.Text.Json.JsonSerializer.Serialize(new { Before = before.Select(r => new { r.Code, r.MinimumPoints, r.Discount, Revision = Convert.ToHexString(r.Revision) }), After = after.Select(r => new { r.Code, r.MinimumPoints, r.Discount, Revision = Convert.ToHexString(r.Revision) }) }));
            Assert(beforeSnapshot.SequenceEqual(afterSnapshot), "Policy changed on cancellation"); host.Controls.Remove(c);
        });
        await Case("R-D05-discount-preview-invalidate", async () => {
            using var c = InternalModule("CafeManager.WinForms.Modules.Loyalty.PromotionList", loyalty, customers, session); await Ready(c);
            await InDialog(() => InvokeTask(c, "Preview"), async d => { Input(d, "Tiền món nguyên VND").Text = "100000"; Input(d, "Phí ship nguyên VND").Text = "10000"; All(d).OfType<CheckBox>().Single().Checked = false; Click(d, "Xem preview SQL"); await Until(() => All(d).OfType<Label>().Any(l => l.Text.StartsWith("Nguồn ")));
                Input(d, "Tiền món nguyên VND").Text = "200000"; Assert(All(d).OfType<Label>().Any(l => l.Text == "Cần tải preview mới."), "Old preview not invalidated");
            }); host.Controls.Remove(c);
        });
    }
}
