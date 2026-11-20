using System.Reflection;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos.Customers;
using CafeManager.WinForms.Configuration;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Modules.Customers;
using CafeManager.WinForms.Modules.Menu;
using CafeManager.WinForms.UI;
using Microsoft.Data.SqlClient;

static partial class Program
{
    static async Task AuditCustomer()
    {
        var config = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = "CoffeeManager_Test_G03_20261019_215457_29e552a9_demo", IntegratedSecurity = true, TrustServerCertificate = true };
        using var c = new SqlConnection(config.ConnectionString); await c.OpenAsync(); using var q = c.CreateCommand(); q.CommandText = "SELECT RunId FROM dbo.G03TestMarker"; Assert(await q.ExecuteScalarAsync() as string == "20261019_215457_29e552a9", "Marker mismatch");
        foreach (var sql in new[] { "SELECT MaKH,TenKH,DiemTichLuy,MaHang,CONVERT(varchar(30),Revision,2) Revision FROM dbo.KHACHHANG WHERE MaKH='KH014'", "SELECT Operation,EntityCode,CONVERT(varchar(30),ResultRevision,2) Revision FROM dbo.G03OperationReceipt WHERE EntityCode='KH014'", "SELECT MaKH,Points,Source FROM dbo.CustomerOpening WHERE MaKH='KH014'" }) {
            q.CommandText = sql; using var reader = await q.ExecuteReaderAsync(); var rows = new List<Dictionary<string, object?>>(); while (await reader.ReadAsync()) { var row = new Dictionary<string, object?>(); for (int i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i); rows.Add(row); } Log("AUDIT", true, System.Text.Json.JsonSerializer.Serialize(rows));
        }
    }
    static async Task Remaining()
    {
        const string db = "CoffeeManager_Test_G03_20261019_215457_29e552a9_demo";
        var guard = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = db, IntegratedSecurity = true, TrustServerCertificate = true };
        using (var c = new SqlConnection(guard.ConnectionString)) { await c.OpenAsync(); using var q = c.CreateCommand(); q.CommandText = "SELECT RunId FROM dbo.G03TestMarker"; Assert(await q.ExecuteScalarAsync() as string == "20261019_215457_29e552a9", "Marker mismatch"); }
        var config = new SqlConnectionStringBuilder(DevelopmentConnection.Read()); config.InitialCatalog = db; var factory = new ConnectionFactory(config.ConnectionString); var auth = new AuthRepository(factory); var permissions = new PermissionService(auth);
        using var session = new SessionContext((await auth.ReadCredentialAsync("admin"))!.Identity);
        var customers = new CustomerService(new CustomerRepository(factory), permissions); var products = new ProductService(new ProductRepository(factory), permissions); var loyalty = new LoyaltyService(new LoyaltyRepository(factory), permissions);
        await Case("R-D09-customer-actual-handlers-edit-open-adjust-cancel", async () => {
            var tag = "DlgRP" + DateTime.Now.ToString("MMddHHmmss");
            using var c = new CustomersControl(customers, session); await Ready(c);
            await InDialog(() => InvokeTask(c, "Edit", true), async d => { Input(d, "Tên khách").Text = tag; await SaveAndWait(d); });
            var row = (await customers.ListAsync(session, tag, 0)).Single(r => r.Name == tag); Log("FIXTURE-customer", true, row.Code + "; " + tag);
            var grid = All(c).OfType<DataGridView>().Single(); void SelectFixture() => grid.CurrentCell = grid.Rows.Cast<DataGridViewRow>().Single(r => ((CustomerRow)r.DataBoundItem!).Code == row.Code).Cells[0]; SelectFixture();
            await InDialog(() => InvokeTask(c, "Edit", false), async d => { Input(d, "Tên khách").Text = tag + " edit"; await SaveAndWait(d); });
            var edited = await customers.GetAsync(session, row.Code); Assert(edited!.Name == tag + " edit" && edited.Points == 0 && edited.Phone == null, "Edit changed points/phone"); SelectFixture();
            await InDialog(() => InvokeTask(c, "Open"), async d => { Input(d, "Điểm mở sổ").Text = "300"; Input(d, "Nguồn / lý do").Text = tag; AnswerNext("Hủy"); Click(d, "Lưu"); Assert(d.ResultCode == null, "Opening committed on cancel"); });
            var afterOpen = await customers.GetAsync(session, row.Code); Assert(afterOpen!.Points == 0 && edited.Revision.SequenceEqual(afterOpen.Revision), "Cancel changed opening/revision");
            using var point = InternalModule("CafeManager.WinForms.Modules.Loyalty.PointList", loyalty, customers, session); await Ready(point); point.GetType().GetField("customer", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(point, afterOpen);
            var history = await loyalty.HistoryAsync(session, row.Code, 0);
            await InDialog(() => InvokeTask(point, "Adjust"), async d => {
                Input(d, "Số điểm dương").Text = "10"; Input(d, "Lý do bắt buộc").Text = tag; Click(d, "Lưu"); Assert(F<Label>(d, "status").Text.Contains("preview"), "Adjustment saved without preview");
                Click(d, "Xem lại số dư từ SQL"); await Until(() => All(d).OfType<Label>().Any(l => l.Text.StartsWith("Sau điều chỉnh:")));
                AnswerNext("Hủy"); Click(d, "Lưu"); Assert(d.ResultCode == null, "Adjustment committed after cancel");
                Input(d, "Số điểm dương").Text = "11"; Click(d, "Lưu"); Assert(F<Label>(d, "status").Text.Contains("preview"), "Old preview accepted after editing");
            });
            var after = await customers.GetAsync(session, row.Code); Assert(after!.Points == 0 && afterOpen.Revision.SequenceEqual(after.Revision) && history.SequenceEqual(await loyalty.HistoryAsync(session, row.Code, 0)), "Point cancel changed balance/history/revision");
            await customers.DeleteAsync(session, after, Guid.NewGuid()); Assert(await customers.GetAsync(session, row.Code) == null, "Customer cleanup failed"); host.Controls.Remove(c); host.Controls.Remove(point);
        });
        await Case("R-D03-store-recipe-readonly-real-role", async () => {
            var identity = (await auth.ReadCredentialAsync("kho"))!.Identity; Assert(identity.Role == "STORE" && identity.Active && identity.Employed, "STORE fixture unavailable"); using var store = new SessionContext(identity);
            using var c = new ProductsControl(products, store); await Ready(c);
            await InDialog(() => InvokeTask(c, "Recipe"), d => { var grid = All(d).OfType<DataGridView>().Single(); Assert(grid.ReadOnly && !grid.AllowUserToAddRows && !grid.AllowUserToDeleteRows && grid.Columns["Đơn vị"]!.ReadOnly, "STORE recipe not read-only");
                Assert(!F<Button>(d, "save").Visible && !All(d).OfType<Button>().Any(b => b.Text == "Xóa dòng đang chọn"), "STORE write action exposed"); return Task.CompletedTask;
            }); host.Controls.Remove(c);
        });
    }
}
