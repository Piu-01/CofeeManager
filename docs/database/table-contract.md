# Contract bàn/khu vực — migration006 / T11

ADMIN CRUD; ADMIN/CASH đọc/chọn bàn; STORE bị chặn ở service và SQL. Mã B0001–9999,
KV001–999, tên required≤50; FK/unique khu vực giữ baseline. Metadata có Revision;
ghi RequestId/ExpectedRevision, receipt cùng transaction. Xóa có FK không cascade.

sp_TableList/Get/SelectCurrent và sp_AreaList/Get, Create/Update/Delete từng loại.
List escaped search/paging50, table có area filter. Table DTO Code/Name/Area/Status/
OrderCode/Revision/ReadAt. Trạng thái và MaHD từ vw_TrangThaiBan trong cùng statement;
không có BAN.TrangThai. Chọn lại đọc view, không tin màu cũ, không tạo/hủy đơn.
Màn G03 hiển thị mã đơn hiện có; mở POS tích hợp T14/T16.

TableStatusService cũ được yêu cầu SessionContext+PermissionService, call site
StartupForm cập nhật. Fixture T05 chỉ có migration001 tiếp tục kiểm DAL trực tiếp;
không giả session/role chưa tồn tại ở DB T05.

SQL technical clean/demo/createarea→createtable→selectcurrent:
G03/runs/technical_20261018_193303_14bb0710; tạo2/dọn2/giữ0DB. Build thành công.
Không thay nghiệm thu seed/paid/cancel/role/GUI/Designer.

## Phiên bàn và trả bàn (16/11/2026, T34/migration034)

`PHIEN_BAN` do SQL sở hữu (`DENY INSERT,UPDATE,DELETE` cho CafeManagerApp): mỗi
dòng là một lần khách ngồi bàn, `GioVao` mặc định `SYSDATETIME()`, `GioRa` NULL khi
còn mở, `MaNVMo`/`MaNVTra` là `char(5)` tham chiếu `NHANVIEN`, `Revision`
rowversion. Unique filtered index `UX_PHIEN_BAN_Mo(MaBan) WHERE GioRa IS NULL` bảo
đảm tối đa một phiên mở mỗi bàn; hai connection cùng tạo đơn trên một bàn sẽ khóa
dòng `BAN` trước nên chỉ một phiên và một đơn thắng, bên còn lại nhận 52104.

`HOADON.MaPhien int NULL` (FK `FK_HD_Phien`, index `IX_HD_Phien`) gắn hóa đơn với
phiên; không backfill đơn cũ. Đơn `Mang di`/`Giao hang` để NULL. `sp_TaoHoaDon` và
`sp_TaoHoaDonTuNhap` tự mở phiên trong cùng transaction tạo đơn khi bàn chưa có
phiên; bàn đã có phiên nhưng hết đơn mở thì đơn mới nối phiên hiện tại (hóa đơn
riêng, không gộp). Nháp chưa Giữ không tạo phiên.

`vw_TrangThaiBan` thêm `MaPhien`, `SessionRevision`, `Retained` (bit) sau các cột
cũ. `TrangThai`: không đơn mở và không phiên → `Trong`; có đơn `Cho` (không còn
`Chua`) → `Cho thanh toan`; có đơn `Chua`/`Cho` hoặc phiên mở → `Dang phuc vu`;
phiên mở nhưng mọi hóa đơn đã trả/hủy → `Dang phuc vu` + `Retained=1`. Đơn cũ
`MaPhien IS NULL` vẫn làm bàn bận như trước. `MaHD` là đơn mở (NULL khi chỉ còn
phiên). `sp_TableList`/`sp_TableSelectCurrent` trả thêm 3 cột nên DTO
`TableRow` có `SessionCode`, `SessionRevision`, `Retained`.

`sp_TraBan @Actor,@Stamp,@MaPhien,@ExpectedRevision,@RequestId` là đường duy nhất
đóng phiên: transaction riêng, `sp_G03Demand` ADMIN/CASH, receipt operation
`TableClose` với `EntityCode=MaPhien`; phiên đã đóng/nonexistent trả 52111, revision
lệch 52103, còn đơn `Chua`/`Cho` gắn phiên trả 52110. Khi thành công ghi
`GioRa=SYSDATETIME(), MaNVTra` theo actor; gọi lặp cùng RequestId không đóng lần
hai. UI không tự đóng theo giờ và không đóng khi thu/hủy; sau thu/hủy chỉ hỏi
“Trả bàn ngay?”. Acceptance SQL: `database/tests/g04/acceptance-table-sessions.sql`;
milestone kỹ thuật v17 trong `Invoke-G04Technical.ps1` (chỉ chạy trong đợt test được
duyệt).
