# Plan — Xóa màn Điểm khách hàng, đưa lịch sử điểm vào màn Khách hàng

- Ngày: 21/11/2026
- Requirements: `requirements.md` (đã duyệt 21/11/2026)
- Quyết định theo mặc định đã nêu: chỉ ADMIN thấy “Lịch sử điểm”; giữ procedure
  `sp_LoyaltyAdjust*`/`sp_CustomerOrders` trong DB; giữ “Mở sổ điểm”.

## Bước 1 — Bỏ tab Điểm khách hàng và `PointList`

Files: `src/CafeManager.WinForms/Modules/Loyalty/LoyaltyControl.cs`

- Trong constructor của `LoyaltyControl`: bỏ dòng thêm tab `Điểm khách hàng`.
- Xóa toàn bộ class `PointList` (dòng 116–139).
- Giữ `PromotionList`, `PolicyList`, `LoyaltyControl.ReadMoney` (còn dùng cho preview ưu đãi).

Verify: `grep -rn "PointList" src/CafeManager.WinForms` không còn kết quả; build 0 error.

## Bước 2 — Bỏ chức năng điều chỉnh điểm phía BLL/DAL/Models

Files:
- `src/CafeManager.Business/Services/LoyaltyService.cs`: bỏ `PointPreviewAsync`,
  `AdjustAsync`, nhánh `Normalize(PointAdjustmentRequest)`.
- `src/CafeManager.DataAccess/Repositories/LoyaltyRepository.cs`: bỏ
  `PointPreviewAsync`, `AdjustAsync`.
- `src/CafeManager.Models/Dtos/Loyalty/LoyaltyRows.cs`: bỏ `PointPreview`.
- `src/CafeManager.Models/Requests/Loyalty/LoyaltyRequests.cs`: bỏ `PointAdjustmentRequest`
  (kiểm tra file còn DTO nào khác đang dùng trước khi sửa).

Verify: `grep -rn "PointPreview\|PointAdjustmentRequest\|sp_LoyaltyAdjust" src` không còn
tham chiếu; build 0 error.

## Bước 3 — Màn Khách hàng: thay “Hóa đơn” bằng “Lịch sử điểm”

Files: `src/CafeManager.WinForms/Modules/Customers/CustomersControl.cs`

- Constructor màn quản lý nhận thêm `LoyaltyService` (picker giữ chữ ký cũ, không đổi hành vi).
- Bỏ action “Hóa đơn” và method `Orders`, xóa class `CustomerOrderList`.
- Thêm action “Lịch sử điểm” (`AddAction(..., session.Identity.Role=="ADMIN")`):
  - Nếu chưa chọn khách: không làm gì (giống Hóa đơn cũ).
  - Nếu có: mở `OperationEditorDialog` chế độ chỉ đọc, tiêu đề
    `"Lịch sử điểm · "+row.Name`, ghi chú “Chỉ đọc; lịch sử điểm không sửa/xóa.”,
    nhúng `CustomerPointHistoryList` — `OrbitRecordListControl` cấu hình như `PointList`
    cũ (cột Mã GD/Thời điểm/Loại/Điểm/Lý do/Hóa đơn, format `dd/MM/yyyy HH:mm`,
    `EmptyHint` = “Khách chưa có giao dịch điểm.”, đọc `LoyaltyService.HistoryAsync`,
    trang 50 dòng, ẩn toolbar/meta như `CustomerOrderList` cũ).
- Không thêm nút ghi/điều chỉnh trong dialog.

Verify: mở màn Khách hàng thấy nút “Lịch sử điểm” với ADMIN, không còn “Hóa đơn”;
chọn khách có lịch sử → dialog hiện đúng dòng; khách rỗng → hiện gợi ý.

## Bước 4 — Wiring `Program.cs`

Files: `src/CafeManager.WinForms/Program.cs` (dòng ~90)

- Truyền `new LoyaltyService(new LoyaltyRepository(new ConnectionFactory(runtimeConnection!)), permissionService!)`
  vào `CustomersControl` của `NavigationTarget.Customers`.
- Không đổi `CustomerPickerDialog` (dùng constructor picker không cần LoyaltyService).

Verify: build 0 error; đăng nhập ADMIN mở màn Khách hàng không lỗi DI/null.

## Bước 5 — Dọn mã hóa đơn theo khách phía app

Files:
- `src/CafeManager.WinForms/Modules/Customers/CustomersControl.cs`: xóa `CustomerOrderList`
  và method `Orders` (đã làm ở bước 3).
- `src/CafeManager.Business/Services/CustomerService.cs`: bỏ `OrdersAsync`.
- `src/CafeManager.DataAccess/Repositories/CustomerRepository.cs`: bỏ `OrdersAsync`.
- `src/CafeManager.Models/Dtos/Customers/CustomerRows.cs`: bỏ `CustomerOrder`.

Verify: `grep -rn "OrdersAsync\|CustomerOrder\b\|sp_CustomerOrders" src` không còn kết quả;
build 0 error. Không sửa `database/`.

## Bước 6 — Cập nhật tài liệu trong phạm vi

Files: `docs/UX_QuanLiQuanCafe.md` dòng 365

- Sửa mô tả hồ sơ khách: bỏ “tab lịch sử điểm”/“ADMIN Điều chỉnh điểm…”, thay bằng
  “nút Lịch sử điểm (chỉ đọc, ADMIN) mở danh sách lịch sử điểm của khách; không còn
  chức năng điều chỉnh điểm trên giao diện”.
- Không sửa checklist T01–T32/history trong kế hoạch.

Verify: đọc lại đoạn đã sửa, khớp hành vi mới.

## Bước 7 — Kiểm tra tĩnh và build

- `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Debug` → 0 error.
- Grep các từ khóa ở bước 1, 2, 5 trả rỗng.
- Liệt kê file thay đổi bằng `git status`/`git diff --stat` để báo cáo; không commit.

## Bước 8 — Chờ duyệt đợt test

Sau khi build sạch, tổng hợp danh sách CP-01..CP-09 (requirements mục 7) và xin duyệt
đợt test riêng trên DB test, DPI 125%. Không chạy test trước khi được duyệt.

## Thứ tự và phụ thuộc

1 → 2 → 3 → 4 → 5 → 6 → 7 → 8. Bước 3 phụ thuộc 2 (dùng `HistoryAsync` đã có sẵn, không
bị xóa); bước 4 phụ thuộc 3; bước 5 sau 3 để tránh tham chiếu treo.
