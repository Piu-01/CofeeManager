# Bug report — promotion-dieu-kien-tu-ap

- Ngày phát hiện: 21/11/2026, cập nhật 22/11/2026
- Người phát hiện: agent (áp 040, sau đó lượt test 039 được duyệt)
- Trạng thái: **B01–B05 đã sửa và PASS lại trong lượt test 22/11/2026**
  (`runs/20261122_135707_532a959f`, xem `test-results.md`)

## B01 — Migration 039 không chạy được: batch DDL trộn ADD cột và CHECK

**Hiện tượng:** `Apply-Migration.ps1 -Version 39` báo `Invalid column name 'TrangThai'`
(3 lần), `'DoiTuongKhach'` (4), `'PhamVi'` (2); rollback, DB vẫn 38.

**Nguyên nhân/bằng chứng:** batch "1. DDL" vừa `ALTER TABLE dbo.KHUYENMAI ADD` ba cột
vừa tạo CHECK constraint tham chiếu chính các cột đó trong cùng batch; SQL Server
biên dịch cả batch theo schema cũ. Probe lại từng batch trong transaction xác nhận
batch 2 FAIL, batch sau không chạy.

**Sửa:** tách mỗi `ALTER TABLE` (ADD cột, ADD CHECK, DROP constraint, ALTER NgayKT)
thành batch `GO` riêng; tương tự cho phần `HoaDonUuDaiSnapshot` (ADD cột → DROP CK →
ADD CK mới). Không đổi thiết kế, không sửa checksum 001–038.

## B02 — `Percent` là từ khóa: sai cú pháp/batch khi tạo object

**Hiện tượng:** `Incorrect syntax near the keyword 'Percent'` ở các batch tạo bảng,
hàm, procedure; lỗi cả khi đã có dấu chấm (`p.Percent`).

**Sửa:** bọc `[Percent]` cho mọi chỗ dùng làm định danh (khai báo cột/bảng, chọn,
chèn, biểu thức) trong 039 và các script test.

## B03 — Gọi `sys_getapplock` sai tên

**Hiện tượng:** `Could not find stored procedure 'sys_getapplock'` khi preview.

**Sửa:** gọi `sys.sp_getapplock`.

## B04 — `sp_PromotionGet` đọc nhầm cột hạng

**Hiện tượng:** `Invalid column name 'DiemToiThieu'`, `'PhanTramGiam'` (các cột này
thuộc `HANGTHANHVIEN`, không thuộc `KHUYENMAI_HANG`).

**Sửa:** lấy từ alias `h` (HANGTHANHVIEN) thay vì alias bảng nối.

## B05 — Các lỗi cú pháp/định danh trong 039 và bộ test

- `sp_DiscountPreview`: set kết quả thứ 2 chọn `MaKM`/`Name` mơ hồ/không tồn tại khi
  join `KHUYENMAI` → đổi sang `c.MaKM`, `k.TenKM`, `c.[Percent]`, `ORDER BY c.MaKM`.
- `sp_ThanhToanHoaDon`: thừa một dấu `)` ở guard PT010 → `Incorrect syntax near ')'`.
- Test 02/03: `EXEC ... @RequestId=NEWID()` không hợp lệ → khai báo biến `uniqueidentifier`.
- Fixture: KM legacy đặt `TamDung` để không tự áp cho đơn mới (vẫn áp qua nhánh legacy);
  KM tương lai đời ngày +60/+90 để không lọt vào biên +30 ngày; baseline stock ép
  `decimal(38,4)` (UNION với `decimal(38,0)` từng làm tròn 96,74 → 97); mốc "hóa đơn
  lịch sử" đổi từ `MaHD NOT LIKE 'HD9%'` sang `Ngày thanh toán < hôm nay` vì mã đơn
  thật không có tiền tố HD9; trigger snapshot kiểm bằng cột `RecordedAt` (đổi
  `PercentDiscount` vi phạm CK trước trigger).
- Runner: truyền `binary(8)` bằng `SqlDbType.Binary` và ép `[byte[]]` cho revision
  (PowerShell trả `Object[]`).

**Phạm vi ảnh hưởng:** chỉ 039 (chưa từng áp thành công ở DB nào trước 22/11) và bộ
test mới. Không sửa 001–038, không đổi dữ liệu dev ngoài thêm object 039.
