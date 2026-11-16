using FastReport;
using FastReport.Export.PdfSimple;

namespace CafeManager.WinForms.Printing.FastReport;

internal static class PdfReportExporter
{
    // PDFSimple là export PDF của bản OpenSource: render trang thành ảnh 300 DPI rồi nhúng.
    public static void Export(Report report, string path, string title)
    {
        report.Prepare();
        using var export = new PDFSimpleExport
        {
            ImageDpi = 300,
            JpegQuality = 90,
            Title = title,
            Author = StoreProfile.Name,
        };
        export.Export(report, path);
    }
}
