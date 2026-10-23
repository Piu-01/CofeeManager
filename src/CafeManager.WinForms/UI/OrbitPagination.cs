using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

public sealed class OrbitPagination : UserControl
{
    public OrbitButton Previous { get; } = new() { AccessibleName = "Trang trước" };
    public OrbitButton Next { get; } = new() { AccessibleName = "Trang sau" };
    public Label Range { get; } = new() { AutoSize = false, Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label indicator = new() { Text = "1 / 1", Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted, TextAlign = ContentAlignment.MiddleCenter };
    public OrbitPagination()
    {
        BackColor = OrbitManagementTheme.Surface;
        OrbitManagementTheme.Button(Previous, ManagementButtonStyle.Pager, "left"); OrbitManagementTheme.Button(Next, ManagementButtonStyle.Pager, "right");
        Controls.AddRange([Range, Previous, indicator, Next]);
    }
    public void SetPage(int page, int pageSize, int total)
    {
        var pages = Math.Max(1, (total + pageSize - 1) / pageSize);
        indicator.Text = $"{page + 1} / {pages}";
        Range.Text = total == 0 ? "0 nguyên liệu" : $"Hiển thị {page * pageSize + 1}–{Math.Min((page + 1) * pageSize, total)} / {total}";
        Previous.Enabled = page > 0; Next.Enabled = page + 1 < pages;
    }
    public void SetServerPage(int page, int rowCount, int pageSize, string noun, bool paginated = true)
    {
        var endKnown = rowCount < pageSize || !paginated;
        indicator.Text = endKnown ? $"{page + 1} / {page + 1}" : $"{page + 1} / …";
        Range.Text = rowCount == 0 ? $"0 {noun}" : $"Hiển thị {page * pageSize + 1}–{page * pageSize + rowCount}" + (endKnown ? $" / {page * pageSize + rowCount} {noun}" : $" {noun}");
        Previous.Enabled = paginated && page > 0; Next.Enabled = paginated && !endKnown;
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e); int P(float n) => OrbitManagementTheme.Px(this, n);
        Next.SetBounds(Width - P(17 + 38), (Height - P(30)) / 2, P(38), P(30));
        indicator.SetBounds(Next.Left - P(9 + 34), 1, P(34), Math.Max(0, Height - 1));
        Previous.SetBounds(indicator.Left - P(9 + 38), Next.Top, P(38), P(30));
        Range.SetBounds(P(17), 1, Math.Max(1, Previous.Left - P(30)), Math.Max(0, Height - 1));
    }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); using var pen = new Pen(OrbitManagementTheme.Border); e.Graphics.DrawLine(pen, 0, 0, Width, 0); }
}
