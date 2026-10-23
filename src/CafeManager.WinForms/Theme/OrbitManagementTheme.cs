using System.Drawing.Drawing2D;
using CafeManager.WinForms.UI;

namespace CafeManager.WinForms.Theme;

// Orbit_9_HTML/design-tokens.json và CSS hiệu lực; px logical tại 96 DPI.
public static class OrbitManagementTheme
{
    public static readonly Color Background = ColorTranslator.FromHtml("#100E15");
    public static readonly Color Sidebar = ColorTranslator.FromHtml("#15111B");
    public static readonly Color Surface = ColorTranslator.FromHtml("#1C1823");
    public static readonly Color Text = ColorTranslator.FromHtml("#FFF4E7");
    public static readonly Color Muted = ColorTranslator.FromHtml("#C3B6CD");
    public static readonly Color Border = ColorTranslator.FromHtml("#3C3146");
    public static readonly Color Selected = ColorTranslator.FromHtml("#342820");
    public static readonly Color Caramel = ColorTranslator.FromHtml("#FFD09A");
    public static readonly Color Lilac = ColorTranslator.FromHtml("#D0BEFF");
    public static readonly Color Input = ColorTranslator.FromHtml("#17131D");
    public static readonly Color InputBorder = ColorTranslator.FromHtml("#55415F");
    public static Font Body { get; } = new("Segoe UI", 10.5F);
    public static Font Table { get; } = new("Segoe UI", 9F);
    public static Font Small { get; } = new("Segoe UI", 8.25F);
    public static Font Header { get; } = new("Segoe UI", 7.5F);
    public static Font Title { get; } = new("Segoe UI", 19.5F, FontStyle.Bold);
    public static Font Code { get; } = new("Consolas", 8.25F);
    public static Font Brand { get; } = new("Segoe UI", 13F, FontStyle.Bold);
    public static Font CardTitle { get; } = new("Segoe UI", 15F);
    public static Font EmptyTitle { get; } = new("Segoe UI", 12F);
    private static readonly Dictionary<(Font Template, int Dpi), Font> drawingFonts = new();
    // Font trong CellStyle/vẽ trực tiếp không được Control tự scale khi DPI đổi.
    // Dùng pixel để không phụ thuộc DPI của HDC được tạo trước lúc đổi Scale.
    public static Font DrawingFont(Control control, Font template)
    {
        var key = (template, control.DeviceDpi);
        if (!drawingFonts.TryGetValue(key, out var font))
        {
            font = new Font(template.FontFamily, template.SizeInPoints * control.DeviceDpi / 72F, template.Style, GraphicsUnit.Pixel);
            drawingFonts.Add(key, font);
        }
        return font;
    }
    public static int Px(Control control, float value) => (int)Math.Round(value * control.DeviceDpi / 96F);
    public static GraphicsPath Round(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        path.AddRoundedRectangle(bounds, new SizeF(radius * 2, radius * 2));
        return path;
    }
    public static void Button(OrbitButton button, ManagementButtonStyle style, string icon)
    {
        button.ManagementStyle = style; button.ManagementIcon = icon;
        button.Font = style == ManagementButtonStyle.Primary ? Body : style == ManagementButtonStyle.Segment ? Header : Table;
        button.UseMnemonic = false; button.AutoSize = false;
        button.Margin = Padding.Empty; button.Padding = Padding.Empty;
        void SizeButton() { if (!button.IsDisposed) button.Size = button.GetPreferredSize(Size.Empty); }
        button.HandleCreated += (_, _) => SizeButton();
        button.DpiChangedAfterParent += (_, _) => SizeButton();
        SizeButton();
    }

    public static void DialogInput(Control input)
    {
        input.Font = Body; input.ForeColor = Text;
        input.BackColor = input is TextBoxBase or ComboBox or UpDownBase ? Input : Surface;
        if (input is ComboBox combo) combo.FlatStyle = FlatStyle.Flat;
        if (input is OrbitButton button) Button(button, ManagementButtonStyle.Secondary, button.ManagementIcon);
        if (input is DataGridView grid) DialogGrid(grid);
        if (input is OrbitDateField or OrbitTimeInput)
        {
            foreach (Control child in input.Controls)
            {
                if (child is OrbitButton calendar) { Button(calendar, ManagementButtonStyle.Icon, ""); continue; }
                DialogInput(child);
                if (child is TextBox text) text.BorderStyle = BorderStyle.FixedSingle;
            }
            if (input is OrbitDateField date) date.ApplyManagementTheme();
        }
    }

