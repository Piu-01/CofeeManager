# Lỗi — DB Development chưa migrate 032–035 làm app F5 văng IndexOutOfRange

Ngày ghi nhận: 18/11/2026. Trạng thái: **đã sửa (phương án A), đã kiểm chứng smoke**.
Loại: lỗi môi trường dev (schema drift), không phải lỗi nghiệp vụ SQL hay logic BLL.

## Hiện tượng

Chạy app bằng profile F5 trong Visual Studio (DB `CoffeeManager_Development`), mở
màn hình cần đọc danh sách bàn (Bán hàng / Bàn – Khu vực) thì Visual Studio bắt
`System.IndexOutOfRangeException: Index was outside the bounds of the array.` tại
`src/CafeManager.DataAccess/Repositories/TableRepository.cs:21` — hàm `Map(SqlDataReader)`.

## Tái hiện (đã xác nhận cấu trúc, không sửa gì)

1. `Environment` F5 dùng `CAFEMANAGER_USE_DEVELOPMENT=1` → `DevelopmentConnection.Read()`
   trả connection tới `localhost / CoffeeManager_Development` (`DevelopmentConnection.cs:9`).
2. `TableRepository.Map` đọc **10 cột** (`r[0]..r[9]`), gồm `MaPhien`, `SessionRevision`,
   `Retained` do T34 thêm ở `sp_TableList` / `sp_TableSelectCurrent`
   (`database/migrations/034_sales_table_sessions.sql:49,59`).
3. DB Development thực tế đang ở **schema 31**: `MAX(Version)=31` trong `SchemaMigrations`
   và `OBJECT_DEFINITION(sp_TableList)` **không** chứa `SessionCode` (định dạng cũ 7 cột,
   `database/migrations/006_tables_areas.sql:27,36`).
4. Vì vậy reader chỉ có 7 cột; `Map` chạm `r.IsDBNull(7)` → `IndexOutOfRangeException`.

## Nguyên nhân

Code trong repo đã gồm T33–T35 (schema 032→035) nhưng DB Development chưa áp các
migration này. Repo chủ trương **không tự migrate DB development** khi chạy app
(AGENTS.md mục 4/6; `Program.cs` chỉ đọc connection, không chạy migration), nên
DB cũ và DAL mới lệch nhau. Đây là drift môi trường, tái hiện được 100% trên máy hiện tại.

Phạm vi ảnh hưởng dự kiến nếu để nguyên: mọi màn dùng contract mới của T33–T35
(danh sách/đọc bàn, chi tiết hóa đơn 27 cột, yêu cầu thanh toán, phiên bàn, trả bàn)
đều lỗi khi chạy F5 trên DB 31; không liên quan app kết nối DB đã migrate (test clone
đã chạy PASS ở lượt 18/11/2026 — xem `docs/tasks/g04-ban-hang-vong-doi-hoa-don/report.md`).

## Đề xuất sửa (đã duyệt 18/11/2026)

- **Phương án A (khuyến nghị):** chạy `tools/database/Apply-Migration.ps1` (hoặc runner
  hiện có) áp **032 → 035** cho `CoffeeManager_Development`, theo đúng thứ tự, rồi mở
  lại app F5. Đây là thay đổi DB dev (thêm bảng `PHIEN_BAN`, cột `HOADON.MaPhien`,
  view/proc mới, dữ liệu điểm danh mục 033) nên cần người dùng duyệt trước khi chạy.
- **Phương án B (không đổi DB dev):** trỏ app F5 sang một DB đã migrate bằng
  `CAFEMANAGER_APP_CONNECTION`, ví dụ clone `CoffeeManager_Test_G04_*` như lượt test
  UI 18/11/2026. Nhanh nhưng mỗi lần F5 phải giữ DB clone.
- **Không đề xuất** nới lỏng `Map` để chịu thiếu cột: sẽ che drift và trả DTO sai
  (`SessionCode`/`Retained` mặc định), vi phạm contract T34.

## Đã sửa — 18/11/2026 (phương án A)

Người dùng duyệt "duyệt" cho phương án A. Agent chạy `tools/database/Apply-Migration.ps1`
cho `CoffeeManager_Development`, apply tuần tự 032 → 033 → 034 → 035; cả 4 báo
**Applied**, checksum được ghi vào `SchemaMigrations`:

| Version | File | Checksum (đầu…cuối) |
|---|---|---|
| 032 | 032_sales_pending_payment.sql | `5F08…5FB3` |
| 033 | 033_category_diacritics.sql | `8B7D…E25A` |
| 034 | 034_sales_table_sessions.sql | `19F0…C6A3` |
| 035 | 035_sales_invoice_detail_sessions.sql | `0A0A…0DFC` |

Không sửa source sản phẩm, không sửa migration đã áp. Bằng chứng kiểm chứng:

- `MAX(Version)=35` trong `SchemaMigrations`; `PHIEN_BAN` tồn tại.
- `OBJECT_DEFINITION(sp_TableList)` và `sp_TableSelectCurrent` đã chứa `SessionCode`;
  `sys.dm_exec_describe_first_result_set_for_object` trả 10 cột:
  `Code, Name, Area, Status, OrderCode, Revision, ReadAt, SessionCode,
  SessionRevision, Retained`.
- Smoke trên DB Development đã migrate (app do agent khởi chạy, PID 22832, tài khoản
  admin): mở màn **Bàn / Khu vực** — màn hình hiển thị "Bàn & khu vực",
  "Da cập nhật 22:12:06", "10 bàn"; tiến trình còn sống, **không còn**
  `IndexOutOfRangeException`. Bằng chứng: `runs/ui_20261118_174233_3319aa73/dev-after-migrate.txt`.

Còn lại (không thuộc lỗi này): các màn T33–T35 khác trên DB Development (chi tiết hóa
đơn, yêu cầu thanh toán, trả bàn) chỉ được smoke gián tiếp qua lượt test UI 18/11/2026
trên DB clone đã migrate — người dùng tự kiểm thêm khi chạy F5 thực tế.
