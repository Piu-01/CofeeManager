using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.States;
using CafeManager.WinForms.Theme;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Modules.Sales;

internal sealed class OrderPanel : OrbitSurface
{
    internal readonly DataGridView Lines = SalesUi.Grid(("Name", L.T("Món")), ("Quantity", L.T("SL")), ("Price", L.T("Đơn giá")), ("Delete", ""));
    internal readonly Label Caption = new() { Font = OrbitManagementTheme.Body, ForeColor = OrbitManagementTheme.Text, UseMnemonic = false, AutoEllipsis = true };
    internal readonly Label Totals = new() { Font = OrbitManagementTheme.Title, ForeColor = OrbitManagementTheme.Caramel, TextAlign = ContentAlignment.MiddleRight, UseMnemonic = false };
    internal readonly Panel HeaderActions = new(), PaymentActions = new();
    private readonly OrbitBadge status = new();
    private readonly Label totalCaption = new() { Text = L.T("Phải thanh toán"), Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted, UseMnemonic = false, AutoEllipsis = true };
    private readonly OrderNoteEditor noteEditor = new();
    private readonly NoteSuggestionPanel suggestions = new();
    // Ghi chú hay dùng của quán, luôn đứng trước gợi ý lịch sử.
    private static readonly string[] fixedNotes = ["Không đá", "Ít đá", "Nhiều đá", "Không đường", "Ít đường", "Nhiều đường",
        "Ít sữa", "Nhiều sữa", "Không sữa", "Uống nóng", "Mang đi", "Không ống hút"];
    private IReadOnlyList<string> historyNotes = [];
    private SalesOrderDto? current;
    private SalesLine? editingLine;
    private int editingRow = -1;
    private bool interactive, unknown, suppressSuggestions;
    internal bool HasPendingNote => editingLine is not null;
    // Ghi chú đang mở có nội dung khác dòng để luồng rời/chuyển đơn lưu trước khi đi tiếp.
    internal (SalesLine Line, string? Note)? PendingNote()
    {
        if (editingLine is not { } line) return null;
        var note = string.IsNullOrWhiteSpace(noteEditor.Input.Text) ? null : noteEditor.Input.Text.Trim();
        if (string.Equals(note, string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim(), StringComparison.Ordinal)) return null;
        return (line, note);
    }
    internal void SetInteractive(bool value) { interactive = value; noteEditor.Enabled = value; if (!value) suggestions.Reset(); }
    internal void SetNoteHistory(IReadOnlyList<string> notes) { historyNotes = notes; if (suggestions.Visible) ShowSuggestions(); }
    internal event Action<SalesLine>? IncreaseRequested, DecreaseRequested, DeleteRequested;
    internal event Action<SalesLine, string?>? NoteSaveRequested;
    public OrderPanel()
    {
        Dock = DockStyle.Fill; Lines.Dock = DockStyle.None;
        ((OrbitDataGridView)Lines).Profile = OrbitGridProfile.OrderLines;
        Lines.Columns["Delete"]!.DataPropertyName = "";
        Controls.AddRange([Lines, Caption, status, HeaderActions, totalCaption, Totals, PaymentActions, noteEditor, suggestions]);
        Lines.CellMouseClick += (_, e) => LineClicked(e);
        Lines.Scroll += (_, _) => PositionNoteEditor();
        noteEditor.SaveRequested += (_, _) => SaveNote();
        noteEditor.CancelRequested += (_, _) => CancelNoteEdit();
        noteEditor.KeyFilter = HandleNoteKey;
        noteEditor.Input.TextChanged += (_, _) => { if (!suppressSuggestions) ShowSuggestions(); };
        suggestions.Accepted += AcceptSuggestion;
    }
    internal void ShowOrder(SalesOrderDto? order)
    {
        CancelNoteEdit();
        var selectedId = (Lines.CurrentRow?.DataBoundItem as SalesLine)?.LineId;
        current = order; Lines.DataSource = order?.Lines.ToList();
        if (selectedId is { } id)
            foreach (DataGridViewRow row in Lines.Rows) if (row.DataBoundItem is SalesLine line && line.LineId == id) { Lines.CurrentCell = row.Cells["Name"]; break; }
        Caption.Text = order?.Header.Code ?? L.T("Chưa tạo đơn");
        status.Text = order is null ? L.T("Trống") : SalesStates.Label(order.Header.Status);
        status.ForeColor = order is null ? OrbitManagementTheme.Muted
            : order.Header.Status == "Huy" ? AppTheme.Error : order.Header.Status == "Da thanh toan" ? OrbitManagementTheme.Lilac : OrbitManagementTheme.Caramel;
        UpdateTotal();
    }
    internal void SetResultUnknown(bool value) { unknown = value; UpdateTotal(); }
    internal void CancelNoteEdit()
    {
        var restoreFocus = noteEditor.ContainsFocus || suggestions.ContainsFocus;
        if (editingRow >= 0 && editingRow < Lines.Rows.Count)
            Lines.Rows[editingRow].Height = OrbitDataGridView.LineContentHeight(this, editingLine);
        editingLine = null; editingRow = -1;
        suggestions.Reset();
        noteEditor.Visible = false; noteEditor.Input.Clear();
        if (restoreFocus && !Lines.IsDisposed) Lines.Focus();
    }
    private void LineClicked(DataGridViewCellMouseEventArgs e)
    {
        if (!interactive || e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (Lines.Rows[e.RowIndex].DataBoundItem is not SalesLine line) return;
        var property = Property(Lines.Columns[e.ColumnIndex]);
        // Bấm một lần vào tên món để mở ghi chú; ô ghi chú nằm dưới nội dung dòng, không đè lên.
        if (property == "Name") { BeginNoteEdit(e.RowIndex, line); return; }
        if (property is not ("Quantity" or "Delete")) return;
        CancelNoteEdit();
        if (property == "Delete") { DeleteRequested?.Invoke(line); return; }
        var third = Math.Max(1, Lines.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false).Width / 3);
        if (e.X < third) DecreaseRequested?.Invoke(line);
        else if (e.X >= third * 2) IncreaseRequested?.Invoke(line);
    }
    private void BeginNoteEdit(int rowIndex, SalesLine line)
    {
        if (editingLine?.LineId == line.LineId)
        {
            // Editor có thể đang ẩn do lưới bị thu nhỏ; đặt lại, không đủ chỗ thì đóng hẳn.
            PositionNoteEditor();
            if (noteEditor.Visible) noteEditor.FocusInput(); else CancelNoteEdit();
            return;
        }
        CancelNoteEdit();
        editingLine = line; editingRow = rowIndex;
        Lines.Rows[rowIndex].Height = OrbitDataGridView.LineContentHeight(this, line) + OrbitManagementTheme.Px(this, 30);
        var row = Lines.GetRowDisplayRectangle(rowIndex, false);
        if (row.Top < 0 || row.Bottom > Lines.ClientSize.Height)
            Lines.FirstDisplayedScrollingRowIndex = Math.Min(rowIndex, Math.Max(0, Lines.Rows.Count - 1));
        suppressSuggestions = true;
        noteEditor.Input.Text = line.Note ?? "";
        suppressSuggestions = false;
        PositionNoteEditor();
        // Không đủ chỗ (lưới thấp): trả chiều cao dòng thay vì để khoảng trống không có ô nhập.
        if (!noteEditor.Visible) { CancelNoteEdit(); return; }
        noteEditor.FocusInput();
    }
    private void SaveNote()
    {
        if (editingLine is not { } line) return;
        var note = string.IsNullOrWhiteSpace(noteEditor.Input.Text) ? null : noteEditor.Input.Text.Trim();
        if (string.Equals(note, string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim(), StringComparison.Ordinal)) { CancelNoteEdit(); return; }
        NoteSaveRequested?.Invoke(line, note);
    }
    private void PositionNoteEditor()
    {
        if (editingLine is null || editingRow < 0 || editingRow >= Lines.Rows.Count)
        {
            noteEditor.Visible = false; suggestions.Reset(); return;
        }
        int P(float n) => OrbitManagementTheme.Px(this, n);
        var row = Lines.GetRowDisplayRectangle(editingRow, false);
        var name = Lines.GetCellDisplayRectangle(Lines.Columns["Name"]!.Index, editingRow, true);
        var price = Lines.GetCellDisplayRectangle(Lines.Columns["Price"]!.Index, editingRow, true);
        var height = P(26);
        // Editor đặt ngay dưới phần nội dung (tên/ghi chú) của dòng, phần dòng được nới thêm 30 logical.
        // Không chừa lề dọc để tận dụng đúng phần lưới còn trống; dưới đáy lưới coi như hết chỗ.
        var top = Lines.Top + row.Top + OrbitDataGridView.LineContentHeight(this, editingLine);
        if (row.Height <= 0 || row.Top < 0 || top < Lines.Top + Lines.ColumnHeadersHeight
            || top + height > Lines.Top + Lines.ClientSize.Height)
        {
            noteEditor.Visible = false; suggestions.Reset(); return;
        }
        var left = Math.Max(Lines.Left + P(2), Lines.Left + name.Left + P(2));
        var right = Math.Min(Lines.Right - P(2), Lines.Left + Math.Max(name.Right, price.Right) - P(2));
        if (right - left < P(70)) { noteEditor.Visible = false; suggestions.Reset(); return; }
        noteEditor.SetBounds(left, top, right - left, height);
        noteEditor.Visible = true; noteEditor.BringToFront();
        if (suggestions.Visible) PositionSuggestions();
    }
    // Gợi ý gộp mục cố định của quán với ghi chú đã dùng trong lịch sử, lọc theo nội dung đang gõ.
    private List<string> SuggestionItems(string query)
    {
        var text = query.Trim();
        var items = fixedNotes.Concat(historyNotes)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Where(x => text.Length == 0 || x.Contains(text, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(x => x.StartsWith(text, StringComparison.CurrentCultureIgnoreCase) ? 0 : 1)
            .Take(6).ToList();
        return items;
    }
    private void ShowSuggestions()
    {
        if (editingLine is null || !noteEditor.Visible || !noteEditor.Enabled) { suggestions.Visible = false; return; }
        var items = SuggestionItems(noteEditor.Input.Text);
        if (items.Count == 0) { suggestions.Visible = false; return; }
        suggestions.SetItems(items);
        PositionSuggestions();
    }
    private void PositionSuggestions()
    {
        int P(float n) => OrbitManagementTheme.Px(this, n);
        var wanted = suggestions.DesiredHeight;
        var below = noteEditor.Bottom + P(2);
        var maxBottom = Height - P(6);
        var spaceBelow = Math.Max(0, maxBottom - below);
        var above = noteEditor.Top - P(2);
        var spaceAbove = Math.Max(0, above - P(4));
        var height = Math.Min(wanted, Math.Max(spaceBelow, spaceAbove));
        if (height < suggestions.ItemHeight + P(2)) { suggestions.Visible = false; return; }
        var top = spaceBelow >= wanted || spaceBelow >= spaceAbove ? below : Math.Max(P(4), above - height);
        if (top + height > maxBottom) height = Math.Max(suggestions.ItemHeight + P(2), maxBottom - top);
        var width = Math.Max(noteEditor.Width, P(150));
        var left = Math.Clamp(noteEditor.Left, P(2), Math.Max(P(2), Width - P(2) - width));
        suggestions.SetBounds(left, top, width, height);
        suggestions.Visible = true; suggestions.BringToFront();
    }
    private bool HandleNoteKey(KeyEventArgs e)
    {
        if (!suggestions.Visible)
        {
            if (e.KeyCode is not (Keys.Down or Keys.Up) || editingLine is null) return false;
            if (SuggestionItems(noteEditor.Input.Text).Count == 0) return false;
            ShowSuggestions();
            if (!suggestions.Visible) return false;
            suggestions.MoveSelection(e.KeyCode == Keys.Down ? 1 : 0);
            return true;
        }
        if (e.KeyCode is Keys.Down or Keys.Up) { suggestions.MoveSelection(e.KeyCode == Keys.Down ? 1 : -1); return true; }
        if (e.KeyCode == Keys.Enter)
        {
            var item = suggestions.Selected;
            if (item is null) return false;
            AcceptSuggestion(suggestions.SelectedIndex);
            return true;
        }
        if (e.KeyCode == Keys.Escape) { suggestions.Visible = false; return true; }
        if (e.KeyCode == Keys.Tab) { suggestions.Visible = false; return false; }
        return false;
    }
    // Chọn gợi ý là chốt luôn ghi chú: điền vào ô rồi lưu như Enter.
    private void AcceptSuggestion(int index)
    {
        var item = suggestions.ItemAt(index);
        if (item is null) return;
        suppressSuggestions = true;
        noteEditor.Input.Text = item;
        noteEditor.Input.SelectionStart = item.Length;
        noteEditor.Input.SelectionLength = 0;
        suppressSuggestions = false;
        suggestions.Visible = false;
        noteEditor.Input.Focus();
        SaveNote();
    }
    private static string Property(DataGridViewColumn column) => column.DataPropertyName.Length > 0 ? column.DataPropertyName : column.Name;
    private void UpdateTotal()
    {
        var valid = current is not null && !unknown && (!current.Header.IsOpen || current.Header.PreviewValid);
        Totals.Text = valid ? SalesUi.Money(current!.Header.Payable) : "—";
        totalCaption.Text = current is null || valid ? L.T("Phải thanh toán") : L.T("Chưa xác minh tổng");
        PerformLayout();
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e); if (Lines is null) return;
        int P(float n) => OrbitManagementTheme.Px(this, n);
        var inner = Math.Max(1, Width - P(24));
        status.SetBounds(Width - P(126), P(12), P(114), P(23));
        Caption.SetBounds(P(12), P(10), Math.Max(1, status.Left - P(20)), P(25));
        // Bỏ dòng ngữ cảnh (loại đơn/bàn/khách/ưu đãi) để panel đơn gọn hơn.
        HeaderActions.SetBounds(P(12), P(37), inner, P(26));
        var headerButtons = HeaderActions.Controls.Cast<Control>().ToArray();
        if (headerButtons.Length > 0)
        {
            // Nút đầu sát trái; các nút còn lại (Trả bàn, Hủy đơn mở…) dồn sát phải,
            // xếp từ phải sang trái để nút giữa không bị bỏ sót như thiết kế 2 nút cũ.
            var headerGap = P(8); var right = inner;
            for (var i = headerButtons.Length - 1; i >= 1; i--)
            {
                var width = Math.Min(inner, Math.Max(1, headerButtons[i].GetPreferredSize(Size.Empty).Width));
                right = Math.Max(0, right - width);
                headerButtons[i].SetBounds(right, 0, width, P(26));
                right = Math.Max(0, right - headerGap);
            }
            headerButtons[0].SetBounds(0, 0, Math.Max(1, right), P(26));
        }
        // Nút thanh toán chính (index 1) cao hơn; các nút phụ xếp dọc, chiều cao panel tính theo số nút.
        var paymentButtons = PaymentActions.Controls.Cast<Control>().ToArray();
        var gap = P(5); var paymentHeight = P(38) + P(27) * Math.Max(0, paymentButtons.Length - 1) + gap * Math.Max(0, paymentButtons.Length - 1);
        PaymentActions.SetBounds(P(12), Height - P(10) - paymentHeight, inner, paymentHeight);
        var y = 0;
        for (var i = 0; i < paymentButtons.Length; i++)
        {
            var h = P(i == 1 ? 38 : 27);
            paymentButtons[i].SetBounds(0, y, inner, h); y += h + gap;
        }
        var titleFont = OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Title);
        var moneyWidth = TextRenderer.MeasureText(Totals.Text, titleFont, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
        var stackedTotal = moneyWidth > Math.Max(1, Width - P(122));
        // Số tiền dài có hàng riêng, dùng CardTitle để giữ đủ chỗ cho bảng và F8.
        Totals.Font = stackedTotal
            ? OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.CardTitle) : titleFont;
        var moneyHeight = Math.Max(P(stackedTotal ? 26 : 34), TextRenderer.MeasureText(Totals.Text, Totals.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Height);
        var captionHeight = stackedTotal ? Math.Max(P(14), TextRenderer.MeasureText(totalCaption.Text, totalCaption.Font).Height) : 0;
        var totalY = PaymentActions.Top - Math.Max(P(40), captionHeight + moneyHeight);
        totalCaption.SetBounds(P(12), totalY, stackedTotal ? inner : P(98), stackedTotal ? captionHeight : moneyHeight);
        Totals.SetBounds(P(stackedTotal ? 12 : 110), totalY + captionHeight,
            stackedTotal ? inner : Math.Max(1, Width - P(122)), moneyHeight);
        Lines.SetBounds(P(1), P(70), Math.Max(1, Width - P(2)), Math.Max(1, totalY - P(10) - P(70)));
        PositionNoteEditor();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); if (Lines is null) return;
        using var pen = new Pen(OrbitManagementTheme.Border);
        e.Graphics.DrawLine(pen, 1, Lines.Top - 1, Width - 2, Lines.Top - 1);
        var divider = Math.Min(Height - 2, Lines.Bottom + OrbitManagementTheme.Px(this, 5));
        e.Graphics.DrawLine(pen, 1, divider, Width - 2, divider);
    }
}

