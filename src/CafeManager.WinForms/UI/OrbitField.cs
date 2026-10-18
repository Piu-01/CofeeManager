using System.Drawing.Drawing2D;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public sealed class OrbitField : Panel
{
    private readonly Control input;
    public OrbitField(Control input)
    {
        this.input = input; TabStop = false; TabIndex = input.TabIndex;
        BackColor = AppTheme.Surface; Margin = input.Margin; Dock = input.Dock;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        input.Dock = DockStyle.None; input.Margin = Padding.Empty;
        if (input is TextBox text) text.BorderStyle = BorderStyle.None;
        Controls.Add(input);
        input.Enter += (_, _) => Invalidate(); input.Leave += (_, _) => Invalidate();
    }

    public static void Wrap(Control input)
    {
        if (input.Parent is not TableLayoutPanel parent) return;
        var cell = parent.GetPositionFromControl(input);
        var frame = new OrbitField(input);
        parent.Controls.Add(frame, cell.Column, cell.Row);
    }

    public override Size GetPreferredSize(Size proposedSize) => new(proposedSize.Width, input.PreferredSize.Height + (int)(18 * DeviceDpi / 96F));
    protected override void OnLayout(LayoutEventArgs levent)
    {
        var inset = (int)(10 * DeviceDpi / 96F);
        Height = GetPreferredSize(new Size(Width, 0)).Height;
        input.SetBounds(inset, Math.Max(0, (Height - input.PreferredSize.Height) / 2), Math.Max(1, Width - inset * 2), input.PreferredSize.Height);
        base.OnLayout(levent);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var radius = 8 * DeviceDpi / 96F;
        var bounds = new RectangleF(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var path = new GraphicsPath();
        path.AddRoundedRectangle(bounds, new SizeF(radius, radius));
        using var pen = new Pen(ContainsFocus ? AppTheme.Accent : AppTheme.InputBorder, ContainsFocus ? 2 : 1);
        e.Graphics.DrawPath(pen, path);
    }
}
