# Yêu cầu — Đầu ra hóa đơn & báo cáo bằng FastReport OpenSource + PDF

**Ngày mở:** 16/11/2026. **Nguồn:** người dùng phản ánh bản in hóa đơn hiện tại
"không đúng định dạng, kiểu viết ngang ngang" và hỏi vì sao không dùng FastReport.
**Phạm vi:** bổ sung đầu ra cho G04/T17 (in hóa đơn) và G07/T28 (in báo cáo),
không thay nghiệp vụ/SQL/quyền/contract tài chính.

## 1. Hiện trạng trước thay đổi

- Hóa đơn: `Printing/InvoicePrintDocument.cs` vẽ danh sách dòng chữ trên A4, ngăn
  bằng dấu `·`, không có cột Món/SL/Đơn giá/Thành tiền, tiền không căn phải, tên
  quán hard-code; preview bằng `PrintPreviewControl`, in bằng `PrintDialog` native.
- Báo cáo: `Modules/Reports/ReportsControl.cs` chỉ có `DataGridView`; không có in
  hoặc xuất file. Dữ liệu đã có `ReportPage`, totals, metadata, phân trang 50 dòng.

## 2. Quyết định đã chốt với người dùng

- Dùng **FastReport.OpenSource 2027.1.0** (MIT) + **FastReport.OpenSource.Export.PdfSimple**
  + **Microsoft.Web.WebView2**; hạ tầng dựng report bằng code C# (không dùng `.frx`
  vì agent không mở được designer).
- Ba đầu ra: (1) hóa đơn A4 và 80mm, (2) in báo cáo A4 ngang, (3) xuất CSV.
- Trade-off đã được thông báo và người dùng chấp nhận: bản free không có
  preview/in WinForms, nên preview đi qua WebView2 và in bằng print UI của Edge;
  PDFSimple xuất PDF dạng **ảnh raster 300 DPI** (không vector, không chọn chữ).

## 3. Yêu cầu chức năng

1. **Hóa đơn**: header quán (tên, địa chỉ/SĐT nếu cấu hình), mã/ngày/kênh/bàn/trạng
   thái/khách, bảng cột `# / Món / SL / Đơn giá / Thành tiền`, ghi chú tiếng Việt
   xuống dòng trong ô Món, khối Tiền món/Giảm/Ship/PHẢI TRẢ, nguồn giảm, thanh toán,
   thời điểm thanh toán, nhãn **IN LẠI**, footer `Trang x/y` (A4) và ghi chú không
   phải hóa đơn VAT. Khổ 80mm co chiều cao theo nội dung, không đẩy đủ 297mm.
2. **Preview/in**: mở form modal `PdfPreviewForm` (WebView2) với nút In/Lưu PDF/
   Mở bằng trình đọc PDF/Đóng; hóa đơn có thêm chọn khổ A4/80mm và tạo lại PDF.
   Quyền in vẫn do `InvoiceService.DemandPrintAsync` kiểm trước khi mở preview.
   Lỗi in/preview không đổi dữ liệu hóa đơn đã chốt.
3. **Báo cáo**: nút **In / PDF** (A4 ngang, tối đa 500 dòng) và **Xuất CSV**
   (UTF-8 BOM, phân cách `;`, tối đa 1.000 dòng) ở thanh hành động; chỉ bật khi đã
   tải dữ liệu hợp lệ; dùng chung mô tả cột giữa grid/PDF/CSV; totals lấy từ nguồn
   SQL, không cộng lại ở client. Vượt ngưỡng thì ghi rõ "chỉ N/M dòng" trong PDF/CSV.
4. **Thông tin quán**: `StoreProfile` đọc `CAFEMANAGER_STORE_NAME/ADDRESS/PHONE`
   (mặc định `COFFEE MANAGER`, không bịa địa chỉ/SĐT).

## 4. Ràng buộc

- Không đổi SQL, BLL, quyền, contract tài chính; hóa đơn đã chốt giữ nguyên khi
  in/hủy in. Không in VAT, không dựng tiền đưa/thối chưa lưu.
- Form preview là hộp thoại Windows có owner theo quy ước `embedded-dialogs`.
- File PDF tạm ở `%TEMP%\CoffeeManager`, dọn sau một ngày; đóng preview xóa file
  trừ khi người dùng mở bằng trình đọc ngoài.

## 5. Tiêu chí hoàn tất

- Build Debug/Release thành công; unit test in ấn/CSV PASS.
- File PDF mở được, đúng khổ A4/80mm/A4 ngang; hóa đơn nhiều dòng không mất dòng.
- Phần hình thức và in thật (máy in nhiệt 80mm, Microsoft Print to PDF) do người
  dùng kiểm; agent ghi SKIPPED, không suy PASS.
