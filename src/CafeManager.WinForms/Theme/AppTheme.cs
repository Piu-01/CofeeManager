namespace CafeManager.WinForms.Theme;

public static class AppTheme
{
    // Nguồn: docs/ui/design-tokens.json v3.0.0; kích thước logical tại 96 DPI.
    public static Font BodyFont { get; } = new("Segoe UI", 10.5F);
    public static Font TitleFont { get; } = new("Segoe UI", 16F, FontStyle.Bold);
    public static Font SectionFont { get; } = new("Segoe UI", 14.25F, FontStyle.Bold);
    public const int ContentSpacing = 16;
    public const int PagePadding = 24;
    public const int ButtonRadius = 10;
    public const int HoverDuration = 160;
    public const int PressDuration = 55;
    public const int ReleaseDuration = 150;
    public const int SheenDuration = 650;
    public const int RippleDuration = 300;
    public static Color Espresso => Color.FromArgb(20, 17, 24);
    public static Color Background => Color.FromArgb(16, 14, 21);
    public static Color Surface => Color.FromArgb(33, 27, 40);
    public static Color Hover => Color.FromArgb(40, 32, 48);
    public static Color Text => Color.FromArgb(255, 244, 231);
    public static Color Muted => Color.FromArgb(208, 194, 215);
    public static Color Primary => Color.FromArgb(255, 208, 154);
    public static Color OnPrimary => Color.FromArgb(40, 24, 14);
    public static Color Accent => Color.FromArgb(208, 190, 255);
    public static Color Border => Color.FromArgb(81, 67, 87);
    public static Color InputBorder => Color.FromArgb(119, 97, 128);
    public static Color Selected => Color.FromArgb(61, 45, 34);
    public static Color SelectedBorder => Color.FromArgb(178, 138, 98);
    public static Color PrimaryLight => Color.FromArgb(255, 230, 192);
    public static Color PrimaryShade => Color.FromArgb(221, 167, 108);
    public static Color Error => Color.FromArgb(255, 180, 180);
    public static Color Warning => Primary;
    public static Color Success => Color.FromArgb(173, 225, 191);

    public static void Apply(Control root)
    {
        if (root is Form or UserControl) root.Font = BodyFont;
        root.BackColor = root is Form or UserControl ? Background : root.Parent?.BackColor ?? Background;
        root.ForeColor = Text;
        if (root is Panel && root is not TableLayoutPanel && root is not FlowLayoutPanel)
            root.BackColor = Surface;
        if (root is TextBoxBase or ComboBox or UpDownBase)
        {
            root.BackColor = Surface;
            root.ForeColor = Text;
        }
        if (root is TextBox textBox) textBox.BorderStyle = BorderStyle.FixedSingle;
        if (root is ComboBox combo) combo.FlatStyle = FlatStyle.Flat;
        if (root is DateTimePicker picker)
        {
            picker.CalendarMonthBackground = Surface;
            picker.CalendarForeColor = Text;
            picker.CalendarTitleBackColor = Selected;
            picker.CalendarTitleForeColor = Text;
        }
        if (root is Button button) SecondaryButton(button);
        if (root is TabControl tabs)
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.Padding = new Point(14, 10);
            tabs.DrawItem -= DrawTab;
            tabs.DrawItem += DrawTab;
        }
        if (root is TabPage page) { page.UseVisualStyleBackColor = false; page.BackColor = Surface; page.Padding = new Padding(8); }
        if (root is DataGridView grid) ApplyGrid(grid);
        foreach (Control child in root.Controls) Apply(child);
    }

    public static void SecondaryButton(Button button)
    {
        button.UseVisualStyleBackColor = false;
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = Surface; button.ForeColor = Text;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = Hover;
        button.FlatAppearance.MouseDownBackColor = Selected;
    }

    public static void PrimaryButton(Button button)
    {
        button.UseVisualStyleBackColor = false;
        button.BackColor = Primary; button.ForeColor = OnPrimary;
        button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = PrimaryLight;
        button.FlatAppearance.MouseDownBackColor = PrimaryShade;
    }

    public static void Status(Label label)
    {
        label.BackColor = Surface; label.ForeColor = Muted;
        label.Padding = new Padding(12, 6, 12, 6);
    }

    public static void SetIcon(Button button, string name)
    {
        using var stream = typeof(AppTheme).Assembly.GetManifestResourceStream($"CafeManager.WinForms.Resources.Orbit.{name}.png");
        if (stream is null) return;
        using var source = new Bitmap(stream);
        var image = new Bitmap(source);
        button.Image = image;
        button.Disposed += (_, _) => image.Dispose();
    }

    private static void ApplyGrid(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = Surface; grid.GridColor = Border; grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Hover;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Hover;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.DefaultCellStyle.BackColor = Surface; grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.Font = BodyFont;
        grid.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
        grid.DefaultCellStyle.SelectionBackColor = Selected; grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Espresso;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = Text;
        grid.RowHeadersDefaultCellStyle.BackColor = Hover;
        grid.RowHeadersDefaultCellStyle.ForeColor = Muted;
        grid.RowTemplate.Height = Math.Max(grid.RowTemplate.Height, (int)(40 * grid.DeviceDpi / 96F));
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
    }

    private static void DrawTab(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tabs || e.Index < 0) return;
        var selected = e.Index == tabs.SelectedIndex;
        using var background = new SolidBrush(selected ? Selected : Surface);
        e.Graphics.FillRectangle(background, e.Bounds);
        TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds,
            selected ? Primary : Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (selected)
        {
            using var line = new Pen(Primary, Math.Max(2, tabs.DeviceDpi / 48F));
            e.Graphics.DrawLine(line, e.Bounds.Left + 10, e.Bounds.Bottom - 3, e.Bounds.Right - 10, e.Bounds.Bottom - 3);
            if (tabs.Focused) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(e.Bounds, -4, -4), Accent, Selected);
        }
    }
}
