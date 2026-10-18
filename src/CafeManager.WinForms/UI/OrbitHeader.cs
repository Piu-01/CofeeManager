using System.Drawing.Drawing2D;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public class OrbitHeader : TableLayoutPanel
{
    public OrbitHeader()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    // Họa tiết nhận diện thuần trang trí, không chiếm focus hoặc nhận thao tác.
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        using var background = new LinearGradientBrush(ClientRectangle, AppTheme.Surface, AppTheme.Espresso, 20F);
        e.Graphics.FillRectangle(background, ClientRectangle);
        var scale = DeviceDpi / 96F;
        // Vùng hẹp ưu tiên tiêu đề; không vẽ họa tiết phía sau chữ.
        if (Width < 600 * scale) return;
        var diameter = Math.Min(Height * .85F, 110 * scale);
        var planet = new RectangleF(Width - diameter - 24 * scale, (Height - diameter) / 2, diameter, diameter);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var glowPath = new GraphicsPath();
        var glowBounds = RectangleF.Inflate(planet, diameter * .35F, diameter * .35F);
        glowPath.AddEllipse(glowBounds);
        using var glow = new PathGradientBrush(glowPath) { CenterColor = Color.FromArgb(35, AppTheme.Accent), SurroundColors = [Color.Transparent] };
        e.Graphics.FillPath(glow, glowPath);
        using var orbit = new Pen(Color.FromArgb(85, AppTheme.Primary), scale);
        var ring = new RectangleF(planet.Left - diameter * .35F, planet.Top + diameter * .3F, diameter * 1.7F, diameter * .45F);
        e.Graphics.DrawArc(orbit, ring, 180, 180);
        using var sphere = new GraphicsPath(); sphere.AddEllipse(planet);
        using var body = new PathGradientBrush(sphere)
        {
            CenterPoint = new PointF(planet.Left + diameter * .28F, planet.Top + diameter * .22F),
            CenterColor = Color.FromArgb(230, AppTheme.PrimaryLight),
            SurroundColors = [Color.FromArgb(255, 37, 25, 43)]
        };
        e.Graphics.FillEllipse(body, planet);
        e.Graphics.DrawArc(orbit, ring, 0, 180);
        using var border = new Pen(AppTheme.Border, scale);
        e.Graphics.DrawLine(border, 0, Height - 1, Width, Height - 1);
    }
}
