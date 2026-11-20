using System.Reflection;
using System.Text.Json;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.DataAccess;
using CafeManager.DataAccess.Repositories;
using CafeManager.Models.Dtos;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;
using Microsoft.Data.SqlClient;
using CafeManager.WinForms.Modules.Sales;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--dal") return DalSmoke(args[1], args[2]).GetAwaiter().GetResult();
        if (args.Length == 3 && args[0] == "--workspace") return WorkspaceSmoke(args[1], args[2]);
        // Fixture hiển thị kỹ thuật, không gửi lệnh checkout và không chụp hình.
        ApplicationConfiguration.Initialize();
        var factory = new ConnectionFactory("Server=localhost;Database=CoffeeManager_UnusedProbe;Integrated Security=True;Encrypt=True;TrustServerCertificate=True");
        var identity = new AccountIdentity(1, "NV001", "Fixture kỹ thuật", "probe", "ADMIN", true, true, new byte[8]);
        using var session = new SessionContext(identity);
        var service = new SalesService(new SalesRepository(factory), new PermissionService(new AuthRepository(factory)));
        var header = new SalesOrderHeader("HD99999999", DateTime.Now, "NV001", null, null, null, null, "Mang di", null, null, null, null, null, 0, 0, 81000, 0, 0, 81000, "Chua thanh toan", new byte[8], true, "None", 0, DateTime.Now, null, null);
        var order = new SalesOrderDto(header, [new(1, "SP001", "Dòng fixture kỹ thuật", 1, 81000, null, 81000)], []);
        var preview = new CheckoutPreview(header.Code, header.Revision, 81000, 0, 0, 81000, "None", 0, 0, 0, 81, new byte[32]);
        var type = typeof(SalesControl).Assembly.GetType("CafeManager.WinForms.Modules.Sales.CheckoutDialog")!;
        using var dialog = (Form)Activator.CreateInstance(type, service, session, order, preview, new List<SalesLookup> { new("PT001", "Tiền mặt"), new("PT002", "Chuyển khoản") })!;
        var result = new Dictionary<string, object?> { ["Kind"] = "Native technical binding/layout only; not acceptance; no visual inspection" };
        using var timer = new System.Windows.Forms.Timer { Interval = 500 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var grid = (DataGridView)type.GetField("grid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialog)!;
            result["DeviceDpi"] = dialog.DeviceDpi; result["ClientSize"] = new { dialog.ClientSize.Width, dialog.ClientSize.Height };
            result["Rows"] = grid.Rows.Count; result["Columns"] = grid.Columns.Count; result["HandleCreated"] = dialog.IsHandleCreated;
            var confirm = (Button)type.GetField("confirm", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialog)!;
            var inside = dialog.RectangleToScreen(dialog.ClientRectangle).Contains(confirm.RectangleToScreen(confirm.ClientRectangle));
            result["ConfirmWithinClient"] = inside;
            result["TechnicalSuccess"] = grid.Rows.Count == 1 && grid.Columns.Count == 3 && dialog.IsHandleCreated && inside;
            // In hóa đơn đang chuyển sang FastReport/PDF (thay đổi ngoài T33–T35);
            // probe không kiểm in để không phụ thuộc bộ in đang thay đổi.
            try
            {
                var longOrder = order with { Header = header with { Status = "Da thanh toan", PaidAt = DateTime.Now }, Lines = Enumerable.Range(1, 100).Select(i => new SalesLine(i, "SP001", "Fixture phân trang " + i, 1, 81000, new string('a', 100), 81000)).ToArray() };
                result["LongOrderLines"] = longOrder.Lines.Count;
                result["PrintPreviewTechnical"] = "NOT RUN: bộ in đang đổi sang FastReport/PDF";
            }
            catch (Exception ex) when (ex is System.Drawing.Printing.InvalidPrinterException or System.ComponentModel.Win32Exception or InvalidOperationException)
            { result["PrintPreviewTechnical"] = "BLOCKED: " + ex.Message; }
            session.Revoke(); dialog.Close();
        };
        dialog.Shown += (_, _) => timer.Start();
        Application.Run(dialog);
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        if (args.Length == 1) File.WriteAllText(args[0], json); else Console.WriteLine(json);
        return result.TryGetValue("TechnicalSuccess", out var passed) && passed is true ? 0 : 1;
    }
    private static async Task<int> DalSmoke(string database, string output)
    {
        var evidence = new Dictionary<string, object?> { ["Kind"] = "Typed DAL/BLL dependency smoke; operator SQL principal; not acceptance/GUI role evidence", ["Database"] = database };
        try
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(database, @"^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_demo$")) throw new InvalidOperationException("Owned demo test target required.");
            var builder = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = database, IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = true };
            AccountIdentity actor;
            await using (var connection = new SqlConnection(builder.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "IF OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52107,'Marker required',1; SELECT t.MaTK,t.MaNV,n.TenNV,t.TenDangNhap,t.MaVT,t.HoatDong,n.DangLamViec,t.SecurityStamp FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV WHERE t.MaNV='NV001'";
                await using var reader = await command.ExecuteReaderAsync(); if (!await reader.ReadAsync()) throw new InvalidOperationException("Smoke actor missing.");
                actor = new(reader.GetInt32(0), reader.GetString(1).TrimEnd(), reader.GetString(2), reader.GetString(3), reader.GetString(4).TrimEnd(), reader.GetBoolean(5), reader.GetBoolean(6), (byte[])reader[7]);
            }
            var factory = new ConnectionFactory(builder.ConnectionString); var permissions = new PermissionService(new AuthRepository(factory));
            using var session = new SessionContext(actor);
            var sales = new SalesService(new SalesRepository(factory), permissions);
            var invoice = new InvoiceService(new InvoiceRepository(factory), permissions);
            var delivery = new DeliveryService(new DeliveryRepository(factory), permissions);
            var code = await sales.AllocateAsync(session);
            await sales.CreateAsync(session, new(code, Guid.NewGuid(), new("Mang di"), "SP001", 1, "Typed smoke"));
            var order = await sales.GetAsync(session, code) ?? throw new InvalidOperationException("Order mapping failed.");
            var preview = await sales.PreviewAsync(session, order.Header);
            await sales.CheckoutAsync(session, new(code, preview.Revision, Guid.NewGuid(), preview.Token, [new("PT001", preview.Payable)]));
            var detail = await invoice.DetailAsync(session, code) ?? throw new InvalidOperationException("Detail mapping failed.");
            if (detail.Order.Header.Status != "Da thanh toan" || detail.Order.Payments.Count != 1 || detail.SnapshotKind != "Checkout") throw new InvalidOperationException("Checkout/detail smoke mismatch.");
            var rows = await invoice.ListAsync(session, new("Created", DateOnly.FromDateTime(order.Header.Created), DateOnly.FromDateTime(order.Header.Created), Search: code));
            evidence["InvoiceRows"] = rows.Count; evidence["PaidCode"] = code; evidence["Payable"] = preview.Payable;
            var deliveryCode = await sales.AllocateAsync(session);
            await sales.CreateAsync(session, new(deliveryCode, Guid.NewGuid(), new("Giao hang", Recipient: "Fixture typed", Phone: "0901234567", Address: "Địa chỉ kỹ thuật"), "SP001", 1, null));
            var current = (await sales.GetAsync(session, deliveryCode))!.Header;
            await delivery.ChangeAsync(session, new(deliveryCode, current.Revision, Guid.NewGuid(), "Cho giao", "Dang giao"));
            var shipped = await delivery.GetAsync(session, deliveryCode);
            if (shipped?.Order.Header.Delivery != "Dang giao") throw new InvalidOperationException("Delivery mapping failed.");
            evidence["DeliveryCode"] = deliveryCode; evidence["Success"] = true;
            File.WriteAllText(output, JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true })); return 0;
        }
        catch (Exception ex)
        {
            evidence["Success"] = false; evidence["Error"] = ex.ToString();
            File.WriteAllText(output, JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true })); return 1;
        }
    }
    private static int WorkspaceSmoke(string database, string output)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(database, @"^CoffeeManager_Test_G04_\d{8}_\d{6}_[a-f0-9]{8}_demo$")) throw new InvalidOperationException("Owned demo test target required.");
        ApplicationConfiguration.Initialize();
        var builder = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = database, IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = true };
        using var connection = new SqlConnection(builder.ConnectionString); connection.Open();
        using var command = connection.CreateCommand(); command.CommandText = "IF OBJECT_ID('dbo.G04TestMarker') IS NULL THROW 52107,'Marker required',1; SELECT t.MaTK,t.MaNV,n.TenNV,t.TenDangNhap,t.MaVT,t.HoatDong,n.DangLamViec,t.SecurityStamp FROM dbo.TAIKHOAN t JOIN dbo.NHANVIEN n ON n.MaNV=t.MaNV WHERE t.MaNV='NV001'";
        AccountIdentity identity;
        using (var reader = command.ExecuteReader())
        { reader.Read(); identity = new(reader.GetInt32(0), reader.GetString(1).TrimEnd(), reader.GetString(2), reader.GetString(3), reader.GetString(4).TrimEnd(), reader.GetBoolean(5), reader.GetBoolean(6), (byte[])reader[7]); }
        using var session = new SessionContext(identity);
        var factory = new ConnectionFactory(builder.ConnectionString); var permissions = new PermissionService(new AuthRepository(factory));
        var salesService = new SalesService(new SalesRepository(factory), permissions);
        var invoices = new InvoiceService(new InvoiceRepository(factory), permissions);
        using var host = new Form { Text = "G04 native dependency smoke · no visual inspection", AutoScaleMode = AutoScaleMode.Dpi, AutoScaleDimensions = new SizeF(96, 96), Size = new Size(1100, 700) };
        var tabs = new TabControl { Dock = DockStyle.Fill }; host.Controls.Add(tabs);
        var sales = new SalesControl(salesService, new TableService(new TableRepository(factory), permissions), new CustomerService(new CustomerRepository(factory), permissions), new LoyaltyService(new LoyaltyRepository(factory), permissions), session, invoices);
        var invoiceControl = new InvoicesControl(invoices, session);
        var deliveryControl = new DeliveryControl(new DeliveryService(new DeliveryRepository(factory), permissions), salesService, invoices, session);
        foreach (var (title, control) in new (string, Control)[] { ("POS", sales), ("Invoices", invoiceControl), ("Delivery", deliveryControl) }) { var page = new TabPage(title); control.Dock = DockStyle.Fill; page.Controls.Add(control); tabs.TabPages.Add(page); }
        var result = new Dictionary<string, object?> { ["Kind"] = "Native read/binding smoke with owned SQL fixture; operator principal; no login/visual acceptance", ["Database"] = database };
        var step = 0;
        using var timer = new System.Windows.Forms.Timer { Interval = 2000 };
        timer.Tick += (_, _) =>
        {
            if (step == 0)
            {
                var products = (Control)typeof(SalesControl).GetField("products", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sales)!;
                result["MenuCards"] = products.Controls.Count; result["DeviceDpi"] = host.DeviceDpi;
                tabs.SelectedIndex = 1;
            }
            else if (step == 1)
            {
                var grid = (DataGridView)typeof(CafeManager.WinForms.UI.OrbitRecordListControl).GetField("Grid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(invoiceControl)!;
                result["InvoiceRows"] = grid.Rows.Count; tabs.SelectedIndex = 2;
            }
            else
            {
                timer.Stop();
                var grid = (DataGridView)typeof(CafeManager.WinForms.UI.OrbitRecordListControl).GetField("Grid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(deliveryControl)!;
                result["DeliveryRows"] = grid.Rows.Count; result["Success"] = Convert.ToInt32(result["MenuCards"]) > 0 && Convert.ToInt32(result["InvoiceRows"]) > 0 && grid.Rows.Count > 0;
                session.Revoke(); host.Close();
            }
            step++;
        };
        host.Shown += (_, _) => timer.Start(); Application.Run(host);
        File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        return result.TryGetValue("Success", out var success) && success is true ? 0 : 1;
    }
}
