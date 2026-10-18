using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Catalogs;
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
    private int skip;
    private bool writing;
    private CatalogRow? unknownDelete;
    private CatalogEditorDialog? editor;
    private readonly ComboBox catalogPicker = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, AccessibleName = "Chọn danh mục", Visible = false };
    private readonly Panel compactWorkspace = new() { Dock = DockStyle.Fill, Visible = false };
    private bool compactCatalog;
    private readonly Label emptyState = new()
    {
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
        AccessibleName = "Danh mục không có dữ liệu", Visible = false
    };
    private CatalogKind Kind => (CatalogKind)Math.Max(0, tabs.SelectedIndex);
    public bool HasUnsavedChanges => editor?.HasUnsavedChanges == true;
    public bool IsWriting => writing || unknownDelete is not null || editor?.IsWriting == true;
    public void DiscardChanges() => editor?.DiscardChanges();
    public CatalogsControl()
    {
        InitializeComponent(); AppTheme.Apply(this); AppTheme.PrimaryButton(addButton);
        titleLabel.BackColor = subtitleLabel.BackColor = Color.Transparent;
        subtitleLabel.ForeColor = AppTheme.Muted; AppTheme.Status(statusLabel);
        allowanceColumn.DefaultCellStyle.Alignment = ratioColumn.DefaultCellStyle.Alignment = factorColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        tabs.SelectedIndexChanged += TabChanged;
        grid.SelectionChanged += (_, _) => UpdateActions();
        AppTheme.Apply(emptyState); emptyState.ForeColor = AppTheme.Muted;
        emptyState.BackColor = AppTheme.Background;
        catalogPicker.Items.AddRange(tabs.TabPages.Cast<TabPage>().Select(page => (object)page.Text).ToArray());
        catalogPicker.SelectedIndex = tabs.SelectedIndex;
        catalogPicker.SelectedIndexChanged += (_, _) => { if (catalogPicker.SelectedIndex >= 0) tabs.SelectedIndex = catalogPicker.SelectedIndex; };
        tabs.EnabledChanged += (_, _) => catalogPicker.Enabled = tabs.Enabled;
        layout.Controls.Remove(tabs);
        var workspace = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        workspace.Controls.Add(tabs); workspace.Controls.Add(compactWorkspace); workspace.Controls.Add(catalogPicker);
        layout.Controls.Add(workspace, 0, 2);
        AppTheme.Apply(catalogPicker); compactWorkspace.BackColor = AppTheme.Background;
        workspace.SizeChanged += (_, _) =>
        {
            compactCatalog = workspace.Width * 96F / DeviceDpi < 780;
            tabs.Visible = !compactCatalog; catalogPicker.Visible = compactWorkspace.Visible = compactCatalog;
            subtitleLabel.Visible = !compactCatalog;
            heading.Padding = compactCatalog ? new Padding(8, 4, 8, 4) : new Padding(12, 6, 12, 6);
            heading.Margin = compactCatalog ? new Padding(0, 0, 0, 4) : new Padding(0, 0, 0, 8);
            AttachWorkspace();
        };
    }
    public CatalogsControl(CatalogService service, SessionContext session) : this()
    { this.service = service; this.session = session; Load += async (_, _) => await RefreshRows(); }
    private async void TabChanged(object? sender, EventArgs e)
    {
        if (IsWriting || loading is not null) return;
        catalogPicker.SelectedIndex = tabs.SelectedIndex;
        AttachWorkspace(); grid.DataSource = null; skip = 0; await RefreshRows();
    }
    private void AttachWorkspace()
    {
        var parent = compactCatalog ? compactWorkspace : (Control?)tabs.SelectedTab;
        if (parent is null) return;
        if (grid.Parent != parent) parent.Controls.Add(grid);
        if (emptyState.Parent != parent) parent.Controls.Add(emptyState);
        if (emptyState.Visible) emptyState.BringToFront(); else grid.BringToFront();
    }
    private void UpdateActions()
    {
        var available = loading is null && !IsWriting && service is not null;
        addButton.Enabled = available; editButton.Enabled = available && grid.CurrentRow?.DataBoundItem is CatalogRow;
        deleteButton.Enabled = editButton.Enabled && !(Kind == CatalogKind.ExpenseCategory && grid.CurrentRow?.DataBoundItem is CatalogRow r && CatalogService.IsSystemExpense(r.Code));
        tabs.Enabled = searchInput.Enabled = searchButton.Enabled = previousButton.Enabled = nextButton.Enabled = available;
        previousButton.Enabled &= skip > 0;
        reloadButton.Enabled = loading is null && !writing && editor is null && service is not null;
    }
    private async Task RefreshRows()
    {
        if (service is null || session is null || IsWriting || loading is not null) return;
        var version = ++generation; var kind = Kind;
        using var request = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, session.Token);
        loading = request; UpdateActions(); OperationStatePresenter.Show(statusLabel, ModuleState.Loading, "Danh mục.");
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
            OperationStatePresenter.Show(statusLabel, rows.Count == 0 ? ModuleState.Empty : ModuleState.Saved, $"Trang {skip / 50 + 1} · {rows.Count} dòng · {DateTime.Now:HH:mm}.");
            ShowEmptyState(rows.Count == 0);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception ex) when (ex is UserOperationException or CafeManager.DataAccess.DataAccessException or UnauthorizedAccessException or ArgumentException)
        {
            if (!IsDisposed && version == generation) OperationStatePresenter.Show(statusLabel, ModuleState.Error,
                ex is CafeManager.DataAccess.DataAccessException d ? d.Error.Message : ex.Message);
        }
        finally { if (ReferenceEquals(loading, request)) loading = null; if (!IsDisposed && version == generation) UpdateActions(); }
    }
    private void ShowEmptyState(bool visible)
    {
        if (visible)
        {
            emptyState.Text = skip > 0 ? "Không còn dữ liệu ở trang này.\r\nChọn Trước để quay lại."
                : string.IsNullOrWhiteSpace(searchInput.Text) ? "Chưa có danh mục.\r\nChọn Thêm để tạo bản ghi đầu tiên."
                : "Không tìm thấy danh mục.\r\nThay đổi tên tìm kiếm rồi thử lại.";
            var parent = compactCatalog ? compactWorkspace : (Control?)tabs.SelectedTab;
            parent?.Controls.Add(emptyState);
            emptyState.BringToFront();
        }
        emptyState.Visible = visible;
        grid.Visible = !visible;
    }
    private async void Reload_Click(object? sender, EventArgs e)
    {
        if (writing || loading is not null || editor is not null || service is null || session is null) return;
        if (unknownDelete is not null)
        {
            writing = true; UpdateActions();
            try
            {
                var row = await service.GetAsync(session, Kind, unknownDelete.Code);
                statusLabel.Text = row is null ? "Đối chiếu: bản ghi không còn trong DB." : "Đối chiếu: bản ghi vẫn tồn tại. Tải lại trước khi xóa.";
                unknownDelete = null;
            }
            catch (UserOperationException ex) { statusLabel.Text = ex.Message; }
            catch (CafeManager.DataAccess.DataAccessException ex) { statusLabel.Text = ex.Error.Message; }
            catch (UnauthorizedAccessException ex) { statusLabel.Text = ex.Message; }
            catch (OperationCanceledException) when (session.IsRevoked) { }
            finally { writing = false; UpdateActions(); }
        }
        if (unknownDelete is null) await RefreshRows();
    }
    private async void Search_Click(object? sender, EventArgs e) { if (loading is not null || IsWriting) return; skip = 0; await RefreshRows(); }
    private async void Previous_Click(object? sender, EventArgs e) { if (loading is not null || IsWriting) return; skip = Math.Max(0, skip - 50); await RefreshRows(); }
    private async void Next_Click(object? sender, EventArgs e) { if (loading is not null || IsWriting) return; skip += 50; await RefreshRows(); }
    private async void Add_Click(object? sender, EventArgs e) => await Edit(null);
    private async void Edit_Click(object? sender, EventArgs e) { if (grid.CurrentRow?.DataBoundItem is CatalogRow row) await Edit(row); }
    private async Task Edit(CatalogRow? row)
    {
        if (service is null || session is null || IsWriting || loading is not null) return;
        using var dialog = new CatalogEditorDialog(service, session, Kind, row); editor = dialog; UpdateActions();
        var result = dialog.ShowDialog(FindForm()); editor = null; UpdateActions();
        if (result == DialogResult.OK) await RefreshRows();
    }
    private async void Delete_Click(object? sender, EventArgs e)
    {
        if (service is null || session is null || IsWriting || loading is not null || grid.CurrentRow?.DataBoundItem is not CatalogRow row) return;
        if (MessageBox.Show(this, $"Xóa {row.Code} · {row.Name}? Danh mục có lịch sử hoặc mã hệ thống sẽ bị từ chối.", "Xóa danh mục", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        writing = true; UpdateActions();
        var deleted = false;
        try { await service.DeleteAsync(session, Kind, row); deleted = true; statusLabel.Text = "Đã xóa danh mục."; }
        catch (UserOperationException ex) { if (ex.Error.RequiresReconciliation) unknownDelete = row; OperationStatePresenter.Show(statusLabel, ex.Error.RequiresReconciliation ? ModuleState.Unknown : ex.Error.Number == 51503 ? ModuleState.Conflict : ModuleState.Error, ex.Message); }
        catch (CafeManager.DataAccess.DataAccessException ex) { statusLabel.Text = ex.Error.Message; }
        catch (Exception ex) when (ex is UnauthorizedAccessException or ArgumentException) { statusLabel.Text = ex.Message; }
        catch (OperationCanceledException) when (session.IsRevoked) { }
        finally { writing = false; UpdateActions(); }
        if (deleted) await RefreshRows();
    }
    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (grid.Columns[e.ColumnIndex] == allowanceColumn && e.Value is decimal money) { e.Value = money.ToString("N0", global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " ₫"; e.FormattingApplied = true; }
        if ((grid.Columns[e.ColumnIndex] == startColumn || grid.Columns[e.ColumnIndex] == endColumn) && e.Value is TimeSpan time) { e.Value = time.ToString(@"hh\:mm"); e.FormattingApplied = true; }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { ++generation; lifetime.Cancel(); loading?.Cancel(); lifetime.Dispose(); }
        base.Dispose(disposing);
    }
}
