using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Dùng lại parser, lịch và ValueChanged của hai trường ngày hiện có.
public sealed class OrbitDateRangeField : UserControl
{
    public OrbitDateField From { get; }
    public OrbitDateField To { get; }
    private readonly Label arrow = new() { Text = "→", TextAlign = ContentAlignment.MiddleCenter, ForeColor = OrbitManagementTheme.Muted };
    public OrbitDateRangeField() : this(new OrbitDateField(), new OrbitDateField()) { }
    public OrbitDateRangeField(OrbitDateField from, OrbitDateField to)
    {
        From = from; To = to; AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        BackColor = OrbitManagementTheme.Input; TabStop = false; DoubleBuffered = true;
        From.SetInputName("Từ ngày, dd/MM/yyyy"); To.SetInputName("Đến ngày, dd/MM/yyyy");
        foreach (var date in new[] { From, To })
        {
            date.MinimumSize = Size.Empty; OrbitManagementTheme.DialogInput(date); date.ApplyManagementTheme(borderless: true);
            date.Enter += (_, _) => Invalidate(); date.Leave += (_, _) => Invalidate();
        }
        Controls.AddRange([From, arrow, To]); OrbitField.ApplyInputBehavior(this);
    }
    public override Size GetPreferredSize(Size proposedSize)
    {
        int P(float n) => OrbitManagementTheme.Px(this, n);
        var width = proposedSize.Width > 0 ? Math.Min(P(300), proposedSize.Width) : P(300);
        return new Size(width, P(width < P(250) ? 80 : 40));
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e); if (From is null || To is null) return;
        int P(float n) => OrbitManagementTheme.Px(this, n);
        var gap = P(5); var vertical = Width < P(250); var row = vertical ? Math.Max(1, Height / 2) : Height;
        var middle = vertical ? 0 : P(24); var width = Math.Max(1, vertical ? Width - 2 * gap : (Width - 2 * gap - middle) / 2);
        From.SetBounds(gap, gap, width, Math.Max(1, row - 2 * gap));
        arrow.Visible = !vertical; arrow.SetBounds(From.Right, 0, middle, Height);
        To.SetBounds(vertical ? gap : From.Right + middle, vertical ? row + gap : gap, width, Math.Max(1, row - 2 * gap));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var scale = DeviceDpi / 96F;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = OrbitManagementTheme.Round(new RectangleF(scale, scale, Math.Max(1, Width - 2 * scale), Math.Max(1, Height - 2 * scale)), 8 * scale);
        using var pen = new Pen(ContainsFocus ? OrbitManagementTheme.Lilac : OrbitManagementTheme.InputBorder, scale); e.Graphics.DrawPath(pen, path);
    }
}
