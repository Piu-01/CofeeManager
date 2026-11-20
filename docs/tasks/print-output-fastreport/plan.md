# Plan — Đầu ra hóa đơn & báo cáo bằng FastReport OpenSource + PDF

**Trạng thái:** đã triển khai trong phiên agent 16/11/2026; chờ người dùng kiểm
hình thức và đợt test riêng. Bản kế hoạch gốc đã duyệt:
`C:\Users\Lenovo\.qoder\plans\endless-jungle-weasel.md`.

## 1. Gói và cấu hình

- `FastReport.OpenSource` 2027.1.0, `FastReport.OpenSource.Export.PdfSimple` 2027.1.0,
  `Microsoft.Web.WebView2` 1.0.4314.35 (pin version trong csproj).
- License MIT FastReport và license WebView2 copy vào `Resources/Licenses/`, ra output.
- Đã probe trên `net10.0-windows`: dựng report bằng code, export PDF thành công
  (magic `%PDF`, MediaBox A4 595pt, 80mm 226pt); PDF là một ảnh JPEG/trang (300 DPI).

## 2. Cấu trúc file

| File | Vai trò |
|---|---|
| `Printing/StoreProfile.cs` | Tên/địa chỉ/SĐT quán từ env, mặc định không bịa dữ liệu |
| `Printing/PrintFormat.cs` | Format vi-VN: tiền `#,##0 ₫`, số lượng, ngày giờ |
| `Printing/PrintOutput.cs` | Thư mục PDF tạm, tạo tên file an toàn, dọn/xóa, mở ngoài |
| `Printing/FastReport/PdfLayout.cs` | Helper mm→px, TextObject/cell, màu, border |
| `Printing/FastReport/PdfReportExporter.cs` | `Prepare()` + `PDFSimpleExport` (300 DPI, JPEG 90) |
| `Printing/FastReport/InvoicePdfReport.cs` | Layout hóa đơn A4/80mm |
| `Printing/FastReport/TabularPdfReport.cs` | Báo cáo A4 ngang, cột động, header lặp trang |
| `Printing/InvoicePrinting.cs` | `PreviewAsync` giữ `DemandPrintAsync` rồi mở preview |
| `Forms/PdfPreviewForm.cs` | WebView2 + In/Lưu/Mở ngoài/Đóng, chọn khổ hóa đơn |
| `Modules/Reports/ReportColumns.cs` | Mô tả cột dùng chung grid/PDF/CSV cho 9 loại |
| `Modules/Reports/ReportOutput.cs` | Đọc nhiều trang có cap/hủy, CSV, bảng dữ liệu PDF |

Đã xóa `Printing/InvoicePrintDocument.cs` và `Forms/InvoicePrintPreviewForm.cs`.

## 3. Quyết định triển khai

- **Code-built report**, không `.frx`: cột báo cáo động theo `ReportKind`; dễ test.
- Bind `DataTable` đã format sẵn, `RegisterData` + `DataBand` + `[Rows.Field]` để
  không lệch locale với format của FastReport.
- 80mm dùng `ReportPage.UnlimitedHeight = true`; A4 dùng `DoublePass = true` để
  `[TotalPages#]` đúng.
- Không kẻ đáy từng dòng dữ liệu: ô Món/Nội dung co giãn khi ghi chú/tên dài, kẻ
  đáy sẽ lệch so với các cột còn lại. Header/bảng vẫn có nền và đường phân cách.
- In: `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser)`; lỗi runtime
  WebView2 thì tự mở PDF bằng trình đọc ngoài và báo trạng thái.
- Cap báo cáo: PDF 500 dòng, CSV 1.000 dòng vì mỗi trang gọi lại procedure báo cáo.
- CSV: BOM UTF-8, `;`, số thô invariant, ngày `dd/MM/yyyy HH:mm`, escape `;`/`"`/xuống dòng.

## 4. Thay đổi call site

- `Modules/Sales/SalesControl.cs`: sau checkout hỏi in → `InvoicePrinting.PreviewAsync`.
- `Modules/Sales/InvoiceDetailDialog.cs`: "Xem trước / In lại" → `PreviewAsync`.
- `Modules/Reports/ReportsControl.cs`: thêm nút In/PDF và Xuất CSV; `Header()` dùng
  `ReportColumns.Header`; nút tắt khi dữ liệu chưa hợp lệ hoặc đang xuất.

## 5. Lệch UX đã chấp nhận

- Hộp in native `PrintDialog` thay bằng **print preview của Edge/WebView2**.
- POS sau khi chốt hiện form xem trước trước khi in; đóng form chỉ là không in.
- PDF là ảnh 300 DPI: nét khi in A4, phóng to trên màn hình bị rỗ, không tìm chữ.
- 80mm chưa in thử trên máy nhiệt thật trong phiên agent.

## 6. Rủi ro còn lại

- `ShowPrintUI` với PDF trên WebView2 phụ thuộc runtime Edge của máy; đã có fallback.
- Báo cáo nhiều trang tốn thời gian vì gọi procedure theo trang (đã cap).
- Dung lượng PDF raster lớn hơn vector (khoảng vài trăm KB/trang).
