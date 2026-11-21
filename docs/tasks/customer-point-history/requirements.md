# Requirements — Xóa màn Điểm khách hàng, đưa lịch sử điểm vào màn Khách hàng

- Ngày: 21/11/2026
- Trạng thái: **Chờ duyệt requirements** (chưa viết plan, chưa sửa code)
- Liên quan: `src/CafeManager.WinForms/Modules/Loyalty/LoyaltyControl.cs` (`PointList`),
  `src/CafeManager.WinForms/Modules/Customers/CustomersControl.cs`,
  `src/CafeManager.WinForms/Program.cs`.

## 1. Mục tiêu

Người dùng thấy tab “Điểm khách hàng” trong Hệ thống ưu đãi không có công dụng thực tế.
Thay đổi:

1. Xóa hẳn tab/màn “Điểm khách hàng” và chức năng **điều chỉnh điểm** khỏi ứng dụng.
2. Ở màn **Khách hàng**, đổi action “Hóa đơn” thành **“Lịch sử điểm”**: mở danh sách
   lịch sử điểm (chỉ đọc) của khách đang chọn — cùng chỗ người dùng đang làm việc với khách.

Không đổi công thức tích điểm, bảng dữ liệu, hóa đơn hay POS.

## 2. Hiện trạng liên quan

- `LoyaltyControl` tạo 3 tab: Khuyến mãi, Hạng thành viên (ADMIN), Điểm khách hàng (ADMIN).
  Tab cuối là `PointList` (chọn khách, xem lịch sử, “Điều chỉnh điểm”).
- Lịch sử điểm đọc qua `sp_LoyaltyHistory` (yêu cầu vai trò ADMIN ở SQL); điều chỉnh điểm
  qua `sp_LoyaltyAdjustPreview` + `sp_LoyaltyAdjust` (preview revision, transaction,
  không sửa/xóa lịch sử) — hiện chỉ dùng ở `PointList`.
- Màn Khách hàng (`CustomersControl`) có action “Hóa đơn” mở dialog danh sách hóa đơn
  của khách (`CustomerOrderList` → `CustomerService.OrdersAsync` → `sp_CustomerOrders`).
  Đây là chỗ duy nhất trong app hiển thị hóa đơn theo khách.
- Màn Khách hàng dùng chung cho cả màn quản lý và dialog chọn khách (picker); action
  “Hóa đơn” chỉ có ở màn quản lý.
- `sp_CustomerOrders`, `sp_LoyaltyAdjust*` nằm trong migration baseline (008); dữ liệu
  lịch sử điểm là bất biến, không sửa/xóa.

## 3. Phạm vi

### Làm

1. Bỏ tab “Điểm khách hàng” khỏi `LoyaltyControl`; xóa `PointList` (gồm nút “Chọn khách”,
   danh sách lịch sử và dialog “Điều chỉnh điểm”).
2. Bỏ chức năng điều chỉnh điểm phía ứng dụng:
   - `LoyaltyService.PointPreviewAsync`, `LoyaltyService.AdjustAsync`, nhánh
     `Normalize(PointAdjustmentRequest)`;
   - `LoyaltyRepository.PointPreviewAsync`, `LoyaltyRepository.AdjustAsync`;
   - DTO `PointPreview`, `PointAdjustmentRequest` nếu không còn chỗ dùng.
   Không thêm migration; không drop procedure (xem mục 4).
3. Màn Khách hàng: thay action “Hóa đơn” bằng **“Lịch sử điểm”**:
   - Bấm khi đã chọn khách → mở dialog chỉ đọc “Lịch sử điểm · <tên khách>” với bảng
     Mã GD, Thời điểm, Loại, Điểm, Lý do, Hóa đơn (dùng `LoyaltyService.HistoryAsync`,
     50 dòng/trang như màn cũ).
   - Chưa chọn khách → không mở gì (như “Hóa đơn” hiện tại).
   - Không có nút điều chỉnh/ghi trong dialog này.
