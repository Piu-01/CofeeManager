using System.Diagnostics;
using System.Drawing.Drawing2D;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public class OrbitButton : Button
{
    [System.ComponentModel.DefaultValue(null)]
    public ManagementButtonStyle? ManagementStyle { get; set; }
    [System.ComponentModel.DefaultValue("")]
    public string ManagementIcon { get; set; } = "";
    [System.ComponentModel.DefaultValue(false)]
    public bool NavigationSelected { get; set; }
    [System.ComponentModel.DefaultValue(false)]
    public bool UnderlineSelection { get; set; }
    protected float HoverAmount => hoverProgress;
    protected float PressAmount => pressProgress;
    [System.ComponentModel.DefaultValue(false)]
    public bool AlignTextLeft { get; set; }
    [System.ComponentModel.DefaultValue(false)]
    public bool Quiet { get; set; }
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
        if (ManagementStyle is { } style)
        {
            var height = style switch { ManagementButtonStyle.Primary or ManagementButtonStyle.Secondary or ManagementButtonStyle.Icon => 40, ManagementButtonStyle.Pager => 30, ManagementButtonStyle.Navigation => 43, ManagementButtonStyle.Tab => 38, ManagementButtonStyle.Segment => 31, _ => 33 };
            var width = style switch
            {
                ManagementButtonStyle.Icon => 40,
                ManagementButtonStyle.Pager => 38,
                ManagementButtonStyle.Navigation => 174,
                _ => (int)Math.Ceiling(TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width / scale) + (ManagementIcon.Length > 0 ? (style == ManagementButtonStyle.Segment ? 21 : 23) : 0) + (style is ManagementButtonStyle.Primary or ManagementButtonStyle.Secondary ? 28 : style == ManagementButtonStyle.Tab ? (UnderlineSelection ? 20 : 34) : style == ManagementButtonStyle.Segment ? 14 : 20)
            };
            return new Size((int)Math.Round(width * scale), (int)Math.Round(height * scale));
        }
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
        if (ManagementStyle is { } style) { PaintManagement(e, style); return; }
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
        if (!Quiet) g.FillPath(shadow, shadowPath);
        var top = !Enabled ? AppTheme.Border : primary ? Blend(AppTheme.Primary, AppTheme.PrimaryLight, hoverProgress) : Blend(BackColor, AppTheme.Hover, hoverProgress);
        var bottom = !Enabled ? AppTheme.Surface : primary ? AppTheme.PrimaryShade : BackColor;
        using var fill = new LinearGradientBrush(bounds, top, bottom, 90F);
        if (!Quiet || hovered || pressed || selected) g.FillPath(fill, path);
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
        if (!Quiet || hovered || selected || Focused) g.DrawPath(border, path);
        var textBounds = Rectangle.Round(bounds);
        textBounds.Inflate(-(int)(10 * scale), 0);
        if (Image is not null)
        {
            var size = (int)(20 * scale);
            var imageBounds = new Rectangle(textBounds.Left, textBounds.Top + (textBounds.Height - size) / 2, size, size);
            g.DrawImage(Image, imageBounds);
            textBounds.X += size + (int)(8 * scale); textBounds.Width -= size + (int)(8 * scale);
        }
        var flags = (AlignTextLeft ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter) | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
        if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
        if (!UseMnemonic) flags |= TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(g, Text, Font, textBounds, !Enabled ? AppTheme.Muted : primary ? AppTheme.OnPrimary : ForeColor, flags);
        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(Rectangle.Round(bounds), -(int)(4 * scale), -(int)(4 * scale));
            ControlPaint.DrawFocusRectangle(g, focus, primary ? AppTheme.OnPrimary : AppTheme.Accent, top);
        }
    }

    private void PaintManagement(PaintEventArgs e, ManagementButtonStyle style)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? OrbitManagementTheme.Surface);
        var scale = DeviceDpi / 96F;
        var primary = style == ManagementButtonStyle.Primary;
        var navigation = style == ManagementButtonStyle.Navigation;
        var quiet = style is ManagementButtonStyle.Action or ManagementButtonStyle.Danger or ManagementButtonStyle.Tab or ManagementButtonStyle.Segment || navigation;
        var bounds = new RectangleF(scale, (1 + pressProgress * 2) * scale, Width - 2 * scale, Height - (primary ? 4 : 2) * scale);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var path = Rounded(bounds, 8 * scale);
        var ink = !Enabled ? Color.FromArgb(126, 112, 137) : primary ? Color.FromArgb(41, 26, 17) : NavigationSelected ? style == ManagementButtonStyle.Segment ? OrbitManagementTheme.Lilac : OrbitManagementTheme.Caramel : style == ManagementButtonStyle.Danger ? Color.FromArgb(241, 170, 167) : OrbitManagementTheme.Muted;
        if (Enabled && hoverProgress > 0)
        {
            using var halo = new Pen(Color.FromArgb((int)(45 * hoverProgress), primary ? OrbitManagementTheme.Caramel : OrbitManagementTheme.Lilac), 4 * scale);
            g.DrawPath(halo, path);
        }
        if (primary)
        {
            using var shadow = new SolidBrush(Color.FromArgb(120, 80, 51));
            var saved = g.Save(); g.TranslateTransform(0, (3 - pressProgress * 2) * scale); g.FillPath(shadow, path); g.Restore(saved);
        }
        var top = primary ? Blend(Color.FromArgb(255, 228, 191), Color.FromArgb(255, 240, 217), hoverProgress) : NavigationSelected ? style == ManagementButtonStyle.Segment ? Color.FromArgb(58, 44, 69) : Color.FromArgb(60, 44, 34) : Blend(Color.FromArgb(37, 30, 47), Color.FromArgb(72, 52, 81), hoverProgress);
        var bottom = primary ? Color.FromArgb(222, 166, 109) : NavigationSelected ? style == ManagementButtonStyle.Segment ? Color.FromArgb(58, 44, 69) : Color.FromArgb(42, 32, 36) : Color.FromArgb(34, 27, 43);
        if (!Enabled) { top = bottom = Color.FromArgb(34, 28, 41); }
        if ((!quiet || hovered || pressed || NavigationSelected) && !(UnderlineSelection && style == ManagementButtonStyle.Tab))
        {
            using var fill = new LinearGradientBrush(bounds, top, bottom, 100F); g.FillPath(fill, path);
            using var border = new Pen(primary ? Color.FromArgb(231, 183, 127) : NavigationSelected ? Color.FromArgb(121, 92, 67) : hovered ? Color.FromArgb(147, 119, 166) : OrbitManagementTheme.Border, scale);
            g.DrawPath(border, path);
            if (navigation && NavigationSelected) { using var line = new Pen(OrbitManagementTheme.Caramel, 2 * scale); g.DrawLine(line, bounds.Left + scale, bounds.Top + 8 * scale, bounds.Left + scale, bounds.Bottom - 8 * scale); }
        }
        if (style == ManagementButtonStyle.Tab && NavigationSelected)
        {
            using var line = new Pen(OrbitManagementTheme.Caramel, 2 * scale);
            g.DrawLine(line, bounds.Left + 1, Height - 2 * scale, bounds.Right - 1, Height - 2 * scale);
        }
        var snapshot = g.Save(); g.SetClip(path);
        if (Enabled && hoverProgress > 0)
        {
            using var shine = new Pen(Color.FromArgb((int)(70 * hoverProgress), Color.White), scale);
            g.DrawLine(shine, bounds.Left + 8 * scale, bounds.Top + scale, bounds.Right - 8 * scale, bounds.Top + scale);
            var phase = sheenStarted == 0 ? 1 : Stopwatch.GetElapsedTime(sheenStarted).TotalMilliseconds / AppTheme.SheenDuration;
            if (hovered && phase < 1)
            {
                var sheen = new RectangleF((float)(-60 * scale + (Width + 60 * scale) * phase), bounds.Top, 60 * scale, bounds.Height);
                using var brush = new LinearGradientBrush(sheen, Color.Transparent, Color.FromArgb(35, Color.White), 0F); g.FillRectangle(brush, sheen);
            }
        }
        if (pressProgress > 0) { using var inset = new Pen(Color.FromArgb((int)(100 * pressProgress), Color.Black), 4 * scale); g.DrawPath(inset, path); }
        g.Restore(snapshot);
        var iconSize = (style == ManagementButtonStyle.Segment ? 14 : style is ManagementButtonStyle.Action or ManagementButtonStyle.Danger ? 15 : 18) * scale;
        var hasIcon = ManagementIcon.Length > 0 || Image is not null;
        var textSize = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        var groupWidth = textSize.Width + (hasIcon ? iconSize + (Text.Length > 0 ? 8 * scale : 0) : 0);
        var x = navigation ? 13 * scale : bounds.Left + (bounds.Width - groupWidth) / 2;
        if (hasIcon)
        {
            var rect = new RectangleF(x, bounds.Top + (bounds.Height - iconSize) / 2, iconSize, iconSize);
            if (ManagementIcon.Length > 0) OrbitManagementTheme.DrawIcon(g, rect, ManagementIcon, ink);
            else if (Image is not null)
            {
                using var tint = new System.Drawing.Imaging.ImageAttributes();
                tint.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix00 = ink.R / 255F, Matrix11 = ink.G / 244F, Matrix22 = ink.B / 231F });
                g.DrawImage(Image, Rectangle.Round(rect), 0, 0, Image.Width, Image.Height, GraphicsUnit.Pixel, tint);
            }
            x += iconSize + 8 * scale;
        }
        if (Text.Length > 0) TextRenderer.DrawText(g, Text, Font, new Rectangle((int)x, (int)bounds.Top, Math.Max(1, (int)(bounds.Right - x - 7 * scale)), (int)bounds.Height), ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(Rectangle.Round(bounds), -(int)(4 * scale), -(int)(4 * scale)), OrbitManagementTheme.Lilac, bottom);
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
