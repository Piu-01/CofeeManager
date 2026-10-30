# Triển khai hỏi in và về đơn mới — 25/10/2026

Người dùng duyệt phạm vi/plan bằng “ok đó”. Đã triển khai, chưa chạy đợt test mới.

## Thay đổi

- SalesControl bỏ action Xem/In và callback ShowInvoiceAsync; Program bỏ wiring
  chỉ phục vụ callback này. Lịch sử hóa đơn/chi tiết/in lại vẫn đi theo luồng cũ.
- ResetDraft dùng cho nút Đơn mới và finally của FinishCheckout. Không gọi
  NewOrder khi busy để tránh guard làm reset không chạy; khóa busy còn giữ đến
  khi kết thúc toàn bước hậu thanh toán.
- Chỉ ResultOrder sau chốt đã xác nhận mới vào FinishCheckout. Paid DTO và tiền
  thối được giữ cục bộ; Yes/No hỏi một lần. Có lấy snapshot/kiểm quyền và mở PrintDialog;
  Không bỏ qua in. Reset ở finally cho kết quả paid, kể cả lỗi đọc/in/hủy in.
- Thông báo vẫn nêu Đã thanh toán/mã/thu/thối và điểm nếu đọc được. Lỗi tải chi
  tiết/bàn được ghi là hậu thanh toán, không biến thành thông báo thu tiền thất bại.
- Printing/InvoicePrinting dùng chung DemandPrintAsync→document mới→PrintDialog→Print.
  POS truyền reprint:false; preview lịch sử dùng mặc địnhtrue. Hủy in không gọi Print.
  Không sửa mẫu in/SQL để lưu hay dựng lại tiền đưa/thối chưa lưu.
- UX kết quả sau thanh toán đã cập nhật. Hủy Checkout/unknown/lỗi trước commit
  không đi vào reset hậu thanh toán. Không có lệnh checkout mới ở đường in.

## Kiểm chứng đã chạy

Build sản phẩm Debug và Release đều0warning/0error. Artifact tại
../runs/post-checkout-print-20261025/Debug vàRelease; build logs cùng thư mục.
Lần build đầu báo namespace Modules.System che System; đã sửa qualifier
global::System trong pha triển khai, giữ build log cũ. Không là kết quả test GUI.

Source review: chỉ SalesControl.cs, Program.cs, InvoicePrintPreviewForm.cs và
file mới InvoicePrinting.cs thay đổi sản phẩm so bản UI05; DAL/BLL/Models/SQL
không đổi. Không chạy app, test chức năng, gửi in hoặc ghiDB trong đợt triển khai.
G04 giữ34PASS/0FAIL/22BLOCKED; phần mới chưa nghiệm thu từ build.

## Bước tiếp theo

Chờ người dùng duyệt một đợt theo post-checkout-print-test-plan.md, sau đó đóng
băng source mới và kiểm các nhánh Có/Không/Hủy/Lỗi, reset và bất biến SQL.
