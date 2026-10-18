using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Giới hạn bề rộng nội dung theo DPI; resize chỉ bố trí lại, không tạo lại input.
public sealed class OrbitContentHost : Panel
{
    private readonly TableLayoutPanel card;
    private readonly int logicalWidth;
    private readonly bool fitHeight;
    private readonly TableLayoutPanel? scrollBody;
    private readonly bool centerVertically;
    private bool arranging;

    public OrbitContentHost(TableLayoutPanel card, int logicalWidth, bool fitHeight = false, TableLayoutPanel? scrollBody = null, bool centerVertically = false)
    {
        this.card = card; this.logicalWidth = logicalWidth; this.fitHeight = fitHeight; this.scrollBody = scrollBody;
        this.centerVertically = centerVertically;
        Dock = DockStyle.Fill; AutoScroll = true; BackColor = AppTheme.Background;
        card.Dock = DockStyle.None; card.AutoSize = false; card.Margin = Padding.Empty;
        StyleCard(card);
        Controls.Add(card);
        card.Layout += (_, _) => { if (!arranging) PerformLayout(); };
        VisibleChanged += (_, _) => PerformLayout();
        if (scrollBody is not null) scrollBody.Layout += (_, _) => { if (!arranging) PerformLayout(); };
    }

    private static void StyleCard(Control control)
    {
        if (control is TableLayoutPanel or FlowLayoutPanel) control.BackColor = AppTheme.Surface;
        if (control is Label or CheckBox) control.BackColor = Color.Transparent;
        foreach (Control child in control.Controls) StyleCard(child);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        if (arranging) return;
        arranging = true;
        try
        {
            var gap = (int)(16 * DeviceDpi / 96F);
            var width = Math.Max(1, Math.Min((int)(logicalWidth * DeviceDpi / 96F), ClientSize.Width - gap * 2 - SystemInformation.VerticalScrollBarWidth));
            card.Width = width;
            var preferred = card.GetPreferredSize(new Size(width, 0)).Height;
            if (scrollBody is not null)
            {
                // Hàng Percent của editor không tự báo chiều cao nội dung cần cuộn.
                var bodyHeight = scrollBody.Controls.Cast<Control>().Where(c => c.Visible)
                    .Select(c => c.Bottom - scrollBody.AutoScrollPosition.Y + c.Margin.Bottom).DefaultIfEmpty(0).Max() + scrollBody.Padding.Bottom;
                preferred = card.Padding.Vertical + card.Controls.Cast<Control>().Where(c => c.Visible)
                    .Sum(c => c.Margin.Vertical + (c == scrollBody ? bodyHeight : c.GetPreferredSize(new Size(Math.Max(1, width - card.Padding.Horizontal - c.Margin.Horizontal), 0)).Height));
            }
            var height = fitHeight ? Math.Min(preferred, Math.Max((int)(240 * DeviceDpi / 96F), ClientSize.Height - gap * 2)) : preferred;
            var top = centerVertically ? Math.Max(gap, (ClientSize.Height - height) / 2) : gap;
            card.SetBounds(Math.Max(gap, (ClientSize.Width - width) / 2), top + AutoScrollPosition.Y, width, height);
            AutoScrollMinSize = new Size(0, height + gap * 2);
            base.OnLayout(levent);
        }
        finally { arranging = false; }
    }
}
