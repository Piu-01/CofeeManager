using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Nhãn có chữ trong palette chung; caller giữ trạng thái nghiệp vụ.
public sealed class OrbitBadge : Label
{
    public OrbitBadge()
    {
        AutoSize = false; UseMnemonic = false; TextAlign = ContentAlignment.MiddleCenter;
        ForeColor = OrbitManagementTheme.Caramel; BackColor = OrbitManagementTheme.Surface;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 2 || Height < 2) return;
        var scale = DeviceDpi / 96F;
        e.Graphics.SmoothingMode = global::System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = OrbitManagementTheme.Round(new RectangleF(scale, scale, Width - 2 * scale, Height - 2 * scale), 4 * scale);
        using var fill = new SolidBrush(OrbitManagementTheme.Selected); e.Graphics.FillPath(fill, path);
        using var pen = new Pen(Color.FromArgb(130, ForeColor), scale); e.Graphics.DrawPath(pen, path);
        TextRenderer.DrawText(e.Graphics, Text, OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Small), ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}
