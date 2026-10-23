using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public sealed class OrbitSearchBox : UserControl
{
    public TextBox Input { get; } = new() { BorderStyle = BorderStyle.None, PlaceholderText = "Tìm tên hoặc mã nguyên liệu…", BackColor = OrbitManagementTheme.Input, ForeColor = OrbitManagementTheme.Text, Font = OrbitManagementTheme.Table };
    public OrbitSearchBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = OrbitManagementTheme.Surface; Controls.Add(Input);
        Input.Enter += (_, _) => Invalidate(); Input.Leave += (_, _) => Invalidate();
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e); var left = OrbitManagementTheme.Px(this, 36);
        Input.SetBounds(left, (Height - Input.PreferredHeight) / 2, Math.Max(1, Width - left - OrbitManagementTheme.Px(this, 12)), Input.PreferredHeight);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var scale = DeviceDpi / 96F; e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = OrbitManagementTheme.Round(new RectangleF(scale, scale, Width - 2 * scale, Height - 2 * scale), 8 * scale);
        using var fill = new SolidBrush(OrbitManagementTheme.Input); e.Graphics.FillPath(fill, path);
        using var border = new Pen(ContainsFocus ? OrbitManagementTheme.Lilac : OrbitManagementTheme.InputBorder, scale); e.Graphics.DrawPath(border, path);
        OrbitManagementTheme.DrawIcon(e.Graphics, new RectangleF(12 * scale, (Height - 16 * scale) / 2, 16 * scale, 16 * scale), "search", OrbitManagementTheme.Muted);
    }
}
