using System.Data;
using System.Drawing;
using FastReport;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.States;

namespace CafeManager.WinForms.Printing.FastReport;

internal enum InvoicePaper { A4, Roll80 }

internal static class InvoicePdfReport
{
    private const string LinesSource = "Lines";

    public static string Write(InvoiceDetailDto detail, bool reprint, InvoicePaper paper)
    {
        var header = detail.Order.Header;
        var path = PrintOutput.NewPath(paper == InvoicePaper.A4 ? "hoadon-a4" : "hoadon-80", header.Code);
        var report = Build(detail, reprint, paper);
        PdfReportExporter.Export(report, path, L.T("Hóa đơn ") + header.Code);
        return path;
    }

    private static Report Build(InvoiceDetailDto detail, bool reprint, InvoicePaper paper)
    {
        var header = detail.Order.Header;
        var a4 = paper == InvoicePaper.A4;
        var report = new Report { ReportInfo = { Name = L.T("Hóa đơn bán hàng") } };
        // [TotalPages#] chỉ có số đúng khi chạy hai lượt (footer A4).
        if (a4) report.DoublePass = true;
        var page = new ReportPage
        {
            PaperWidth = a4 ? 210f : 80f,
            PaperHeight = a4 ? 297f : 200f,
            Landscape = false,
            LeftMargin = a4 ? 15f : 4f,
            RightMargin = a4 ? 15f : 4f,
            TopMargin = a4 ? 12f : 4f,
            BottomMargin = a4 ? 12f : 4f,
        };
        if (!a4) page.UnlimitedHeight = true;
        report.Pages.Add(page);

        var title = new ReportTitleBand();
        page.ReportTitle = title;
        BuildTitle(title, detail, reprint, a4);

        // Header cột lặp lại trên mọi trang; hóa đơn 80mm chỉ một trang co theo nội dung.
        var columnHeader = new PageHeaderBand { Height = PdfLayout.Mm * (a4 ? 7f : 5.2f) };
        page.PageHeader = columnHeader;
        var widths = ColumnWidths(a4);
        var labels = a4
            ? new[] { "#", L.T("Món"), L.T("SL"), L.T("Đơn giá"), L.T("Thành tiền") }
            : new[] { L.T("Món"), L.T("SL"), L.T("Đơn giá"), L.T("Tiền") };
        var x = 0f;
        for (var i = 0; i < labels.Length; i++)
        {
            var cell = PdfLayout.Text(labels[i], x, 0, widths[i], a4 ? 7f : 5.2f, a4 ? 8.5f : 7f, FontStyle.Bold,
                i is 0 && a4 ? HorzAlign.Center : i == 0 ? HorzAlign.Left : i == 1 ? HorzAlign.Right : HorzAlign.Right);
            if (a4 && i == 1) cell.HorzAlign = HorzAlign.Left;
            cell.FillColor = PdfLayout.Shade;
            cell.Border = new Border { Lines = BorderLines.Top | BorderLines.Bottom, Color = PdfLayout.Rule, Width = 0.4f };
            columnHeader.Objects.Add(cell);
            x += widths[i];
        }

        var table = LinesTable(detail);
        report.RegisterData(table, LinesSource);
        var source = report.GetDataSource(LinesSource);
        if (source is not null) source.Enabled = true;

        var data = new DataBand
        {
            Height = PdfLayout.Mm * (a4 ? 7f : 5.4f),
            CanGrow = true,
            DataSource = source,
        };
        page.Bands.Add(data);
        x = 0f;
        var fields = a4 ? new[] { "No", "Item", "Qty", "Price", "Amount" } : ["Item", "Qty", "Price", "Amount"];
        for (var i = 0; i < fields.Length; i++)
        {
            var align = a4
                ? i == 0 ? HorzAlign.Center : i == 1 ? HorzAlign.Left : HorzAlign.Right
                : i == 0 ? HorzAlign.Left : HorzAlign.Right;
            // Không kẻ đáy từng dòng: ô Món co giãn khi có ghi chú, kẻ đáy sẽ lệch so với các cột khác.
            var cell = PdfLayout.Cell($"[{LinesSource}.{fields[i]}]", x, 0, widths[i], a4 ? 7f : 5.4f,
                a4 ? 8.5f : 7f, align, grow: i == (a4 ? 1 : 0));
            data.Objects.Add(cell);
            x += widths[i];
        }

        var summary = new ReportSummaryBand { CanGrow = true };
        page.ReportSummary = summary;
        BuildSummary(summary, detail, a4);

        if (a4)
        {
            var footer = new PageFooterBand { Height = PdfLayout.Mm * 6f };
            page.PageFooter = footer;
            footer.Objects.Add(PdfLayout.Text($"{StoreProfile.Name} · " + L.T("Trang [Page#]/[TotalPages#]"), 0, 0, 180, 6, 7.5f,
                FontStyle.Regular, HorzAlign.Center, VertAlign.Bottom, PdfLayout.Muted));
        }
        return report;
    }

