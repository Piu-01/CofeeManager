# Đơn mở — migration010 / T14

Đơn tạo bằng sp_TaoHoaDon cùng món đầu trong một transaction SQL. Mã lấy sp_CapMa,
actor/stamp từ session, MaNV lấy DB. Khóa loại/bàn sau tạo; sửa khách/KM và thông
tin giao khi còn Chờ giao qua sp_SuaThongTinHoaDon. Giá/giảm/tổng không nhận từ UI.

sp_SuaDongHoaDon Add/Update/Delete theo MaHD+MaDong và ExpectedRevision binary8.
Add lấy giá DB, Update giữ snapshot; mỗi lần Add tạo dòng riêng. Hạ giảm về0 trước
đổi dòng, trigger tổng rồi fn_DiscountPreview. Dòng cuối xóa giữ đơn mở0.

Mọi ghi có RequestId/receipt G03OperationReceipt, hash payload+revision và actor.
sp_SalesReconcile lấy request fence trước đọc receipt; receipt vắng chưa đủ để kết
luận rollback nếu lời gọi cũ chưa kết thúc. Không tự retry thu tiền. SQL dùng
Policy→Recipes→Order→header, khóa transaction-owned; runtime DENY DML header/dòng.

SalesOrderGet trả3result set: header/preview, dòng snapshot và payment đã lưu.
Chỉ phần preview đơn mở dùng chính sách hiện tại; paid giữ tiền đã lưu, không suy
tỷ lệ lịch sử. DTO ReadAt và Revision đi kèm. Lỗi52101–52107 map tiếng Việt.

Triển khai chưa là nghiệm thu: xem plan/checklist T14 trong G04.