internal sealed class OrderNoteEditor : Panel
{
    internal readonly TextBox Input = new()
    {
        MaxLength = 100, BorderStyle = BorderStyle.None, Font = OrbitManagementTheme.Small,
        BackColor = OrbitManagementTheme.Input, ForeColor = OrbitManagementTheme.Text
    };
    private readonly Button save = new(), cancel = new();
    internal event EventHandler? SaveRequested, CancelRequested;
    // Panel chặn phím trước khi Enter/Esc mặc định để điều hướng danh sách gợi ý.
    internal Func<KeyEventArgs, bool>? KeyFilter;
    internal OrderNoteEditor()
    {
        BackColor = OrbitManagementTheme.Input; Padding = new Padding(6, 3, 3, 3);
        SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
        save.Text = L.T("Lưu"); cancel.Text = L.T("Hủy");
        foreach (var button in new[] { save, cancel })
        {
            button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.TabStop = true;
            button.BackColor = OrbitManagementTheme.Input; button.ForeColor = OrbitManagementTheme.Muted;
            button.Font = OrbitManagementTheme.Small; button.Cursor = Cursors.Hand; button.UseVisualStyleBackColor = false;
        }
        save.ForeColor = OrbitManagementTheme.Caramel;
        save.Click += (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty);
        cancel.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
        Input.KeyDown += (_, e) =>
        {
            if (KeyFilter?.Invoke(e) == true) { e.SuppressKeyPress = true; return; }
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SaveRequested?.Invoke(this, EventArgs.Empty); }
            else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; CancelRequested?.Invoke(this, EventArgs.Empty); }
        };
        Controls.AddRange([Input, save, cancel]);
        Visible = false;
    }
    internal void FocusInput() { Input.Focus(); Input.SelectAll(); }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int P(float n) => OrbitManagementTheme.Px(this, n);
        var buttonWidth = Math.Max(P(34), TextRenderer.MeasureText(save.Text, save.Font).Width + P(12));
        var height = Math.Max(1, Height - Padding.Vertical);
        cancel.SetBounds(Math.Max(0, Width - Padding.Right - buttonWidth), Padding.Top, buttonWidth, height);
        save.SetBounds(Math.Max(0, cancel.Left - buttonWidth - P(4)), Padding.Top, buttonWidth, height);
        Input.SetBounds(Padding.Left, Padding.Top + Math.Max(0, (height - Input.PreferredSize.Height) / 2),
            Math.Max(1, save.Left - P(4) - Padding.Left), Input.PreferredSize.Height);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(OrbitManagementTheme.Lilac);
        e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
    }
}

