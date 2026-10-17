namespace CafeManager.WinForms.Theme;

public static class AppTheme
{
    public static Font BodyFont { get; } = new("Segoe UI", 10F);
    public const int ContentSpacing = 16;
    public static Color Espresso => Color.FromArgb(36, 24, 20);
    public static Color Primary => Color.FromArgb(75, 46, 35);
    public static Color Hover => Color.FromArgb(99, 62, 45);
    public static Color Accent => Color.FromArgb(197, 139, 82);
    public static Color Background => Color.FromArgb(245, 239, 230);
    public static Color Surface => Color.FromArgb(255, 252, 248);
    public static Color Text => Color.FromArgb(41, 30, 24);
    public static Color Border => Color.FromArgb(221, 208, 194);
    public static Color Selected => Color.FromArgb(234, 216, 195);
    public static void Apply(Control root)
    {
        if (root is Form or UserControl) root.Font = BodyFont;
        root.BackColor = Background; root.ForeColor = Text;
        foreach (Control child in root.Controls) Apply(child);
        if (root is TextBox or ComboBox or DataGridView or Panel) root.BackColor = Surface;
        if (root is Button button)
        {
            button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.MouseOverBackColor = Selected;
            button.BackColor = button.Enabled ? Surface : Background;
        }
        if (root is DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = Surface; grid.GridColor = Border; grid.BorderStyle = BorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Selected;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Selected;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Text;
            grid.DefaultCellStyle.BackColor = Surface; grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = Selected; grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Background;
        }
    }
    public static void PrimaryButton(Button button)
    {
        button.BackColor = button.Enabled ? Primary : Selected; button.ForeColor = button.Enabled ? Color.White : Text;
        button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = button.Enabled ? Hover : Selected;
    }
}
