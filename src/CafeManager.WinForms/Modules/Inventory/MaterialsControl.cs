using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
using CafeManager.WinForms.Forms;
using CafeManager.WinForms.UI;


namespace CafeManager.WinForms.Modules.Inventory;

public partial class MaterialsControl : UserControl
{
    private readonly MaterialService? service;
    private readonly SessionContext? session;
    private CancellationTokenSource? loading;
    private OrbitRowEditor? inline;
    private int page, generation;
    private const int PageSize = 50;
    private List<MaterialRow> snapshot = [];
    private bool loaded, shuttingDown, bindingUnits, hasSnapshot;
    public MaterialsControl() { InitializeComponent(); }
    public MaterialsControl(MaterialService service, SessionContext session) : this()
    {
        this.service = service; this.session = session;
        var writable = session.Identity.Role is "ADMIN" or "STORE";
        add.Visible = writable;
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Delete", HeaderText = "" });
        workspace.ShowMeta = false;
        inline = new OrbitRowEditor(grid, workspace.Body, text => state.Text = text);
        inline.RowKey = item => ((MaterialRow)item).Code;
        inline.ColumnFor = (item, cell) => Column(cell.OwningColumn?.Name ?? "");
        inline.Edit = item => Edit((MaterialRow)item); inline.Delete = DeleteInline;
        inline.Reconcile = async (_, id, t) => await service.ReconcileAsync(session, id, t) is not null;
        inline.Reload = LoadRows; inline.CanWrite = _ => writable;
        inline.StateChanged += (_, _) => { if (IsDisposed) return; add.Enabled = !inline.IsBusy && grid.Enabled; };
        VisibleChanged += async (_, _) => { if (shuttingDown || Disposing || IsDisposed) return; if (!Visible) { loading?.Cancel(); generation++; loaded = false; inline?.Reset(); } else if (!loaded) { loaded = true; await LoadRows(); } };
        Load += async (_, _) => { if (!loaded) { loaded = true; await LoadRows(); } };
    }
    private MaterialRow? Selected => grid.CurrentRow?.DataBoundItem as MaterialRow;
    private static InlineColumn? Column(string property) => property switch
    {
        "Name" => new("Name", InlineCellKind.Text),
        "Unit" => new("Unit", InlineCellKind.Text),
        _ => null
    };
    private async Task Edit(MaterialRow row)
    {
        if (service is null || session is null) return;
        using var dialog = new OperationEditorDialog(L.F("Sửa nguyên liệu · {0}", row.Code), id => service.ReconcileAsync(session, id));
        var name = dialog.AddText(L.T("Tên nguyên liệu"), row.Name);
        var unit = dialog.AddText(L.T("Đơn vị chuẩn"), row.Unit);
        dialog.CaptureWrite = () =>
        {
            var request = MaterialService.Normalize(new(row.Code, name.Text, unit.Text, null, null));
            return id => service.SaveAsync(session, request, row.Revision, id);
        };
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) == DialogResult.OK) await LoadRows();
    }
    private async Task<bool> DeleteInline(object item, Guid id, CancellationToken token)
    {
        if (service is null || session is null) return false;
        var row = (MaterialRow)item;
        if (await OrbitConfirmationDialog.ShowAsync(this, L.F("Xóa {0} · {1}? Nguyên liệu đã dùng sẽ bị chặn.", row.Code, row.Name), L.T("Xóa nguyên liệu"), MessageBoxButtons.YesNo, L.T("Xóa"), L.T("Hủy")) != DialogResult.Yes) return false;
        await service.DeleteAsync(session, row, id, token); return true;
    }
    private async Task LoadRows()
    {
        if (shuttingDown || service is null || session is null) return;
        loading?.Cancel(); loading?.Dispose(); loading = CancellationTokenSource.CreateLinkedTokenSource(session.Token);
        var current = ++generation; grid.Enabled = false; add.Enabled = false; previous.Enabled = next.Enabled = false; unitFilter.Enabled = false; empty.Visible = false;
        try
        {
            // Contract vẫn trả batch50. Chỉ phân trang/lọc snapshot đủ ở lớp trình bày.
            var rows = new List<MaterialRow>(); var term = search.Text.Trim();
            for (var skip = 0; ; skip += 50)
            {
                var batch = await service.ListAsync(session, term, skip, loading.Token);
                if (IsDisposed || current != generation) return;
                rows.AddRange(batch); if (batch.Count < 50) break;
            }
            if (IsDisposed || current != generation) return;
            snapshot = rows; hasSnapshot = true; var selectedUnit = unitFilter.SelectedItem as string; bindingUnits = true;
            try
            {
                unitFilter.Items.Clear(); unitFilter.Items.Add(L.T("Tất cả đơn vị"));
                unitFilter.Items.AddRange(snapshot.Select(row => row.Unit).Distinct(StringComparer.Ordinal).OrderBy(unit => unit).Cast<object>().ToArray());
                unitFilter.SelectedIndex = selectedUnit is not null && unitFilter.Items.Contains(selectedUnit) ? unitFilter.Items.IndexOf(selectedUnit) : 0;
            }
            finally { bindingUnits = false; }
            RenderRows(); state.Text = rows.Count == 0 ? L.T("Không tìm thấy nguyên liệu phù hợp.") : L.F("Đã cập nhật {0}", string.Format(global::System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:HH:mm:ss}", rows[0].ReadAt));
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or ArgumentException) { if (!IsDisposed && current == generation) state.Text = L.T("Chưa cập nhật · ") + ex.Message; }
        finally { if (!IsDisposed && current == generation) { grid.Enabled = true; add.Enabled = true; unitFilter.Enabled = true; if (hasSnapshot) UpdatePagination(); else { workspace.Count.Text = L.T("Chưa tải được dữ liệu"); workspace.Pagination.Range.Text = L.T("Chưa có kết quả"); } } }
    }
    private List<MaterialRow> FilteredRows() => unitFilter.SelectedIndex > 0 ? snapshot.Where(row => row.Unit == unitFilter.SelectedItem as string).ToList() : snapshot;
    private void UpdatePagination() => workspace.Pagination.SetPage(page, PageSize, FilteredRows().Count);
    private void RenderRows()
    {
        var selectedCode = Selected?.Code;
        var rows = FilteredRows(); page = Math.Clamp(page, 0, Math.Max(0, (rows.Count - 1) / PageSize));
        grid.DataSource = rows.Skip(page * PageSize).Take(PageSize).ToList();
        var selectedRow = grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(row => (row.DataBoundItem as MaterialRow)?.Code == selectedCode);
        if (selectedRow is not null) grid.CurrentCell = selectedRow.Cells[0];
        foreach (DataGridViewRow row in grid.Rows)
            if (row.DataBoundItem is MaterialRow material)
                foreach (DataGridViewCell cell in row.Cells) cell.ToolTipText = L.F("{0} · {1}\nMốc tồn đầu: {2}", material.Code, material.Name, material.OpeningKind ?? L.T("Chưa có mốc tồn đầu"));
        workspace.Count.Text = L.F("{0} nguyên liệu", rows.Count);
        empty.Visible = rows.Count == 0; empty.BringToFront(); UpdatePagination();
    }
    private async Task Create()
    {
        if (service is null || session is null) return;
        using var dialog = new OperationEditorDialog(L.T("Thêm nguyên liệu"), id => service.ReconcileAsync(session, id));
        var name = dialog.AddText(L.T("Tên nguyên liệu"), ""); var unit = dialog.AddText(L.T("Đơn vị chuẩn"), "");
        dialog.AddNote(L.T("Tồn đầu kỳ là tùy chọn, chỉ nhập khi nguyên liệu đã có sẵn. Hàng mua sau đó nhập bằng phiếu nhập."));
        var opening = dialog.AddText(L.T("Lượng tồn đầu (tùy chọn)"), "");
        var source = dialog.AddText(L.T("Nguồn tồn đầu"), "");
        dialog.CaptureWrite = () =>
        {
            decimal? amount = null;
            dialog.Confirmation = null;
            if (!string.IsNullOrWhiteSpace(opening.Text))
            {
                if (!decimal.TryParse(opening.Text, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out var parsed)) throw new ArgumentException(L.T("Lượng không hợp lệ. Dùng dấu phẩy cho phần thập phân."));
                MaterialService.Quantity(parsed); amount = parsed;
                if (amount > 0)
                {
                    var origin = source.Text.Trim();
                    if (origin.Length == 0) throw new ArgumentException(L.T("Cần nguồn cho tồn đầu."), L.T("Nguồn tồn đầu"));
                    dialog.Confirmation = L.F("Tạo nguyên liệu với tồn đầu {0} · nguồn {1}? Không khai lại tồn đầu sau khi phát sinh.",
                        parsed.ToString("N4", CultureInfo.GetCultureInfo("vi-VN")), origin);
                }
            }
            var request = MaterialService.Normalize(new("", name.Text, unit.Text, amount, amount is not null ? source.Text : null), true);
            return id => service.SaveAsync(session, request, null, id);
        };
        if (await CafeManager.WinForms.UI.OrbitDialogHost.ShowAsync(dialog, this) == DialogResult.OK) await LoadRows();
    }
    protected override void Dispose(bool disposing) { if (disposing && !shuttingDown) { shuttingDown = true; generation++; loading?.Cancel(); loading?.Dispose(); loading = null; inline?.Reset(); hints.Dispose(); } base.Dispose(disposing); }
}