    // Chỉ style; không thay ReadOnly, cột, binding hoặc quyền nhập/xóa của bảng nháp.
    public static void DialogGrid(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false; grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Surface; grid.GridColor = Border;
        grid.ColumnHeadersBorderStyle = grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.DefaultCellStyle = new DataGridViewCellStyle(grid.DefaultCellStyle) { BackColor = Surface, ForeColor = Text, SelectionBackColor = Selected, SelectionForeColor = Text, Alignment = DataGridViewContentAlignment.MiddleLeft, WrapMode = DataGridViewTriState.False };
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Input, ForeColor = Muted, SelectionBackColor = Input, SelectionForeColor = Muted, Alignment = DataGridViewContentAlignment.MiddleLeft };
        grid.AlternatingRowsDefaultCellStyle.BackColor = Surface;
        grid.RowHeadersDefaultCellStyle = new DataGridViewCellStyle(grid.DefaultCellStyle);
        void Metrics()
        {
            grid.DefaultCellStyle.Font = DrawingFont(grid, Table);
            grid.ColumnHeadersDefaultCellStyle.Font = DrawingFont(grid, Small);
            grid.RowTemplate.Height = Px(grid, 40); grid.ColumnHeadersHeight = Px(grid, 40); grid.RowHeadersWidth = Px(grid, 34);
            grid.DefaultCellStyle.Padding = grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(Px(grid, 10), 0, Px(grid, 10), 0);
            foreach (DataGridViewRow row in grid.Rows) row.Height = grid.RowTemplate.Height;
            foreach (DataGridViewColumn column in grid.Columns) { column.MinimumWidth = Px(grid, 70); if (column.ReadOnly) { column.DefaultCellStyle.ForeColor = Muted; column.DefaultCellStyle.BackColor = Input; } }
        }
        grid.HandleCreated += (_, _) => Metrics(); grid.DpiChangedAfterParent += (_, _) => Metrics();
        grid.DataBindingComplete += (_, _) => Metrics();
        grid.EditingControlShowing += (_, e) => { e.Control.BackColor = Input; e.Control.ForeColor = Text; e.Control.Font = Body; if (e.Control is ComboBox combo) combo.FlatStyle = FlatStyle.Flat; };
        Metrics();
    }

    // Các nét Lucide đơn giản; cùng hệ tọa độ 24px, không phụ thuộc font glyph.
    public static void DrawIcon(Graphics graphics, RectangleF bounds, string name, Color color)
    {
        var state = graphics.Save();
        graphics.TranslateTransform(bounds.X, bounds.Y); graphics.ScaleTransform(bounds.Width / 24, bounds.Height / 24);
        using var pen = new Pen(color, 1.7F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        switch (name)
        {
            case "plus": graphics.DrawLine(pen, 12, 5, 12, 19); graphics.DrawLine(pen, 5, 12, 19, 12); break;
            case "search": graphics.DrawEllipse(pen, 3, 3, 13, 13); graphics.DrawLine(pen, 15, 15, 21, 21); break;
            case "left": graphics.DrawLines(pen, [new PointF(15, 6), new(9, 12), new(15, 18)]); break;
            case "right": graphics.DrawLines(pen, [new PointF(9, 6), new(15, 12), new(9, 18)]); break;
            case "down": graphics.DrawLines(pen, [new PointF(7, 10), new(12, 15), new(17, 10)]); break;
            case "refresh": graphics.DrawArc(pen, 3, 3, 18, 18, 45, 300); graphics.DrawLines(pen, [new PointF(21, 3), new(21, 9), new(15, 9)]); break;
            case "pencil": graphics.DrawPolygon(pen, [new PointF(4, 15), new(15, 4), new(20, 9), new(9, 20), new(3, 21)]); graphics.DrawLine(pen, 13, 6, 18, 11); break;
            case "trash": graphics.DrawLines(pen, [new PointF(5, 7), new(6, 21), new(18, 21), new(19, 7)]); graphics.DrawLine(pen, 3, 6, 21, 6); graphics.DrawLines(pen, [new PointF(9, 6), new(9, 3), new(15, 3), new(15, 6)]); graphics.DrawLine(pen, 10, 10, 10, 17); graphics.DrawLine(pen, 14, 10, 14, 17); break;
            case "notebook": graphics.DrawRectangle(pen, 5, 3, 13, 18); for (var y = 6; y < 20; y += 4) graphics.DrawLine(pen, 3, y, 7, y); graphics.DrawLine(pen, 12, 15, 21, 6); graphics.DrawLine(pen, 12, 15, 11, 19); break;
            case "book": graphics.DrawLines(pen, [new PointF(12, 5), new(5, 3), new(2, 4), new(2, 19), new(5, 18), new(12, 20), new(19, 18), new(22, 19), new(22, 4), new(19, 3), new(12, 5), new(12, 20)]); break;
            case "ticket": graphics.DrawPolygon(pen, [new PointF(3, 5), new(21, 5), new(21, 9), new(18, 12), new(21, 15), new(21, 19), new(3, 19), new(3, 15), new(6, 12), new(3, 9)]); graphics.DrawLine(pen, 14, 8, 10, 16); break;
            case "settings": graphics.DrawLine(pen, 3, 6, 21, 6); graphics.DrawLine(pen, 3, 18, 21, 18); graphics.DrawEllipse(pen, 6, 3, 6, 6); graphics.DrawEllipse(pen, 12, 15, 6, 6); break;
            case "armchair": graphics.DrawArc(pen, 5, 3, 14, 12, 180, 180); graphics.DrawRectangle(pen, 5, 10, 14, 9); graphics.DrawLine(pen, 3, 9, 3, 17); graphics.DrawLine(pen, 21, 9, 21, 17); graphics.DrawLine(pen, 6, 19, 6, 22); graphics.DrawLine(pen, 18, 19, 18, 22); break;
            case "grid": for (var x = 3; x <= 13; x += 10) for (var y = 3; y <= 13; y += 10) graphics.DrawRectangle(pen, x, y, 7, 7); break;
            case "list": for (var y = 5; y <= 19; y += 7) { graphics.DrawLine(pen, 3, y, 5, y); graphics.DrawLine(pen, 9, y, 21, y); } break;
            case "pin": graphics.DrawEllipse(pen, 8, 6, 8, 8); graphics.DrawLines(pen, [new PointF(4, 10), new(6, 3), new(18, 3), new(20, 10), new(12, 22), new(4, 10)]); break;
            case "user": graphics.DrawEllipse(pen, 7, 2, 8, 8); graphics.DrawArc(pen, 3, 12, 16, 14, 180, 180); graphics.DrawEllipse(pen, 16, 14, 5, 5); graphics.DrawLine(pen, 20, 18, 23, 22); break;
            case "receipt": graphics.DrawLines(pen, [new PointF(5, 22), new(5, 2), new(19, 2), new(19, 22), new(16, 20), new(12, 22), new(8, 20), new(5, 22)]); graphics.DrawLine(pen, 8, 7, 16, 7); graphics.DrawLine(pen, 8, 12, 16, 12); break;
            case "history": graphics.DrawArc(pen, 3, 3, 18, 18, 220, 310); graphics.DrawLines(pen, [new PointF(2, 3), new(2, 9), new(8, 9)]); graphics.DrawLine(pen, 12, 6, 12, 12); graphics.DrawLine(pen, 12, 12, 17, 15); break;
            default: graphics.DrawEllipse(pen, 4, 4, 16, 16); graphics.DrawLine(pen, 12, 7, 12, 13); graphics.DrawLine(pen, 12, 13, 16, 15); break;
        }
        graphics.Restore(state);
    }
}

public enum ManagementButtonStyle { Primary, Action, Danger, Icon, Pager, Navigation, Tab, Secondary, Segment }
