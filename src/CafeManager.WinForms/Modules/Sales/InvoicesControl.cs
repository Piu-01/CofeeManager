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
    private bool openingDetail;
    public InvoicesControl(InvoiceService service, SessionContext session)
    {
        this.service = service; this.session = session;
        basis.DataSource = new[] { new SalesLookup("Paid", L.T("Ngày thanh toán")), new SalesLookup("Created", L.T("Ngày lập")) };
        status.DataSource = new[] { new SalesLookup("", L.T("Mọi trạng thái")), new("Chua thanh toan", L.T("Chưa thanh toán")), new("Cho thanh toan", L.T("Chờ thanh toán")), new("Da thanh toan", L.T("Đã thanh toán")), new("Huy", L.T("Đã hủy")) };
        channel.DataSource = new[] { new SalesLookup("", L.T("Mọi kênh")), new("Tai quan", L.T("Tại quán")), new("Mang di", L.T("Mang đi")), new("Giao hang", L.T("Giao hàng")) };
        Configure("Lịch sử hóa đơn", session, async (q, skip, token) => await service.ListAsync(session,
            new(basis.SelectedValue as string ?? "Paid", DateOnly.FromDateTime(from.Value), DateOnly.FromDateTime(to.Value),
                SalesService.Optional(status.SelectedValue as string, 30), SalesService.Optional(channel.SelectedValue as string, 20), Search: q, Skip: skip), token),
            ("Code", L.T("Hóa đơn")), ("Created", L.T("Ngày lập")), ("Channel", L.T("Kênh")), ("Customer", L.T("Khách")), ("Payable", L.T("Phải trả")), ("Status", L.T("Trạng thái")));
        Workspace.Eyebrow.Text = "INVOICE WORKSPACE";
        Workspace.Description.Text = L.T("Tra cứu hóa đơn theo thời gian, trạng thái và kênh bán.")
            + " " + L.T("Bấm vào hóa đơn để xem chi tiết và in lại.");
        Grid.Profile = OrbitGridProfile.InvoiceHistory;
        TotalCountSelector = rows => rows.Count == 0 ? 0 : ((SalesOrderRow)rows[0]!).TotalCount;
        var dateRange = new OrbitDateRangeField(from, to);
        AddFilter(new Label { Text = L.T("Lọc theo"), TextAlign = ContentAlignment.MiddleLeft }, 44);
        basis.AccessibleName = L.T("Loại ngày"); status.AccessibleName = L.T("Trạng thái hóa đơn"); channel.AccessibleName = L.T("Kênh bán");
        AddFilter(basis, 125, plain: true); AddFilter(dateRange, 300); AddFilter(status, 140, plain: true); AddFilter(channel, 120, plain: true);
        UseStackedSearch(L.T("hóa đơn"), L.T("Thử thay đổi khoảng ngày hoặc điều kiện tìm kiếm."));
        Search.AccessibleName = L.T("Tìm hóa đơn");
        Search.PlaceholderText = L.T("Mã hóa đơn / tên / số điện thoại khách");
        // Mở chi tiết (và in lại) bằng một lần bấm vào dòng; Enter khi đang chọn dòng cũng mở.
        Grid.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || openingDetail || IsBusy) return;
            if (Selected is not SalesOrderRow row) return;
            openingDetail = true;
            // Đợi cú bấm xử lý xong rồi mới mở hộp, tránh mở khi chuột còn bấm giữ.
            BeginInvoke(new Action(async () =>
            {
                try { await OpenDetailAsync(row); } finally { if (!IsDisposed) openingDetail = false; }
            }));
        };
        Grid.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter && Selected is SalesOrderRow row) { e.SuppressKeyPress = true; await OpenDetailAsync(row); } };
        Grid.CellFormatting += (_, e) => { if (e.ColumnIndex >= 0 && Grid.Columns[e.ColumnIndex].DataPropertyName == "Channel" && e.Value is string value) { e.Value = Models.States.SalesStates.Label(value); e.FormattingApplied = true; } };
        foreach (var field in new[] { basis, status, channel }) field.SelectedValueChanged += async (_, _) => { if (IsHandleCreated) await ResetAndReload(); };
        from.ValueChanged += async (_, _) => { if (IsHandleCreated) await ResetAndReload(); };
        to.ValueChanged += async (_, _) => { if (IsHandleCreated) await ResetAndReload(); };
    }
    // Mở từ click/Enter phải tự báo lỗi tại chỗ như nút hành động cũ (Register), tránh lỗi async void.
    private async Task OpenDetailAsync(SalesOrderRow row)
    {
        try { await ShowDetail(row.Code); }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or ArgumentException) { if (!IsDisposed) State.Text = ex.Message; }
        catch (OperationCanceledException) { }
    }
    public async Task ShowDetail(string code)
    {
        var detail = await service.DetailAsync(session, code);
        if (IsDisposed) return;
        if (detail is null) { State.Text = L.T("Hóa đơn không tồn tại. Tải lại."); return; }
        using var dialog = new InvoiceDetailDialog(detail, service, session);
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) == DialogResult.OK && dialog.ReturnToPos && FindForm() is Forms.MainForm main) await main.OpenSalesOrderAsync(code);
    }
}

