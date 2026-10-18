using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Requests.Catalogs;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Forms;

public partial class CatalogEditorDialog : Form, IUnsavedChanges
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
        InitializeComponent(); AppTheme.Apply(this); AppTheme.PrimaryButton(saveButton);
        AppTheme.Status(statusLabel);
        statusLabel.Visible = false;
        statusLabel.TextChanged += (_, _) => statusLabel.Visible = !string.IsNullOrWhiteSpace(statusLabel.Text);
        foreach (Control field in fields.Controls) field.Margin = new Padding(4, 8, 4, 8);
        foreach (var input in new[] { codeInput, nameInput, phoneInput, ratioInput, allowanceInput, factorInput }) OrbitField.Wrap(input);
        OrbitFieldLayout.Attach(fields);
        Controls.Add(new OrbitContentHost(layout, 640, fitHeight: true, scrollBody: fields));
        initial = Snapshot();
    }
    public CatalogEditorDialog(CatalogService service, SessionContext session, CatalogKind kind, CatalogRow? row) : this()
    {
        this.service = service; this.session = session; this.kind = kind; original = row;
        Text = row is null ? "Thêm danh mục" : $"Sửa danh mục · {row.Code}";
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
            case ExpenseCategoryRow e: typeInput.SelectedItem = e.Type.ToString(); typeInput.Enabled = !CatalogService.IsSystemExpense(e.Code); break;
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
            CultureInfo.GetCultureInfo("vi-VN"), out var value)) throw new ArgumentException("Nhập số hợp lệ (dấu phẩy thập phân, không dấu phân nhóm).", field);
        return value;
    }
    private CatalogRequest Request() => kind switch
    {
        CatalogKind.ProductCategory => new ProductCategoryRequest(codeInput.Text, nameInput.Text, requestId),
        CatalogKind.Supplier => new SupplierRequest(codeInput.Text, nameInput.Text, phoneInput.Text),
        CatalogKind.PaymentMethod => new PaymentMethodRequest(codeInput.Text, nameInput.Text, supportedInput.Checked),
        CatalogKind.DeliveryPartner => new DeliveryPartnerRequest(codeInput.Text, nameInput.Text, Amount(ratioInput, "Ratio")),
        CatalogKind.ExpenseCategory => new ExpenseCategoryRequest(codeInput.Text, nameInput.Text, Enum.Parse<ExpenseType>(typeInput.Text)),
        CatalogKind.Position => new PositionRequest(codeInput.Text, nameInput.Text, Amount(allowanceInput, "Allowance"), Amount(factorInput, "Factor")),
        _ => new ShiftRequest(codeInput.Text, nameInput.Text, new TimeSpan(startInput.Value.Hour, startInput.Value.Minute, 0), new TimeSpan(endInput.Value.Hour, endInput.Value.Minute, 0))
    };
    private void Busy(bool value)
    {
        writing = value; fields.Enabled = !value && unknown is null;
        saveButton.Enabled = !value && unknown is null; reloadButton.Enabled = !value; cancelButton.Enabled = !value && unknown is null;
    }
    private async void Save_Click(object? sender, EventArgs e)
    {
        if (service is null || session is null || IsWriting) return;
        errors.Clear(); CatalogRequest request;
        try { request = CatalogService.Normalize(Request(), original is null); }
        catch (ArgumentException ex) { Validation(ex); return; }
        Busy(true); OperationStatePresenter.Show(statusLabel, ModuleState.Loading, "Đang lưu danh mục.");
        try
        {
            await service.SaveAsync(session, request, original?.Revision);
            saved = true; writing = false; DialogResult = DialogResult.OK;
        }
        catch (ArgumentException ex) { Validation(ex); }
        catch (UserOperationException ex)
        {
            if (ex.Error.RequiresReconciliation) unknown = request;
            OperationStatePresenter.Show(statusLabel, ex.Error.RequiresReconciliation ? ModuleState.Unknown : ex.Error.Number == 51503 ? ModuleState.Conflict : ModuleState.Error,
                ex.Message + (unknown is null ? "" : " Chọn Kiểm tra / tải lại; không gửi lại."));
        }
        catch (CafeManager.DataAccess.DataAccessException ex) { OperationStatePresenter.Show(statusLabel, ModuleState.Error, ex.Error.Message); }
        catch (UnauthorizedAccessException ex) { statusLabel.Text = ex.Message; }
        catch (OperationCanceledException) when (session.IsRevoked) { statusLabel.Text = "Phiên đã kết thúc."; }
        finally { Busy(false); }
    }
    private void Validation(ArgumentException ex)
    {
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
                if (row is not null && Matches(unknown, row)) { unknown = null; saved = true; writing = false; DialogResult = DialogResult.OK; return; }
                if (row is null && original is null)
                {
                    unknown = null; statusLabel.Text = "Chưa có bản ghi/receipt đã commit. Có thể thử lại đúng mã yêu cầu cũ."; return;
                }
                statusLabel.Text = "Dữ liệu DB khác nháp. Đối chiếu đã xong; chọn Hủy để rời hoặc tải lại bản hiện tại.";
                unknown = null; return;
            }
            if (original is not null && MessageBox.Show(this, "Bỏ nháp và tải lại dữ liệu hiện tại?", "Tải lại", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var row = await service.GetAsync(session, kind, original.Code);
                if (row is null) { statusLabel.Text = "Bản ghi đã bị xóa. Chọn Hủy để rời."; saveButton.Visible = false; }
                else { Populate(row); errors.Clear(); statusLabel.Text = "Đã tải bản hiện tại; nháp cũ đã bỏ."; }
            }
        }
        catch (UserOperationException ex) { statusLabel.Text = ex.Message; }
        catch (CafeManager.DataAccess.DataAccessException ex) { statusLabel.Text = ex.Error.Message; }
        catch (UnauthorizedAccessException ex) { statusLabel.Text = ex.Message; }
        catch (OperationCanceledException) when (session.IsRevoked) { statusLabel.Text = "Phiên đã kết thúc."; }
        finally { Busy(false); }
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (session?.IsRevoked == true) { base.OnFormClosing(e); return; }
        if (IsWriting) { e.Cancel = true; statusLabel.Text = "Chờ/đối chiếu kết quả ghi trước khi đóng."; }
        else if (!saved && HasUnsavedChanges && MessageBox.Show(this, "Bỏ nháp danh mục?", "Chưa lưu", MessageBoxButtons.YesNo) != DialogResult.Yes) e.Cancel = true;
        base.OnFormClosing(e);
    }
    protected override void Dispose(bool disposing) { if (disposing) errors?.Dispose(); base.Dispose(disposing); }
}
