using System.Drawing.Drawing2D;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public enum InlineCellKind { Text, Money, Integer, Decimal, Dropdown, Date, Time, Check }

public enum InlineMove { None, Down, Right, Left }

// Editor overlay cho một ô master data: dùng control native, theme chung và chọn hết khi mới vào.
// Không thay chế độ edit native của lưới vì DTO là record immutable và lưới tự paint.
public sealed class OrbitCellEditor : Panel
{
    private readonly TextBox text = new() { BorderStyle = BorderStyle.None, Dock = DockStyle.None };
    private readonly ComboBox combo = new() { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Visible = false };
    private InlineCellKind kind;
    private bool begun, closing, suppressLeave, committing, commitFailed;
    public event EventHandler? CommitRequested;
    public event Action<InlineMove>? MoveRequested;
    public event EventHandler? CancelRequested;

    public OrbitCellEditor()
    {
        Visible = false;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = OrbitManagementTheme.Input;
        OrbitField.ApplyInputBehavior(this);
        text.Font = OrbitManagementTheme.Body; text.ForeColor = OrbitManagementTheme.Text; text.BackColor = OrbitManagementTheme.Input;
        combo.Font = OrbitManagementTheme.Body; combo.ForeColor = OrbitManagementTheme.Text; combo.BackColor = OrbitManagementTheme.Input;
        combo.DrawMode = DrawMode.OwnerDrawFixed; combo.ItemHeight = OrbitManagementTheme.Px(this, 26);
        combo.DrawItem += PaintItem;
        Controls.AddRange([text, combo]);
        text.TextChanged += (_, _) => commitFailed = false;
        combo.SelectedIndexChanged += (_, _) => commitFailed = false;
        text.Leave += (_, _) => RequestCommit();
        combo.Leave += (_, _) => RequestCommit();
    }

    public Control Input => kind == InlineCellKind.Dropdown ? combo : text;
    public new string Text => kind == InlineCellKind.Dropdown ? combo.SelectedItem as string ?? combo.Text : text.Text;

    public void Begin(InlineCellKind cellKind, string value, IReadOnlyList<string>? options, bool rightAligned)
    {
        kind = cellKind; begun = true; closing = false; suppressLeave = false; committing = false; commitFailed = false;
        var dropdown = kind == InlineCellKind.Dropdown;
        text.Visible = !dropdown; combo.Visible = dropdown;
        text.TextAlign = rightAligned ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        if (dropdown)
        {
            combo.BeginUpdate(); combo.Items.Clear();
            if (options is not null) foreach (var option in options) combo.Items.Add(option);
            combo.EndUpdate();
            combo.SelectedIndex = -1;
            var index = combo.Items.IndexOf(value);
            if (index < 0 && !string.IsNullOrEmpty(value)) { combo.Items.Insert(0, value); index = 0; }
            combo.SelectedIndex = index >= 0 ? index : -1;
        }
        else
        {
            text.Text = value;
        }
    }

    public void FocusInput()
    {
        if (!Visible || !begun) return;
        if (kind == InlineCellKind.Dropdown)
        {
            combo.Focus();
            // Mở danh sách ngay để chọn bằng chuột/bàn phím như yêu cầu "click vào danh mục".
            if (!combo.DroppedDown) combo.DroppedDown = true;
        }
        else
        {
            text.Focus();
            text.SelectAll();
        }
    }

    // Ẩn editor mà không phát commit (đổi tab, reload, ô bị cuộn khỏi vùng nhìn).
    public void HideEditor()
    {
        closing = true; begun = false; committing = false; commitFailed = false; Visible = false;
        closing = false;
    }

    // Đổi hiển thị khi cuộn/resize mà không coi là rời ô để commit.
    public void SetSilentVisible(bool visible)
    {
        var saved = suppressLeave; suppressLeave = true;
        try { Visible = visible; }
        finally { suppressLeave = saved; }
    }

    // Ghi bất đồng bộ: giữ begun cho đến khi engine đóng editor hoặc trả về thất bại,
    // nhờ vậy Esc/click chuyển ô vẫn dùng được sau khi commit bị từ chối.
    public void EndCommit() => committing = false;
    // Lần ghi vừa thất bại: giữ editor để sửa/Esc, không tự ghi lại khi rời ô.
    public bool CommitFailed => commitFailed;
    public void FailCommit() { commitFailed = true; committing = false; }

    private void RequestCommit()
    {
        if (!begun || committing || commitFailed || closing || suppressLeave || !Visible) return;
        committing = true;
        CommitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        if (!begun || committing) return;
        begun = false; BeginInvokeIfHandle(() => CancelRequested?.Invoke(this, EventArgs.Empty));
    }

    private void Confirm(InlineMove move)
    {
        if (!begun || committing) return;
        committing = true;
        BeginInvokeIfHandle(() => MoveRequested?.Invoke(move));
    }

    private void BeginInvokeIfHandle(Action? action)
    {
        if (action is null) return;
        if (IsHandleCreated) BeginInvoke(action); else action();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Cancel(); return true; }
        if (keyData == Keys.Enter) { Confirm(InlineMove.Down); return true; }
        if (keyData == (Keys.Shift | Keys.Tab)) { Confirm(InlineMove.Left); return true; }
        if (keyData == Keys.Tab) { Confirm(InlineMove.Right); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        var inset = OrbitManagementTheme.Px(this, 7);
        var top = OrbitManagementTheme.Px(this, 5);
        var height = Math.Max(1, Height - top * 2);
        text.SetBounds(inset, top + Math.Max(0, (height - text.PreferredSize.Height) / 2), Math.Max(1, Width - inset * 2), text.PreferredSize.Height);
        combo.SetBounds(inset, top, Math.Max(1, Width - inset * 2), Math.Max(1, Math.Min(OrbitManagementTheme.Px(this, 28), height)));
        base.OnLayout(levent);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics; graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(OrbitManagementTheme.Input);
        var bounds = new RectangleF(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var path = OrbitManagementTheme.Round(bounds, OrbitManagementTheme.Px(this, 6));
        using var fill = new SolidBrush(OrbitManagementTheme.Input);
        graphics.FillPath(fill, path);
        using var border = new Pen(ContainsFocus ? OrbitManagementTheme.Lilac : OrbitManagementTheme.Caramel, Math.Max(1, OrbitManagementTheme.Px(this, 1)));
        graphics.DrawPath(border, path);
    }

    private void PaintItem(object? sender, DrawItemEventArgs e)
    {
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var fill = new SolidBrush(selected ? OrbitManagementTheme.Selected : OrbitManagementTheme.Input);
        e.Graphics.FillRectangle(fill, e.Bounds);
        var label = e.Index >= 0 && e.Index < combo.Items.Count ? Convert.ToString(combo.Items[e.Index]) ?? "" : combo.Text;
        TextRenderer.DrawText(e.Graphics, label, combo.Font, Rectangle.Inflate(e.Bounds, -4, 0), OrbitManagementTheme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if ((e.State & DrawItemState.Focus) != 0) e.DrawFocusRectangle();
    }
}