// Danh sách gợi ý nổi ngay dưới ô ghi chú; chỉ hiện chuỗi, không nhận focus bàn phím.
internal sealed class NoteSuggestionPanel : Panel
{
    private readonly ListBox list = new()
    {
        Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed,
        BackColor = OrbitManagementTheme.Surface, ForeColor = OrbitManagementTheme.Text,
        Font = OrbitManagementTheme.Small, IntegralHeight = false, TabStop = false, ScrollAlwaysVisible = false
    };
    private string[] items = [];
    internal event Action<int>? Accepted;
    internal NoteSuggestionPanel()
    {
        BackColor = OrbitManagementTheme.Border; Padding = new Padding(1); Visible = false;
        list.ItemHeight = OrbitManagementTheme.Px(this, 20);
        list.DrawItem += Draw;
        list.MouseDown += (_, e) => { var index = list.IndexFromPoint(e.Location); if (index >= 0) Accepted?.Invoke(index); };
        Controls.Add(list);
    }
    internal int ItemHeight => Math.Max(1, list.ItemHeight);
    internal int DesiredHeight => items.Length * list.ItemHeight + Padding.Vertical;
    internal int SelectedIndex => list.SelectedIndex;
    internal string? Selected => list.SelectedIndex >= 0 && list.SelectedIndex < items.Length ? items[list.SelectedIndex] : null;
    internal void SetItems(IReadOnlyList<string> values)
    {
        items = values.ToArray();
        list.BeginUpdate(); list.Items.Clear(); foreach (var value in items) list.Items.Add(value);
        list.SelectedIndex = -1; list.EndUpdate();
        list.ItemHeight = OrbitManagementTheme.Px(this, 20);
    }
    internal string? ItemAt(int index) => index >= 0 && index < items.Length ? items[index] : null;
    internal void MoveSelection(int delta)
    {
        if (items.Length == 0) return;
        list.SelectedIndex = list.SelectedIndex < 0
            ? delta > 0 ? 0 : items.Length - 1
            : (list.SelectedIndex + delta + items.Length) % items.Length;
    }
    internal void Reset() { items = []; list.Items.Clear(); list.SelectedIndex = -1; Visible = false; }
    private void Draw(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= items.Length) return;
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var fill = new SolidBrush(selected ? OrbitManagementTheme.Input : OrbitManagementTheme.Surface);
        e.Graphics.FillRectangle(fill, e.Bounds);
        var pad = OrbitManagementTheme.Px(this, 6);
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
        TextRenderer.DrawText(e.Graphics, items[e.Index], list.Font,
            new Rectangle(e.Bounds.Left + pad, e.Bounds.Top, Math.Max(0, e.Bounds.Width - pad * 2), e.Bounds.Height),
            selected ? OrbitManagementTheme.Caramel : OrbitManagementTheme.Text, flags);
    }
}
