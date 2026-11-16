using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Header/footer cố định; chỉ menu cuộn. Button gốc giữ handler và quyền điều hướng.
public sealed class OrbitSidebar : Panel
{
    private readonly TableLayoutPanel shell = Column();
    private readonly Panel header = new(), footer = new(), unused = new() { Visible = false };
    private readonly Panel scroll = new() { AutoScroll = true, Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly TableLayoutPanel menu = Column();
    private readonly ToolTip hints = new();
    private readonly List<Group> groups = [];
    private Label? title, caption, status;
    private Button? home, logout;
    private bool compact, arranging;

    public Label Person { get; } = new() { Font = OrbitManagementTheme.Small, AutoEllipsis = true };
    public Label Role { get; } = new() { Font = OrbitManagementTheme.Header, AutoEllipsis = true };
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<Button> NavigationButtons => (home is null ? Enumerable.Empty<Button>() : [home])
        .Concat(groups.SelectMany(group => group.Items)).ToArray();
    [System.ComponentModel.DefaultValue(false)]
    public bool Compact
    {
        get => compact;
        set { if (compact == value) return; compact = value; RefreshLayout(); }
    }
    // Vai trò không có trang chủ thì thu hàng home về 0 thay vì để khoảng trống.
    [System.ComponentModel.DefaultValue(true)]
    public bool HomeVisible
    {
        get => showHome;
        set { if (showHome == value) return; showHome = value; if (home is not null) home.Visible = value; RefreshLayout(); }
    }
    private bool showHome = true;

    public OrbitSidebar()
    {
        DoubleBuffered = true; Margin = Padding.Empty;
        BackColor = OrbitManagementTheme.Sidebar;
        shell.Dock = DockStyle.Fill; shell.RowCount = 3;
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        header.Dock = footer.Dock = DockStyle.Fill; header.Margin = footer.Margin = Padding.Empty;
        menu.Dock = DockStyle.Top; menu.AutoSize = true; menu.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        scroll.Controls.Add(menu);
        shell.Controls.Add(header, 0, 0); shell.Controls.Add(scroll, 0, 1); shell.Controls.Add(footer, 0, 2);
        Controls.Add(shell); Controls.Add(unused);
        footer.Controls.Add(Person); footer.Controls.Add(Role);
        header.Paint += PaintHeader; footer.Paint += PaintFooter;
        Person.TextChanged += (_, _) => footer.Invalidate();
    }

    public void InitializeNavigation(Label brand, Label workspace, Button homeButton, Button logoutButton, Label sessionStatus)
    {
        title = brand; caption = workspace; home = homeButton; logout = logoutButton; status = sessionStatus;
        title.AutoSize = caption.AutoSize = status.AutoSize = false;
        title.AutoEllipsis = caption.AutoEllipsis = status.AutoEllipsis = true;
        header.Controls.Add(title); header.Controls.Add(caption);
        footer.Controls.Add(logout); footer.Controls.Add(status);
        menu.Controls.Add(home, 0, 0); menu.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        home.Dock = DockStyle.Fill; home.Margin = Padding.Empty; home.Visible = showHome;
        home.AccessibleName = home.Text; logout.AccessibleName = logout.Text;
        hints.SetToolTip(home, home.Text); hints.SetToolTip(logout, logout.Text);
        RefreshLayout();
    }

    public void SetGroups(params (string Title, Button[] Items)[] definitions)
    {
        menu.SuspendLayout();
        // Giữ ownership cả nút bị loại theo role; không dispose handler khi dựng lại nhóm.
        foreach (var group in groups)
        {
            foreach (var button in group.Items) { unused.Controls.Add(button); button.Visible = false; }
            menu.Controls.Remove(group.Container); group.Container.Dispose();
        }
        groups.Clear();
        menu.RowStyles.Clear(); menu.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        menu.RowCount = 1;
        foreach (var definition in definitions.Where(item => item.Items.Length > 0))
        {
            var heading = new Label
            {
                Text = definition.Title, AccessibleName = definition.Title,
                AccessibleDescription = L.T("Nhóm điều hướng"), TabStop = false,
                Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted,
                BackColor = OrbitManagementTheme.Sidebar, TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true, UseMnemonic = false
            };
            var body = Column(); body.RowCount = definition.Items.Length;
            for (var i = 0; i < definition.Items.Length; i++)
            {
                var button = definition.Items[i]; button.Visible = true;
                button.Dock = DockStyle.Fill; button.Margin = Padding.Empty; button.TabIndex = i;
                button.AccessibleName = button.Text; hints.SetToolTip(button, button.Text);
                body.RowStyles.Add(new RowStyle(SizeType.Absolute, 43)); body.Controls.Add(button, 0, i);
            }
            var container = Column(); container.RowCount = 2;
            container.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            container.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            heading.Dock = body.Dock = container.Dock = DockStyle.Fill;
            container.Controls.Add(heading, 0, 0); container.Controls.Add(body, 0, 1);
            var group = new Group(heading, body, container, definition.Items); groups.Add(group);
            container.Paint += (_, e) =>
            {
                if (!Compact) return;
                using var line = new Pen(OrbitManagementTheme.Border);
                e.Graphics.DrawLine(line, 0, 0, container.Width, 0);
            };
            menu.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            menu.Controls.Add(container, 0, menu.RowCount++);
        }
        menu.ResumeLayout(false); RefreshLayout();
    }

    // Chỉ cuộn tới mục đã chọn; label nhóm không mở/thu hoặc ẩn bất kỳ tab nào.
    public void Reveal(Button selected)
    {
        scroll.ScrollControlIntoView(selected);
    }

    private static TableLayoutPanel Column()
    {
        var panel = new TableLayoutPanel { ColumnCount = 1, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = OrbitManagementTheme.Sidebar, TabStop = false };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); return panel;
    }

