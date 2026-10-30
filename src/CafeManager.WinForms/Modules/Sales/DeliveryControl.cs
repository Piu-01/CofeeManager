using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

public sealed class DeliveryControl : OrbitRecordListControl, IUnsavedChanges
{
    private readonly DeliveryService service;
    private readonly SalesService sales;
    private readonly InvoiceService invoices;
    private readonly SessionContext session;
    private readonly ComboBox status = SalesUi.Combo(), payment = SalesUi.Combo();
    private Guid requestId;
    private string? pendingCode;
    private bool writing, unknown;
    public bool HasUnsavedChanges => false;
    public bool IsWriting => writing || unknown;
    public void DiscardChanges() { }
    public DeliveryControl(DeliveryService service, SalesService sales, InvoiceService invoices, SessionContext session)
    {
        this.service = service; this.sales = sales; this.invoices = invoices; this.session = session;
        status.DataSource = new[] { new SalesLookup("", "Mọi tiến độ giao"), new("Cho giao", "Chờ giao"), new("Dang giao", "Đang giao"), new("Da giao", "Đã giao"), new("Huy giao", "Hủy giao") };
        payment.DataSource = new[] { new SalesLookup("", "Mọi trạng thái tiền"), new("Chua thanh toan", "Chưa thanh toán"), new("Da thanh toan", "Đã thanh toán"), new("Huy", "Đã hủy") };
        Configure("Giao hàng", session,
            async (q, skip, t) => await service.ListAsync(session, q, SalesService.Optional(status.SelectedValue as string, 20), SalesService.Optional(payment.SelectedValue as string, 30), skip, t),
            ("Code", "Hóa đơn"), ("Recipient", "Người nhận"), ("Phone", "Điện thoại"), ("Address", "Địa chỉ"), ("Delivery", "Tiến độ giao"), ("PaymentStatus", "Thanh toán"));
        Workspace.Eyebrow.Text = "DELIVERY WORKSPACE";
        Workspace.Description.Text = "Tra cứu và chuyển tiến độ giao hàng · Chỉ lưu trạng thái hiện tại.";
        Grid.Profile = OrbitGridProfile.Delivery;
        TotalCountSelector = rows => rows.Count == 0 ? 0 : ((DeliveryRow)rows[0]!).TotalCount;
        Search.PlaceholderText = "Mã hóa đơn / số điện thoại người nhận";
        Search.AccessibleName = "Tìm đơn giao hàng";
        status.AccessibleName = "Tiến độ giao hàng"; payment.AccessibleName = "Trạng thái thanh toán";
        AddFilter(new Label { Text = "Lọc theo", TextAlign = ContentAlignment.MiddleLeft }, 44);
        AddFilter(status, 180, plain: true); AddFilter(payment, 195, plain: true);
        UseStackedSearch("đơn giao", "Thử thay đổi tiến độ, trạng thái thanh toán hoặc từ khóa tìm kiếm.");
        AddAction("Chi tiết / Bước tiếp", Detail, needsRow: true); AddAction("Kiểm tra kết quả", Reconcile);
        foreach (var filter in new[] { status, payment }) filter.SelectedValueChanged += async (_, _) => { if (IsHandleCreated) await ResetAndReload(); };
        Grid.CellFormatting += (_, e) => { if (e.Value is string value) e.Value = Models.States.SalesStates.Label(value); };
    }
    protected override bool IsActionAvailable(string command) => command switch
    {
        "Chi tiết / Bước tiếp" => !writing && !unknown,
        "Kiểm tra kết quả" => !writing && unknown && pendingCode is not null,
        _ => !writing
    };
    private async Task Detail()
    {
        if (writing || unknown || Selected is not DeliveryRow row) return;
        var detail = await service.GetAsync(session, row.Code); if (IsDisposed || detail is null) return;
        using var dialog = new DeliveryDetailDialog(detail);
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) != DialogResult.OK) return;
        if (dialog.OpenInvoice)
        {
            using var invoice = new InvoiceDetailDialog(detail, invoices, session);
            if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(invoice, this) == DialogResult.OK && invoice.ReturnToPos && FindForm() is Forms.MainForm main) await main.OpenSalesOrderAsync(row.Code);
            return;
        }
        var h = detail.Order.Header; var next = DeliveryService.Next(h.Delivery ?? ""); if (next is null || h.Status == "Huy") return;
        requestId = Guid.NewGuid(); pendingCode = h.Code; writing = true; UpdateActions();
        var committed = false;
        try { await service.ChangeAsync(session, new(h.Code, h.Revision, requestId, h.Delivery!, next)); committed = true; unknown = false; await Reload(); }
        catch (UserOperationException ex) { unknown = committed || ex.Error.RequiresReconciliation; if(!IsDisposed)State.Text = ex.Message; }
        catch (OperationCanceledException) { unknown = true; if(!IsDisposed)State.Text = "Chưa xác định kết quả; kiểm tra theo mã đơn."; }
        finally { writing = false; if (!IsDisposed) UpdateActions(); }
    }
    private async Task Reconcile()
    {
        if (writing || !unknown || pendingCode is null) return; writing = true; UpdateActions();
        try
        {
            var receipt = await sales.ReconcileAsync(session, requestId, pendingCode, "SalesDelivery");
            var actual = await service.GetAsync(session, pendingCode); if (IsDisposed) return;
            unknown = false; await Reload();
            State.Text = receipt is null ? "Yêu cầu cũ đã phân xử. Kiểm tra trạng thái hiện tại trước khi xác nhận mới." : "Đã xác minh chuyển giao; không gửi lại.";
        }
        finally { writing = false; if (!IsDisposed) UpdateActions(); }
    }
}