4. Nối `LoyaltyService` vào `CustomersControl` ở `Program.cs` (chỉ màn quản lý; picker
   không cần).
5. Dọn mã ứng dụng thành dư thừa do thay đổi: `CustomerOrderList`, action/dialog Hóa đơn,
   `CustomerService.OrdersAsync`, `CustomerRepository.OrdersAsync`, DTO `CustomerOrder`
   (nếu không còn dùng). Giữ procedure `sp_CustomerOrders` trong DB.
6. Cập nhật tài liệu liên quan trong phạm vi được duyệt (UX/plan ghi màn Điểm khách hàng
   và action Hóa đơn của khách) sau khi triển khai.

### Không làm

- Không thêm migration, không drop procedure/table; không sửa dữ liệu lịch sử điểm.
- Không đổi công thức điểm/ưu đãi, không đổi POS/hóa đơn, không đổi màn Khuyến mãi/hạng.
- Không thêm màn hình mới ngoài dialog lịch sử điểm.
- Không đổi cách “Mở sổ điểm” của khách (giữ nguyên).
- Không tự chạy đợt test khi chưa được duyệt riêng.

## 4. Giả định và quyết định thiết kế

1. **Giữ SQL procedures điều chỉnh điểm**: baseline + test lịch sử đang tham chiếu
   `sp_LoyaltyAdjust*`; user yêu cầu bỏ chức năng, không yêu cầu đổi schema. Gỡ hẳn
   procedure cần migration riêng — nêu là việc tùy chọn, không làm trong task này.
2. **Quyền xem lịch sử điểm**: `sp_LoyaltyHistory` hiện bắt ADMIN. Vì vậy action
   “Lịch sử điểm” chỉ hiện với ADMIN (giống tab cũ); CASH không thấy action này.
   Hệ quả: CASH mất luôn chỗ xem “Hóa đơn” của khách. Nếu muốn CASH xem lịch sử điểm cần
   đổi quyền SQL — ngoài phạm vi task này (cần duyệt riêng).
3. **Không còn xem hóa đơn theo khách trong app**: action bị thay thế theo yêu cầu; lịch
   sử hóa đơn vẫn xem được ở màn “Lịch sử hóa đơn”.
4. Dialog lịch sử điểm dùng `OperationEditorDialog` chế độ chỉ đọc (giống dialog hóa đơn
   cũ), bảng đọc theo trang 50 như `PointList` cũ.
5. `sp_CustomerOrders` giữ trong DB nhưng không còn caller; ghi chú trong báo cáo, không
   drop.
6. Không đổi `LoyaltyControl` cho các tab còn lại; `PromotionList` vẫn cần `CustomerService`.

## 5. Luồng UI

### 5.1 Hệ thống ưu đãi

- ADMIN: chỉ còn tab “Khuyến mãi” và “Hạng thành viên”.
- CASH: chỉ “Khuyến mãi” như trước.

### 5.2 Khách hàng

- Action: Thêm, Sửa, Xóa, Mở sổ điểm (ADMIN), **Lịch sử điểm** (ADMIN) — thay “Hóa đơn”.
- Chọn khách → Lịch sử điểm → dialog chỉ đọc, tiêu đề “Lịch sử điểm · <tên>”, có dòng
  “Chỉ đọc; lịch sử điểm không sửa/xóa.”; nút “Đóng”.
- Khách chưa có lịch sử: danh sách rỗng với gợi ý “Khách chưa có giao dịch điểm.”.

### 5.3 Trạng thái

- Đang tải: dialog mở với loading của danh sách, không có nút ghi.
- Lỗi DB/quyền: hiện ở caption trạng thái của danh sách trong dialog, không đóng dialog.

## 6. Thay đổi kỹ thuật dự kiến

