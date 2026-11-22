using CafeManager.Business.Services;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Mô tả một cột sửa được tại ô. Màn cấp Read/Parse/Options và gọi service trong Save/Delete.
public sealed record InlineColumn(
    string Property,
    InlineCellKind Kind,
    Func<object, object?> Read,
    Func<object, string, object?> Parse,
    Func<object, IReadOnlyList<string>?>? Options = null,
    bool RightAligned = false,
    Func<object, bool>? Editable = null);

// Engine dùng chung cho master data: click ô mở editor overlay, Enter/Tab ghi, Esc hủy,
// thùng rác xóa đúng dòng. Chỉ "kết quả chưa xác định" mới khóa dòng và hiện nút Đối chiếu.
public sealed class OrbitRowEditor
{
    private readonly OrbitDataGridView grid;
    private readonly Control host;
    private readonly Action<string> status;
    private readonly OrbitCellEditor editor = new();
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<string, Guid> unknown = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OrbitButton> reconcileButtons = new(StringComparer.Ordinal);
    private string? activeKey;
    private string? activeProperty;
    // Dòng đã nhận click chọn; click kế tiếp vào ô sửa được mới mở editor.
    private string? armedKey;
    private bool sending;

    public OrbitRowEditor(OrbitDataGridView grid, Control host, Action<string> status)
    {
        this.grid = grid; this.host = host; this.status = status;
        host.Controls.Add(editor);
        grid.CellMouseClick += OnCellClick;
        grid.KeyDown += OnGridKeyDown;
        grid.Scroll += (_, _) => Reposition();
        grid.SizeChanged += (_, _) => Reposition();
        grid.DataBindingComplete += (_, _) => { if (armedKey is not null && FindRowIndex(armedKey) < 0) armedKey = null; SyncButtons(); PositionEditor(); };
        host.SizeChanged += (_, _) => { PositionEditor(); SyncButtons(); };
        editor.MoveRequested += move => _ = RunAsync(() => CommitAsync(move));
        editor.CommitRequested += (_, _) => _ = RunAsync(() => CommitAsync(InlineMove.None));
        editor.CancelRequested += (_, _) => { CloseEditor(); UpdateState(); };
        grid.InlineEditable = EditableCell;
        grid.InlineCursor = CursorFor;
        grid.RowBlocked = IsBlockedRow;
    }

    public Func<object, string> RowKey { get; set; } = static _ => "";
    public Func<object, DataGridViewCell, InlineColumn?> ColumnFor { get; set; } = static (_, _) => null;
    public Func<object, string, object?, Guid, CancellationToken, Task> Save { get; set; } = static (_, _, _, _, _) => Task.CompletedTask;
    // Trả false khi người dùng hủy xác nhận; lỗi/unknown phát qua exception như service hiện có.
    public Func<object, Guid, CancellationToken, Task<bool>> Delete { get; set; } = static (_, _, _) => Task.FromResult(false);
    public Func<object, Guid, CancellationToken, Task<bool?>> Reconcile { get; set; } = static (_, _, _) => Task.FromResult<bool?>(null);
    public Func<Task> Reload { get; set; } = static () => Task.CompletedTask;
    public Func<object, bool> CanWrite { get; set; } = static _ => true;
    public CancellationToken Token { get; set; } = CancellationToken.None;

    public bool IsBusy => sending;
    public bool HasUnknown => unknown.Count > 0;
    public event EventHandler? StateChanged;

    // Đổi tab/đóng màn: bỏ editor và trạng thái khóa cũ, không ghi gì.
    public void Reset()
    {
        CloseEditor();
        foreach (var button in reconcileButtons.Values) button.Dispose();
        reconcileButtons.Clear(); unknown.Clear();
        grid.Invalidate(); UpdateState();
    }

    public void CloseEditor()
    {
        activeKey = null; activeProperty = null;
        editor.HideEditor();
    }

