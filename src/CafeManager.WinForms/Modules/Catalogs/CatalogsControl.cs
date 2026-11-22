using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Catalogs;
using CafeManager.Models.Requests.Catalogs;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Catalogs;

public partial class CatalogsControl : UserControl, IUnsavedChanges
{
    private readonly CatalogService? service;
    private readonly SessionContext? session;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? loading;
    private int generation;
    private int skip, rowCount, dataSkip, lastTab;
    private OrbitListWorkspace management = null!;
    private OrbitRowEditor? inline;
    private CatalogEditorDialog? editor;
    private bool RowBusy => inline?.IsBusy == true;
    private readonly OrbitEmptyState emptyState = new()
    {
        AccessibleName = L.T("Danh mục không có dữ liệu"), Visible = false
    };
    private CatalogKind Kind => (CatalogKind)Math.Max(0, tabs.SelectedIndex);
    public bool HasUnsavedChanges => editor?.HasUnsavedChanges == true;
    public bool IsWriting => editor?.IsWriting == true;
    public void DiscardChanges() => editor?.DiscardChanges();
    public CatalogsControl()
    {
        InitializeComponent();
        allowanceColumn.DefaultCellStyle.Alignment = ratioColumn.DefaultCellStyle.Alignment = factorColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        lastTab = tabs.SelectedIndex; tabs.SelectedIndexChanged += TabChanged; grid.SelectionChanged += (_, _) => UpdateActions();
        searchInput.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Search_Click(searchInput, EventArgs.Empty); } };
        management.Body.SizeChanged += (_, _) => PositionEmpty(); UpdateActions();
    }
    public CatalogsControl(CatalogService service, SessionContext session) : this()
    {
        this.service = service; this.session = session;
        inline = new OrbitRowEditor(grid, management.Body, text => statusLabel.Text = text);
        inline.RowKey = item => ((CatalogRow)item).Code;
        inline.ColumnFor = (item, cell) => Column((CatalogRow)item, cell.OwningColumn?.Name ?? "");
        inline.Save = SaveInline; inline.Delete = DeleteInline; inline.Reconcile = ReconcileInline;
        inline.Reload = RefreshRows; inline.CanWrite = _ => true;
        inline.StateChanged += (_, _) => { if (!IsDisposed) UpdateActions(); };
        Load += async (_, _) => await RefreshRows();
    }
    private async void TabChanged(object? sender, EventArgs e)
    {
        // Chặn đổi tab khi đang ghi/đang tải: trả tab về vị trí cũ để lưới không lệch loại danh mục.
        if (IsWriting || RowBusy || loading is not null)
        {
            if (tabs.SelectedIndex != lastTab) { var keep = lastTab; tabs.SelectedIndexChanged -= TabChanged; try { tabs.SelectedIndex = keep; } finally { tabs.SelectedIndexChanged += TabChanged; } }
            return;
        }
        lastTab = tabs.SelectedIndex;
        inline?.Reset(); grid.DataSource = null; skip = 0; await RefreshRows();
    }
    private void PositionEmpty() => emptyState.SetBounds(0, grid.ColumnHeadersHeight, management.Body.Width, Math.Max(0, management.Body.Height - grid.ColumnHeadersHeight));
    private void UpdateActions()
    {
        var available = loading is null && !IsWriting && !RowBusy && service is not null;
        addButton.Enabled = available;
        tabs.Enabled = searchInput.Enabled = searchButton.Enabled = previousButton.Enabled = nextButton.Enabled = available;
        previousButton.Enabled &= skip > 0; nextButton.Enabled &= rowCount == 50;
        reloadButton.Enabled = available && editor is null;
    }
    private async Task RefreshRows()
    {
        if (service is null || session is null || IsWriting || loading is not null) return;
        var version = ++generation; var kind = Kind;
        using var request = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, session.Token);
        loading = request; UpdateActions(); OperationStatePresenter.Show(statusLabel, ModuleState.Loading, L.T("Danh mục."));
        ShowEmptyState(false);
        try
        {
            var rows = await service.ListAsync(session, kind, searchInput.Text, skip, request.Token);
            if (IsDisposed || version != generation || session.IsRevoked) return;
            grid.DataSource = kind switch
            {
                CatalogKind.ProductCategory => rows.Cast<ProductCategoryRow>().ToList(), CatalogKind.Supplier => rows.Cast<SupplierRow>().ToList(),
                CatalogKind.PaymentMethod => rows.Cast<PaymentMethodRow>().ToList(), CatalogKind.DeliveryPartner => rows.Cast<DeliveryPartnerRow>().ToList(),
                CatalogKind.ExpenseCategory => rows.Cast<ExpenseCategoryRow>().ToList(), CatalogKind.Position => rows.Cast<PositionRow>().ToList(),
                _ => rows.Cast<ShiftRow>().ToList() as object
            };
            phoneColumn.Visible = kind == CatalogKind.Supplier; ratioColumn.Visible = kind == CatalogKind.DeliveryPartner;
            allowanceColumn.Visible = factorColumn.Visible = kind == CatalogKind.Position;
            startColumn.Visible = endColumn.Visible = kind == CatalogKind.Shift;
            typeColumn.Visible = kind == CatalogKind.ExpenseCategory; supportedColumn.Visible = kind == CatalogKind.PaymentMethod;
            OperationStatePresenter.Show(statusLabel, rows.Count == 0 ? ModuleState.Empty : ModuleState.Saved, L.F("Trang {0} · {1} dòng · {2}.", skip / 50 + 1, rows.Count, string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:HH:mm}", DateTime.Now)));
            rowCount=rows.Count;dataSkip=skip;
            var noun=kind switch{CatalogKind.ProductCategory=>L.T("danh mục sản phẩm"),CatalogKind.Supplier=>L.T("nhà cung cấp"),CatalogKind.PaymentMethod=>L.T("phương thức"),CatalogKind.DeliveryPartner=>L.T("đối tác"),CatalogKind.ExpenseCategory=>L.T("loại thu / chi"),CatalogKind.Position=>L.T("chức vụ"),_=>L.T("ca làm")};
            management.Pagination.SetServerPage(skip/50,rows.Count,50,noun);
            ShowEmptyState(rows.Count == 0);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception ex) when (ex is UserOperationException or CafeManager.DataAccess.DataAccessException or UnauthorizedAccessException or ArgumentException)
        {
            if (!IsDisposed && version == generation) OperationStatePresenter.Show(statusLabel, ModuleState.Error,
                ex is CafeManager.DataAccess.DataAccessException d ? d.Error.Message : ex.Message);
            skip=dataSkip;
        }
        finally { if (ReferenceEquals(loading, request)) loading = null; if (!IsDisposed && version == generation) UpdateActions(); }
    }
    private void ShowEmptyState(bool visible)
    {
        if (visible)
        {
            emptyState.Text = skip > 0 ? L.T("Không còn dữ liệu ở trang này.\r\nChọn Trước để quay lại.")
                : string.IsNullOrWhiteSpace(searchInput.Text) ? L.T("Chưa có danh mục.\r\nChọn Thêm để tạo bản ghi đầu tiên.")
                : L.T("Không tìm thấy danh mục.\r\nThay đổi tên tìm kiếm rồi thử lại.");
            PositionEmpty();
            emptyState.BringToFront();
        }
        emptyState.Visible = visible;
        grid.Visible = true;
    }
    private async void Reload_Click(object? sender, EventArgs e)
    {
        if (IsWriting || RowBusy || loading is not null || editor is not null || service is null || session is null) return;
        await RefreshRows();
    }
    private async void Search_Click(object? sender, EventArgs e) { if (loading is not null || IsWriting || RowBusy) return; skip = 0; await RefreshRows(); }
    private async void Previous_Click(object? sender, EventArgs e) { if (loading is not null || IsWriting || RowBusy) return; skip = Math.Max(0, skip - 50); await RefreshRows(); }
    private async void Next_Click(object? sender, EventArgs e) { if (loading is not null || IsWriting || RowBusy) return; skip += 50; await RefreshRows(); }
    private async void Add_Click(object? sender, EventArgs e) => await Edit(null);
    private async Task Edit(CatalogRow? row)
    {
        if (service is null || session is null || IsWriting || RowBusy || loading is not null) return;
        using var dialog = new CatalogEditorDialog(service, session, Kind, row); editor = dialog; UpdateActions();
        var result = await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this); editor = null; UpdateActions();
        if (result == DialogResult.OK) await RefreshRows();
    }
    private static InlineColumn? Column(CatalogRow row, string property) => (row, property) switch
    {
        (_, "Name") => new("Name", InlineCellKind.Text, item => ((CatalogRow)item).Name, (_, text) => text.Trim()),
        (SupplierRow, "Phone") => new("Phone", InlineCellKind.Text, item => ((SupplierRow)item).Phone, (_, text) => text.Trim()),
        (DeliveryPartnerRow, "Ratio") => new("Ratio", InlineCellKind.Decimal, item => ((DeliveryPartnerRow)item).Ratio, (_, text) => Number(text), RightAligned: true),
        (PositionRow, "Allowance") => new("Allowance", InlineCellKind.Money, item => ((PositionRow)item).Allowance, (_, text) => Money(text), RightAligned: true),
        (PositionRow, "Factor") => new("Factor", InlineCellKind.Decimal, item => ((PositionRow)item).Factor, (_, text) => Number(text), RightAligned: true),
        (ShiftRow, "StartTime") => new("StartTime", InlineCellKind.Time, item => ((ShiftRow)item).StartTime, (_, text) => Clock(text)),
        (ShiftRow, "EndTime") => new("EndTime", InlineCellKind.Time, item => ((ShiftRow)item).EndTime, (_, text) => Clock(text)),
        (ExpenseCategoryRow expense, "Type") => new("Type", InlineCellKind.Dropdown,
            item => CafeManager.Models.States.FinanceStates.TypeLabel(((ExpenseCategoryRow)item).Type.ToString()),
            (_, text) => text == L.T("Chi") ? ExpenseType.Chi : ExpenseType.Thu, _ => [L.T("Thu"), L.T("Chi")],
            Editable: _ => !CatalogService.IsSystemExpense(expense.Code)),
        (PaymentMethodRow payment, "Supported") => new("Supported", InlineCellKind.Check,
            item => ((PaymentMethodRow)item).Supported, (_, text) => text == "True",
            Editable: _ => payment.Code != "PT010"),
        _ => null
    };
    // Tiền: dấu chấm là phân nhóm nghìn, dấu phẩy là thập phân; phải nguyên VND.
    private static decimal Money(string text)
    {
        var cleaned = text.Replace("₫", "").Replace(" ", "").Replace(".", "").Replace(',', '.');
        if (!decimal.TryParse(cleaned, global::System.Globalization.NumberStyles.Number, global::System.Globalization.CultureInfo.InvariantCulture, out var value) || decimal.Truncate(value) != value)
            throw new ArgumentException(L.T("Giá trị phải là số nguyên."));
        return value;
    }
    // Hệ số/chiết khấu: có dấu phẩy thì phẩy là thập phân, không thì chấm là thập phân.
    private static decimal Number(string text)
    {
        var cleaned = text.Replace("₫", "").Replace(" ", "");
        if (cleaned.Contains(',')) cleaned = cleaned.Replace(".", "").Replace(',', '.');
        if (!decimal.TryParse(cleaned, global::System.Globalization.NumberStyles.Number, global::System.Globalization.CultureInfo.InvariantCulture, out var value))
            throw new ArgumentException(L.T("Giá trị số không hợp lệ."));
        return value;
    }
    private static TimeSpan Clock(string text)
    {
        if (!TimeSpan.TryParseExact(text.Trim(), [@"hh\:mm", @"h\:mm"], global::System.Globalization.CultureInfo.InvariantCulture, out var time))
            throw new ArgumentException(L.T("Giờ phải theo dạng HH:mm."));
        return time;
    }
    private CatalogRequest Request(CatalogRow row, string property, object? value)
    {
        var name = property == "Name" ? (string)value! : row.Name;
        return row switch
        {
            ProductCategoryRow r => new ProductCategoryRequest(r.Code, name, Guid.Empty),
            SupplierRow r => new SupplierRequest(r.Code, name, property == "Phone" ? (string)value! : r.Phone),
            PaymentMethodRow r => new PaymentMethodRequest(r.Code, name, property == "Supported" ? (bool)value! : r.Supported),
            DeliveryPartnerRow r => new DeliveryPartnerRequest(r.Code, name, property == "Ratio" ? (decimal)value! : r.Ratio),
            ExpenseCategoryRow r => new ExpenseCategoryRequest(r.Code, name, property == "Type" ? (ExpenseType)value! : r.Type),
            PositionRow r => new PositionRequest(r.Code, name, property == "Allowance" ? (decimal)value! : r.Allowance, property == "Factor" ? (decimal)value! : r.Factor),
            ShiftRow r => new ShiftRequest(r.Code, name, property == "StartTime" ? (TimeSpan)value! : r.StartTime, property == "EndTime" ? (TimeSpan)value! : r.EndTime),
            _ => throw new ArgumentException(L.T("Loại danh mục không hợp lệ."))
        };
    }
    private async Task SaveInline(object item, string property, object? value, Guid id, CancellationToken token)
    {
        if (service is null || session is null) return;
        var row = (CatalogRow)item;
        await service.SaveAsync(session, CatalogService.Normalize(Request(row, property, value), false), row.Revision, token);
    }
    private async Task<bool> DeleteInline(object item, Guid id, CancellationToken token)
    {
        if (service is null || session is null) return false;
        var row = (CatalogRow)item; var kind = Kind;
        if (kind == CatalogKind.ExpenseCategory && CatalogService.IsSystemExpense(row.Code)) throw new ArgumentException(L.T("Không được xóa mã Chi hệ thống."));
        var canceled = kind is CatalogKind.Supplier or CatalogKind.ExpenseCategory
            && await OrbitConfirmationDialog.ShowAsync(this, L.F("Xóa {0} · {1}? Danh mục có lịch sử sẽ bị từ chối.", row.Code, row.Name), L.T("Xóa danh mục"), MessageBoxButtons.YesNo, L.T("Xóa"), L.T("Hủy")) != DialogResult.Yes;
        if (canceled) return false;
        await service.DeleteAsync(session, kind, row, token); return true;
    }
    // Catalog không có requestId: đối chiếu bằng bản ghi hiện tại và revision của dòng đã khóa.
    private async Task<bool?> ReconcileInline(object item, Guid id, CancellationToken token)
    {
        if (service is null || session is null) return null;
        var row = (CatalogRow)item;
        var current = await service.GetAsync(session, Kind, row.Code, token);
        return current is null || !current.Revision.SequenceEqual(row.Revision);
    }
    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (grid.Columns[e.ColumnIndex].DataPropertyName == "Type" && e.Value is not null)
        { e.Value = CafeManager.Models.States.FinanceStates.TypeLabel(e.Value.ToString()!); e.FormattingApplied = true; }
        if (grid.Columns[e.ColumnIndex] == allowanceColumn && e.Value is decimal money) { e.Value = money.ToString("N0", global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " ₫"; e.FormattingApplied = true; }
        if ((grid.Columns[e.ColumnIndex] == startColumn || grid.Columns[e.ColumnIndex] == endColumn) && e.Value is TimeSpan time) { e.Value = time.ToString(@"hh\:mm"); e.FormattingApplied = true; }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { ++generation; lifetime.Cancel(); loading?.Cancel(); lifetime.Dispose(); }
        base.Dispose(disposing);
    }
}





