using CafeManager.WinForms.Theme;

namespace CafeManager.WinForms.UI;

// Chỉ trình bày cặp nhãn/giá trị; form sở hữu dữ liệu và phép tính.
public sealed class OrbitSummaryPanel : TableLayoutPanel
{
    public OrbitSummaryPanel(string title)
    {
        Dock = DockStyle.Fill; AutoSize = true; ColumnCount = 2; RowCount = 0;
        Padding = new Padding(16); Margin = Padding.Empty;
        BackColor = OrbitManagementTheme.Background; ForeColor = OrbitManagementTheme.Text;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56)); ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        var heading = new Label { Text = title, Font = OrbitManagementTheme.Body, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12) };
        AddRow(heading); SetColumnSpan(heading, 2);
    }
    public Label AddValue(string caption, bool prominent = false)
    {
        var label = new Label { Text = caption, Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 8, 4) };
        var value = new Label { Text = "—", Font = prominent ? OrbitManagementTheme.Title : OrbitManagementTheme.Table, ForeColor = prominent ? OrbitManagementTheme.Caramel : OrbitManagementTheme.Text, AutoSize = true, Dock = DockStyle.Fill, TextAlign = prominent ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight, Margin = new Padding(0, 4, 0, 4), UseMnemonic = false };
        AddRow(label);
        if (prominent) { SetColumnSpan(label, 2); AddRow(value); SetColumnSpan(value, 2); }
        else Controls.Add(value, 1, RowCount - 1);
        return value;
    }
    public void AddDivider()
    {
        var line = new Panel { BackColor = OrbitManagementTheme.Border, Height = 1, Dock = DockStyle.Top, Margin = new Padding(0, 12, 0, 8) };
        AddRow(line); SetColumnSpan(line, 2);
    }
    public Label AddNote()
    {
        var note = new Label { AutoSize = true, Dock = DockStyle.Fill, Font = OrbitManagementTheme.Small, ForeColor = OrbitManagementTheme.Muted, Margin = new Padding(0, 0, 0, 6), UseMnemonic = false };
        AddRow(note); SetColumnSpan(note, 2); return note;
    }
    private void AddRow(Control control) { RowStyles.Add(new RowStyle(SizeType.AutoSize)); RowCount++; Controls.Add(control, 0, RowCount - 1); }
    protected override void OnLayout(LayoutEventArgs levent)
    {
        var width = Math.Max(1, ClientSize.Width - Padding.Horizontal);
        foreach (var label in Controls.OfType<Label>())
        {
            var available = GetColumnSpan(label) == 2 ? width : (int)(width * (GetColumn(label) == 0 ? .56F : .44F));
            label.MaximumSize = new Size(Math.Max(1, available - label.Margin.Horizontal), 0);
        }
        base.OnLayout(levent);
    }
}
