using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public sealed class OrbitFilterBox : UserControl
{
    public ComboBox Input { get; }
    [System.ComponentModel.DefaultValue(124)]
    public int LogicalWidth { get; set; } = 124;
    public OrbitFilterBox() : this(new ComboBox()) { }
    public OrbitFilterBox(ComboBox input)
    {
        Input = input; Input.DropDownStyle = ComboBoxStyle.DropDownList; Input.FlatStyle = FlatStyle.Flat;
        Input.BackColor = OrbitManagementTheme.Input; Input.ForeColor = OrbitManagementTheme.Text;
        Input.Font = OrbitManagementTheme.Table; Input.IntegralHeight = true;
        DoubleBuffered = true; BackColor = OrbitManagementTheme.Surface; Controls.Add(Input);
        Input.Enter += (_, _) => Invalidate(); Input.Leave += (_, _) => Invalidate();
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e); var gap = OrbitManagementTheme.Px(this, 8);
        Input.SetBounds(gap, (Height - Input.PreferredHeight) / 2, Math.Max(1, Width - gap * 2), Input.PreferredHeight);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var scale = DeviceDpi / 96F; e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = OrbitManagementTheme.Round(new RectangleF(scale, scale, Width - 2 * scale, Height - 2 * scale), 8 * scale);
        using var fill = new SolidBrush(OrbitManagementTheme.Input); e.Graphics.FillPath(fill, path);
        using var pen = new Pen(ContainsFocus ? OrbitManagementTheme.Lilac : OrbitManagementTheme.InputBorder, scale); e.Graphics.DrawPath(pen, path);
    }
}
