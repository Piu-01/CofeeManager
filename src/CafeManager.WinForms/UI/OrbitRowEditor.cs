using CafeManager.Business.Services;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Mô tả một cột sửa được: dùng cho hover/quyền và mở hộp sửa. Không còn nhập tại ô.
public sealed record InlineColumn(
    string Property,
    InlineCellKind Kind,
    Func<object, bool>? Editable = null);

// Engine dùng chung cho master data: double-click/F2 mở hộp sửa, thùng rác xóa đúng dòng.
// Chỉ "kết quả chưa xác định" mới khóa dòng và hiện nút Đối chiếu.
public sealed class OrbitRowEditor
{
    private readonly OrbitDataGridView grid;
    private readonly Control host;
    private readonly Action<string> status;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<string, Guid> unknown = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OrbitButton> reconcileButtons = new(StringComparer.Ordinal);
    private bool sending;
    private bool activating;

    public OrbitRowEditor(OrbitDataGridView grid, Control host, Action<string> status)
    {
        this.grid = grid; this.host = host; this.status = status;
        grid.CellMouseClick += OnCellClick;
        grid.CellDoubleClick += OnCellDoubleClick;
        grid.KeyDown += OnGridKeyDown;
        grid.DataBindingComplete += (_, _) => SyncButtons();
        host.SizeChanged += (_, _) => SyncButtons();
        grid.InlineEditable = EditableCell;
        grid.InlineCursor = CursorFor;
        grid.RowBlocked = IsBlockedRow;
    }

    public Func<object, string> RowKey { get; set; } = static _ => "";
    public Func<object, DataGridViewCell, InlineColumn?> ColumnFor { get; set; } = static (_, _) => null;
    // Mở hộp sửa của màn; modal tự chặn lưới, engine không giữ RowWriteBusy trong lúc mở.
    public Func<object, Task>? Edit { get; set; }
    // Trả false khi người dùng hủy xác nhận; lỗi/unknown phát qua exception như service hiện có.
    public Func<object, Guid, CancellationToken, Task<bool>> Delete { get; set; } = static (_, _, _) => Task.FromResult(false);
    public Func<object, Guid, CancellationToken, Task<bool?>> Reconcile { get; set; } = static (_, _, _) => Task.FromResult<bool?>(null);
    public Func<Task> Reload { get; set; } = static () => Task.CompletedTask;
    public Func<object, bool> CanWrite { get; set; } = static _ => true;
    public CancellationToken Token { get; set; } = CancellationToken.None;

    public bool IsBusy => sending;
    public bool HasUnknown => unknown.Count > 0;
    public event EventHandler? StateChanged;

    // Đổi tab/đóng màn: bỏ trạng thái khóa cũ, không ghi gì.
    public void Reset()
    {
        foreach (var button in reconcileButtons.Values) button.Dispose();
        reconcileButtons.Clear(); unknown.Clear();
        grid.Invalidate(); UpdateState();
    }