| Lớp | File/khu vực | Nội dung |
|---|---|---|
| UI | `Modules/Loyalty/LoyaltyControl.cs` | Bỏ tab Điểm khách hàng, xóa `PointList` |
| UI | `Modules/Customers/CustomersControl.cs` | Thay action Hóa đơn → Lịch sử điểm; thêm `CustomerPointHistoryList`; xóa `CustomerOrderList`; nhận `LoyaltyService` |
| UI wiring | `Program.cs` | Truyền `LoyaltyService` vào `CustomersControl` màn quản lý |
| BLL | `LoyaltyService.cs` | Bỏ `PointPreviewAsync`, `AdjustAsync`, `Normalize(PointAdjustmentRequest)` |
| DAL | `LoyaltyRepository.cs` | Bỏ `PointPreviewAsync`, `AdjustAsync`; giữ `HistoryAsync` |
| BLL/DAL | `CustomerService.cs`, `CustomerRepository.cs` | Bỏ `OrdersAsync` |
| Models | `LoyaltyRows.cs`, `CustomerRows.cs`, request DTO | Bỏ `PointPreview`, `PointAdjustmentRequest`, `CustomerOrder` nếu không còn dùng |
| SQL | — | Không đổi |

## 7. Ca nghiệm thu dự kiến (chạy sau khi được duyệt đợt test riêng)

| Mã | Nội dung |
|---|---|
| CP-01 | ADMIN: Hệ thống ưu đãi không còn tab Điểm khách hàng; còn Khuyến mãi + Hạng thành viên |
| CP-02 | CASH: menu/tab không có Điểm khách hàng; không lỗi khi mở Hệ thống ưu đãi |
| CP-03 | Khách hàng: không còn nút Hóa đơn; có nút Lịch sử điểm với ADMIN |
| CP-04 | Chọn khách có lịch sử → Lịch sử điểm hiển thị đúng dòng, đúng cột, thứ tự mới nhất trước |
| CP-05 | Khách không có lịch sử → danh sách rỗng, có gợi ý, không lỗi |
| CP-06 | Chưa chọn khách → bấm Lịch sử điểm không mở gì, không crash |
| CP-07 | CASH: không thấy nút Lịch sử điểm (do quyền ADMIN), không có nút Hóa đơn thay thế |
| CP-08 | Không còn đường vào màn điều chỉnh điểm; build sạch, không còn tham chiếu `PointList` |
| CP-09 | Regression: Thêm/Sửa/Xóa khách, Mở sổ điểm, tab Khuyến mãi và Hạng thành viên hoạt động như cũ |
| CP-10 | Hình thức (spacing dialog, cột bảng) | SKIPPED theo phân công — người dùng tự kiểm |

## 8. Tiêu chí hoàn thành

- Không còn tab/màn Điểm khách hàng và không còn chức năng điều chỉnh điểm phía app.
- Màn Khách hàng có “Lịch sử điểm” thay “Hóa đơn”, chỉ đọc, đọc từ SQL.
- Không thêm migration/drop procedure; dữ liệu khách/lịch sử điểm/hóa đơn không đổi.
- Build 0 error; CP-01..CP-09 PASS trong đợt test được duyệt, CP-10 người dùng tự kiểm.

## 9. Điểm cần người dùng xác nhận khi duyệt

1. “Lịch sử điểm” chỉ ADMIN thấy (giữ đúng quyền SQL hiện tại) — đồng ý? Nếu muốn CASH
   cũng xem được cần đổi quyền SQL (task riêng).
2. Chấp nhận mất chỗ xem “Hóa đơn” theo khách trong app (vẫn xem ở màn Lịch sử hóa đơn) — đúng ý?
3. Giữ procedure điều chỉnh điểm trong DB, chỉ bỏ phía ứng dụng — đồng ý? (muốn drop thì
   cần migration riêng)
4. Giữ “Mở sổ điểm” của khách — đúng ý?
