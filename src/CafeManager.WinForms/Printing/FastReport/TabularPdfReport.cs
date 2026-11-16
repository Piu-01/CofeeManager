using System.Data;
using System.Drawing;
using FastReport;
using CafeManager.Models.Dtos.Reports;
using CafeManager.WinForms.Modules.Reports;
using ModelReportPage = CafeManager.Models.Dtos.Reports.ReportPage;

namespace CafeManager.WinForms.Printing.FastReport;

internal static class TabularPdfReport
{
    private const string RowsSource = "Rows";

    public static string Write(ModelReportPage page, IReadOnlyList<IReportRow> rows, bool complete, string? filters)
    {
        var path = PrintOutput.NewPath("baocao", page.Metadata.Kind.ToString());
        var report = Build(page, rows, complete, filters);
        PdfReportExporter.Export(report, path, ReportsControl.Label(page.Metadata.Kind));
        return path;
    }

    private static Report Build(ModelReportPage page, IReadOnlyList<IReportRow> rows, bool complete, string? filters)
    {
        var columns = ReportColumns.For(page.Metadata.Kind);
        var report = new Report { ReportInfo = { Name = ReportsControl.Label(page.Metadata.Kind) } };
        // [TotalPages#] trong footer cần hai lượt dựng.
        report.DoublePass = true;
        var sheet = new global::FastReport.ReportPage
        {
            PaperWidth = 297f,
            PaperHeight = 210f,
            Landscape = true,
            LeftMargin = 10f,
            RightMargin = 10f,
            TopMargin = 9f,
            BottomMargin = 10f,
        };
        report.Pages.Add(sheet);
        const float usable = 277f;

        var title = new ReportTitleBand();
        sheet.ReportTitle = title;
        var y = BuildTitle(title, page, rows, complete, filters, usable);

        var widths = ScaleColumns(columns, usable);
        var header = new PageHeaderBand { Height = PdfLayout.Mm * 7f };
        sheet.PageHeader = header;
        var x = 0f;
        for (var i = 0; i < columns.Count; i++)
        {
            var cell = PdfLayout.Text(columns[i].Header, x, 0, widths[i], 7f, 7.5f, FontStyle.Bold,
                columns[i].Right ? HorzAlign.Right : HorzAlign.Left);
            cell.FillColor = PdfLayout.Shade;
            cell.Border = new Border { Lines = BorderLines.Top | BorderLines.Bottom, Color = PdfLayout.Rule, Width = 0.4f };
            header.Objects.Add(cell);
            x += widths[i];
        }

        var table = ReportOutput.Table(page.Metadata.Kind, rows);
        report.RegisterData(table, RowsSource);
        var source = report.GetDataSource(RowsSource);
        if (source is not null) source.Enabled = true;

        var data = new DataBand { Height = PdfLayout.Mm * 5.8f, CanGrow = true, DataSource = source };
        sheet.Bands.Add(data);
        x = 0f;
        for (var i = 0; i < columns.Count; i++)
        {
            var grow = !columns[i].Right && columns[i].Field is "Name" or "Coverage" or "Code";
            // Ô dài co giãn nên không kẻ đáy từng dòng để đường kẻ không lệch giữa các cột.
            var cell = PdfLayout.Cell($"[{RowsSource}.{columns[i].Field}]", x, 0, widths[i], 5.8f, 7.5f,
                columns[i].Right ? HorzAlign.Right : HorzAlign.Left, grow);
            data.Objects.Add(cell);
            x += widths[i];
        }

        var summary = new ReportSummaryBand { CanGrow = true };
        sheet.ReportSummary = summary;
        summary.Objects.Add(PdfLayout.Text(ReportsControl.Totals(page), 0, 2, usable, 6f, 8f, FontStyle.Bold,
            HorzAlign.Left, VertAlign.Top, null, true, true));
        summary.Objects.Add(PdfLayout.Text(L.T("Tổng lấy từ nguồn SQL của kỳ báo cáo; dòng in không tự cộng lại."),
            0, 15, usable, 5f, 7f, FontStyle.Regular, HorzAlign.Left, VertAlign.Top, PdfLayout.Muted));
        summary.Height = PdfLayout.Mm * 24f;

        var footer = new PageFooterBand { Height = PdfLayout.Mm * 6f };
        sheet.PageFooter = footer;
        footer.Objects.Add(PdfLayout.Text($"{StoreProfile.Name} · " + L.T("Trang [Page#]/[TotalPages#]"), 0, 0, usable, 6f,
            7.5f, FontStyle.Regular, HorzAlign.Center, VertAlign.Bottom, PdfLayout.Muted));
        return report;
    }

    private static float BuildTitle(ReportTitleBand title, ModelReportPage page, IReadOnlyList<IReportRow> rows,
        bool complete, string? filters, float usable)
    {
        var m = page.Metadata;
        float y = 0;
        title.Objects.Add(PdfLayout.Text(StoreProfile.Name, 0, y, usable, 4.5f, 8f, FontStyle.Regular,
            HorzAlign.Left, VertAlign.Center, PdfLayout.Muted));
        y += 4.5f;
        title.Objects.Add(PdfLayout.Text(ReportsControl.Label(m.Kind), 0, y, usable, 8f, 15f, FontStyle.Bold));
        y += 8f;
        title.Objects.Add(PdfLayout.Text(L.T("Kỳ: ") + $"{m.From:dd/MM/yyyy} - {m.To:dd/MM/yyyy} · " +
            L.T("Cơ sở ngày: ") + Models.States.ReportLabels.Text(m.DateBasis), 0, y, usable, 4.6f, 8.5f));
        y += 4.8f;
        if (!string.IsNullOrWhiteSpace(filters))
        {
            title.Objects.Add(PdfLayout.Text(filters, 0, y, usable, 4.6f, 8.5f, FontStyle.Regular, HorzAlign.Left,
                VertAlign.Center, PdfLayout.Muted));
            y += 4.8f;
        }
        var scope = L.F("{0} dòng · Đọc {1}", m.TotalCount, PrintFormat.DateTime(m.ReadAt)) +
            (m.Complete ? string.Empty : " · " + L.T("Nguồn chưa đầy đủ; xem cột phạm vi")) +
            (complete ? string.Empty : L.F(" · Chỉ in {0}/{1} dòng", rows.Count, m.TotalCount));
        title.Objects.Add(PdfLayout.Text(scope, 0, y, usable, 4.6f, 8f, FontStyle.Regular, HorzAlign.Left,
            VertAlign.Center, PdfLayout.Muted));
        y += 5f;
        var note = Models.States.ReportLabels.Text(m.Note);
        title.Objects.Add(PdfLayout.Text(note, 0, y, usable, 9f, 7.5f, FontStyle.Regular, HorzAlign.Left,
            VertAlign.Top, PdfLayout.Muted, true, true));
        y += Math.Max(6f, Math.Min(18f, note.Length * 0.11f));
        title.Height = y * PdfLayout.Mm;
        return y;
    }

    private static float[] ScaleColumns(IReadOnlyList<ReportColumn> columns, float usable)
    {
        var total = columns.Sum(column => column.Weight);
        var widths = columns.Select(column => Math.Max(14f, usable * column.Weight / total)).ToArray();
        var sum = widths.Sum();
        if (sum > usable) for (var i = 0; i < widths.Length; i++) widths[i] *= usable / sum;
        return widths;
    }
}