    private async Task RunAsync(Func<Task> action)
    {
        await gate.WaitAsync();
        try { await action(); }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or ArgumentException) { if (!grid.IsDisposed) status(ex.Message); }
        finally { gate.Release(); }
    }

    private async void OnCellClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (!grid.Enabled || e.Button != MouseButtons.Left || e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (!string.Equals(grid.Columns[e.ColumnIndex].Name, "Delete", StringComparison.Ordinal)) return;
        await RunAsync(() => DeleteRowAsync(e.RowIndex));
    }

    private async void OnCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (!grid.Enabled || e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (string.Equals(grid.Columns[e.ColumnIndex].Name, "Delete", StringComparison.Ordinal)) return;
        if (sending || activating || Edit is null) return;
        var item = grid.Rows[e.RowIndex].DataBoundItem; if (item is null) return;
        if (unknown.ContainsKey(RowKey(item)) || !CanWrite(item) || !HasEditableColumn(item, e.RowIndex)) return;
        await ActivateEditAsync(item);
    }

    private async void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.F2) return;
        e.SuppressKeyPress = true;
        if (sending || activating || Edit is null || !grid.Enabled) return;
        var rowIndex = grid.CurrentCell?.RowIndex ?? -1;
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
        var item = grid.Rows[rowIndex].DataBoundItem; if (item is null) return;
        if (unknown.ContainsKey(RowKey(item)) || !CanWrite(item)) return;
        await ActivateEditAsync(item);
    }

    private async Task ActivateEditAsync(object item)
    {
        activating = true; UpdateState();
        try
        {
            var rowIndex = FindRowIndex(RowKey(item));
            if (rowIndex >= 0) grid.CurrentCell = grid.Rows[rowIndex].Cells[0];
            await Edit!(item);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or ArgumentException) { if (!grid.IsDisposed) status(ex.Message); }
        finally { activating = false; if (!grid.IsDisposed) { SyncButtons(); UpdateState(); } }
    }

    private bool HasEditableColumn(object item, int rowIndex)
    {
        for (var i = 0; i < grid.Columns.Count; i++)
        {
            if (string.Equals(grid.Columns[i].Name, "Delete", StringComparison.Ordinal)) continue;
            var column = ColumnFor(item, grid.Rows[rowIndex].Cells[i]);
            if (column is not null && (column.Editable?.Invoke(item) ?? true)) return true;
        }
        return false;
    }

    private bool EditableCell(DataGridViewCell cell)
    {
        if (cell.RowIndex < 0 || cell.ColumnIndex < 0) return false;
        var item = grid.Rows[cell.RowIndex].DataBoundItem; if (item is null) return false;
        if (unknown.ContainsKey(RowKey(item))) return false;
        var column = ColumnFor(item, cell);
        return column is not null && (column.Editable?.Invoke(item) ?? true) && CanWrite(item);
    }

    private Cursor? CursorFor(DataGridViewCell cell) => EditableCell(cell) ? Cursors.Hand : null;

    private async Task DeleteRowAsync(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
        var clicked = grid.Rows[rowIndex].DataBoundItem; if (clicked is null) return;
        var clickedKey = RowKey(clicked);
        rowIndex = FindRowIndex(clickedKey);
        if (rowIndex < 0) return;
        var item = grid.Rows[rowIndex].DataBoundItem; if (item is null) return;
        var key = RowKey(item);
        if (unknown.ContainsKey(key)) { status(L.T("Dòng đang chờ đối chiếu.")); return; }
        if (!CanWrite(item)) return;
        var requestId = Guid.NewGuid();
        sending = true; UpdateState(); status(L.T("Đang xóa…"));
        try
        {
            if (!await Delete(item, requestId, Token)) { status(L.T("Đã hủy xóa.")); return; }
            status(L.F("Đã xóa {0}.", key));
            await ReloadAndSelect(null);
        }
        catch (UserOperationException ex) when (ex.Error.RequiresReconciliation) { Lock(item, requestId, ex.Message); }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or ArgumentException) { status(ex.Message); }
        finally { sending = false; if (!grid.IsDisposed) UpdateState(); }
    }

    private async Task ReconcileAsync(string key)
    {
        if (!unknown.TryGetValue(key, out var requestId)) return;
        var rowIndex = FindRowIndex(key);
        if (rowIndex < 0) { unknown.Remove(key); RemoveButton(key); grid.Invalidate(); return; }
        var item = grid.Rows[rowIndex].DataBoundItem; if (item is null) return;
        sending = true; UpdateState(); status(L.T("Đang đối chiếu…"));
        try
        {
            var result = await Reconcile(item, requestId, Token);
            if (result is null) { status(L.T("Không xác định được kết quả đối chiếu.")); return; }
            unknown.Remove(key); RemoveButton(key); grid.Invalidate();
            await ReloadAndSelect(key);
            status(result.Value ? L.T("Đã đối chiếu: đã ghi.") : L.T("Đối chiếu: chưa ghi; có thể sửa lại."));
        }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or ArgumentException) { status(ex.Message); }
        finally { sending = false; if (!grid.IsDisposed) UpdateState(); SyncButtons(); }
    }

    private async Task ReloadAndSelect(string? key)
    {
        await Reload();
        if (key is not null) { var index = FindRowIndex(key); if (index >= 0) grid.CurrentCell = grid.Rows[index].Cells[0]; }
        SyncButtons();
    }

    private void Lock(object item, Guid requestId, string message)
    {
        var key = RowKey(item);
        unknown[key] = requestId;
        status(L.F("{0} · Mã yêu cầu {1}. Bấm Đối chiếu trên dòng.", message, requestId));
        EnsureButton(key); grid.Invalidate(); SyncButtons(); UpdateState();
    }

    private void EnsureButton(string key)
    {
        if (reconcileButtons.ContainsKey(key)) return;
        var button = new OrbitButton { Text = L.T("Đối chiếu"), AccessibleName = L.T("Đối chiếu") };
        OrbitManagementTheme.Button(button, ManagementButtonStyle.Action, "");
        button.Click += async (_, _) => { if (button.Enabled) await RunAsync(() => ReconcileAsync(key)); };
        host.Controls.Add(button); reconcileButtons[key] = button;
    }

    private void RemoveButton(string key)
    {
        if (!reconcileButtons.Remove(key, out var button)) return;
        host.Controls.Remove(button); button.Dispose();
    }

    private void SyncButtons()
    {
        foreach (var (key, button) in reconcileButtons.ToArray())
        {
            var rowIndex = FindRowIndex(key);
            if (rowIndex < 0) { button.Visible = false; continue; }
            var row = grid.GetRowDisplayRectangle(rowIndex, true);
            if (row.Width <= 0 || row.Height <= 0 || row.Bottom <= grid.ColumnHeadersHeight || row.Top >= grid.ClientSize.Height) { button.Visible = false; continue; }
            button.Size = button.GetPreferredSize(Size.Empty);
            var point = host.PointToClient(grid.PointToScreen(new Point(row.Left + Math.Max(0, (row.Width - button.Width) / 2), row.Top + Math.Max(0, (row.Height - button.Height) / 2))));
            button.Location = point; button.Visible = true; button.BringToFront();
        }
    }

    private bool IsBlockedRow(int rowIndex) => rowIndex >= 0 && rowIndex < grid.Rows.Count
        && grid.Rows[rowIndex].DataBoundItem is { } item && unknown.ContainsKey(RowKey(item));

    private int FindRowIndex(string key)
    {
        for (var i = 0; i < grid.Rows.Count; i++)
            if (grid.Rows[i].DataBoundItem is { } item && string.Equals(RowKey(item), key, StringComparison.Ordinal)) return i;
        return -1;
    }

    private void UpdateState() => StateChanged?.Invoke(this, EventArgs.Empty);
}
