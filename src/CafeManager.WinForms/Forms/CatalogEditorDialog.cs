using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Requests.Catalogs;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Forms;

public partial class CatalogEditorDialog : CafeManager.WinForms.Forms.OrbitDialogForm, IUnsavedChanges
{
    private readonly CatalogService? service;
    private readonly SessionContext? session;
    private readonly CatalogKind kind;
    private CatalogRow? original;
    private readonly Guid requestId = Guid.NewGuid();
    private string initial = "";
    private bool writing;
    private CatalogRequest? unknown;
    private bool saved;
    public bool HasUnsavedChanges => !saved && Snapshot() != initial;
    public bool IsWriting => writing || unknown is not null;
    public void DiscardChanges() { saved = true; }
    public CatalogEditorDialog()
    {
        InitializeComponent();
        foreach (Control field in fields.Controls)
        {
            field.Margin = new Padding(4, 8, 4, 8);
            OrbitManagementTheme.DialogInput(field);
            if (field is Label) { field.Font = OrbitManagementTheme.Table; field.ForeColor = OrbitManagementTheme.Muted; field.Anchor = AnchorStyles.Left; }
        }
        foreach (var input in new Control[] { codeInput, nameInput, phoneInput, ratioInput, allowanceInput, factorInput, typeInput }) OrbitField.Wrap(input, true);
        OrbitFieldLayout.Attach(fields);
        initial = Snapshot();
    }
    public CatalogEditorDialog(CatalogService service, SessionContext session, CatalogKind kind, CatalogRow? row) : this()
    {
        this.service = service; this.session = session; this.kind = kind; original = row;
        Text = row is null ? L.T("Thêm danh mục") : L.F("Sửa danh mục · {0}", row.Code);
        codeInput.Text = row?.Code ?? ""; nameInput.Text = row?.Name ?? "";
        codeInput.ReadOnly = row is not null || kind == CatalogKind.ProductCategory;
        nameInput.MaxLength = kind == CatalogKind.Supplier ? 100 : kind == CatalogKind.Shift ? 30 : 50;
        ShowField(phoneLabel, phoneInput, kind == CatalogKind.Supplier);
        ShowField(ratioLabel, ratioInput, kind == CatalogKind.DeliveryPartner);
        ShowField(allowanceLabel, allowanceInput, kind == CatalogKind.Position);
        ShowField(factorLabel, factorInput, kind == CatalogKind.Position);
        ShowField(typeLabel, typeInput, kind == CatalogKind.ExpenseCategory);
        ShowField(startLabel, startInput, kind == CatalogKind.Shift); ShowField(endLabel, endInput, kind == CatalogKind.Shift);
        supportedInput.Visible = kind == CatalogKind.PaymentMethod;
        supportedInput.Enabled = row is not null && row.Code != "PT010";
        Populate(row);
    }
    private void Populate(CatalogRow? row)
    {
        original = row;
        if (row is not null) { codeInput.Text = row.Code; nameInput.Text = row.Name; }
        switch (row)
        {
            case SupplierRow s: phoneInput.Text = s.Phone; break;
            case DeliveryPartnerRow d: ratioInput.Text = d.Ratio.ToString(CultureInfo.GetCultureInfo("vi-VN")); break;
            case PositionRow p: allowanceInput.Text = p.Allowance.ToString(CultureInfo.GetCultureInfo("vi-VN")); factorInput.Text = p.Factor.ToString(CultureInfo.GetCultureInfo("vi-VN")); break;
            case ShiftRow s: startInput.Value = DateTime.Today.Add(s.StartTime); endInput.Value = DateTime.Today.Add(s.EndTime); break;
            case ExpenseCategoryRow e: typeInput.SelectedIndex = e.Type == ExpenseType.Thu ? 0 : 1; typeInput.Enabled = !CatalogService.IsSystemExpense(e.Code); break;
            case PaymentMethodRow p: supportedInput.Checked = p.Supported; break;
        }
        initial = Snapshot();
    }
    private static void ShowField(Control label, Control input, bool visible) { label.Visible = input.Visible = visible; if (input.Parent is OrbitField frame) frame.Visible = visible; }
    private string Snapshot() => string.Join("\u001f", codeInput.Text, nameInput.Text, phoneInput.Text, ratioInput.Text,
        allowanceInput.Text, factorInput.Text, typeInput.Text, startInput.Value.TimeOfDay, endInput.Value.TimeOfDay, supportedInput.Checked);
    private decimal Amount(TextBox input, string field)
    {
        if (!decimal.TryParse(input.Text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
            CultureInfo.GetCultureInfo("vi-VN"), out var value)) throw new ArgumentException(L.T("Nhập số hợp lệ (dấu phẩy thập phân, không dấu phân nhóm)."), field);
        return value;
    }
    private CatalogRequest Request() => kind switch
    {
        CatalogKind.ProductCategory => new ProductCategoryRequest(codeInput.Text, nameInput.Text, requestId),
        CatalogKind.Supplier => new SupplierRequest(codeInput.Text, nameInput.Text, phoneInput.Text),
        CatalogKind.PaymentMethod => new PaymentMethodRequest(codeInput.Text, nameInput.Text, supportedInput.Checked),
        CatalogKind.DeliveryPartner => new DeliveryPartnerRequest(codeInput.Text, nameInput.Text, Amount(ratioInput, "Ratio")),
        CatalogKind.ExpenseCategory => new ExpenseCategoryRequest(codeInput.Text, nameInput.Text, typeInput.SelectedIndex == 0 ? ExpenseType.Thu : ExpenseType.Chi),
        CatalogKind.Position => new PositionRequest(codeInput.Text, nameInput.Text, Amount(allowanceInput, "Allowance"), Amount(factorInput, "Factor")),
        _ => new ShiftRequest(codeInput.Text, nameInput.Text, new TimeSpan(startInput.Value.Hour, startInput.Value.Minute, 0), new TimeSpan(endInput.Value.Hour, endInput.Value.Minute, 0))
    };
    private void Busy(bool value)
    {
        if (IsDisposed) return;
        writing = value; fields.Enabled = !value && unknown is null;
        saveButton.Enabled = !value && unknown is null; reloadButton.Enabled = !value; cancelButton.Enabled = !value && unknown is null;
    }
    private async void Save_Click(object? sender, EventArgs e)
    {
        if (service is null || session is null || IsWriting) return;
        errors.Clear(); CatalogRequest request;
        try { request = CatalogService.Normalize(Request(), original is null); }
        catch (ArgumentException ex) { Validation(ex); return; }
        Busy(true); OperationStatePresenter.Show(statusLabel, ModuleState.Loading, L.T("Đang lưu danh mục."));
        try
        {
            await service.SaveAsync(session, request, original?.Revision);
            if (IsDisposed) return;
            saved = true; writing = false; DialogResult = DialogResult.OK; Close();
        }
        catch (ArgumentException ex) { Validation(ex); }
        catch (UserOperationException ex)
        {
            if (IsDisposed) return;
            if (ex.Error.RequiresReconciliation) unknown = request;
            OperationStatePresenter.Show(statusLabel, ex.Error.RequiresReconciliation ? ModuleState.Unknown : ex.Error.Number == 51503 ? ModuleState.Conflict : ModuleState.Error,
                ex.Message + (unknown is null ? "" : L.T(" Chọn Kiểm tra / tải lại; không gửi lại.")));
        }
        catch (CafeManager.DataAccess.DataAccessException ex) { OperationStatePresenter.Show(statusLabel, ModuleState.Error, ex.Error.Message); }
        catch (UnauthorizedAccessException ex) { statusLabel.Text = ex.Message; }
        catch (OperationCanceledException) when (session.IsRevoked) { statusLabel.Text = L.T("Phiên đã kết thúc."); }
        finally { Busy(false); }
    }
    private void Validation(ArgumentException ex)
    {
        if (IsDisposed) return;
        Control field = ex.ParamName switch { "Code" => codeInput, "Phone" => phoneInput, "Ratio" => ratioInput, "Allowance" => allowanceInput,
            "Factor" => factorInput, "Type" => typeInput, "StartTime" => startInput, "Supported" => supportedInput, _ => nameInput };
        errors.SetError(field, ex.Message); field.Focus(); statusLabel.Text = ex.Message;
    }
    private static bool Matches(CatalogRequest request, CatalogRow row) => request.Name == row.Name && (request, row) switch
    {
        (SupplierRequest a, SupplierRow b) => a.Phone == b.Phone,
        (PaymentMethodRequest a, PaymentMethodRow b) => a.Supported == b.Supported,
        (DeliveryPartnerRequest a, DeliveryPartnerRow b) => a.Ratio == b.Ratio,
        (ExpenseCategoryRequest a, ExpenseCategoryRow b) => a.Type == b.Type,
        (PositionRequest a, PositionRow b) => a.Allowance == b.Allowance && a.Factor == b.Factor,
        (ShiftRequest a, ShiftRow b) => a.StartTime == b.StartTime && a.EndTime == b.EndTime,
        (ProductCategoryRequest, ProductCategoryRow) => true, _ => false
    };
    private async void Reload_Click(object? sender, EventArgs e)
    {
        if (writing || service is null || session is null) return;
        Busy(true);
        try
        {
            if (unknown is not null)
            {
                var row = await service.ReconcileAsync(session, unknown, original is null);
                if (IsDisposed) return;
                if (row is not null && Matches(unknown, row)) { unknown = null; saved = true; writing = false; DialogResult = DialogResult.OK; Close(); return; }
                if (row is null && original is null)
                {
                    unknown = null; statusLabel.Text = L.T("Chưa có bản ghi/receipt đã commit. Có thể thử lại đúng mã yêu cầu cũ."); return;
                }
                statusLabel.Text = L.T("Dữ liệu DB khác nháp. Đối chiếu đã xong; chọn Hủy để rời hoặc tải lại bản hiện tại.");
                unknown = null; return;
            }
            if (original is not null && await OrbitConfirmationDialog.ShowAsync(this, L.T("Bỏ nháp và tải lại dữ liệu hiện tại?"), L.T("Tải lại"), MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var row = await service.GetAsync(session, kind, original.Code);
                if (IsDisposed) return;
                if (row is null) { statusLabel.Text = L.T("Bản ghi đã bị xóa. Chọn Hủy để rời."); saveButton.Visible = false; }
                else { Populate(row); errors.Clear(); statusLabel.Text = L.T("Đã tải bản hiện tại; nháp cũ đã bỏ."); }
            }
        }
        catch (UserOperationException ex) { statusLabel.Text = ex.Message; }
        catch (CafeManager.DataAccess.DataAccessException ex) { statusLabel.Text = ex.Error.Message; }
        catch (UnauthorizedAccessException ex) { statusLabel.Text = ex.Message; }
        catch (OperationCanceledException) when (session.IsRevoked) { statusLabel.Text = L.T("Phiên đã kết thúc."); }
        finally { Busy(false); }
    }
    protected override async Task<bool> CanCloseAsync()
    {
        if (session?.IsRevoked == true) return true;
        if (IsWriting) { statusLabel.Text = L.T("Chờ/đối chiếu kết quả ghi trước khi đóng."); return false; }
        if (saved || !HasUnsavedChanges) return true;
        if (await OrbitConfirmationDialog.ShowAsync(this, L.T("Bỏ nháp danh mục?"), L.T("Chưa lưu"), MessageBoxButtons.YesNo) != DialogResult.Yes) return false;
        DiscardChanges(); return true;
    }
    protected override void OnShown(EventArgs e) { base.OnShown(e); layout.FitToContent(this); }
    protected override void Dispose(bool disposing) { if (disposing) errors?.Dispose(); base.Dispose(disposing); }
}




