using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public sealed class OrbitEmptyState : Control
{
    [System.ComponentModel.DefaultValue(false)]
    public bool HistoryPrompt { get; set; }
    public OrbitEmptyState()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Text = "Không tìm thấy dữ liệu phù hợp."; Font = OrbitManagementTheme.Table; ForeColor = OrbitManagementTheme.Muted; BackColor = OrbitManagementTheme.Surface; TabStop = false;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (!HistoryPrompt) { TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak); return; }
        var p = DeviceDpi / 96F; var middle = Height / 2;
        OrbitManagementTheme.DrawIcon(e.Graphics, new RectangleF(Width / 2F - 16 * p, middle - 60 * p, 32 * p, 32 * p), "history", OrbitManagementTheme.Lilac);
        TextRenderer.DrawText(e.Graphics, "Chưa có dữ liệu trong bộ lọc này", OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.EmptyTitle), new Rectangle(0, middle - (int)(10*p), Width, (int)(30*p)), OrbitManagementTheme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(e.Graphics, Text, OrbitManagementTheme.DrawingFont(this, OrbitManagementTheme.Table), new Rectangle(0, middle + (int)(22*p), Width, (int)(30*p)), OrbitManagementTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
