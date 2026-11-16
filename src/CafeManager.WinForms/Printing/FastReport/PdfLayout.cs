using System.Drawing;
using FastReport;

namespace CafeManager.WinForms.Printing.FastReport;

// Hệ tọa độ in: kích thước trang/margin tính bằng mm, tọa độ phần tử tính bằng pixel 96 DPI.
internal static class PdfLayout
{
    public static readonly float Mm = global::FastReport.Utils.Units.Millimeters;
    public const float WidthMm = 0.6f;
    public static readonly Color Muted = Color.FromArgb(90, 90, 90);
    public static readonly Color Rule = Color.FromArgb(150, 150, 150);
    public static readonly Color Shade = Color.FromArgb(238, 238, 238);

    public static TextObject Text(string text, float x, float y, float w, float h, float size = 9f,
        FontStyle style = FontStyle.Regular, HorzAlign align = HorzAlign.Left, VertAlign vert = VertAlign.Center,
        Color? color = null, bool wrap = true, bool grow = false)
    {
        return new TextObject
        {
            Bounds = new RectangleF(x * Mm, y * Mm, w * Mm, h * Mm),
            Text = text,
            Font = new Font("Segoe UI", size, style, GraphicsUnit.Point),
            HorzAlign = align,
            VertAlign = vert,
            TextColor = color ?? Color.Black,
            WordWrap = wrap,
            CanGrow = grow,
            Padding = new Padding(0),
            AllowExpressions = true,
        };
    }

    public static TextObject Cell(string text, float x, float y, float w, float h, float size, HorzAlign align,
        bool grow = false, bool underline = false)
    {
        var cell = Text(text, x, y, w, h, size, FontStyle.Regular, align, VertAlign.Center, null, true, grow);
        if (underline)
            cell.Border = new Border { Lines = BorderLines.Bottom, Color = Rule, Width = 0.3f };
        return cell;
    }

    public static void Add(ReportComponentCollection target, params ReportComponentBase[] objects)
    {
        foreach (var item in objects) target.Add(item);
    }
}
