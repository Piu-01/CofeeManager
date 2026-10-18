using System.Diagnostics;
using System.Drawing.Drawing2D;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public class OrbitButton : Button
{
    private bool hovered;
    private bool pressed;
    private float hoverProgress;
    private float pressProgress;
    private float hoverFrom;
    private float pressFrom;
    private long started;
    private long sheenStarted;
    private long rippleStarted;
    private Point rippleOrigin;

    public OrbitButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var scale = DeviceDpi / 96F;
        var text = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.SingleLine);
        return new Size(text.Width + (int)((Image is null ? 24 : 52) * scale) + Padding.Horizontal,
            Math.Max(text.Height, Image is null ? 0 : (int)(20 * scale)) + (int)(12 * scale) + Padding.Vertical);
    }

    // Hiệu ứng chỉ vẽ; Button gốc giữ Click, DialogResult và xử lý bàn phím.
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; sheenStarted = Stopwatch.GetTimestamp(); Animate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; pressed = false; Animate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { pressed = true; Ripple(e.Location); Animate(); } }
    protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Animate(); base.OnMouseUp(e); }
    protected override void OnMouseCaptureChanged(EventArgs e) { pressed = false; Animate(); base.OnMouseCaptureChanged(e); }
    protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Space && !pressed) { pressed = true; Ripple(new Point(Width / 2, Height / 2)); Animate(); } }
    protected override void OnKeyUp(KeyEventArgs e) { pressed = false; Animate(); base.OnKeyUp(e); }
    protected override void OnLostFocus(EventArgs e) { pressed = false; Animate(); base.OnLostFocus(e); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (!Enabled) { hovered = pressed = false; hoverProgress = pressProgress = 0; ButtonMotion.Remove(this); }
        Invalidate();
    }
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) { hovered = pressed = false; hoverProgress = pressProgress = 0; ButtonMotion.Remove(this); }
    }

    private void Animate()
    {
        if (!Enabled || !Visible || IsDisposed) return;
        hoverFrom = hoverProgress; pressFrom = pressProgress; started = Stopwatch.GetTimestamp();
        ButtonMotion.Add(this); Invalidate();
    }

    private void Ripple(Point origin)
    {
        rippleOrigin = origin; rippleStarted = Stopwatch.GetTimestamp();
    }

    internal bool Advance()
    {
        if (IsDisposed || !Visible || !Enabled) return false;
        var elapsed = (float)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        hoverProgress = Lerp(hoverFrom, hovered ? 1 : 0, Math.Min(1, elapsed / AppTheme.HoverDuration));
        pressProgress = Lerp(pressFrom, pressed ? 1 : 0, Math.Min(1, elapsed / (pressed ? AppTheme.PressDuration : AppTheme.ReleaseDuration)));
        Invalidate();
        return elapsed < Math.Max(AppTheme.HoverDuration, AppTheme.ReleaseDuration)
            || hovered && sheenStarted != 0 && Stopwatch.GetElapsedTime(sheenStarted).TotalMilliseconds < AppTheme.SheenDuration
            || rippleStarted != 0 && Stopwatch.GetElapsedTime(rippleStarted).TotalMilliseconds < AppTheme.RippleDuration;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? AppTheme.Background);
        var scale = DeviceDpi / 96F;
        var primary = BackColor == AppTheme.Primary;
        var selected = BackColor == AppTheme.Selected;
        var bounds = new RectangleF(2 * scale, (2 + pressProgress * 2) * scale, Width - 4 * scale, Height - 7 * scale);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var radius = Math.Min(AppTheme.ButtonRadius * scale, bounds.Height / 2);
        using var path = Rounded(bounds, radius);
        var shadowBounds = bounds; shadowBounds.Offset(0, (3 - pressProgress * 2) * scale);
        using var shadowPath = Rounded(shadowBounds, radius);
        using var shadow = new SolidBrush(Color.FromArgb(Enabled ? 100 : 35, Color.Black));
        g.FillPath(shadow, shadowPath);
        var top = !Enabled ? AppTheme.Border : primary ? Blend(AppTheme.Primary, AppTheme.PrimaryLight, hoverProgress) : Blend(BackColor, AppTheme.Hover, hoverProgress);
        var bottom = !Enabled ? AppTheme.Surface : primary ? AppTheme.PrimaryShade : BackColor;
        using var fill = new LinearGradientBrush(bounds, top, bottom, 90F);
        g.FillPath(fill, path);
        // Sheen/ripple được cắt trong nút, khác hoàn toàn overlay bụi sao thanh toán.
        var drawingState = g.Save();
        g.SetClip(path);
        var sheenTime = sheenStarted == 0 ? 1 : (float)Stopwatch.GetElapsedTime(sheenStarted).TotalMilliseconds / AppTheme.SheenDuration;
        if (Enabled && hovered && sheenTime < 1)
        {
            var sheenBounds = new RectangleF(bounds.Left + (bounds.Width + 80 * scale) * sheenTime - 80 * scale, bounds.Top, 80 * scale, bounds.Height);
            using var sheen = new LinearGradientBrush(sheenBounds, Color.Transparent, Color.FromArgb(40, Color.White), 0F);
            g.FillRectangle(sheen, sheenBounds);
        }
        var rippleTime = rippleStarted == 0 ? 1 : (float)Stopwatch.GetElapsedTime(rippleStarted).TotalMilliseconds / AppTheme.RippleDuration;
        if (Enabled && rippleTime < 1)
        {
            var radiusRipple = Math.Max(Width, Height) * rippleTime;
            using var ripple = new SolidBrush(Color.FromArgb((int)(40 * (1 - rippleTime)), primary ? AppTheme.OnPrimary : AppTheme.Accent));
            g.FillEllipse(ripple, rippleOrigin.X - radiusRipple, rippleOrigin.Y - radiusRipple, radiusRipple * 2, radiusRipple * 2);
        }
        g.Restore(drawingState);
        using var border = new Pen(Enabled && hovered ? AppTheme.Accent : selected ? AppTheme.SelectedBorder : AppTheme.Border, scale);
        g.DrawPath(border, path);
        var textBounds = Rectangle.Round(bounds);
        textBounds.Inflate(-(int)(10 * scale), 0);
        if (Image is not null)
        {
            var size = (int)(20 * scale);
            var imageBounds = new Rectangle(textBounds.Left, textBounds.Top + (textBounds.Height - size) / 2, size, size);
            g.DrawImage(Image, imageBounds);
            textBounds.X += size + (int)(8 * scale); textBounds.Width -= size + (int)(8 * scale);
        }
        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
        if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
        if (!UseMnemonic) flags |= TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(g, Text, Font, textBounds, !Enabled ? AppTheme.Muted : primary ? AppTheme.OnPrimary : ForeColor, flags);
        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(Rectangle.Round(bounds), -(int)(4 * scale), -(int)(4 * scale));
            ControlPaint.DrawFocusRectangle(g, focus, primary ? AppTheme.OnPrimary : AppTheme.Accent, top);
        }
    }

    private static float Lerp(float from, float to, float amount) => from + (to - from) * amount;
    private static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
        (int)Lerp(from.R, to.R, amount), (int)Lerp(from.G, to.G, amount), (int)Lerp(from.B, to.B, amount));

    private static GraphicsPath Rounded(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath(); var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure(); return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) ButtonMotion.Remove(this);
        base.Dispose(disposing);
    }
}

// Một timer dùng chung cho các nút đang chuyển trạng thái, dừng và hủy khi rảnh.
internal static class ButtonMotion
{
    private static readonly HashSet<OrbitButton> active = [];
    private static System.Windows.Forms.Timer? timer;
    public static void Add(OrbitButton button)
    {
        active.Add(button);
        if (timer is not null) return;
        timer = new System.Windows.Forms.Timer { Interval = 16 };
        timer.Tick += Tick; timer.Start();
    }
    public static void Remove(OrbitButton button) { active.Remove(button); StopIfIdle(); }
    private static void Tick(object? sender, EventArgs e)
    {
        foreach (var button in active.ToArray()) if (!button.Advance()) active.Remove(button);
        StopIfIdle();
    }
    private static void StopIfIdle()
    {
        if (active.Count != 0 || timer is null) return;
        timer.Stop(); timer.Tick -= Tick; timer.Dispose(); timer = null;
    }
}
