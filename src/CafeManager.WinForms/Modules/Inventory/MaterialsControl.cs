using System.Globalization;
using CafeManager.Business.Security;
using CafeManager.Business.Services;
using CafeManager.Models.Dtos.Inventory;
using CafeManager.Models.Requests.Inventory;
using CafeManager.WinForms.Forms;


namespace CafeManager.WinForms.Modules.Inventory;

public partial class MaterialsControl : UserControl
{
    private readonly MaterialService? service;
    private readonly SessionContext? session;
    private CancellationTokenSource? loading;
    private int page, generation;
    private const int PageSize = 50;
    private List<MaterialRow> snapshot = [];
    private bool loaded, shuttingDown, bindingUnits, hasSnapshot;
    public MaterialsControl() { InitializeComponent(); }
    public MaterialsControl(MaterialService service, SessionContext session) : this()
    {
        this.service = service; this.session = session;
        add.Visible = edit.Visible = remove.Visible = session.Identity.Role is "ADMIN" or "STORE";
        open.Visible = session.Identity.Role == "ADMIN";
        VisibleChanged += async (_, _) => { if (shuttingDown || Disposing || IsDisposed) return; if (!Visible) { loading?.Cancel(); generation++; loaded = false; } else if (!loaded) { loaded = true; await LoadRows(); } };
        Load += async (_, _) => { if (!loaded) { loaded = true; await LoadRows(); } };
    }
    private MaterialRow? Selected => grid.CurrentRow?.DataBoundItem as MaterialRow;
    private async Task LoadRows()
    {
        if (shuttingDown || service is null || session is null) return;
        loading?.Cancel(); loading?.Dispose(); loading = CancellationTokenSource.CreateLinkedTokenSource(session.Token);
        var current = ++generation; state.Text = "Đang tải tồn…"; grid.Enabled = false; add.Enabled = false; previous.Enabled = next.Enabled = false; unitFilter.Enabled = false; empty.Visible = false; UpdateRowActions();
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
                unitFilter.Items.Clear(); unitFilter.Items.Add("Tất cả đơn vị");
                unitFilter.Items.AddRange(snapshot.Select(row => row.Unit).Distinct(StringComparer.Ordinal).OrderBy(unit => unit).Cast<object>().ToArray());
                unitFilter.SelectedIndex = selectedUnit is not null && unitFilter.Items.Contains(selectedUnit) ? unitFilter.Items.IndexOf(selectedUnit) : 0;
            }
            finally { bindingUnits = false; }
            RenderRows(); state.Text = rows.Count == 0 ? "Không tìm thấy nguyên liệu phù hợp." : $"Đã cập nhật {rows[0].ReadAt:HH:mm:ss}";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or ArgumentException) { if (!IsDisposed && current == generation) state.Text = "Chưa cập nhật · " + ex.Message; }
        finally { if (!IsDisposed && current == generation) { grid.Enabled = true; add.Enabled = true; unitFilter.Enabled = true; if (hasSnapshot) UpdatePagination(); else { workspace.Count.Text = "Chưa tải được dữ liệu"; workspace.Pagination.Range.Text = "Chưa có kết quả"; } UpdateRowActions(); } }
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
                foreach (DataGridViewCell cell in row.Cells) cell.ToolTipText = $"{material.Code} · {material.Name}\nMốc mở sổ: {material.OpeningKind ?? "Chưa mở sổ"}";
        workspace.Count.Text = $"{rows.Count} nguyên liệu";
        empty.Visible = rows.Count == 0; empty.BringToFront(); UpdatePagination(); UpdateRowActions();
    }
    private void UpdateRowActions() { edit.Enabled = remove.Enabled = open.Enabled = grid.Enabled && Selected is not null; }
    private async Task Edit(bool creating)
    {
        if (service is null || session is null || (!creating && Selected is null)) return;
        var row = creating ? null : Selected;
        using var dialog = new OperationEditorDialog(creating ? "Thêm nguyên liệu" : "Sửa nguyên liệu", id => service.ReconcileAsync(session, id));
        var code = dialog.AddText("Mã NVL01–NVL99", row?.Code ?? "", row is not null);
        var name = dialog.AddText("Tên nguyên liệu", row?.Name ?? ""); var unit = dialog.AddText("Đơn vị chuẩn", row?.Unit ?? "");
        dialog.CaptureWrite = () => { var request = MaterialService.Normalize(new(code.Text, name.Text, unit.Text)); return id => service.SaveAsync(session, request, row?.Revision, id); };
        if (dialog.ShowDialog(this) == DialogResult.OK) await LoadRows();
    }
    private async Task OpenBalance()
    {
        if (service is null || session is null || Selected is not { } row) return;
        using var dialog = new OperationEditorDialog("Mở sổ nguyên liệu", id => service.ReconcileAsync(session, id));
        dialog.AddNote($"{row.Code} · {row.Name} · {row.Unit}. Mở sổ một lần, chỉ trước khi phát sinh.");
        var quantity = dialog.AddText("Lượng theo đơn vị chuẩn", "0"); var source = dialog.AddText("Nguồn / lý do");
        dialog.CaptureWrite = () =>
        {
            if (!decimal.TryParse(quantity.Text, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out var amount)) throw new ArgumentException("Lượng không hợp lệ. Dùng dấu phẩy cho phần thập phân.");
            MaterialService.Quantity(amount); var request = new MaterialOpeningRequest(row.Code, amount, MaterialService.Required(source.Text, 100, "Nguồn"));
            dialog.Confirmation = $"Ghi tồn đầu kỳ {amount:N4} {row.Unit} cho {row.Code}? Nguồn: {request.Source}. Không thể mở sổ lại.";
            return id => service.OpenAsync(session, request, row.Revision, id);
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) await LoadRows();
    }
    private async Task Delete()
    {
        if (service is null || session is null || Selected is not { } row) return;
        using var dialog = new OperationEditorDialog("Xóa nguyên liệu", id => service.ReconcileAsync(session, id));
        dialog.AddNote($"Xóa {row.Code} · {row.Name}. Chỉ xóa được nguyên liệu chưa được dùng.");
        dialog.Confirmation = $"Xóa nguyên liệu {row.Code}?"; dialog.CaptureWrite = () => id => service.DeleteAsync(session, row, id);
        if (dialog.ShowDialog(this) == DialogResult.OK) await LoadRows();
    }
    protected override void Dispose(bool disposing) { if (disposing && !shuttingDown) { shuttingDown = true; generation++; loading?.Cancel(); loading?.Dispose(); loading = null; hints.Dispose(); } base.Dispose(disposing); }
}





