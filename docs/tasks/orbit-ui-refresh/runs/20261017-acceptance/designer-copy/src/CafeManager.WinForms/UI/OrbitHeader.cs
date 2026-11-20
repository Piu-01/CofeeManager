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
        var diameter = Math.Min(Height * .85F, 130 * scale);
        var planet = new RectangleF(Width - diameter - 24 * scale, (Height - diameter) / 2, diameter, diameter);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var body = new LinearGradientBrush(planet, Color.FromArgb(80, AppTheme.Primary), Color.FromArgb(18, AppTheme.Accent), 45F);
        e.Graphics.FillEllipse(body, planet);
        using var orbit = new Pen(Color.FromArgb(65, AppTheme.Primary), scale);
        e.Graphics.DrawEllipse(orbit, planet.Left - diameter * .35F, planet.Top + diameter * .3F, diameter * 1.7F, diameter * .45F);
        using var border = new Pen(AppTheme.Border, scale);
        e.Graphics.DrawLine(border, 0, Height - 1, Width, Height - 1);
    }
}