internal sealed class DeliveryDetailDialog : CafeManager.WinForms.Forms.OrbitDialogForm
{
    public bool OpenInvoice { get; private set; }
    internal DeliveryDetailDialog(InvoiceDetailDto detail)
    {
        var h = detail.Order.Header; Text = "Giao hàng · " + h.Code;
        var text = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Text =
            $"{h.Code}\r\n{h.Recipient}\r\n{h.Phone}\r\n{h.Address}\r\nĐối tác: {h.Partner ?? "Tự giao"}\r\nTiến độ: {Models.States.SalesStates.Label(h.Delivery)}\r\nThanh toán: {Models.States.SalesStates.Label(h.Status)}\r\n\r\nChỉ lưu trạng thái hiện tại, không có lịch sử chuyển bước.\r\nĐã giao không có nghĩa đã thu tiền. Nếu giao thất bại đơn đã trả, liên hệ quản lý xử lý thủ công; app không tự hoàn tiền. Hủy đơn mở thực hiện ở POS." };
        var next = DeliveryService.Next(h.Delivery ?? "");
        var change = SalesUi.Button(next is null ? "Không có bước tiếp" : "Chuyển → " + Models.States.SalesStates.Label(next), async (_, _) =>
        {
            if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, $"Chuyển {h.Code}: {Models.States.SalesStates.Label(h.Delivery)} → {Models.States.SalesStates.Label(next)}?", "Xác nhận tiến độ", MessageBoxButtons.OKCancel) == DialogResult.OK) { DialogResult = DialogResult.OK; Close(); }
        }); change.Enabled = next is not null && h.Status != "Huy";
        var invoice = SalesUi.Button("Xem hóa đơn / POS", (_, _) => { OpenInvoice = true; DialogResult = DialogResult.OK; Close(); });
        Controls.Add(text); Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 100, Controls = { SalesUi.Bar(change, invoice, SalesUi.Button("Đóng", (_, _) => Close())) } });
        Theme.AppTheme.Apply(this); SalesUi.Fit(this, new Size(760, 560));
    }
}



