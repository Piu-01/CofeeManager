# Plan — Khuyến mãi sửa bằng hộp sửa, xóa tại dòng, lọc trạng thái

Thứ tự SQL → DAL → BLL → UI. Mỗi bước nêu file và cách kiểm chứng.

## Bước 1 — Migration 047: lọc trạng thái ở `sp_PromotionList`

- File: `database/migrations/047_promotion_list_status.sql`
  - `ALTER PROCEDURE dbo.sp_PromotionList` thêm `@Status varchar(10)=NULL` cuối tham số.
  - Chuẩn hóa/validate: `NULL` hoặc `''` = tất cả; hợp lệ `DangChay`, `TamDung`,
    `ChuaToiKy`, `HetHan`; khác → `THROW 52001`.
  - ADMIN branch: tính trạng thái hiệu lực hôm nay thành cột `TrangThai` với 4 giá trị
    (`TamDung` giữ nguyên; `DangChay` trong kỳ; `ChuaToiKy` = DangChay, không trong kỳ,
    không sau kỳ; `HetHan` = DangChay và `fn_NgaySauKy`=1) và thêm điều kiện
    `(@Status IS NULL OR computed=@Status)` trước `ORDER BY`.
  - CASH branch giữ nguyên hành vi (chỉ KM đang áp dụng hôm nay; bỏ qua `@Status`).
  - Giữ `GRANT EXECUTE` hiện có (ALTER giữ quyền).
- Kiểm chứng: chạy migration trên DB test đã duyệt; gọi thủ tục với từng `@Status`
  (đối chiếu số dòng với dữ liệu seed), và `NULL` trả đủ.

## Bước 2 — DAL

- File: `src/CafeManager.DataAccess/Repositories/LoyaltyRepository.cs`
  - `PromotionsAsync(..., DateOnly? onDate, string? status, CancellationToken t)` thêm
    `P("@Status",SqlDbType.VarChar,status,10)`.
- Kiểm chứng: build DataAccess; gọi qua UI ở bước 4.

## Bước 3 — BLL

- File: `src/CafeManager.Business/Services/LoyaltyService.cs`
  - `PromotionsAsync(..., DateOnly? onDate=null, string? status=null, ...)`: chuẩn hóa
    chuỗi rỗng thành `null`, kiểm giá trị thuộc bộ 4 mã, sai → `ArgumentException`
    tiếng Việt.
- Kiểm chứng: build; thử giá trị sai từ test (nếu cần) trả lỗi rõ.

## Bước 4 — UI `PromotionList`

- File: `src/CafeManager.WinForms/Modules/Loyalty/LoyaltyControl.cs`
  - Bỏ `AddAction("Sửa")`, `AddAction("Xóa")`; giữ `Thêm` (ADMIN).
  - `AddRowDeleteColumn()`; `inline=CreateInlineEditor()`; `RowKey`, `CanWrite`=admin,
    `Edit=item=>Edit((PromotionRow)item)`, `Delete=DeleteInline`, `Reconcile`, `Reload`.
  - Đổi `Edit(bool create)` → `Edit(PromotionRow? row)`; `Thêm` gọi `Edit(null)`.
    `Delete()` → `DeleteInline(row)` dùng nguyên hộp xác nhận hiện có.
  - Combobox lọc (ADMIN): `Tất cả` (null), `Áp dụng`=`DangChay`, `Tạm ngưng`=`TamDung`,
    `Chưa tới kỳ`=`ChuaToiKy`, `Hết hạn`=`HetHan`; `AddFilter(combo, 170)`; đổi chọn →
    `ResetAndReload()`. Query trong `Configure` gửi trạng thái đang chọn.
  - Cột Trạng thái: bỏ suy diễn theo ngày ở client, map trực tiếp 4 mã → nhãn/màu
    (Áp dụng Caramel, Tạm ngưng Muted, Chưa tới kỳ Muted, Hết hạn đỏ nhạt) để khớp lọc.
- Kiểm chứng: build Debug/Release 0 lỗi; UI: double-click/F2/xóa/lọc từng giá trị +
  tìm kiếm kết hợp; CASH không thấy bộ lọc.

## Bước 5 — Tài liệu

- `docs/ui/README.md`, `docs/ui/HuongDan_UI_Orbit_Cafe.md` (11.5), `docs/UX_QuanLiQuanCafe.md`
  (10.1): Khuyến mãi đã chuyển sang hộp sửa; chỉ Chấm công chưa chuyển. Ghi bộ lọc
  trạng thái cho danh sách khuyến mãi.
- Kiểm chứng: đọc lại, không đổi nội dung quy tắc khác.

## Bước 6 — Kiểm thử (đợt riêng)

- Sau triển khai, chờ người dùng duyệt đợt test: migration trên DB test, lọc 4 trạng
  thái + Tất cả + kết hợp tìm kiếm, sửa/xóa tại dòng, quyền CASH, build. Không sửa
  source trong lượt test.

## Phát sinh đã duyệt — sửa BUG-02 (25/11/2026)

- File mới: `database/migrations/048_promotion_legacy_code_edit.sql` — `ALTER
  PROCEDURE dbo.sp_PromotionWrite` chỉ siết định dạng `KM+8 số` khi `@Action='Create'`
  (như 009); `Update`/`Delete`/`Pause`/`Resume` giữ mã cũ. Không đổi DAL/BLL/UI.
- Áp dụng trên DB test và `CoffeeManager_Development` bằng
  `tools/database/Apply-Migration.ps1 -Version 48`.
- Kiểm chứng: SQL V01–V06 và UI sửa/trả trạng thái KM24NEL (P-23…P-28 trong
  `test-results.md`).
- Bổ sung UI (yêu cầu người dùng 25/11/2026): `Workspace.ShowMeta=false` cho danh sách
  Khuyến mãi để bỏ dải trống 49px mà hai nút Sửa/Xóa cũ để lại; giống các màn master
  data đã chuyển (`TablesControl`, `MaterialsControl`). Build Debug 0 lỗi.

## Đề xuất ngoài phạm vi

- Thêm cột "Đang áp dụng" cho CASH nếu sau này CASH cần xem lịch sử KM; chưa làm.
- Tách thông điệp lỗi 52001 thành câu cụ thể hơn theo từng điều kiện SQL; chưa làm.
- Đổi nhãn nút chốt của `OperationEditorDialog` khi xóa từ "Lưu" thành "Xóa" (áp chung
  mọi màn master data); chưa làm, cần quyết định UI chung.
