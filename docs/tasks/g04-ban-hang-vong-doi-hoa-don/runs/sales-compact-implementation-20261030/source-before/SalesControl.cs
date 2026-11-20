using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Dtos.Tables;
using CafeManager.Models.Requests.Sales;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;
using CafeManager.WinForms.Printing;

namespace CafeManager.WinForms.Modules.Sales;

public sealed class SalesControl : UserControl, IUnsavedChanges
{
    private readonly SalesService service;
    private readonly TableService tables;
    private readonly CustomerService customers;
    private readonly LoyaltyService loyalty;
    private readonly SessionContext session;
    private readonly InvoiceService? invoices;
    private readonly ComboBox channel = SalesUi.Combo(), area = SalesUi.Combo(), table = SalesUi.Combo(), category = SalesUi.Combo();
    private readonly TextBox search = new() { Width = 180, PlaceholderText = "F2 · Tìm món" };
    private readonly FlowLayoutPanel products = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };
    private readonly OrderPanel orderPanel = new();
    private readonly Label state = new() { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(8) };
    private readonly Panel tableHost = new() { Dock = DockStyle.Top, AutoSize = true };
    private readonly TabControl compactTabs = new() { Dock = DockStyle.Fill };
    private readonly TableLayoutPanel workspace = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
    private readonly Panel menuPanel = new() { Dock = DockStyle.Fill };
    private readonly Button pay, edit, delete, header, cancel, reconcile;
    private readonly List<Button> actions = [];
    private readonly CancellationTokenSource lifetime = new();
    private SalesOrderDto? order;
    private OrderHeaderInput? draft;
    private OrderHeaderInput? headerDraft;
    private (string Product, int Quantity, string? Note)? productDraft;
    private OrderLineRequest? lineDraft;
    private Guid pendingId;
    private string? pendingCode, pendingOperation;
    private bool busy, unknown, initialized, changingLayout, disposed;
    private int menuGeneration, menuPage;
    public bool HasUnsavedChanges => headerDraft is not null || productDraft is not null || lineDraft is not null || order is null && draft is not null;
    public bool IsWriting => busy || unknown;
    public void DiscardChanges() { draft = null; headerDraft = null; productDraft = null; lineDraft = null; }
    public SalesControl(SalesService service, TableService tables, CustomerService customers, LoyaltyService loyalty, SessionContext session, InvoiceService? invoices = null)
    {
        this.service = service; this.tables = tables; this.customers = customers; this.loyalty = loyalty; this.session = session;
        this.invoices = invoices;
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96); Dock = DockStyle.Fill;
        channel.DataSource = new[] { new SalesLookup("Tai quan", "Tại quán"), new SalesLookup("Mang di", "Mang đi"), new SalesLookup("Giao hang", "Giao hàng") };
        Button Action(string text, Func<Task> callback) { var b = SalesUi.Button(text, async (_, _) => await Guard(callback)); actions.Add(b); return b; }
        var newOrder = Action("Đơn mới", NewOrder); var open = Action("F4 · Đơn mở", OpenOrders);
        var selectTable = Action("Chọn bàn", SelectTable); var reload = Action("Làm mới", ReloadAll);
        tableHost.Controls.Add(SalesUi.Bar(new Label { Text = "Khu vực / Bàn", AutoSize = true }, area, table, selectTable));
        var top = SalesUi.Bar(channel, newOrder, open, reload);
        menuPanel.Controls.Add(products);
        menuPanel.Controls.Add(SalesUi.Bar(search, category, Action("Tìm món", async () => { menuPage = 0; await LoadMenu(); }),
            Action("Trước", async () => { menuPage = Math.Max(0, menuPage - 1); await LoadMenu(); }), Action("Sau", async () => { menuPage++; await LoadMenu(); })));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58)); workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        workspace.Controls.Add(menuPanel, 0, 0); workspace.Controls.Add(orderPanel, 1, 0);
        pay = Action("F8 · Thanh toán", Checkout); header = Action("F3 · Khách / Ưu đãi", EditHeader);
        edit = Action("Sửa dòng", EditLine); delete = Action("Xóa dòng", DeleteLine); cancel = Action("Hủy đơn mở", CancelOrder);
        reconcile = SalesUi.Button("Kiểm tra kết quả", async (_, _) => await Guard(Reconcile));
        orderPanel.Actions.Controls.AddRange([header, edit, delete, cancel, pay, reconcile]);
        Controls.Add(workspace); Controls.Add(tableHost); Controls.Add(top); Controls.Add(state);
        compactTabs.TabPages.Add("Món"); compactTabs.TabPages.Add("Đơn"); compactTabs.Visible = false; Controls.Add(compactTabs); compactTabs.BringToFront();
        AppTheme.Apply(this); AppTheme.PrimaryButton(pay); orderPanel.ShowOrder(null);
        area.SelectedValueChanged += async (_, _) => { if (initialized && !busy && !unknown) await Guard(LoadTables); };
        channel.SelectedValueChanged += async (_, _) =>
        {
            if (order is not null || draft is null || channel.SelectedValue as string == draft.Channel) return;
            var previous = draft.Channel;
            if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, "Bỏ thông tin nháp để đổi loại đơn?", "Đổi loại trước tạo", MessageBoxButtons.OKCancel) == DialogResult.OK) DiscardChanges();
            else channel.SelectedValue = previous;
        };
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; menuPage = 0; await Guard(LoadMenu); } };
        Load += async (_, _) => { if (!initialized) { initialized = true; await Guard(ReloadAll); } };
        VisibleChanged += async (_, _) => { if (Visible && initialized && !disposed && !busy && !unknown) await Guard(ReloadAll); };
        SizeChanged += (_, _) => Arrange(); products.SizeChanged += (_, _) => ArrangeCards();
        SetBusy(false);
    }
    private async Task Guard(Func<Task> action)
    {
        if (disposed || session.IsRevoked) return;
        try { await action(); }
        catch (Exception ex) when (ex is UserOperationException or ArgumentException or UnauthorizedAccessException or OperationCanceledException or OverflowException)
        { if (!disposed) state.Text = "Chưa cập nhật · " + ex.Message; }
    }
    private void SetBusy(bool value)
    {
        busy = value;
        foreach (var b in actions) b.Enabled = !busy && !unknown;
        channel.Enabled = area.Enabled = table.Enabled = category.Enabled = search.Enabled = products.Enabled = !busy && !unknown;
        channel.Enabled = !busy && !unknown && order is null;
        products.Enabled = !busy && !unknown && (order is null || order.Header.IsOpen);
        pay.Enabled = !busy && !unknown && !HasUnsavedChanges && order?.Header.IsOpen == true && order.Lines.Count > 0 && order.Header.PreviewValid;
        edit.Enabled = delete.Enabled = cancel.Enabled = !busy && !unknown && order?.Header.IsOpen == true;
        header.Enabled = !busy && !unknown && (order is null || order.Header.IsOpen);
        reconcile.Enabled = !busy && unknown;
    }
    private async Task ReloadAll()
    {
        if (busy || unknown) return; SetBusy(true);
        try
        {
            var areas = new List<AreaRow>();
            for (var skip = 0; ; skip += 50) { var page = await tables.AreasAsync(session, "", skip, lifetime.Token); areas.AddRange(page); if (page.Count < 50) break; }
            var categories = await service.CategoriesAsync(session, lifetime.Token); if (disposed) return;
            var selectedArea = area.SelectedValue as string; var selectedCategory = category.SelectedValue as string;
            area.DataSource = new[] { new SalesLookup("", "Tất cả khu vực") }.Concat(areas.Select(x => new SalesLookup(x.Code, x.Name))).ToList();
            category.DataSource = new[] { new SalesLookup("", "Tất cả danh mục") }.Concat(categories).ToList();
            area.SelectedValue = selectedArea ?? ""; category.SelectedValue = selectedCategory ?? "";
            await LoadTables(); await LoadMenu();
            if (order is not null) await LoadOrder(order.Header.Code);
            if (!disposed) state.Text = "Đã tải từ SQL · " + DateTime.Now.ToString("HH:mm");
        }
        finally { if (!disposed) SetBusy(false); }
    }
    private async Task LoadTables()
    {
        var rows = new List<TableRow>(); var selected = table.SelectedValue as string;
        for (var skip = 0; ; skip += 50) { var page = await tables.ListAsync(session, "", skip, string.IsNullOrEmpty(area.SelectedValue as string) ? null : area.SelectedValue as string, lifetime.Token); rows.AddRange(page); if (page.Count < 50) break; }
        if (disposed) return;
        table.DataSource = rows.Select(x => new SalesLookup(x.Code, $"{x.Name} · {(x.OrderCode is null ? "Trống" : x.OrderCode)}")).ToList();
        if (selected is not null) table.SelectedValue = selected;
    }
    private async Task LoadMenu()
    {
        var generation = ++menuGeneration;
        var rows = await service.MenuAsync(session, search.Text, category.SelectedValue as string, menuPage * 50, lifetime.Token);
        if (disposed || generation != menuGeneration) return;
        foreach (Control c in products.Controls.Cast<Control>().ToArray()) c.Dispose(); products.Controls.Clear();
        foreach (var item in rows) products.Controls.Add(new ProductCardControl(item, async (_, _) => await Guard(() => AddProduct(item))));
        ArrangeCards(); state.Text = rows.Count == 0 ? "Không có món trong trang/bộ lọc này." : $"Trang món {menuPage + 1} · {rows.Count} món · tồn chỉ là tham khảo trước checkout.";
    }
    private void ArrangeCards()
    {
        var width = Math.Max(1, products.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
        var logical = width * 96F / DeviceDpi; var columns = logical >= 430 ? 2 : 1;
        foreach (Control card in products.Controls) card.Width = Math.Max(100, width / columns - (int)(12 * DeviceDpi / 96F));
    }
    private void Arrange()
    {
        if (changingLayout || disposed) return; changingLayout = true;
        try
        {
            var compact = ClientSize.Width * 96F / DeviceDpi < 680;
            if (compact && menuPanel.Parent != compactTabs.TabPages[0]) { compactTabs.TabPages[0].Controls.Add(menuPanel); compactTabs.TabPages[1].Controls.Add(orderPanel); }
            else if (!compact && menuPanel.Parent != workspace) { workspace.Controls.Add(menuPanel, 0, 0); workspace.Controls.Add(orderPanel, 1, 0); }
            compactTabs.Visible = compact; workspace.Visible = !compact;
            if (compact) { compactTabs.BringToFront(); tableHost.BringToFront(); state.BringToFront(); }
            ArrangeCards();
        }
        finally { changingLayout = false; }
    }
    public async Task OpenOrderAsync(string code)
    {
        if (busy || unknown) return;
        if (HasUnsavedChanges && await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, "Bỏ input chưa ghi để mở đơn khác?", "Nháp chưa gửi", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        await Guard(async () => { SetBusy(true); try { await LoadOrder(code); DiscardChanges(); } finally { if (!disposed) SetBusy(false); } });
    }
    private async Task LoadOrder(string code)
    {
        var actual = await service.GetAsync(session, code, lifetime.Token); if (disposed) return;
        order = actual; orderPanel.ShowOrder(order);
        if (order is not null) channel.SelectedValue = order.Header.Channel;
        if (order is null) state.Text = "Hóa đơn không tồn tại. Tải lại danh sách.";
    }
    private async Task NewOrder()
    {
        if (busy || unknown) return;
        if (HasUnsavedChanges && await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, "Bỏ thông tin nháp để tạo đơn khác?", "Nháp chưa lưu", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        ResetDraft(); SetBusy(false);
    }
    private void ResetDraft()
    {
        order = null; DiscardChanges(); orderPanel.ShowOrder(null);
    }
    private async Task SelectTable()
    {
        if (busy || unknown || table.SelectedValue is not string code) return;
        await OpenTableAsync(code);
    }
    public async Task OpenTableAsync(string code)
    {
        if (busy || unknown) return;
        var current = await tables.SelectAsync(session, code, lifetime.Token); if (disposed || current is null) return;
        if (current.OrderCode is not null) await OpenOrderAsync(current.OrderCode);
        else { await NewOrder(); if (HasUnsavedChanges) return; channel.SelectedValue = "Tai quan"; draft = new("Tai quan", code); state.Text = $"Đã chọn {current.Name}. Thêm món đầu mới tạo đơn."; }
    }
    private OrderHeaderInput CurrentHeader() => order is null ? draft ?? new(channel.SelectedValue as string ?? "Mang di", (channel.SelectedValue as string) == "Tai quan" ? table.SelectedValue as string : null) :
        new(order.Header.Channel, order.Header.TableCode, order.Header.Customer, order.Header.Promotion, order.Header.Partner, order.Header.Recipient, order.Header.Phone, order.Header.Address, order.Header.Ship);
    private async Task EditHeader()
    {
        if (busy || unknown) return;
        using var dialog = new OrderHeaderDialog(headerDraft ?? CurrentHeader(), order?.Header.Created ?? DateTime.Now, service, customers, loyalty, session);
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) != DialogResult.OK || dialog.Result is null) return;
        if (order is null) { draft = dialog.Result; state.Text = "Thông tin còn là nháp; thêm món đầu để lưu."; return; }
        var h = order.Header; var input = dialog.Result; headerDraft = input;
        await Write(h.Code, "SalesHeader", id => service.HeaderAsync(session, new(h.Code, h.Revision, id, input), lifetime.Token));
    }
    private async Task AddProduct(SalesMenuItem item)
    {
        if (busy || unknown || !item.CanAdd || order is not null && !order.Header.IsOpen) return;
        var previous = productDraft is { } pending && pending.Product == item.Code ? productDraft : null;
        using var dialog = new OrderLineDialog(item.Name, previous?.Quantity ?? 1, previous?.Note); if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) != DialogResult.OK) return;
        productDraft = (item.Code, dialog.Quantity, dialog.Note);
        if (order is null)
        {
            if (draft is null || CurrentHeader().Channel == "Giao hang") { await EditHeader(); if (draft is null) return; }
            var input = SalesService.Normalize(CurrentHeader()); var code = await service.AllocateAsync(session, lifetime.Token);
            await Write(code, "SalesCreate", id => service.CreateAsync(session, new(code, id, input, item.Code, dialog.Quantity, dialog.Note), lifetime.Token));
        }
        else
        {
            var h = order.Header; await Write(h.Code, "SalesLine", id => service.LineAsync(session, new(h.Code, h.Revision, id, "Add", Product: item.Code, Quantity: dialog.Quantity, Note: dialog.Note), lifetime.Token));
        }
    }
    private async Task EditLine()
    {
        if (order?.Header.IsOpen != true || orderPanel.Lines.CurrentRow?.DataBoundItem is not SalesLine line) return;
        var previous = lineDraft?.LineId == line.LineId ? lineDraft : null;
        using var dialog = new OrderLineDialog(line.Name, previous?.Quantity ?? line.Quantity, previous?.Note ?? line.Note); if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) != DialogResult.OK) return;
        var h = order.Header; lineDraft = new(h.Code, h.Revision, Guid.Empty, "Update", line.LineId, Quantity: dialog.Quantity, Note: dialog.Note);
        var input = lineDraft; await Write(h.Code, "SalesLine", id => service.LineAsync(session, input with { RequestId = id }, lifetime.Token));
    }
    private async Task DeleteLine()
    {
        if (order?.Header.IsOpen != true || orderPanel.Lines.CurrentRow?.DataBoundItem is not SalesLine line) return;
        if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, $"Xóa dòng {line.Name} khỏi {order.Header.Code}?", "Xóa món", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        var h = order.Header; await Write(h.Code, "SalesLine", id => service.LineAsync(session, new(h.Code, h.Revision, id, "Delete", line.LineId), lifetime.Token));
    }
    private async Task Write(string code, string operation, Func<Guid, Task<SalesWriteResult>> send)
    {
        if (busy || unknown) return; pendingId = Guid.NewGuid(); pendingCode = code; pendingOperation = operation; SetBusy(true);
        var committed = false;
        try
        {
            await send(pendingId); committed = true; DiscardChanges(); await LoadOrder(code); await LoadTables();
            if (!disposed) state.Text = "Đã ghi và tải kết quả từ SQL.";
        }
        catch (UserOperationException ex) { unknown = committed || ex.Error.RequiresReconciliation; if (!disposed) state.Text = ex.Message + (unknown ? $" · {code} · Kiểm tra kết quả" : ""); }
        catch (OperationCanceledException) { unknown = true; if (!disposed) state.Text = $"Chưa xác định kết quả {code}; kiểm tra lại."; }
        finally { if (!disposed) SetBusy(false); }
    }
    private async Task Reconcile()
    {
        if (busy || pendingCode is null || pendingOperation is null) return; SetBusy(true);
        try
        {
            var receipt = await service.ReconcileAsync(session, pendingId, pendingCode, pendingOperation, lifetime.Token);
            await LoadOrder(pendingCode); if (disposed) return;
            unknown = false; if (receipt is not null) DiscardChanges(); state.Text = receipt is null ? "Đã phân xử yêu cầu cũ; chưa có receipt. Input được giữ để tham khảo; kiểm tra đơn và xác nhận lại nếu cần." : "Đã xác minh giao dịch. Dữ liệu hiện tại đã tải lại.";
        }
        finally { if (!disposed) SetBusy(false); }
    }
    private async Task OpenOrders()
    {
        if (busy || unknown) return;
        using var dialog = new OpenOrdersDialog(service, session);
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) == DialogResult.OK && dialog.SelectedCode is not null) await OpenOrderAsync(dialog.SelectedCode);
    }
    private async Task CancelOrder()
    {
        if (order?.Header.IsOpen != true || busy || unknown) return; var h = order.Header;
        if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, $"Hủy đơn mở {h.Code}, bàn {h.TableCode ?? "—"}, {SalesUi.Money(h.Payable)}? Không hoàn tiền/kho. Nếu đã pha, nhờ quản lý/kho lập xuất hủy ở phân hệ kho khi có chức năng T21.", "Hủy đơn mở", MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        await Write(h.Code, "SalesCancel", id => service.CancelAsync(session, h, id, lifetime.Token));
    }
    private async Task Checkout()
    {
        if (busy || unknown || HasUnsavedChanges || order?.Header.IsOpen != true) return; SetBusy(true);
        try
        {
            var current = await service.GetAsync(session, order.Header.Code, lifetime.Token); if (current is null) return;
            var preview = await service.PreviewAsync(session, current.Header, lifetime.Token); var methods = await service.PaymentMethodsAsync(session, lifetime.Token);
            if (disposed) return;
            using var dialog = new CheckoutDialog(service, session, current, preview, methods);
            if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) == DialogResult.OK && dialog.ResultOrder is not null)
            {
                await FinishCheckout(dialog.ResultOrder, dialog.Change);
            }
            else await LoadOrder(current.Header.Code);
        }
        finally { if (!disposed) SetBusy(false); }
    }
    private async Task FinishCheckout(SalesOrderDto paid, decimal? change)
    {
        var message = $"Đã thanh toán {paid.Header.Code} · Thu {SalesUi.Money(paid.Payments.Sum(x => x.Amount))}"
            + (change is not null ? $" · Thối {SalesUi.Money(change.Value)}" : "");
        var printRequested = false;
        try
        {
            printRequested = await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, $"Thanh toán thành công {paid.Header.Code}. Bạn có muốn in hóa đơn không?",
                "In hóa đơn", MessageBoxButtons.YesNo) == DialogResult.Yes;
            var detail = invoices is null ? null : await invoices.DetailAsync(session, paid.Header.Code, lifetime.Token);
            if (disposed) return;
            if (detail is not null) message += $" · Điểm mua đã cộng {detail.Points}";
            if (printRequested)
            {
                if (invoices is null || detail is null) throw new InvalidOperationException("Chưa tải được dữ liệu hóa đơn để in.");
                message += await InvoicePrinting.PrintAsync(this, detail, invoices, session, reprint: false)
                    ? " · Đã gửi yêu cầu in." : " · Đã hủy in.";
            }
        }
        catch (Exception ex) when (ex is global::System.Drawing.Printing.InvalidPrinterException or global::System.ComponentModel.Win32Exception
            or InvalidOperationException or UnauthorizedAccessException or UserOperationException or OperationCanceledException)
        {
            message += (printRequested ? " · Chưa in được hóa đơn. " : " · Chưa tải được chi tiết hóa đơn. ") + ex.Message;
        }
        finally
        {
            // Chỉ vào đây sau kết quả đã chốt; lỗi/hủy in không giữ đơn đã trả trên POS.
            if (!disposed) ResetDraft();
        }
        if (disposed) return;
        try { await LoadTables(); }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or OperationCanceledException)
        { message += " · Chưa tải lại được bàn. " + ex.Message; }
        if (!disposed) state.Text = message + " Tra cứu hoặc in lại tại Lịch sử hóa đơn.";
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (busy || unknown || FindForm() is Forms.MainForm { Dialogs.IsOpen: true } || FindForm()?.OwnedForms.Any(x => x.Visible) == true) return base.ProcessCmdKey(ref msg, keyData);
        if (keyData == Keys.F2) { search.Focus(); return true; }
        if (keyData == Keys.F3) { _ = Guard(EditHeader); return true; }
        if (keyData == Keys.F4) { _ = Guard(OpenOrders); return true; }
        if (keyData == Keys.F8) { _ = Guard(Checkout); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed) { disposed = true; lifetime.Cancel(); lifetime.Dispose(); }
        base.Dispose(disposing);
    }
}




