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
