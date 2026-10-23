using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// AppShell giữ các button/handler gốc; sidebar chỉ bố trí và vẽ nhận diện.
public sealed class OrbitSidebar : FlowLayoutPanel
{
    public Label Person { get; } = new() { Text = "", Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Text, AutoEllipsis = true };
    public Label Role { get; } = new() { Text = "", Font = OrbitManagementTheme.Header, ForeColor = OrbitManagementTheme.Muted, AutoEllipsis = true };
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Label? SessionStatus { get; set; }
    [System.ComponentModel.DefaultValue(false)]
    public bool Compact { get; set; }
    private bool arranging;
    public OrbitSidebar()
    {
        DoubleBuffered = true; Margin = Padding.Empty; Padding = Padding.Empty;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = OrbitManagementTheme.Sidebar; WrapContents = false; AutoScroll = true;
        Controls.AddRange([Person, Role]);
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        if (arranging) return; arranging = true;
        try
        {
            base.OnLayout(e); int P(float n) => OrbitManagementTheme.Px(this, n);
            var labels = Controls.OfType<Label>().Where(label => label != Person && label != Role && label != SessionStatus).ToArray();
            var buttons = Controls.OfType<Button>().Where(button => button.Visible).ToArray();
            var width = Compact ? P(62) : P(200);
            var navigationCount = buttons.Count(button => button.Name != "logout");
            // Thu khoảng trống đầu trang khi DPI cao; giữ nguyên kích thước nút.
            var navigationTop = Math.Clamp(Height - P(navigationCount * 49 + 150), P(80), P(129));
            var y = navigationTop;
            if (labels.Length >= 2)
            {
                labels[0].Visible = labels[1].Visible = !Compact;
                labels[0].AutoSize = labels[1].AutoSize = false;
                labels[0].SetBounds(P(24), P(22), P(174), P(32));
                labels[1].SetBounds(P(24), navigationTop - P(26), P(174), P(18));
            }
            foreach (var button in buttons.Where(button => button.Name != "logout"))
            {
                button.SetBounds(P(12), y + AutoScrollPosition.Y, Compact ? P(38) : P(174), P(43)); y += P(49);
            }
            var bottom = Math.Max(Height - P(64), y + P(86));
            var logout = buttons.FirstOrDefault(button => button.Name == "logout");
            logout?.SetBounds(P(12), bottom - P(49) + AutoScrollPosition.Y, Compact ? P(38) : P(174), P(33));
            Person.Visible = Role.Visible = !Compact;
            Person.SetBounds(P(62), bottom + AutoScrollPosition.Y, width - P(70), P(19));
            Role.SetBounds(P(62), bottom + P(19) + AutoScrollPosition.Y, width - P(70), P(17));
            SessionStatus?.SetBounds(P(12), bottom + P(42) + AutoScrollPosition.Y, width - P(24), P(16));
            AutoScrollMinSize = new Size(0, bottom + P(64));
            // Avatar được vẽ trên nền; xóa cả vị trí cũ khi profile chuyển theo chiều cao.
            Invalidate();
        }
        finally { arranging = false; }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var scale = DeviceDpi / 96F;
        using var border = new Pen(OrbitManagementTheme.Border); e.Graphics.DrawLine(border, Width - 1, 0, Width - 1, Height);
        if (Compact || Person.Text.Length == 0) return;
        var rect = new RectangleF(21 * scale, Person.Top + 3 * scale, 32 * scale, 32 * scale);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(OrbitManagementTheme.Selected); e.Graphics.FillEllipse(fill, rect);
        using var outline = new Pen(Color.FromArgb(109, 80, 57)); e.Graphics.DrawEllipse(outline, rect);
        var initials = string.Concat(Person.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).TakeLast(2).Select(word => word[0])).ToUpperInvariant();
        TextRenderer.DrawText(e.Graphics, initials, OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Small), Rectangle.Round(rect), OrbitManagementTheme.Caramel, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