    private int P(float value) => OrbitManagementTheme.Px(this, value);

    private void RefreshLayout()
    {
        if (arranging || IsDisposed) return;
        arranging = true;
        try
        {
            shell.SuspendLayout(); menu.SuspendLayout();
            BackColor = shell.BackColor = header.BackColor = footer.BackColor = scroll.BackColor = menu.BackColor = OrbitManagementTheme.Sidebar;
            shell.Padding = new Padding(0, 0, 1, 0);
            shell.RowStyles[0].Height = P(Compact ? 50 : 84);
            shell.RowStyles[2].Height = P(Compact ? 52 : 128);
            menu.Padding = new Padding(P(Compact ? 2 : 8), P(4), P(Compact ? 2 : 8), P(8));
            if (menu.RowStyles.Count > 0) menu.RowStyles[0].Height = showHome ? P(43) : 0;
            if (home is not null) home.Visible = showHome;
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                group.Heading.Visible = !Compact;
                group.Heading.Padding = new Padding(P(8), 0, 0, 0);
                group.Body.Visible = true;
                var headingHeight = Compact ? 0 : P(34);
                var bodyHeight = group.Items.Length * P(43);
                group.Container.RowStyles[0].Height = headingHeight;
                group.Container.RowStyles[1].Height = bodyHeight;
                group.Body.Padding = new Padding(P(Compact ? 0 : 8), 0, 0, 0);
                foreach (RowStyle row in group.Body.RowStyles) row.Height = P(43);
                group.Container.Margin = new Padding(0, P(6), 0, 0);
                menu.RowStyles[i + 1].Height = headingHeight + bodyHeight + P(6);
            }
            if (title is not null && caption is not null)
            {
                title.Visible = caption.Visible = !Compact;
                title.BackColor = caption.BackColor = OrbitManagementTheme.Sidebar;
            }
            Person.Visible = Role.Visible = !Compact;
            Person.BackColor = Role.BackColor = OrbitManagementTheme.Sidebar;
            Person.ForeColor = OrbitManagementTheme.Text; Role.ForeColor = OrbitManagementTheme.Muted;
            if (status is not null) status.Visible = !Compact;
            menu.ResumeLayout(true); shell.ResumeLayout(true);
            PerformLayout(); header.Invalidate(); footer.Invalidate();
        }
        finally { arranging = false; }
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (title is not null && caption is not null)
        {
            title.SetBounds(P(16), P(14), Math.Max(0, header.Width - P(26)), P(32));
            caption.SetBounds(P(16), P(49), Math.Max(0, header.Width - P(26)), P(20));
        }
        Person.SetBounds(P(58), P(12), Math.Max(0, footer.Width - P(68)), P(19));
        Role.SetBounds(P(58), P(33), Math.Max(0, footer.Width - P(68)), P(18));
        logout?.SetBounds(P(Compact ? 6 : 8), P(Compact ? 7 : 59), Math.Max(0, footer.Width - P(Compact ? 12 : 16)), P(36));
        status?.SetBounds(P(12), P(99), Math.Max(0, footer.Width - P(24)), P(27));
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e); RefreshLayout();
    }

    private void PaintHeader(object? sender, PaintEventArgs e)
    {
        using var border = new Pen(OrbitManagementTheme.Border);
        e.Graphics.DrawLine(border, P(12), header.Height - 1, header.Width - P(12), header.Height - 1);
        if (Compact) OrbitManagementTheme.DrawIcon(e.Graphics, new RectangleF((header.Width - P(22)) / 2F, P(14), P(22), P(22)), "coffee", OrbitManagementTheme.Caramel);
    }

    private void PaintFooter(object? sender, PaintEventArgs e)
    {
        using var border = new Pen(OrbitManagementTheme.Border);
        e.Graphics.DrawLine(border, P(8), 0, footer.Width - P(8), 0);
        if (Compact || Person.Text.Length == 0) return;
        var rect = new Rectangle(P(16), P(15), P(32), P(32));
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(OrbitManagementTheme.Selected); e.Graphics.FillEllipse(fill, rect);
        var initials = string.Concat(Person.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).TakeLast(2).Select(word => word[0])).ToUpperInvariant();
        TextRenderer.DrawText(e.Graphics, initials, OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Small), rect, OrbitManagementTheme.Caramel, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) hints.Dispose(); base.Dispose(disposing);
    }

    private sealed record Group(Label Heading, TableLayoutPanel Body, TableLayoutPanel Container, Button[] Items);
}
