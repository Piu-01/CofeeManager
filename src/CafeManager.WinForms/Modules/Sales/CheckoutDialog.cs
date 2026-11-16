using System.ComponentModel;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.Requests;
using CafeManager.Models.Requests.Sales;
using CafeManager.Models.States;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed partial class CheckoutDialog : CafeManager.WinForms.Forms.OrbitDialogForm
{
    public sealed class PaymentDraft { public string Method { get; set; } = ""; public string Amount { get; set; } = ""; public string? Reference { get; set; } }
    private readonly SalesService service;
    private readonly SessionContext session;
    private SalesOrderDto order;
    private CheckoutPreview preview;
    private readonly BindingList<PaymentDraft> rows = [];
    private readonly OrbitDataGridView grid = new() { Profile = OrbitGridProfile.Payment, Dock = DockStyle.Fill };
    private readonly OrbitNotice state = new();
    private readonly TextBox tendered = new() { Width = 140, AccessibleName = L.T("Tiền khách đưa cho phần tiền mặt") };
    private readonly Button confirm, check, back, add, remove;
    private readonly ErrorProvider errors;
    private readonly CancellationTokenSource lifetime = new();
    private bool resourcesDisposed;
    private readonly StarDustOverlay stars;
    private Guid requestId;
    private bool busy, unknown, saved;
    private bool inputValid;
    private readonly HashSet<string> methodCodes;
    public SalesOrderDto? ResultOrder { get; private set; }
    public decimal? TenderedCash { get; private set; }
    public decimal? Change { get; private set; }
    public CheckoutDialog(SalesService service, SessionContext session, SalesOrderDto order, CheckoutPreview preview, List<SalesLookup> methods)
    {
        this.service = service; this.session = session; this.order = order; this.preview = preview;
        methodCodes = methods.Select(x => x.Code).Where(x => x != "PT010").ToHashSet(StringComparer.Ordinal);
        Text = L.T("Xác nhận thanh toán · ") + order.Header.Code;
        errors = new ErrorProvider { ContainerControl = this }; stars = new StarDustOverlay(this);
        grid.Columns.Add(new OrbitPaymentMethodColumn { Name = "Method", DataPropertyName = "Method", HeaderText = L.T("Phương thức"), DataSource = new[] { new SalesLookup("", L.T("Chọn phương thức")) }.Concat(methods).ToList(), ValueMember = "Code", DisplayMember = "Name" });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", DataPropertyName = "Amount", HeaderText = L.T("Số thu (VND)") });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reference", DataPropertyName = "Reference", HeaderText = L.T("Tham chiếu"), MaxInputLength = 100 });
        grid.DataSource = rows;
        add = SalesUi.Button(L.T("Thêm dòng"), (_, _) => rows.Add(new()));
        remove = SalesUi.Button(L.T("Xóa dòng"), (_, _) => { if (grid.CurrentRow?.DataBoundItem is PaymentDraft row) rows.Remove(row); UpdateSummary(); });
        confirm = SalesUi.Button(L.T("Xác nhận thanh toán"), async (_, _) => await Confirm());
        check = SalesUi.Button(L.T("Kiểm tra lại"), async (_, _) => await Reconcile()); check.Enabled = false;
        back = SalesUi.Button(L.T("Quay lại"), (_, _) => Close());
        grid.DataError += (_, e) => { e.ThrowException = false; inputValid = false; confirm.Enabled = false; ShowState(L.T("Chọn phương thức hợp lệ và nhập tiền nguyên VND."), ModuleState.Error); };
        BuildPaymentLayout();
        OrbitField.SelectAllOnEntry(tendered);
        if (preview.Payable > 0) rows.Add(new() { Method = methods.Any(x => x.Code == "PT001") ? "PT001" : "", Amount = preview.Payable.ToString("0", SalesUi.Culture) });
        tendered.Text = preview.Payable.ToString("N0", SalesUi.Culture);
        rows.ListChanged += (_, _) => UpdateSummary();
        grid.CellValueChanged += (_, _) => UpdateSummary();
        grid.CurrentCellDirtyStateChanged += (_, _) => { if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewComboBoxCell) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        grid.EditingControlShowing += (_, e) => { e.Control.TextChanged -= PaymentEditorChanged; e.Control.TextChanged += PaymentEditorChanged; };
        grid.CellFormatting += (_, e) =>
        {
            if (e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Amount" || e.Value is not string value) return;
            try { e.Value = SalesUi.ParseMoney(value).ToString("N0", SalesUi.Culture); e.FormattingApplied = true; }
            catch (ArgumentException) { /* Giữ nguyên input sai để người dùng sửa. */ }
        };
        tendered.Leave += (_, _) =>
        {
            try { var value = SalesUi.ParseMoney(tendered.Text); LoyaltyService.Money(value); tendered.Text = value.ToString("N0", SalesUi.Culture); }
            catch (ArgumentException) { /* Không chuẩn hóa hoặc xóa input sai. */ }
        };
        tendered.TextChanged += (_, _) => UpdateSummary(); UpdateSummary();
    }
    private IReadOnlyList<PaymentAllocation> CaptureAllocations()
    {
        grid.EndEdit(); (BindingContext?[rows] as CurrencyManager)?.EndCurrentEdit();
        return SalesService.NormalizeAllocations(rows.Select(x => new PaymentAllocation(x.Method, SalesUi.ParseMoney(x.Amount), x.Reference)).ToArray());
    }
    private void UpdateSummary()
    {
        totalValue.Text = SalesUi.Money(preview.Total); discountValue.Text = "−" + SalesUi.Money(preview.Discount);
        discountSource.Text = $"{SalesStates.Label(preview.Source)} · {preview.PercentDiscount}%";
        shipValue.Text = SalesUi.Money(preview.Ship); payableValue.Text = SalesUi.Money(preview.Payable);
        confirm.Text = preview.Payable == 0 ? L.T("Chốt hóa đơn 0 ₫") : L.T("Xác nhận thanh toán");
        confirm.Size = confirm.GetPreferredSize(Size.Empty);
        inputValid = false;
        allocatedValue.Text = receivedValue.Text = differenceValue.Text = "—";
        tendered.Enabled = !busy && !unknown;
        try
        {
            if (rows.Any(x => string.IsNullOrWhiteSpace(x.Method))) throw new ArgumentException(L.T("Chọn phương thức thanh toán."));
            var allocations = SalesService.NormalizeAllocations(ReadDraftAllocations());
            var sum = allocations.Sum(x => x.SoTien); var cash = allocations.Where(x => x.MaPT == "PT001").Sum(x => x.SoTien);
            allocatedValue.Text = SalesUi.Money(sum); cashHint.Text = cash > 0 ? L.T("Tiền mặt cần thu · ") + SalesUi.Money(cash) : L.T("Không có phần tiền mặt · Tiền khách đưa không áp dụng.");
            tendered.Enabled = !busy && !unknown && cash > 0;
            if (allocations.Any(x => !methodCodes.Contains(x.MaPT))) throw new ArgumentException(L.T("Chọn phương thức thanh toán được hỗ trợ."));
            if (sum != preview.Payable) throw new ArgumentException(L.F("Tổng số thu phải bằng {0} · {1} {2}.", SalesUi.Money(preview.Payable), (sum < preview.Payable ? L.T("Thiếu") : L.T("Dư")), SalesUi.Money(Math.Abs(preview.Payable - sum))));
            var received = cash > 0 ? SalesUi.ParseMoney(tendered.Text) : 0;
            receivedValue.Text = cash > 0 ? SalesUi.Money(received) : L.T("Không áp dụng");
            if (cash > 0 && received < cash)
            {
                differenceValue.Text = L.T("Thiếu ") + SalesUi.Money(cash - received); differenceValue.ForeColor = AppTheme.Error;
                throw new ArgumentException(L.T("Tiền khách đưa chưa đủ · Còn thiếu ") + SalesUi.Money(cash - received) + ".");
            }
            var change = cash > 0 ? SalesService.CashChange(cash, received) : 0;
            differenceValue.Text = cash > 0 ? SalesUi.Money(change) : L.T("Không áp dụng"); differenceValue.ForeColor = OrbitManagementTheme.Caramel;
            inputValid = true;
            if (!busy && !unknown) ShowState(cash > 0 ? L.T("Đủ tiền thanh toán · Tiền thối ") + SalesUi.Money(change) : L.T("Phân bổ hợp lệ · Không có phần tiền mặt."), ModuleState.Saved);
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException) { if (!busy && !unknown) ShowState(ex is OverflowException ? L.T("Số tiền quá lớn; kiểm tra lại dữ liệu nhập.") : ex.Message, ModuleState.Error); }
        confirm.Enabled = !busy && !unknown && inputValid;
    }
    private void PaymentEditorChanged(object? sender, EventArgs e) => UpdateSummary();
    private PaymentAllocation[] ReadDraftAllocations() => rows.Select((row, index) =>
    {
        var amount = row.Amount; var reference = row.Reference;
        var cell = grid.CurrentCell;
        if (cell is { IsInEditMode: true } && cell.RowIndex == index && grid.EditingControl is TextBox editor)
        {
            if (cell.OwningColumn?.Name == "Amount") amount = editor.Text;
            if (cell.OwningColumn?.Name == "Reference") reference = editor.Text;
        }
        return new PaymentAllocation(row.Method, SalesUi.ParseMoney(amount), reference);
    }).ToArray();
    private void ShowState(string message, ModuleState kind) { state.Kind = kind; state.Text = message; }
    private void SetBusy(bool value)
    {
        busy = value; grid.Enabled = tendered.Enabled = add.Enabled = remove.Enabled = !busy && !unknown;
        confirm.Enabled = !busy && !unknown && inputValid; check.Enabled = !busy && unknown; back.Enabled = !busy && !unknown;
        tendered.Enabled = !busy && !unknown && rows.Any(x => x.Method == "PT001");
    }
    private async Task Confirm()
    {
        if (busy || unknown) return; errors.Clear();
        IReadOnlyList<PaymentAllocation> allocations;
        try
        {
            allocations = CaptureAllocations();
            if (allocations.Sum(x => x.SoTien) != preview.Payable) throw new ArgumentException(L.T("Phân bổ phải bằng số phải trả."));
            var cash = allocations.Where(x => x.MaPT == "PT001").Sum(x => x.SoTien);
            TenderedCash = cash > 0 ? SalesUi.ParseMoney(tendered.Text) : null;
            Change = cash > 0 ? SalesService.CashChange(cash, TenderedCash!.Value) : null;
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        { ShowState(ex.Message, ModuleState.Error); errors.SetError(grid, ex.Message); grid.Focus(); return; }
        if (await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, L.F("Chốt {0} với số tiền {1}? Sau khi chốt không sửa tiền/món.", order.Header.Code, SalesUi.Money(preview.Payable)), L.T("Xác nhận thanh toán"), MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        requestId = Guid.NewGuid(); SetBusy(true);
        var committed = false;
        try
        {
            ShowState(L.T("Đang xử lý; không gửi lại…"), ModuleState.Loading); stars.Burst(confirm);
            await service.CheckoutAsync(session, new(order.Header.Code, preview.Revision, requestId, preview.Token, allocations), lifetime.Token);
            committed = true;
            unknown = true; // Commit confirmed; a failed follow-up read still needs reconciliation.
            await Complete();
        }
        catch (UserOperationException ex)
        {
            unknown = committed || ex.Error.RequiresReconciliation; if (!IsDisposed) ShowState(ex.Message, unknown ? ModuleState.Unknown : ModuleState.Error);
            if (!unknown && ex.Error.Number is 52201 or 52103 or 52102) await ReloadPreview();
        }
        catch (OperationCanceledException) { unknown = true; if (!IsDisposed) ShowState(L.T("Chưa xác định kết quả. Kiểm tra lại theo mã đơn."), ModuleState.Unknown); }
        catch (UnauthorizedAccessException ex) { unknown = true; if (!IsDisposed) ShowState(ex.Message, ModuleState.Unknown); }
        finally { if (!IsDisposed) SetBusy(false); }
    }
    private async Task Complete()
    {
        var actual = await service.GetAsync(session, order.Header.Code, lifetime.Token);
        if (IsDisposed) return;
        if (actual?.Header.Status == "Da thanh toan") { ResultOrder = actual; saved = true; unknown = false; DialogResult = DialogResult.OK; Close(); }
        else { unknown = true; ShowState(L.T("Cần đối chiếu trạng thái hóa đơn trước khi tiếp tục."), ModuleState.Unknown); }
    }
    private async Task ReloadPreview()
    {
        try
        {
            var actual = await service.GetAsync(session, order.Header.Code, lifetime.Token);
            if (actual is null) { unknown = true; ShowState(L.T("Không tìm thấy hóa đơn. Kiểm tra lại."), ModuleState.Unknown); return; }
            if (!actual.Header.IsPayable) { if (actual.Header.Status == "Da thanh toan") await Complete(); else { saved = true; DialogResult = DialogResult.Cancel; Close(); } return; }
            var next = await service.PreviewAsync(session, actual.Header, lifetime.Token);
            if (IsDisposed) return;
            var old = preview.Payable; order = actual; preview = next; UpdateSummary();
            ShowState(L.F("Số cũ {0} → số mới {1}. Kiểm tra phân bổ và xác nhận lại.", SalesUi.Money(old), SalesUi.Money(next.Payable)), ModuleState.Conflict);
        }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or OperationCanceledException) { unknown = true; if (!IsDisposed) ShowState(ex.Message, ModuleState.Unknown); }
    }
    private async Task Reconcile()
    {
        if (busy) return; SetBusy(true);
        try
        {
            var receipt = await service.ReconcileAsync(session, requestId, order.Header.Code, "SalesCheckout", lifetime.Token);
            var actual = await service.GetAsync(session, order.Header.Code, lifetime.Token);
            if (IsDisposed) return;
            if (actual?.Header.Status == "Da thanh toan") { TenderedCash = Change = null; await Complete(); }
            else if (receipt is null && actual?.Header.IsPayable == true) { unknown = false; await ReloadPreview(); }
            else if (actual?.Header.Status == "Huy") { saved = true; DialogResult = DialogResult.Cancel; Close(); }
            else { unknown = true; ShowState(L.T("Kết quả cần kiểm tra. Giữ mã đơn; không tự gửi lại."), ModuleState.Unknown); }
        }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or OperationCanceledException) { unknown = true; if (!IsDisposed) ShowState(ex.Message, ModuleState.Unknown); }
        finally { if (!IsDisposed) SetBusy(false); }
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Enter) { grid.EndEdit(); return true; }
        if (keyData == Keys.Escape && !busy && !unknown) { Close(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    protected override async Task<bool> CanCloseAsync()
    {
        if (session.IsRevoked || saved) return true;
        if (busy || unknown) return false;
        return rows.Count == 0 || await CafeManager.WinForms.Forms.OrbitConfirmationDialog.ShowAsync(this, L.T("Bỏ phân bổ chưa chốt và quay lại?"), L.T("Thanh toán chưa gửi"), MessageBoxButtons.OKCancel) == DialogResult.OK;
    }
    protected override void Dispose(bool disposing) { if (disposing && !resourcesDisposed) { resourcesDisposed = true; lifetime.Cancel(); lifetime.Dispose(); stars.Dispose(); errors.Dispose(); } base.Dispose(disposing); }
}



