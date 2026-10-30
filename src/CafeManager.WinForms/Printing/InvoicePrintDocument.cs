using System.Drawing.Printing;
using CafeManager.Models.Dtos.Sales;
using CafeManager.Models.States;

namespace CafeManager.WinForms.Printing;

public sealed class InvoicePrintDocument : PrintDocument
{
    private static string Money(decimal value) => value.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
    private readonly List<string> lines = [];
    private readonly Font body = new("Segoe UI", 10);
    private readonly Font title = new("Segoe UI", 13, FontStyle.Bold);
    private int cursor, page;
    private readonly string heading;
    public InvoicePrintDocument(InvoiceDetailDto detail, bool reprint = true)
    {
        var h = detail.Order.Header; DocumentName = "Hóa đơn " + h.Code;
        heading = $"COFFEE MANAGER · {h.Code}" + (reprint ? " · IN LẠI" : "");
        lines.Add($"{h.Created:dd/MM/yyyy HH:mm} · {SalesStates.Label(h.Channel)} · Bàn {h.TableCode ?? "—"}");
        lines.Add(SalesStates.Label(h.Status));
        lines.Add("Tên hiển thị theo danh mục hiện tại; giá/số lượng theo snapshot.");
        lines.Add("MÓN · SỐ LƯỢNG × GIÁ = TIỀN DÒNG TRƯỚC GIẢM");
        foreach (var line in detail.Order.Lines)
        {
            lines.Add($"{line.Product} · {line.Name}\n{line.Quantity} × {Money(line.Price)} = {Money(line.Amount)}");
            if (line.Note is not null) lines.Add("Ghi chú: " + line.Note);
        }
        lines.Add($"Tiền món: {Money(h.Total)}");
        lines.Add($"Giảm: {Money(h.Discount)}" + (detail.PercentDiscount is int percent ? $" ({SalesStates.Label(detail.Source)} {percent}%)" : " · nguồn/tỷ lệ lịch sử chưa lưu"));
        lines.Add($"Ship: {Money(h.Ship)}"); lines.Add($"PHẢI TRẢ: {Money(h.Payable)}");
        foreach (var payment in detail.Order.Payments) lines.Add($"{payment.Name}: {Money(payment.Amount)}" + (payment.Reference is null ? "" : $" · {payment.Reference}"));
        if (h.PaidAt is not null) lines.Add($"Thanh toán: {h.PaidAt:dd/MM/yyyy HH:mm}");
        lines.Add("Không phải hóa đơn VAT. In lại không dựng tiền đưa/thối chưa lưu.");
        DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);
        var a4 = PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault(x => x.Kind == PaperKind.A4);
        if (a4 is not null) DefaultPageSettings.PaperSize = a4;
    }
    protected override void OnBeginPrint(PrintEventArgs e) { cursor = 0; page = 0; base.OnBeginPrint(e); }
    protected override void OnPrintPage(PrintPageEventArgs e)
    {
        base.OnPrintPage(e); if (e.Graphics is null) throw new InvalidOperationException("Không tạo được trang in.");
        page++; var bounds = e.MarginBounds; var y = (float)bounds.Top;
        var titleHeight = e.Graphics.MeasureString(heading, title, bounds.Width).Height + 8;
        e.Graphics.DrawString(heading, title, Brushes.Black, new RectangleF(bounds.Left, y, bounds.Width, titleHeight)); y += titleHeight;
        if (page > 1) { e.Graphics.DrawString("Món / nội dung tiếp theo", body, Brushes.Black, bounds.Left, y); y += body.GetHeight(e.Graphics) + 8; }
        var bottom = bounds.Bottom - body.GetHeight(e.Graphics) - 12;
        var first = cursor;
        while (cursor < lines.Count)
        {
            var height = (float)Math.Ceiling(e.Graphics.MeasureString(lines[cursor], body, bounds.Width).Height) + 8;
            if (y + height > bottom) break;
            e.Graphics.DrawString(lines[cursor], body, Brushes.Black, new RectangleF(bounds.Left, y, bounds.Width, height)); y += height; cursor++;
        }
        if (cursor == first && cursor < lines.Count) throw new InvalidOperationException("Khổ giấy quá nhỏ. Chọn khổ lớn hơn để không mất nội dung.");
        e.Graphics.DrawString($"Trang {page}", body, Brushes.Black, bounds.Left, bottom + 4);
        e.HasMorePages = cursor < lines.Count;
    }
    protected override void Dispose(bool disposing) { if (disposing) { body.Dispose(); title.Dispose(); } base.Dispose(disposing); }
}
