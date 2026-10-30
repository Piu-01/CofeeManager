using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Khung trình bày dùng chung; caller bố trí children ở trong viền.
public class OrbitSurface : UserControl
{
    public OrbitSurface()
    {
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = OrbitManagementTheme.Surface; ForeColor = OrbitManagementTheme.Text;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); if (Width < 2 || Height < 2) return;
        var scale = DeviceDpi / 96F;
        e.Graphics.SmoothingMode = global::System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = OrbitManagementTheme.Round(new RectangleF(scale, scale, Width - 2 * scale, Height - 2 * scale), 10 * scale);
        using var pen = new Pen(OrbitManagementTheme.Border, scale); e.Graphics.DrawPath(pen, path);
    }
}
