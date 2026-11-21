# Bug report — promotion-dieu-kien-tu-ap

- Ngày phát hiện: 21/11/2026
- Người phát hiện: agent, khi áp migration 040 của task `auto-generated-codes`
- Trạng thái: **Chưa sửa, chờ người dùng duyệt** (không thuộc phạm vi task đang làm)

## B01 — Migration 039 không chạy được trên DB dev

**Hiện tượng:** `Apply-Migration.ps1 -Database CoffeeManager_Development -Version 39`
báo `Invalid column name 'TrangThai'` (3 lần), `'DoiTuongKhach'` (4 lần), `'PhamVi'`
(2 lần); transaction rollback, `SchemaMigrations` vẫn ở 38, bảng `KHUYENMAI` không có
cột mới.

**Nguyên nhân/bằng chứng:** batch 1 (DDL, dòng 17–25) vừa `ALTER TABLE dbo.KHUYENMAI
ADD TrangThai, DoiTuongKhach, PhamVi` vừa tạo các CHECK constraint tham chiếu chính
các cột đó trong **cùng một batch**. Bảng đã tồn tại nên SQL Server biên dịch batch
theo schema cũ và báo lỗi tên cột trước khi batch chạy. Đã xác nhận bằng probe chạy
lại từng batch trong transaction rồi rollback: batch 1 (mục "1. DDL") FAIL, các batch
sau không chạy. Không phải lỗi quyền hay DB thiếu object.

**Đề xuất sửa (chưa làm):** tách bằng `GO` thành `ALTER TABLE ... ADD` (cột + DF) →
`GO` → các `ALTER TABLE ... ADD CONSTRAINT CK_*` (và phần DROP/ALTER NgayKT) ở batch
kế tiếp; chỉ sửa 039 nếu migration đó chưa từng được áp thành công ở DB nào.

**Phạm vi ảnh hưởng:** DB dev chưa có 039; migration 040 đã áp trên schema 38 (không
phụ thuộc 039). Các test/tính năng dùng `KHUYENMAI.TrangThai/DoiTuongKhach/PhamVi`
sẽ lỗi cho tới khi 039 được sửa và áp.

**Rủi ro:** 040 hiện đứng trước 039 trong `SchemaMigrations` của DB dev; runner không
bắt thứ tự nên chấp nhận được, nhưng cần áp 039 (bản đã sửa) trước khi nghiệm thu
tính năng ưu đãi.
