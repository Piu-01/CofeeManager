# Lỗi — POS cho phép tạo nháp trên bàn đang có đơn, "Giữ" thất bại và nháp nằm lại

Ngày ghi nhận: 18/11/2026. Trạng thái: **đã điều tra; đề xuất sửa riêng bị thay thế
bởi hướng bỏ nháp 19/11/2026 (chờ duyệt)**.
Loại: khoảng trống kiểm tra nghiệp vụ ở UI POS (tạo nháp), không phải lỗi SQL hay
schema. Không liên quan lỗi schema drift đã sửa (`../development-db-schema-drift/`).

> Cập nhật 19/11/2026: người dùng yêu cầu bỏ hẳn cơ chế nháp POS. Nếu hướng đó được
> duyệt thì lỗi này tự hết (không còn nháp trên bàn bận); xem
> `docs/tasks/g04-ban-hang-vong-doi-hoa-don/t14-tao-sua-don/audit-bo-nhap-pos.md`.
> Phần "Đề xuất sửa" bên dưới giữ làm lịch sử, không triển khai riêng.

## Hiện tượng (theo báo cáo người dùng và ảnh chụp)

Ở màn Bán hàng, panel bên phải hiện "Đơn nháp" (badge "Nháp") cho
"Tại quán · Bàn B0001", có dòng món và tạm tính. Người dùng bấm "Đơn mới" (hoặc rời
tab) → hộp "Giữ đơn nháp?" → chọn "Giữ" → sau đó chọn "Chọn bàn khác" trong hộp
xung đột, nhưng panel **vẫn còn "Đơn nháp"** ở bàn cũ. Người dùng nghi chạy sai
phiên bản.

## Kiểm tra phiên bản (loại trừ sai build)

- `bin/Debug/net10.0-windows/CafeManager.WinForms.exe` 17/11/2026 02:06; commit gần
  nhất `5673f51` 17/11/2026 02:04 — build chứa đúng code đang có trong HEAD.
- `CoffeeManager_Development` ở `SchemaMigrations` 35 (áp 032–035 lúc 15:09 18/11).
- Vậy không phải do chạy nhầm bản cũ hay DB thiếu migration.

## Tái hiện (đã xác nhận trên bản sao DB dev, không ghi vào DB dev)

DB test: `CoffeeManager_Test_DevUi_20261118` (COPY_ONLY backup của
`CoffeeManager_Development`, schema 35, có đơn seed mở ở B0001–B0006). Ứng dụng
`bin/Debug` build 17/11, đăng nhập ADMIN.

1. Mở Bán hàng. ComboBox **Bàn** đang để "Ban 01 – HD20250411" (B0001 **đã có đơn**).
2. **Không** bấm "Chọn bàn", bấm nút thêm món "Thêm Trà sữa trân châu".
   Kết quả: panel hiện **"Đơn nháp"**, badge "Nháp",
   "Tại quán · Bàn B0001", tạm tính 45.000 ₫; thanh trạng thái:
   "Đã thêm vào nháp; chưa ghi SQL. Chọn Giữ hoặc Thanh toán để lưu."
3. Bấm "Đơn mới" → hộp "Giữ đơn nháp?" → **Giữ**.
   Kết quả: thanh trạng thái "Bàn đã có đơn. Tải đơn hiện tại trước khi thêm món."
   và hộp **"Bàn đã có đơn"**: "Bàn B0001 vừa có đơn ở phiên khác. Nháp vẫn được giữ;
   chọn cách xử lý." với 4 nút Ở lại / Bỏ nháp / **Chọn bàn khác** / Mở đơn hiện có.
4. Chọn **Chọn bàn khác** → panel vẫn "Đơn nháp" ở Bàn B0001; thanh trạng thái
   "Chọn bàn trống khác rồi bấm Chọn bàn; nháp được giữ."

Đối chứng luồng hợp lệ: nháp tạo trên bàn trống B0002 → "Đơn mới" → Giữ → SQL tạo
hóa đơn `HD26093447` tại B0002 (phiên 1, 1 dòng), panel được xóa, app chuyển sang
bàn trống kế (Bàn 07). Vậy cơ chế Giữ/flush **không hỏng**; lỗi nằm ở việc tạo nháp
trên bàn không hợp lệ.

## Nguyên nhân

- `src/CafeManager.WinForms/Modules/Sales/SalesControl.cs:561-581` (`AddProduct`):
  khi chưa có đơn và kênh "Tại quán", code chỉ kiểm `TableCode is null` rồi tạo dòng
  nháp, **không kiểm bàn đang có đơn**. ComboBox Bàn mặc định chọn bàn đầu tiên
  (B0001), nên người dùng chỉ cần bấm thêm món là có nháp trên bàn đang bận.
- Flush kiểm tra muộn: `sp_TaoHoaDon` chặn 52104 khi bàn có hóa đơn
  `Chua thanh toan`/`Cho thanh toan` (`032_sales_pending_payment.sql:235,275`;
  `034_sales_table_sessions.sql:85,135`).
- Hộp xung đột `PosDraftConflictDialog` giữ nháp theo thiết kế ("Nháp vẫn được giữ"),
  nên panel tiếp tục hiện "Đơn nháp" — đúng code, nhưng bắt đầu từ trạng thái nháp
  không nên tồn tại.

## Đề xuất sửa (chờ duyệt)

- **Phương án A (khuyến nghị, sửa nhỏ):** trong `AddProduct`, khi `order is null` và
  kênh "Tại quán", kiểm bàn đang chọn trong `tableRows`; nếu bàn đã có `OrderCode`
  (hoặc không tải được trạng thái) thì **không tạo nháp**, báo thanh trạng thái
  "Bàn X đang có đơn. Chọn bàn trống khác hoặc bấm Chọn bàn để mở đơn hiện có."
  và không đổi màn. Tái dụng đúng dữ liệu bàn đang có, không thêm query mới.
- **Phương án B (thay đổi hành vi):** khi thêm món đầu, nếu bàn đang chọn bận thì tự
  chuyển sang bàn trống đầu tiên theo `TableForNewOrder()` (giống "Đơn mới"), kèm
  thông báo đã chuyển bàn. Mượt hơn nhưng tự đổi bàn có thể ngoài ý người dùng.
- **Không đề xuất** bỏ chặn 52104 hoặc nới `sp_TaoHoaDon`: kiểm tra ở SQL là chốt
  cuối, cần giữ.

Phạm vi ảnh hưởng dự kiến: 1 file UI (`SalesControl.cs`), không đổi SQL/contract.
Kèm kiểm chứng lại: nháp trên bàn bận bị chặn; nháp trên bàn trống vẫn Giữ/Thanh toán
bình thường; đổi bàn trống khi có nháp vẫn chuyển nháp; "Giữ" từ hộp rời tab không
hồi quy. Cân nhắc thêm unit test cho quy tắc chọn bàn của nháp nếu tách hàm thuần.

## Kiểm chứng sau sửa (nếu được duyệt)

- Trên DB test: chọn bàn bận → thêm món → không tạo nháp, có thông báo; bàn trống →
  nháp/Giữ/Thanh toán như cũ; rời tab khi có nháp bàn trống vẫn lưu được và panel sạch.
- Không ghi vào DB sử dụng thật; dùng DB test copy từ dev.