    private static float[] ColumnWidths(bool a4) => a4 ? [10f, 82f, 14f, 32f, 42f] : [40f, 8f, 12f, 12f];

    private static DataTable LinesTable(InvoiceDetailDto detail)
    {
        var table = new DataTable(LinesSource);
        table.Columns.Add("No", typeof(string));
        table.Columns.Add("Item", typeof(string));
        table.Columns.Add("Qty", typeof(string));
        table.Columns.Add("Price", typeof(string));
        table.Columns.Add("Amount", typeof(string));
        var index = 0;
        foreach (var line in detail.Order.Lines)
        {
            index++;
            var gift = line.GiftPromotion is not null;
            var item = gift ? L.T("Tặng kèm · ") + line.Name : line.Name;
            if (!string.IsNullOrWhiteSpace(line.Note)) item += "\n" + L.T("Ghi chú: ") + line.Note;
            table.Rows.Add(index.ToString(PrintFormat.Vi), item, PrintFormat.Integer(line.Quantity),
                gift ? L.T("Tặng") : PrintFormat.Money(line.Price), PrintFormat.Money(line.Amount));
        }
        return table;
    }

    private static void BuildTitle(ReportTitleBand band, InvoiceDetailDto detail, bool reprint, bool a4)
    {
        var h = detail.Order.Header;
        var width = a4 ? 180f : 72f;
        float y = 0;
        band.Objects.Add(PdfLayout.Text(StoreProfile.Name, 0, y, width - (a4 ? 38 : 0), a4 ? 9f : 6.5f, a4 ? 16f : 11.5f,
            FontStyle.Bold, HorzAlign.Left, VertAlign.Center, null, false));
        if (reprint)
        {
            var badge = PdfLayout.Text(L.T("IN LẠI"), width - 30, y, 30, 6f, 8f, FontStyle.Bold, HorzAlign.Center,
                VertAlign.Center, Color.Black, false);
            badge.Border = new Border { Lines = BorderLines.All, Color = Color.Black, Width = 0.4f };
            band.Objects.Add(badge);
        }
        y += a4 ? 9f : 7f;
        if (StoreProfile.Address is { Length: > 0 } address)
        {
            band.Objects.Add(PdfLayout.Text(address, 0, y, width, 4.4f, a4 ? 8f : 6.5f, FontStyle.Regular, HorzAlign.Left,
                VertAlign.Center, PdfLayout.Muted));
            y += 4.4f;
        }
        if (StoreProfile.Phone is { Length: > 0 } phone)
        {
            band.Objects.Add(PdfLayout.Text(L.T("Điện thoại: ") + phone, 0, y, width, 4.4f, a4 ? 8f : 6.5f,
                FontStyle.Regular, HorzAlign.Left, VertAlign.Center, PdfLayout.Muted));
            y += 4.4f;
        }
        y += a4 ? 2f : 1f;
        band.Objects.Add(PdfLayout.Text(L.T("HÓA ĐƠN BÁN HÀNG"), 0, y, width, a4 ? 8f : 6f, a4 ? 14f : 10f,
            FontStyle.Bold));
        y += a4 ? 8f : 6f;
        band.Objects.Add(PdfLayout.Text(L.T("Số: ") + h.Code + " · " + PrintFormat.DateTime(h.Created), 0, y, width, 4.6f,
            a4 ? 9f : 7f));
        y += 4.8f;
        var place = string.IsNullOrWhiteSpace(h.TableCode) ? L.T("Không bàn") : L.T("Bàn ") + h.TableCode;
        band.Objects.Add(PdfLayout.Text(SalesStates.Label(h.Channel) + " · " + place + " · " + SalesStates.Label(h.Status),
            0, y, width, 4.4f, a4 ? 8f : 6.5f, FontStyle.Regular, HorzAlign.Left, VertAlign.Center, PdfLayout.Muted));
        y += 4.6f;
        band.Objects.Add(PdfLayout.Text(L.T("Khách: ") + (h.Customer ?? L.T("Vãng lai")), 0, y, width, 4.4f,
            a4 ? 8f : 6.5f, FontStyle.Regular, HorzAlign.Left, VertAlign.Center, PdfLayout.Muted));
        y += 5f;
        band.Height = y * PdfLayout.Mm;
    }

