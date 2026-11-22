# Plan t04 — Hiển thị quà ở bán hàng, chi tiết và in

Phụ thuộc t02 (DTO `PreviewGift`, `SalesLine.GiftPromotion`, `GiftProposals`);
dữ liệu do t01 cung cấp.

## Bước 1 — Checkout

`src/CafeManager.WinForms/Modules/Sales/CheckoutDialog.cs`
- `UpdateSummary`: sau danh sách ưu đãi giảm, nếu `preview.Gifts.Count > 0` thêm khối
  "Tặng kèm (mỗi hóa đơn một lần):" + từng dòng `TenKM · TenSP ×SL`.
- Không đổi thứ tự/khóa nút; quà là thông tin chỉ đọc.
- Nếu `preview.Gifts.Count>0` và tổng phải trả 0, vẫn giữ luồng chốt 0 ₫ hiện có.

`src/CafeManager.WinForms/Modules/Sales/OrderPanel.cs`
- Dòng tóm tắt ưu đãi thêm hậu tố `· Tặng N phần` khi preview có quà (nếu
  `SalesOrderDto.Header` chưa mang GiftCount, dùng `PromotionCount` hiện có + đọc
  `SalesReadCore` set quà — nếu không có dữ liệu thì bỏ hậu tố, không bịa số).
  Thực tế chốt tại t02: chỉ hiển thị nếu DTO có trường; nếu không, bỏ bước này.

## Bước 2 — Chi tiết hóa đơn

`src/CafeManager.WinForms/Modules/Sales/InvoiceDetailDialog.cs`
- Grid món: cột Note hiển thị "Tặng kèm" cho dòng có `GiftPromotion` (thêm cột
  "Quà" hoặc ghép tiền tố; chọn ghép để không phá layout 900×620).
- Summary thêm dòng "Tặng kèm: …" từ `detail.Order.Lines` (đơn đã trả) hoặc
  `detail.GiftProposals` (đơn mở, ghi rõ "sẽ thêm khi thanh toán").
- Không đổi nút/in lại.

## Bước 3 — In

`src/CafeManager.WinForms/Printing/FastReport/InvoicePdfReport.cs`
- Danh sách món: dòng quà (`GiftPromotion != null`) in tên + SL + "Tặng" thay vì
  0 ₫ (hoặc 0 ₫ kèm nhãn; chọn "Tặng" để rõ).
- Sau khối "Ưu đãi áp dụng", thêm khối "Tặng kèm:":
  - Đơn đã trả: lấy từ `detail.Order.Lines` có `GiftPromotion`.
  - Đơn mở (in tạm): lấy `detail.GiftProposals`, nhãn "Tặng kèm khi thanh toán".
- Tăng `y` theo `EstimateLines` như các khối text hiện có; không tràn footer.
- Xử lý `GiftProposals` rỗng/không có set 6 an toàn.

`src/CafeManager.WinForms/Printing/InvoicePrinting.cs`: chỉ kiểm tra không cần đổi
nếu preview dùng cùng `InvoiceDetailDto`; nếu có kiểm tra null thì giữ.

## Bước 4 — POS/thông báo

- POS không hiển thị dòng quà trước khi chốt (quà ghi lúc thanh toán); nếu người
  dùng muốn thấy trước, checkout preview đã có. Ghi rõ trong report.
- Thông báo lỗi thiếu nguyên liệu do quà dùng nguyên văn 52203 hiện có; không đổi.

## Kiểm chứng

- Build Debug/Release.
- Đơn có KM tặng: mở checkout thấy danh sách quà; in tạm thấy khối "sẽ thêm khi
  thanh toán"; sau thanh toán chi tiết/in thấy dòng 0 ₫ nhãn Tặng và tổng tiền
  không đổi.
- Đơn không có quà: giao diện/in giống trước (không thêm khối rỗng).
- Test DB đầy đủ thuộc t05; hình thức do người dùng nghiệm thu.
