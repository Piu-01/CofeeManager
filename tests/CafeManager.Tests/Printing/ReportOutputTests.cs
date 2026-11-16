using System.Globalization;
using System.Text;
using CafeManager.Models.Dtos.Reports;
using CafeManager.Models.Requests.Reports;
using CafeManager.WinForms.Modules.Reports;
using CafeManager.WinForms.Printing;
using CafeManager.WinForms.Printing.FastReport;
using Xunit;

namespace CafeManager.Tests.Printing;

public sealed class ReportOutputTests
{
    [Fact]
    public void Formats_money_quantity_and_date_for_vietnamese_output()
    {
        Assert.Equal("81.000 ₫", PrintFormat.Money(81_000m));
        Assert.Equal("81.000", PrintFormat.PlainMoney(81_000m));
        Assert.Equal("1,5", PrintFormat.Quantity(1.5m));
        Assert.Equal("16/11/2026 09:30", PrintFormat.DateTime(new DateTime(2026, 11, 16, 9, 30, 0)));
        Assert.Equal("—", PrintFormat.DateTime((DateTime?)null));
    }

    [Fact]
    public void Csv_has_bom_semicolon_delimiter_and_escapes_values()
    {
        var path = Path.Combine(Path.GetTempPath(), "CoffeeManager", $"csv-{Guid.NewGuid():N}.csv");
        var page = Page([new SalesReportRow("HD01", "Cà phê; \"đặc\"\nxuống dòng", 2, 81_000m, 0m, 81_000m, 0m, 81_000m, 0m)]);
        try
        {
            ReportOutput.WriteCsv(path, page, page.Rows, complete: true);
            var bytes = File.ReadAllBytes(path);
            Assert.Equal([0xEF, 0xBB, 0xBF], bytes.Take(3).ToArray());
            var text = Encoding.UTF8.GetString(bytes);
            Assert.Contains("Doanh thu theo ngày", text);
            Assert.Contains("\"Cà phê; \"\"đặc\"\"\nxuống dòng\"", text);
            Assert.Contains("HD01;", text);
            Assert.Contains("81000;", text);
            Assert.Contains(";2;81000;", text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Report_pdf_exports_with_landscape_a4_width()
    {
        var page = Page([new SalesReportRow("HD01", "Tại quán", 2, 81_000m, 0m, 81_000m, 0m, 81_000m, 0m)]);
        var path = TabularPdfReport.Write(page, page.Rows, complete: true, filters: "Kênh: Tại quán");
        try
        {
            var bytes = File.ReadAllBytes(path);
            Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
            var text = Encoding.Latin1.GetString(bytes);
            var box = text.Substring(text.IndexOf("MediaBox", StringComparison.Ordinal), 80);
            // A4 ngang: rộng 297mm ≈ 841.89pt.
            Assert.Contains("841", box);
        }
        finally { PrintOutput.TryDelete(path); }
    }

    [Fact]
    public void Report_pdf_with_zero_rows_still_exports()
    {
        var page = Page([]);
        var path = TabularPdfReport.Write(page, [], complete: true, filters: null);
        try { Assert.True(new FileInfo(path).Length > 1024); }
        finally { PrintOutput.TryDelete(path); }
    }

    [Fact]
    public void Every_report_kind_has_headers_and_positive_weights()
    {
        foreach (var kind in Enum.GetValues<ReportKind>())
        {
            var columns = ReportColumns.For(kind);
            Assert.NotEmpty(columns);
            Assert.All(columns, column =>
            {
                Assert.False(string.IsNullOrWhiteSpace(column.Header));
                Assert.True(column.Weight > 0);
            });
            Assert.Equal(columns.Count, columns.Select(column => column.Field).Distinct().Count());
        }
    }

    private static ReportPage Page(List<IReportRow> rows)
    {
        var metadata = new ReportMetadata(ReportKind.SalesDay, new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 16),
            new DateTime(2026, 11, 16, 10, 0, 0), rows.Count, "PaidAt", "Nguồn đầy đủ", true);
        var totals = new ReportTotals(81_000m, 0m, 0m, 81_000m, 0m, 81_000m, 0m, 0m, 0m, 0m, 0m, 0m);
        return new ReportPage(metadata, totals, rows);
    }
}
