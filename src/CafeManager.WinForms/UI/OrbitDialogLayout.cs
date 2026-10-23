using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Chỉ quản lý hình thức; form sở hữu validation, nháp và thao tác ghi.
public sealed class OrbitDialogLayout : TableLayoutPanel
{
    private readonly Label heading;
    private readonly Control body;
    private readonly Label status;
    private readonly FlowLayoutPanel actions;

    public OrbitDialogLayout(Form owner, Control body, Label status, FlowLayoutPanel actions)
    {
        this.body = body; this.status = status; this.actions = actions;
        Dock = DockStyle.Fill;
        BackColor = OrbitManagementTheme.Background; ForeColor = OrbitManagementTheme.Text;
        Font = OrbitManagementTheme.Body; Padding = new Padding(24); ColumnCount = 1; RowCount = 4;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize)); RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize)); RowStyles.Add(new RowStyle(SizeType.AutoSize));
        heading = new Label { Text = owner.Text, Font = OrbitManagementTheme.CardTitle, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 20), UseMnemonic = false };
        owner.TextChanged += (_, _) => heading.Text = owner.Text;
        body.Dock = DockStyle.Fill; body.Margin = Padding.Empty; body.BackColor = OrbitManagementTheme.Surface;
        status.AutoSize = true; status.Dock = DockStyle.Fill; status.Margin = new Padding(0, 12, 0, 0);
        status.Padding = new Padding(12); status.BackColor = OrbitManagementTheme.Surface; status.ForeColor = OrbitManagementTheme.Text;
        status.Visible = !string.IsNullOrEmpty(status.Text);
        status.TextChanged += (_, _) => { status.Visible = !string.IsNullOrEmpty(status.Text); PerformLayout(); };
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 20, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actions.Dock = DockStyle.Fill; actions.AutoSize = true; actions.WrapContents = false; actions.Padding = Padding.Empty; actions.Margin = Padding.Empty;
        footer.Controls.Add(actions, 1, 0);
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

    public void FitToContent(Form owner, bool hasTable = false)
    {
        var area = Screen.FromControl(owner).WorkingArea; int P(int n) => OrbitManagementTheme.Px(owner, n);
        owner.MaximumSize = area.Size;
        var width = Math.Min(Math.Max(P(hasTable ? 860 : 560), owner.ClientSize.Width), Math.Max(P(320), area.Width - P(32)));
        var bodyWidth = Math.Max(1, width - Padding.Horizontal);
        var content = hasTable ? P(390) : body.GetPreferredSize(new Size(bodyWidth, 0)).Height;
        var height = Padding.Vertical + heading.GetPreferredSize(new Size(bodyWidth, 0)).Height + heading.Margin.Vertical
            + Math.Max(P(80), content) + P(20) + actions.GetPreferredSize(Size.Empty).Height;
        if (!string.IsNullOrEmpty(status.Text)) height += status.GetPreferredSize(new Size(bodyWidth, 0)).Height + status.Margin.Vertical;
        var frame = owner.Size - owner.ClientSize;
        owner.ClientSize = new Size(Math.Max(1, width - frame.Width), Math.Min(height, Math.Max(P(200), area.Height - frame.Height - P(32))));
        owner.Location = new Point(Math.Clamp(owner.Left, area.Left, Math.Max(area.Left, area.Right - owner.Width)), Math.Clamp(owner.Top, area.Top, Math.Max(area.Top, area.Bottom - owner.Height)));
    }
}
