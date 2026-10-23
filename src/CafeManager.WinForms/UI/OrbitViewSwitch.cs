using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Nhóm nút native; host giữ handler và trạng thái chuyển cách xem.
public sealed class OrbitViewSwitch : UserControl
{
    public OrbitViewSwitch(params OrbitButton[] buttons)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = OrbitManagementTheme.Surface;
        Controls.AddRange(buttons);
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e); if (Controls.Count == 0) return;
        var gap = OrbitManagementTheme.Px(this, 3); var x = gap;
        var preferred = Controls.Cast<Control>().Sum(c => c.GetPreferredSize(Size.Empty).Width);
        var available = Math.Max(1, Width - gap * (Controls.Count + 1));
        foreach (OrbitButton button in Controls)
        {
            var size = button.GetPreferredSize(Size.Empty); var width = Math.Max(1, available * size.Width / Math.Max(1, preferred));
            button.SetBounds(x, (Height - size.Height) / 2, width, size.Height); x += width + gap;
        }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = OrbitManagementTheme.Round(new RectangleF(.5F, .5F, Width - 1, Height - 1), OrbitManagementTheme.Px(this, 8));
        using var border = new Pen(OrbitManagementTheme.InputBorder); e.Graphics.DrawPath(border, path);
    }
}
