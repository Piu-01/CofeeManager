using System.Drawing.Drawing2D;
using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Button native giữ focus/click/keyboard; dữ liệu trạng thái do TableList cấp từ DTO.
public sealed class OrbitTableCard : OrbitButton
{
    [System.ComponentModel.DefaultValue("")]
    public string TableCode { get; set; } = "";
    [System.ComponentModel.DefaultValue("")]
    public string Caption { get; set; } = "";
    [System.ComponentModel.DefaultValue("")]
    public string Status { get; set; } = "";
    [System.ComponentModel.DefaultValue("")]
    public string OrderCode { get; set; } = "";
    private bool selected;
    [System.ComponentModel.DefaultValue(false)]
    public bool Selected { get => selected; set { if (selected == value) return; selected = value; Invalidate(); } }
    public OrbitTableCard() { Font = OrbitManagementTheme.Body; AutoSize = false; UseMnemonic = false; }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; var scale = DeviceDpi / 96F; int P(float n) => (int)Math.Round(n * scale);
        g.Clear(Parent?.BackColor ?? OrbitManagementTheme.Surface); g.SmoothingMode = SmoothingMode.AntiAlias;
        var busy = Status != "Trong"; var bounds = new RectangleF(scale, (1 + PressAmount) * scale, Width - 2 * scale, Height - 3 * scale);
        using var path = OrbitManagementTheme.Round(bounds, 12 * scale);
        using var fill = new LinearGradientBrush(bounds, selected || busy ? Color.FromArgb(51, 40, 35) : Color.FromArgb(37, 30, 46), OrbitManagementTheme.Surface, 110F); g.FillPath(fill, path);
        using var border = new Pen(selected ? OrbitManagementTheme.Caramel : busy ? Color.FromArgb(121, 92, 67) : HoverAmount > 0 ? OrbitManagementTheme.Lilac : OrbitManagementTheme.Border, scale); g.DrawPath(border, path);
        if (HoverAmount > 0) { using var gloss = new Pen(Color.FromArgb((int)(70 * HoverAmount), OrbitManagementTheme.Lilac), 3 * scale); g.DrawPath(gloss, path); }
        OrbitManagementTheme.DrawIcon(g, new RectangleF(17 * scale, 18 * scale, 18 * scale, 18 * scale), "armchair", OrbitManagementTheme.Muted);
        TextRenderer.DrawText(g, TableCode, OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Small), new Rectangle(P(48), P(16), Width - P(65), P(22)), OrbitManagementTheme.Muted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, Caption, OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.CardTitle), new Rectangle(P(17), P(49), Width - P(34), P(28)), OrbitManagementTheme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        var ink = busy ? OrbitManagementTheme.Caramel : Color.FromArgb(177, 201, 164);
        using var dot = new SolidBrush(ink); g.FillEllipse(dot, 18 * scale, 95 * scale, 4 * scale, 4 * scale);
        TextRenderer.DrawText(g, busy ? "Đang phục vụ" : "Trống", OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Table), new Rectangle(P(28), P(85), Width - P(45), P(23)), ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, OrderCode.Length == 0 ? "Chưa có đơn" : OrderCode, OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Small), new Rectangle(P(17), P(108), Width - P(34), P(20)), OrbitManagementTheme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(Rectangle.Round(bounds), -P(5), -P(5)), OrbitManagementTheme.Lilac, OrbitManagementTheme.Surface);
    }
}
