using System.Data;
using System.Drawing;
using FastReport;
using CafeManager.Models.Dtos.Inventory;

namespace CafeManager.WinForms.Printing.FastReport;

// Phiếu nhập nội bộ: bản Chờ nhận có ô thực nhận trống để nhân viên ghi tay; bản đã chốt in số thực nhận.
internal static class PurchaseSlipPdfReport
{
    private const string LinesSource = "Lines";
    public static string Write(PurchaseRow row)
    {
        var path = PrintOutput.NewPath("phieunhap", row.Code);
        PdfReportExporter.Export(Build(row), path, L.T("Phiếu nhập ") + row.Code);
        return path;
    }

    internal static string StatusLabel(string status) => status switch
    {
        "ChoNhan" => L.T("Chờ nhận"),
        "DaKiem" => L.T("Đã kiểm"),
        "DaChot" => L.T("Đã chốt"),
        "DaHuy" => L.T("Đã huỷ"),
        _ => status
    };

    private static Report Build(PurchaseRow row)
    {
        const float width = 180f;
        var report = new Report { ReportInfo = { Name = L.T("Phiếu nhập hàng") } };
        var page = new ReportPage
        {
            PaperWidth = 210f, PaperHeight = 297f, Landscape = false,
            LeftMargin = 15f, RightMargin = 15f, TopMargin = 12f, BottomMargin = 12f,
        };
        report.Pages.Add(page);
        var title = new ReportTitleBand();
        page.ReportTitle = title;
        BuildTitle(title, row, width);

        var widths = new[] { 8f, 60f, 14f, 18f, 20f, 29f, 31f };
        var labels = new[] { "#", L.T("Nguyên liệu"), L.T("ĐVT"), L.T("SL đặt"), L.T("Thực nhận"), L.T("Đơn giá"), L.T("Thành tiền") };
        var header = new PageHeaderBand { Height = PdfLayout.Mm * 7f };
        page.PageHeader = header;
        var x = 0f;
        for (var i = 0; i < labels.Length; i++)
        {
            var cell = PdfLayout.Text(labels[i], x, 0, widths[i], 7f, 8.5f, FontStyle.Bold,
                i >= 3 ? HorzAlign.Right : HorzAlign.Left);
            cell.FillColor = PdfLayout.Shade;
            cell.Border = new Border { Lines = BorderLines.Top | BorderLines.Bottom, Color = PdfLayout.Rule, Width = 0.4f };
            header.Objects.Add(cell);
            x += widths[i];
        }

        var table = LinesTable(row);
        report.RegisterData(table, LinesSource);
        var source = report.GetDataSource(LinesSource);
        if (source is not null) source.Enabled = true;
        var data = new DataBand { Height = PdfLayout.Mm * 6.2f, CanGrow = true, DataSource = source };
        page.Bands.Add(data);
        x = 0f;
        var fields = new[] { "No", "Item", "Unit", "Ordered", "Received", "Price", "Amount" };
        for (var i = 0; i < fields.Length; i++)
        {
            var cell = PdfLayout.Cell($"[{LinesSource}.{fields[i]}]", x, 0, widths[i], 6.2f, 8.5f,
                i >= 3 ? HorzAlign.Right : HorzAlign.Left, grow: i == 1);
            data.Objects.Add(cell);
            x += widths[i];
        }

        var summary = new ReportSummaryBand { CanGrow = true };
        page.ReportSummary = summary;
        BuildSummary(summary, row, width);

        var footer = new PageFooterBand { Height = PdfLayout.Mm * 6f };
        page.PageFooter = footer;
        footer.Objects.Add(PdfLayout.Text($"{StoreProfile.Name} · " + L.T("Trang [Page#]/[TotalPages#]"), 0, 0, width, 6f,
            7.5f, FontStyle.Regular, HorzAlign.Center, VertAlign.Bottom, PdfLayout.Muted));
        return report;
    }

    private static void BuildTitle(ReportTitleBand band, PurchaseRow row, float width)
    {
        float y = 0;
        band.Objects.Add(PdfLayout.Text(StoreProfile.Name, 0, y, width, 6f, 12f, FontStyle.Bold));
        y += 6f;
        if (StoreProfile.Address is { Length: > 0 } address) { band.Objects.Add(PdfLayout.Text(address, 0, y, width, 4.2f, 8f, FontStyle.Regular, HorzAlign.Left, VertAlign.Center, PdfLayout.Muted)); y += 4.2f; }
        if (StoreProfile.Phone is { Length: > 0 } phone) { band.Objects.Add(PdfLayout.Text(L.T("Điện thoại: ") + phone, 0, y, width, 4.2f, 8f, FontStyle.Regular, HorzAlign.Left, VertAlign.Center, PdfLayout.Muted)); y += 4.2f; }
        y += 2f;
        band.Objects.Add(PdfLayout.Text(L.T("PHIẾU NHẬP HÀNG"), 0, y, width, 8f, 15f, FontStyle.Bold));
        y += 8f;
        band.Objects.Add(PdfLayout.Text(L.T("Số: ") + row.Code + " · " + PrintFormat.DateTime(row.Date), 0, y, width, 4.8f, 9f));
        y += 5f;
        band.Objects.Add(PdfLayout.Text(L.T("Nhà cung cấp: ") + row.Supplier + " · " + L.T("Trạng thái: ") + StatusLabel(row.Status),
            0, y, width, 4.8f, 9f, FontStyle.Regular, HorzAlign.Left, VertAlign.Center));
        y += 5f;
        var extra = row.CountedAt is DateTime counted
            ? L.T("Người kiểm: ") + (row.CountedBy ?? "—") + " · " + L.T("Lúc kiểm: ") + PrintFormat.DateTime(counted)
            : row.Status == "ChoNhan" ? L.T("Chưa kiểm nhận; số thực nhận để trống cho nhân viên ghi khi nhận hàng.") : string.Empty;
        if (extra.Length > 0) { band.Objects.Add(PdfLayout.Text(extra, 0, y, width, 4.4f, 8f, FontStyle.Regular, HorzAlign.Left, VertAlign.Top, PdfLayout.Muted)); y += 5f; }
        if (row.Status == "DaHuy")
        {
            band.Objects.Add(PdfLayout.Text(L.T("Đã huỷ: ") + (row.CancelReason ?? L.T("Không ghi lý do")), 0, y, width, 4.4f, 8f, FontStyle.Bold, HorzAlign.Left, VertAlign.Top, Color.DarkRed, true, true));
            y += 5f;
        }
        y += 1f;
        band.Height = y * PdfLayout.Mm;
    }