    private static void BuildSummary(ReportSummaryBand band, InvoiceDetailDto detail, bool a4)
    {
        var h = detail.Order.Header;
        var width = a4 ? 180f : 72f;
        var labelX = a4 ? width - 80f : 0f;
        var labelW = a4 ? 44f : 36f;
        var valueX = a4 ? width - 36f : 36f;
        var valueW = a4 ? 36f : 36f;
        var size = a4 ? 9f : 7.5f;
        float y = 2f;

        var line = PdfLayout.Text(string.Empty, 0, 0, width, 0.2f);
        line.Border = new Border { Lines = BorderLines.Top, Color = Color.Black, Width = 0.4f };
        band.Objects.Add(line);

        void Row(string label, string value, bool strong = false)
        {
            var style = strong ? FontStyle.Bold : FontStyle.Regular;
            band.Objects.Add(PdfLayout.Text(label, labelX, y, labelW, 6f, strong ? size + 1 : size, style,
                HorzAlign.Right, VertAlign.Center, strong ? Color.Black : PdfLayout.Muted));
            band.Objects.Add(PdfLayout.Text(value, valueX, y, valueW, 6f, strong ? size + 1 : size, style,
                HorzAlign.Right));
            y += strong ? 7.5f : 6f;
        }

        Row(L.T("Tiền món"), PrintFormat.Money(h.Total));
        Row(L.T("Giảm"), PrintFormat.Money(h.Discount));
        Row("Ship", PrintFormat.Money(h.Ship));
        Row(L.T("PHẢI TRẢ"), PrintFormat.Money(h.Payable), strong: true);

        if (!string.IsNullOrWhiteSpace(detail.Source) || detail.PercentDiscount is not null)
        {
            var percentText = detail.PercentDiscount is int percent
                ? detail.Source == "TierAndPromotion"
                    ? L.F(" {0}% (hạng {1}% + KM {2}%)", percent, detail.TierPercent ?? 0, detail.PromotionPercent ?? 0)
                    : $" {percent}%"
                : string.Empty;
            band.Objects.Add(PdfLayout.Text(L.T("Nguồn giảm: ") + SalesStates.Label(detail.Source) + percentText,
                0, y, width, 4.4f, a4 ? 8f : 6.5f, FontStyle.Regular, HorzAlign.Left, VertAlign.Center, PdfLayout.Muted));
        }
        else
            band.Objects.Add(PdfLayout.Text(L.T("Nguồn/tỷ lệ ưu đãi lịch sử chưa lưu."), 0, y, width, 4.4f,
                a4 ? 8f : 6.5f, FontStyle.Regular, HorzAlign.Left, VertAlign.Center, PdfLayout.Muted));
        y += 4.6f;

        if (detail.Promotions.Count > 0)
        {
            var promos = L.T("Ưu đãi áp dụng: ") + string.Join(" · ", detail.Promotions.Select(p =>
                $"{p.Code} {p.Name} {p.Percent}% ({PrintFormat.Money(p.Discount)})"));
            band.Objects.Add(PdfLayout.Text(promos, 0, y, width, 4.4f, a4 ? 8f : 6.5f,
                FontStyle.Regular, HorzAlign.Left, VertAlign.Top, null, true, true));
            y += Math.Max(5f, EstimateLines(promos, width, a4 ? 8f : 6.5f) * 4.2f);
        }

        // Đơn đã trả có dòng quà 0 ₫; đơn mở (in tạm) hiển thị quà SQL sẽ thêm khi thanh toán.
        var gifts = detail.Order.Lines.Where(x => x.GiftPromotion is not null)
            .Select(x => x.Name + " ×" + PrintFormat.Integer(x.Quantity)).ToArray();
        var proposals = detail.GiftProposals ?? [];
        var giftText = gifts.Length > 0 ? L.T("Tặng kèm: ") + string.Join(" · ", gifts)
            : proposals.Count > 0 ? L.T("Tặng kèm khi thanh toán: ") + string.Join(" · ", proposals.Select(p => p.ProductName + " ×" + PrintFormat.Integer(p.Quantity)))
            : null;
        if (giftText is not null)
        {
            band.Objects.Add(PdfLayout.Text(giftText, 0, y, width, 4.4f, a4 ? 8f : 6.5f,
                FontStyle.Regular, HorzAlign.Left, VertAlign.Top, null, true, true));
            y += Math.Max(5f, EstimateLines(giftText, width, a4 ? 8f : 6.5f) * 4.2f);
        }

        if (detail.Order.Payments.Count > 0)
        {
            var payments = string.Join(" · ", detail.Order.Payments.Select(p =>
                p.Name + ": " + PrintFormat.Money(p.Amount) + (string.IsNullOrWhiteSpace(p.Reference) ? string.Empty : " (" + p.Reference + ")")));
            band.Objects.Add(PdfLayout.Text(L.T("Thanh toán: ") + payments, 0, y, width, 4.4f, a4 ? 8f : 6.5f,
                FontStyle.Regular, HorzAlign.Left, VertAlign.Top, null, true, true));
            y += Math.Max(5f, EstimateLines(payments, width, a4 ? 8f : 6.5f) * 4.2f);
        }
        if (h.PaidAt is not null)
        {
            band.Objects.Add(PdfLayout.Text(L.T("Đã thanh toán: ") + PrintFormat.DateTime(h.PaidAt), 0, y, width, 4.4f,
                a4 ? 8f : 6.5f, FontStyle.Regular, HorzAlign.Left, VertAlign.Center, PdfLayout.Muted));
            y += 4.6f;
        }
        band.Objects.Add(PdfLayout.Text(L.T("Không phải hóa đơn VAT. Giá/số lượng theo snapshot đã lưu; in lại không dựng tiền đưa/thối chưa lưu."),
            0, y, width, 5f, a4 ? 7f : 6f, FontStyle.Regular, HorzAlign.Left, VertAlign.Top, PdfLayout.Muted));
        y += 7f;
        band.Height = y * PdfLayout.Mm;
    }

    private static int EstimateLines(string text, float width, float size) =>
        Math.Max(1, (int)Math.Ceiling(text.Length * size * 0.52f / Math.Max(10f, width * PdfLayout.Mm)));
}
