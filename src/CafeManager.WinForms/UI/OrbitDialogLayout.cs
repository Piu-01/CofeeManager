using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Chỉ quản lý hình thức; form sở hữu validation, nháp và thao tác ghi.
public sealed class OrbitDialogLayout : TableLayoutPanel
{
    private readonly Control heading;
    private readonly Control body;
    private readonly Label status;
    private readonly FlowLayoutPanel actions;
    private readonly TableLayoutPanel footer;
    private bool responsiveActions;
    private bool arrangingActions;

    public OrbitDialogLayout(Form owner, Control body, Label status, FlowLayoutPanel actions, Control? header = null, Control? leadingAction = null)
    {
        this.body = body; this.status = status; this.actions = actions;
        Dock = DockStyle.Fill;
        BackColor = OrbitManagementTheme.Background; ForeColor = OrbitManagementTheme.Text;
        Font = OrbitManagementTheme.Body; Padding = new Padding(24); ColumnCount = 1; RowCount = 4;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize)); RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize)); RowStyles.Add(new RowStyle(SizeType.AutoSize));
        heading = header ?? new Label { Text = owner.Text, Font = OrbitManagementTheme.CardTitle, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 20), UseMnemonic = false };
        if (header is null) owner.TextChanged += (_, _) => heading.Text = owner.Text;
        body.Dock = DockStyle.Fill; body.Margin = Padding.Empty; body.BackColor = OrbitManagementTheme.Surface;
        status.AutoSize = true; status.Dock = DockStyle.Fill; status.Margin = new Padding(0, 12, 0, 0);
        status.Padding = new Padding(12); status.BackColor = OrbitManagementTheme.Surface; status.ForeColor = OrbitManagementTheme.Text;
        status.Visible = !string.IsNullOrEmpty(status.Text);
        status.TextChanged += (_, _) => { status.Visible = !string.IsNullOrEmpty(status.Text); PerformLayout(); };
        footer = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 20, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actions.Dock = DockStyle.Fill; actions.AutoSize = true; actions.WrapContents = false; actions.Padding = Padding.Empty; actions.Margin = Padding.Empty;
        footer.Controls.Add(actions, 1, 0);
        if (leadingAction is not null) { leadingAction.Anchor = AnchorStyles.Left; footer.Controls.Add(leadingAction, 0, 0); }
        footer.Visible = actions.Controls.Count > 0;
        Controls.Add(heading, 0, 0); Controls.Add(body, 0, 1); Controls.Add(status, 0, 2); Controls.Add(footer, 0, 3);
        owner.Font = OrbitManagementTheme.Body; owner.BackColor = BackColor; owner.ForeColor = ForeColor;
        owner.MinimizeBox = false; owner.ShowInTaskbar = false;
        Layout += (_, _) => status.MaximumSize = new Size(Math.Max(1, ClientSize.Width - Padding.Horizontal), 0);
        actions.ControlAdded += (_, e) => { if (e.Control is OrbitButton button) StyleButton(button); };
        foreach (var button in actions.Controls.OfType<OrbitButton>()) StyleButton(button);
    }

    private static void StyleButton(OrbitButton button)
    {
        var primary = button.Text.Replace("&", "") is "Lưu" or "Xác nhận";
        OrbitManagementTheme.Button(button, primary ? ManagementButtonStyle.Primary : ManagementButtonStyle.Secondary, "");
        button.UseMnemonic = true; button.Margin = new Padding(8, 0, 0, 0);
    }

    // Opt-in cho các dialog G05 có nhiều hành động; không đổi hành vi dialog cũ.
    public void EnableResponsiveActions(Form owner, bool hasTable = false)
    {
        if (responsiveActions) return;
        responsiveActions = true;
        // Cấp chiều ngang thật trước khi đo wrap; cột AutoSize chỉ lấy rộng nút lớn nhất.
        footer.AutoSize = false;
        footer.ColumnStyles[0].SizeType = SizeType.Absolute;
        footer.ColumnStyles[1].SizeType = SizeType.Percent;
        footer.ColumnStyles[1].Width = 100;
        actions.AutoSize = false;
        actions.Dock = DockStyle.None;
        actions.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        actions.WrapContents = true;
        RowStyles[3].SizeType = SizeType.Absolute;
        Layout += (_, _) => ArrangeResponsiveActions();
        actions.Layout += (_, _) => ArrangeResponsiveActions();
        owner.Shown += (_, _) =>
        {
            var area = Screen.FromControl(owner).WorkingArea;
            owner.MinimumSize = new Size(Math.Min(owner.MinimumSize.Width, area.Width), Math.Min(owner.MinimumSize.Height, area.Height));
            ArrangeResponsiveActions(); FitToContent(owner, hasTable); ArrangeResponsiveActions();
        };
    }

    private int LeadingWidth => footer.GetControlFromPosition(0, 0) is { Visible: true } leading
        ? leading.GetPreferredSize(Size.Empty).Width + leading.Margin.Horizontal : 0;

    private int ResponsiveActionHeight(int width)
    {
        var actionHeight = actions.GetPreferredSize(new Size(Math.Max(1, width - LeadingWidth), 0)).Height;
        var leading = footer.GetControlFromPosition(0, 0);
        return Math.Max(actionHeight, leading is { Visible: true } ? leading.Height + leading.Margin.Vertical : 0);
    }

    private void ArrangeResponsiveActions()
    {
        if (arrangingActions || IsDisposed) return;
        arrangingActions = true;
        try
        {
            var width = Math.Max(1, ClientSize.Width - Padding.Horizontal - footer.Margin.Horizontal);
            footer.ColumnStyles[0].Width = LeadingWidth;
            var height = ResponsiveActionHeight(width);
            actions.Size = new Size(Math.Min(Math.Max(1, width - LeadingWidth), actions.GetPreferredSize(Size.Empty).Width), height);
            footer.Height = height;
            RowStyles[3].Height = height + footer.Margin.Vertical;
        }
        finally { arrangingActions = false; }
    }

    public void FitToContent(Form owner, bool hasTable = false)
    {
        var area = Screen.FromControl(owner).WorkingArea; int P(int n) => OrbitManagementTheme.Px(owner, n);
        owner.MaximumSize = area.Size;
        var width = Math.Min(Math.Max(P(hasTable ? 860 : 560), owner.ClientSize.Width), Math.Max(P(320), area.Width - P(32)));
        var bodyWidth = Math.Max(1, width - Padding.Horizontal);
        var content = hasTable ? P(390) : body.GetPreferredSize(new Size(bodyWidth, 0)).Height;
        var height = Padding.Vertical + heading.GetPreferredSize(new Size(bodyWidth, 0)).Height + heading.Margin.Vertical
            + Math.Max(P(80), content) + P(20) + (responsiveActions ? ResponsiveActionHeight(bodyWidth) : actions.GetPreferredSize(Size.Empty).Height);
        if (!string.IsNullOrEmpty(status.Text)) height += status.GetPreferredSize(new Size(bodyWidth, 0)).Height + status.Margin.Vertical;
        var frame = owner.Size - owner.ClientSize;
        var desired = new Size(Math.Max(1, width - frame.Width), Math.Min(height, Math.Max(P(200), area.Height - frame.Height - P(32))));
        owner.ClientSize = desired;
        owner.Location = new Point(Math.Clamp(owner.Left, area.Left, Math.Max(area.Left, area.Right - owner.Width)), Math.Clamp(owner.Top, area.Top, Math.Max(area.Top, area.Bottom - owner.Height)));
    }
}