    private async Task RunAsync(Func<Task> action)
    {
        await gate.WaitAsync();
        try { await action(); }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or ArgumentException) { if (!grid.IsDisposed) status(ex.Message); }
        finally { gate.Release(); editor.EndCommit(); }
    }

    private async void OnCellClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (!grid.Enabled || e.Button != MouseButtons.Left || e.RowIndex < 0 || e.ColumnIndex < 0) return;
        var delete = string.Equals(grid.Columns[e.ColumnIndex].Name, "Delete", StringComparison.Ordinal);
        var item = grid.Rows[e.RowIndex].DataBoundItem;
        var targetKey = item is null ? null : RowKey(item);
        var targetProperty = grid.Columns[e.ColumnIndex].Name;
        if (delete) { await RunAsync(() => DeleteRowAsync(e.RowIndex)); return; }
        // Click đầu chọn cả dòng; chỉ click kế tiếp trên chính dòng đó mới sửa ô.
        if (targetKey is null || !string.Equals(armedKey, targetKey, StringComparison.Ordinal))
        {
            armedKey = targetKey;
            await RunAsync(CommitOpenEditorAsync);
            return;
        }
        await RunAsync(() => OpenByKeysAsync(targetKey, targetProperty));
    }

    private async void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.F2) return;
        e.SuppressKeyPress = true;
        if (sending || !grid.Enabled) return;
        var row = grid.CurrentCell?.RowIndex ?? -1; var column = grid.CurrentCell?.ColumnIndex ?? -1;
        await RunAsync(() => OpenAsync(row, column));
    }

    private bool EditableCell(DataGridViewCell cell)
    {
        if (cell.RowIndex < 0 || cell.ColumnIndex < 0) return false;
        var item = grid.Rows[cell.RowIndex].DataBoundItem; if (item is null) return false;
        if (unknown.ContainsKey(RowKey(item))) return false;
        var column = ColumnFor(item, cell);
        return column is not null && (column.Editable?.Invoke(item) ?? true) && CanWrite(item);
    }

    private Cursor? CursorFor(DataGridViewCell cell)
    {
        if (!EditableCell(cell)) return null;
        var item = grid.Rows[cell.RowIndex].DataBoundItem!;
        return ColumnFor(item, cell)?.Kind switch
        {
            InlineCellKind.Dropdown or InlineCellKind.Date or InlineCellKind.Time or InlineCellKind.Check => Cursors.Hand,
            _ => Cursors.IBeam
        };
    }

    private async Task CommitOpenEditorAsync()
    {
        if (!editor.Visible) return;
        if (editor.CommitFailed) { CloseEditor(); return; }
        if (await CommitAsync(InlineMove.None)) return;
        if (editor.Visible && editor.CommitFailed) CloseEditor();
    }

    private async Task OpenByKeysAsync(string? targetKey, string targetProperty)
    {
        if (editor.Visible && editor.CommitFailed) CloseEditor();
        else if (editor.Visible && !await CommitAsync(InlineMove.None) && editor.Visible)
        {
            if (!editor.CommitFailed) return;
            CloseEditor();
        }
        if (targetKey is null) return;
        var rowIndex = FindRowIndex(targetKey); var columnIndex = ColumnIndex(targetProperty);
        if (rowIndex < 0 || columnIndex < 0) return;
        await OpenAsync(rowIndex, columnIndex);
    }

    private async Task OpenAsync(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count || columnIndex < 0 || columnIndex >= grid.Columns.Count) return;
        var item = grid.Rows[rowIndex].DataBoundItem; if (item is null) return;
        if (unknown.ContainsKey(RowKey(item))) return;
        var cell = grid.Rows[rowIndex].Cells[columnIndex];
        var column = ColumnFor(item, cell);
        if (column is null || !(column.Editable?.Invoke(item) ?? true) || !CanWrite(item)) { grid.CurrentCell = grid.Rows[rowIndex].Cells[0]; return; }
        if (column.Kind == InlineCellKind.Check) { await WriteAsync(item, column, column.Read(item) is not true, InlineMove.None); return; }
        activeKey = RowKey(item); activeProperty = column.Property;
        editor.Begin(column.Kind, Display(column.Kind, column.Read(item)), column.Options?.Invoke(item), column.RightAligned);
        PositionEditor();
        if (!editor.Visible) { CloseEditor(); return; }
        editor.BringToFront(); editor.FocusInput();
    }

    private async Task<bool> CommitAsync(InlineMove move)
    {
        if (!editor.Visible || activeKey is null || activeProperty is null) return true;
        var rowIndex = FindRowIndex(activeKey);
        if (rowIndex < 0) { CloseEditor(); return true; }
        var item = grid.Rows[rowIndex].DataBoundItem; if (item is null) { CloseEditor(); return true; }
        var columnIndex = ColumnIndex(activeProperty); if (columnIndex < 0) { CloseEditor(); return true; }
        var column = ColumnFor(item, grid.Rows[rowIndex].Cells[columnIndex]);
        if (column is null) { CloseEditor(); return true; }
        object? value;
        try { value = column.Parse(item, editor.Text); }
        catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException or UserOperationException)
        {
            editor.FailCommit();
            status(ex.Message); editor.FocusInput(); return false;
        }
        if (Equals(value, column.Read(item))) { CloseEditor(); UpdateState(); return true; }
        var property = column.Property;
        var written = await WriteAsync(item, column, value, move);
        if (written && move != InlineMove.None) await OpenNeighbourAsync(RowKey(item), property, move);
        return written;
    }

    private async Task<bool> WriteAsync(object item, InlineColumn column, object? value, InlineMove move)
    {
        var key = RowKey(item);
        var requestId = Guid.NewGuid();
        sending = true; UpdateState(); status(L.T("Đang ghi…"));
        try
        {
            await Save(item, column.Property, value, requestId, Token);
            CloseEditor();
            status(L.F("Đã ghi {0}.", key));
            await ReloadAndSelect(key);
            return true;
        }
        catch (UserOperationException ex) when (ex.Error.RequiresReconciliation) { Lock(item, requestId, ex.Message); return false; }
        catch (OperationCanceledException) { editor.FailCommit(); status(L.T("Đã hủy.")); if (editor.Visible) editor.FocusInput(); return false; }
        catch (Exception ex) when (ex is UserOperationException or UnauthorizedAccessException or ArgumentException)
        {
            editor.FailCommit();
            status(ex.Message); if (editor.Visible) editor.FocusInput(); return false;
        }
        finally { sending = false; if (!grid.IsDisposed) UpdateState(); }
    }

    private async Task DeleteRowAsync(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
        var clicked = grid.Rows[rowIndex].DataBoundItem; if (clicked is null) return;
        var clickedKey = RowKey(clicked);
        if (editor.Visible && editor.CommitFailed) CloseEditor();
        else if (editor.Visible && !await CommitAsync(InlineMove.None) && editor.Visible)
        {
            if (!editor.CommitFailed) return;
            CloseEditor();
        }
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
        SyncButtons(); PositionEditor();
    }

    private async Task OpenNeighbourAsync(string key, string property, InlineMove move)
    {
        var rowIndex = FindRowIndex(key); if (rowIndex < 0) return;
        var columnIndex = ColumnIndex(property); if (columnIndex < 0) return;
        var targetRow = move == InlineMove.Down ? rowIndex + 1 : rowIndex;
        if (targetRow >= grid.Rows.Count) return;
        var targetColumn = columnIndex;
        if (move is InlineMove.Right or InlineMove.Left)
        {
            var step = move == InlineMove.Right ? 1 : -1;
            for (var candidate = columnIndex + step; candidate >= 0 && candidate < grid.Columns.Count; candidate += step)
            {
                var item = grid.Rows[targetRow].DataBoundItem;
                if (item is null) break;
                var definition = ColumnFor(item, grid.Rows[targetRow].Cells[candidate]);
                if (definition is not null && (definition.Editable?.Invoke(item) ?? true) && CanWrite(item)) { targetColumn = candidate; break; }
            }
            if (targetColumn == columnIndex) return;
        }
        await OpenAsync(targetRow, targetColumn);
    }

    private void Lock(object item, Guid requestId, string message)
    {
        var key = RowKey(item);
        unknown[key] = requestId;
        CloseEditor();
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

    private void Reposition() { if (editor.Visible) PositionEditor(); SyncButtons(); }

    private void PositionEditor()
    {
        if (activeKey is null || activeProperty is null) { if (editor.Visible) editor.HideEditor(); return; }
        var rowIndex = FindRowIndex(activeKey); var columnIndex = ColumnIndex(activeProperty);
        if (rowIndex < 0 || columnIndex < 0) { CloseEditor(); return; }
        var rect = grid.GetCellDisplayRectangle(columnIndex, rowIndex, true);
        var visible = rect.Width > 0 && rect.Height > 0 && rect.Top >= grid.ColumnHeadersHeight
            && rect.Bottom <= grid.ClientSize.Height && rect.Left >= 0 && rect.Right <= grid.ClientSize.Width;
        if (!visible) { editor.SetSilentVisible(false); return; }
        var point = host.PointToClient(grid.PointToScreen(rect.Location));
        editor.SetBounds(point.X, point.Y, rect.Width, rect.Height);
        editor.SetSilentVisible(true); editor.BringToFront();
    }

    private int FindRowIndex(string key)
    {
        for (var i = 0; i < grid.Rows.Count; i++)
            if (grid.Rows[i].DataBoundItem is { } item && string.Equals(RowKey(item), key, StringComparison.Ordinal)) return i;
        return -1;
    }

    private int ColumnIndex(string property)
    {
        for (var i = 0; i < grid.Columns.Count; i++)
            if (string.Equals(grid.Columns[i].Name, property, StringComparison.Ordinal)) return i;
        return -1;
    }

    private void UpdateState() => StateChanged?.Invoke(this, EventArgs.Empty);

    private static string Display(InlineCellKind kind, object? value) => kind switch
    {
        InlineCellKind.Date when value is DateOnly date => date.ToString("dd/MM/yyyy"),
        InlineCellKind.Time when value is TimeOnly time => time.ToString("HH:mm"),
        InlineCellKind.Time when value is TimeSpan span => span.ToString(@"hh\:mm"),
        InlineCellKind.Decimal when value is decimal number => number.ToString("0.####", global::System.Globalization.CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, global::System.Globalization.CultureInfo.InvariantCulture) ?? ""
    };
}
