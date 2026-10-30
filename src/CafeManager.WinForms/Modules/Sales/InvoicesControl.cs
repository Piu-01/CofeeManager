using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

public sealed class InvoicesControl : OrbitRecordListControl
{
    private readonly InvoiceService service;
    private readonly SessionContext session;
    private readonly OrbitDateField from = new() { Value = DateTime.Today, Width = 180 }, to = new() { Value = DateTime.Today, Width = 180 };
    private readonly ComboBox basis = SalesUi.Combo(), status = SalesUi.Combo(), channel = SalesUi.Combo();
    public InvoicesControl(InvoiceService service, SessionContext session)
    {
        this.service = service; this.session = session;
        basis.DataSource = new[] { new SalesLookup("Paid", "Ngày thanh toán"), new SalesLookup("Created", "Ngày lập") };
        status.DataSource = new[] { new SalesLookup("", "Mọi trạng thái"), new("Chua thanh toan", "Chưa thanh toán"), new("Da thanh toan", "Đã thanh toán"), new("Huy", "Đã hủy") };
        channel.DataSource = new[] { new SalesLookup("", "Mọi kênh"), new("Tai quan", "Tại quán"), new("Mang di", "Mang đi"), new("Giao hang", "Giao hàng") };
        Configure("Lịch sử hóa đơn", session, async (q, skip, token) => await service.ListAsync(session,
            new(basis.SelectedValue as string ?? "Paid", DateOnly.FromDateTime(from.Value), DateOnly.FromDateTime(to.Value),
                SalesService.Optional(status.SelectedValue as string, 30), SalesService.Optional(channel.SelectedValue as string, 20), Search: q, Skip: skip), token),
            ("Code", "Hóa đơn"), ("Created", "Ngày lập"), ("Channel", "Kênh"), ("Customer", "Khách"), ("Payable", "Phải trả"), ("Status", "Trạng thái"));
        Workspace.Eyebrow.Text = "INVOICE WORKSPACE";
        Workspace.Description.Text = "Tra cứu hóa đơn theo thời gian, trạng thái và kênh bán.";
        Grid.Profile = OrbitGridProfile.InvoiceHistory;
        TotalCountSelector = rows => rows.Count == 0 ? 0 : ((SalesOrderRow)rows[0]!).TotalCount;
        var dateRange = new OrbitDateRangeField(from, to);
        AddFilter(new Label { Text = "Lọc theo", TextAlign = ContentAlignment.MiddleLeft }, 44);
        basis.AccessibleName = "Loại ngày"; status.AccessibleName = "Trạng thái hóa đơn"; channel.AccessibleName = "Kênh bán";
        AddFilter(basis, 125, plain: true); AddFilter(dateRange, 300); AddFilter(status, 140, plain: true); AddFilter(channel, 120, plain: true);
        UseStackedSearch("hóa đơn", "Thử thay đổi khoảng ngày hoặc điều kiện tìm kiếm.");
        Search.AccessibleName = "Tìm hóa đơn";
        Search.PlaceholderText = "Mã hóa đơn / tên / số điện thoại khách";
        var detail = AddAction("Chi tiết / In", async () => { if (Selected is SalesOrderRow row) await ShowDetail(row.Code); }, needsRow: true);
        Theme.OrbitManagementTheme.Button(detail, Theme.ManagementButtonStyle.Action, "printer");
        Grid.CellFormatting += (_, e) => { if (e.ColumnIndex >= 0 && Grid.Columns[e.ColumnIndex].DataPropertyName == "Channel" && e.Value is string value) { e.Value = Models.States.SalesStates.Label(value); e.FormattingApplied = true; } };
        foreach (var field in new[] { basis, status, channel }) field.SelectedValueChanged += async (_, _) => { if (IsHandleCreated) await ResetAndReload(); };
        from.ValueChanged += async (_, _) => { if (IsHandleCreated) await ResetAndReload(); };
        to.ValueChanged += async (_, _) => { if (IsHandleCreated) await ResetAndReload(); };
    }
    public async Task ShowDetail(string code)
    {
        var detail = await service.DetailAsync(session, code);
        if (IsDisposed) return;
        if (detail is null) { State.Text = "Hóa đơn không tồn tại. Tải lại."; return; }
        using var dialog = new InvoiceDetailDialog(detail, service, session);
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) == DialogResult.OK && dialog.ReturnToPos && FindForm() is Forms.MainForm main) await main.OpenSalesOrderAsync(code);
    }
}

