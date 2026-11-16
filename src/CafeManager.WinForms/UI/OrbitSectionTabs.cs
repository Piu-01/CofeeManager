using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Thanh tab bằng Button thật; host sở hữu nội dung và vòng đời query.
public sealed class OrbitSectionTabs : FlowLayoutPanel
{
    private int selectedIndex = -1;
    [System.ComponentModel.DefaultValue(-1)]
    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            if (value == selectedIndex || value < 0 || value >= Controls.Count) return;
            selectedIndex = value;
            foreach (OrbitButton button in Controls) { button.NavigationSelected = button.TabIndex == value; button.Invalidate(); }
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? SelectedIndexChanged;
    [System.ComponentModel.DefaultValue(false)]
    public bool Underline { get; set; }
    private bool wrap;
    // Wrap: tab tự xuống dòng khi hết chỗ; chiều cao do host tính và gán.
    [System.ComponentModel.DefaultValue(false)]
    public bool Wrap
    {
        get => wrap;
        set
        {
            if (wrap == value) return;
            wrap = value; WrapContents = value; AutoScroll = !value; PerformLayout();
        }
    }
    public OrbitSectionTabs()
    {
        Dock = DockStyle.Fill; WrapContents = false; AutoScroll = true; Margin = Padding.Empty;
        BackColor = OrbitManagementTheme.Background;
    }
    public void Add(string caption, string icon = "")
    {
        var index = Controls.Count;
        var button = new OrbitButton { Text = caption, AccessibleName = caption, TabIndex = index, UnderlineSelection = Underline };
        OrbitManagementTheme.Button(button, ManagementButtonStyle.Tab, icon);
        button.Margin = new Padding(0, 0, OrbitManagementTheme.Px(this, 8), 0);
        button.Click += (_, _) => SelectedIndex = index;
        button.KeyDown += (_, e) =>
        {
            if (e.KeyCode is not (Keys.Left or Keys.Right)) return;
            e.Handled = e.SuppressKeyPress = true;
            SelectedIndex = (selectedIndex + (e.KeyCode == Keys.Right ? 1 : Controls.Count - 1)) % Controls.Count;
            Controls[selectedIndex].Focus();
        };
        Controls.Add(button); if (selectedIndex < 0) SelectedIndex = 0;
    }
    // Dựng lại tab động (caption lấy từ DB) mà không bắn sự kiện chọn.
    public void Clear()
    {
        foreach (Control control in Controls.Cast<Control>().ToArray()) control.Dispose();
        Controls.Clear(); selectedIndex = -1;
    }
    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        // Sau khi HWND con có DPI thật, áp lại kích thước tab thay giá trị trước autoscale.
        PerformLayout();
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        var gap = OrbitManagementTheme.Px(this, 8);
        if (Wrap)
        {
            // FlowLayoutPanel tự xuống dòng theo WrapContents; chỉ gán preferred size + khoảng cách.
            foreach (OrbitButton button in Controls)
            {
                var preferred = button.GetPreferredSize(Size.Empty);
                button.Size = new Size(preferred.Width, preferred.Height);
                button.Margin = new Padding(0, 0, gap, OrbitManagementTheme.Px(this, 6));
            }
            base.OnLayout(e); return;
        }
        var totalWidth = Controls.Cast<OrbitButton>().Sum(button => button.GetPreferredSize(Size.Empty).Width + gap);
        var availableHeight = Math.Max(1, Height - Padding.Vertical - (totalWidth > Width - Padding.Horizontal ? SystemInformation.HorizontalScrollBarHeight : 0));
        foreach (OrbitButton button in Controls)
        {
            var preferred = button.GetPreferredSize(Size.Empty);
            // Autoscale có thể làm host thấp hơn preferred 1px ở125%; không bật cuộn dọc.
            button.Size = new Size(preferred.Width, Math.Min(preferred.Height, availableHeight));
            button.Margin = new Padding(0, 0, gap, 0);
        }
        base.OnLayout(e);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); if (!Underline) return;
        using var border = new Pen(OrbitManagementTheme.Border); e.Graphics.DrawLine(border, 0, Height - 1, Width, Height - 1);
    }
}