    private static void BuildSummary(ReportSummaryBand band, PurchaseRow row, float width)
    {
        var posted = row.Status == "DaChot";
        var ordered = row.Lines.Sum(l => l.Quantity * l.Price);
        var actual = row.Lines.Sum(l => (l.Received ?? l.Quantity) * l.Price);
        var immediate = row.Immediate ?? row.PlannedImmediate;
        float y = 2f;
        void Row(string label, string value, bool strong = false)
        {
            var style = strong ? FontStyle.Bold : FontStyle.Regular;
            band.Objects.Add(PdfLayout.Text(label, width - 82f, y, 44f, 6f, strong ? 10f : 9f, style, HorzAlign.Right, VertAlign.Center, strong ? Color.Black : PdfLayout.Muted));
            band.Objects.Add(PdfLayout.Text(value, width - 36f, y, 36f, 6f, strong ? 10f : 9f, style, HorzAlign.Right));
            y += strong ? 7.5f : 6f;
        }
        var rule = PdfLayout.Text(string.Empty, 0, 0, width, 0.2f);
        rule.Border = new Border { Lines = BorderLines.Top, Color = Color.Black, Width = 0.4f };
        band.Objects.Add(rule);
        Row(L.T("Tổng theo số đặt"), PrintFormat.Money(ordered));
        if (row.Status is "DaKiem" or "DaChot") Row(L.T("Tổng theo thực nhận"), PrintFormat.Money(actual), strong: true);
        Row(posted ? L.T("Đã trả ngay") : L.T("Dự kiến trả ngay"), PrintFormat.Money(immediate));
        if (row.Status is "ChoNhan" or "DaKiem") Row(L.T("Còn lại dự kiến"), PrintFormat.Money(actual - immediate));
        else if (actual - immediate > 0) Row(L.T("Nợ nhà cung cấp"), PrintFormat.Money(actual - immediate));
        if (row.DueDate is DateTime due) Row(L.T("Hạn trả dự kiến"), PrintFormat.Date(due));
        y += 3f;
        const float col = 60f;
        var captions = new[] { L.T("Người lập phiếu"), L.T("Người kiểm nhận"), L.T("Người chốt kho") };
        for (var i = 0; i < captions.Length; i++)
            band.Objects.Add(PdfLayout.Text(captions[i], i * col, y, col, 5f, 8.5f, FontStyle.Bold, HorzAlign.Center));
        y += 16f;
        for (var i = 0; i < captions.Length; i++)
            band.Objects.Add(PdfLayout.Text(L.T("(Ký, ghi rõ họ tên)"), i * col, y, col, 4.5f, 7.5f, FontStyle.Regular, HorzAlign.Center, VertAlign.Center, PdfLayout.Muted));
        y += 6f;
        band.Objects.Add(PdfLayout.Text(L.T("Phiếu nhập nội bộ — không phải hóa đơn GTGT; không tự tạo thuế, chiết khấu hay phí."),
            0, y, width, 5f, 7.5f, FontStyle.Regular, HorzAlign.Left, VertAlign.Top, PdfLayout.Muted));
        y += 8f;
        band.Height = y * PdfLayout.Mm;
    }

    private static DataTable LinesTable(PurchaseRow row)
    {
        var table = new DataTable(LinesSource);
        foreach (var column in new[] { "No", "Item", "Unit", "Ordered", "Received", "Price", "Amount" }) table.Columns.Add(column, typeof(string));
        var index = 0;
        foreach (var line in row.Lines)
        {
            index++;
            var item = line.Name;
            if (!string.IsNullOrWhiteSpace(line.Reason)) item += "\n" + L.T("Lý do lệch: ") + line.Reason;
            var received = line.Received is decimal value ? PrintFormat.Quantity(value) : string.Empty;
            var amount = (line.Received ?? line.Quantity) * line.Price;
            table.Rows.Add(index.ToString(PrintFormat.Vi), item, line.Unit, PrintFormat.Quantity(line.Quantity), received,
                PrintFormat.Money(line.Price), PrintFormat.Money(amount));
        }
        return table;
    }
}
