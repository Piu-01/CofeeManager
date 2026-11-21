using System.Text;
using CafeManager.Models.Dtos.Sales;
using CafeManager.WinForms.Printing;
using CafeManager.WinForms.Printing.FastReport;
using Xunit;

namespace CafeManager.Tests.Printing;

// Probe FastReport.OpenSource trên net10.0-windows + hình dạng PDF (magic, khổ giấy) của hóa đơn.
public sealed class PdfSmokeTests
{
    [Fact]
    public void A4_invoice_export_is_pdf_with_a4_media_box()
    {
        var path = InvoicePdfReport.Write(Sample(3), reprint: true, InvoicePaper.A4);
        try
        {
            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length > 1024);
            Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
            var text = Encoding.Latin1.GetString(bytes);
            Assert.Contains("MediaBox", text);
            // A4 rộng 210mm ≈ 595.28pt.
            Assert.Contains("595", MediaBox(text));
        }
        finally { PrintOutput.TryDelete(path); }
    }

    [Fact]
    public void Roll80_invoice_export_uses_80mm_width()
    {
        var path = InvoicePdfReport.Write(Sample(2), reprint: false, InvoicePaper.Roll80);
        try
        {
            var bytes = File.ReadAllBytes(path);
            Assert.StartsWith("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
            var box = MediaBox(Encoding.Latin1.GetString(bytes));
            Assert.StartsWith("MediaBox", box);
            // 80mm ≈ 226.77pt; cho phép FastReport làm tròn.
            Assert.Contains("226", box);
        }
        finally { PrintOutput.TryDelete(path); }
    }

    [Fact]
    public void Long_invoice_does_not_lose_last_line_and_adds_pages()
    {
        var detail = Sample(60, note: "Ít đá, không đường");
        var path = InvoicePdfReport.Write(detail, reprint: false, InvoicePaper.A4);
        try
        {
            var text = Encoding.Latin1.GetString(File.ReadAllBytes(path));
            var pages = Count(text, "/Type /Page");
            Assert.True(pages >= 2, "Hóa đơn 60 dòng phải sang trang, thực tế " + pages);
        }
        finally { PrintOutput.TryDelete(path); }
    }

    private static string MediaBox(string pdf)
    {
        var index = pdf.IndexOf("MediaBox", StringComparison.Ordinal);
        return index < 0 ? string.Empty : pdf.Substring(index, Math.Min(80, pdf.Length - index));
    }

    private static int Count(string text, string token)
    {
        var count = 0;
        for (var index = 0; (index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0; index += token.Length) count++;
        return count;
    }

    private static InvoiceDetailDto Sample(int lines, string? note = null)
    {
        var items = Enumerable.Range(1, lines)
            .Select(index => new SalesLine(index, "SP" + index.ToString("000"),
                "Cà phê sữa đá " + index, 2, 25_000m, index % 4 == 0 ? note : null, 50_000m))
            .ToArray();
        var total = items.Sum(item => item.Amount);
        var header = new SalesOrderHeader("HD0001", new DateTime(2026, 11, 16, 9, 30, 0), "NV01", "Khách lẻ",
            "B01", null, null, "Tai quan", null, null, null, null, new DateTime(2026, 11, 16, 9, 35, 0),
            0m, 0m, total, 10_000m, 5_000m, total + 5_000m, "Da thanh toan", [], true, null, null,
            new DateTime(2026, 11, 16, 9, 35, 0), null, null);
        var order = new SalesOrderDto(header, items, [new SalesPayment(1, "TienMat", "Tiền mặt", header.Payable, null)]);
        return new InvoiceDetailDto(order, "Promotion", "Giảm giá", 10, 0, 0, 120, header.PaidAt);
    }
}
