using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Khung chung: chỉ vùng Body cuộn; header, tools, actions và pager có hàng riêng.
public sealed class OrbitListWorkspace : UserControl
{
    public Label Eyebrow { get; } = Label("INVENTORY WORKSPACE", OrbitManagementTheme.Header, OrbitManagementTheme.Caramel);
    public Label Title { get; } = Label("", OrbitManagementTheme.Title, OrbitManagementTheme.Text);
    public Label Description { get; } = Label("", OrbitManagementTheme.Table, OrbitManagementTheme.Muted);
    public Label Count { get; } = Label("", OrbitManagementTheme.Small, OrbitManagementTheme.Text);
    public Label Status { get; } = Label("Sẵn sàng", OrbitManagementTheme.Small, OrbitManagementTheme.Caramel);
    public Panel PrimaryActions { get; } = new();
    public Panel Filters { get; } = new();
    public Panel RowActions { get; } = new();
    public Panel Body { get; } = new();
    public Panel Tabs { get; } = new();
    [System.ComponentModel.DefaultValue(true)]
    public bool ShowTools { get; set; } = true;
    [System.ComponentModel.DefaultValue(true)]
    public bool ShowMeta { get; set; } = true;
    [System.ComponentModel.DefaultValue(false)]
    public bool Compact { get; set; }
    public OrbitPagination Pagination { get; } = new();
    private readonly RoundedPanel card = new();
    private readonly Control search;
    private bool arranging;
    private static Label Label(string text, Font font, Color color) => new() { Text = text, Font = font, ForeColor = color, UseMnemonic = false, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, BackColor = Color.Transparent };
    public OrbitListWorkspace(Control search)
    {
        this.search = search; Dock = DockStyle.Fill; AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        BackColor = OrbitManagementTheme.Background;
        Controls.AddRange([Eyebrow, Title, Description, PrimaryActions, Tabs, card, Status]);
        card.Controls.AddRange([search, Filters, Count, RowActions, Body, Pagination]);
        foreach (var panel in new[] { Filters, RowActions, Body }) panel.BackColor = OrbitManagementTheme.Surface;
        PrimaryActions.BackColor = BackColor;
        foreach (var panel in new[] { Filters, RowActions, PrimaryActions })
        {
            panel.ControlAdded += (_, e) => { if (e.Control is { } control) { control.VisibleChanged += (_, _) => PerformLayout(); control.SizeChanged += (_, _) => { if (!arranging) PerformLayout(); }; } PerformLayout(); };
        }
        Tabs.ControlAdded += (_, _) => PerformLayout(); Tabs.ControlRemoved += (_, _) => PerformLayout();
        VisibleChanged += (_, _) => PerformLayout();
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        if (arranging) return; arranging = true;
        try
        {
            base.OnLayout(e); int P(float n) => OrbitManagementTheme.Px(this, n);
            var width = Math.Max(P(320), ClientSize.Width - P(52));
            var x = P(26); var y = P(22);
            if (Compact) { width = Math.Max(1, ClientSize.Width); x = y = 0; }
            Eyebrow.Visible = Title.Visible = Description.Visible = PrimaryActions.Visible = !Compact;
            Eyebrow.SetBounds(x, y, width, P(16)); Title.SetBounds(x, y + P(18), Math.Max(P(100), width - P(200)), P(36));
            Description.SetBounds(x, y + P(60), width, P(20));
            var primaryWidth = ArrangeButtons(PrimaryActions, P(8));
            PrimaryActions.SetBounds(x + width - primaryWidth, y + P(19), primaryWidth, P(44));
            var tabHeight = Tabs.Controls.Count == 0 ? 0 : 52;
            Tabs.Visible = tabHeight > 0; Tabs.SetBounds(x, y + P(98), width, P(38));
            card.SetBounds(x, Compact ? 0 : y + P(98 + tabHeight), width, Math.Max(P(180), ClientSize.Height - (Compact ? P(28) : y + P(98 + tabHeight + 42))));
            Status.SetBounds(x, card.Bottom + P(8), width, P(18));
            var filterWidth = ArrangeButtons(Filters, P(10));
            Filters.Visible = ShowTools; search.Visible = ShowTools;
            Filters.SetBounds(card.Width - P(17) - filterWidth, P(14), filterWidth, P(40));
            search.SetBounds(P(17), P(14), Math.Max(P(100), Filters.Left - P(27)), P(40));
            var actionsWidth = ArrangeButtons(RowActions, P(6));
            var toolsHeight = ShowTools ? 70 : 0; var metaHeight = ShowMeta ? (ShowTools ? 49 : 55) : 0;
            RowActions.Visible = ShowMeta;
            Count.Visible = false;
            var actionsHeight = Math.Max(P(33), RowActions.Controls.Cast<Control>().Where(c => c.Visible).Select(c => c.Height).DefaultIfEmpty(0).Max());
            RowActions.SetBounds(card.Width - P(17) - actionsWidth, P(toolsHeight) + (P(metaHeight) - actionsHeight) / 2, actionsWidth, actionsHeight);
            Count.SetBounds(P(17), P(toolsHeight), Math.Max(1, RowActions.Left - P(34)), P(metaHeight));
            var bodyTop = toolsHeight + metaHeight;
            Body.SetBounds(P(1), P(bodyTop), card.Width - P(2), Math.Max(P(30), card.Height - P(bodyTop + 54)));
            card.ToolsDivider = ShowTools;
            Pagination.SetBounds(P(1), card.Height - P(54), card.Width - P(2), P(53));
            card.Invalidate();
        }
        finally { arranging = false; }
    }
    private static int ArrangeButtons(Panel panel, int gap)
    {
        var x = 0;
        foreach (Control control in panel.Controls)
        {
            if (!control.Visible) continue;
            if (control is OrbitButton button) button.Size = button.GetPreferredSize(Size.Empty);
            if (control is OrbitFilterBox filter) control.Size = new Size(OrbitManagementTheme.Px(control, filter.LogicalWidth), OrbitManagementTheme.Px(control, 40));
            control.Location = new Point(x, 0); x += control.Width + gap;
        }
        return Math.Max(0, x - gap);
    }
    private sealed class RoundedPanel : Panel
    {
        [System.ComponentModel.DefaultValue(true)]
        public bool ToolsDivider { get; set; } = true;
        public RoundedPanel() { DoubleBuffered = true; BackColor = OrbitManagementTheme.Surface; }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e); if (Width < 2 || Height < 2) return;
            using var path = OrbitManagementTheme.Round(new RectangleF(0, 0, Width, Height), OrbitManagementTheme.Px(this, 12));
            var old = Region; Region = new Region(path); old?.Dispose();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); var scale = DeviceDpi / 96F;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = OrbitManagementTheme.Round(new RectangleF(.5F, .5F, Width - 1, Height - 1), 12 * scale);
            using var pen = new Pen(OrbitManagementTheme.Border); e.Graphics.DrawPath(pen, path);
            if (ToolsDivider) e.Graphics.DrawLine(pen, 0, 70 * scale, Width, 70 * scale);
        }
    }
}
