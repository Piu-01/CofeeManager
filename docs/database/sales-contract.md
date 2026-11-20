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

## Nháp đơn mới (11/11/2026, G04-POS-02)

Chỉ đơn **chưa có trong SQL**: UI giữ header + dòng món trong bộ nhớ, không cấp mã.
Giữ/Thanh toán gọi sp_TaoHoaDonTuNhap với một RequestId SalesCreate duy nhất và TVP
dbo.SalesDraftLines (RowNo PK, Product, Quantity, Note, Price). Procedure tự mở
transaction, tái dùng receipt/reconcile của SalesCreate, khóa bàn và đơn, kiểm
header bằng sp_SalesValidateHeader. Giá mỗi dòng phải bằng SANPHAM.GiaBan tại thời
điểm lưu; lệch trả 52105 kèm thông báo tải lại menu. Bàn đã có đơn trả 52104 và
không ghi một phần. Toàn bộ dòng chèn theo RowNo nên giữ thứ tự nháp; tổng do
trigger/sp_SalesRefreshDiscount quyết định, UI không gửi tổng.

Không có API lưu header riêng cho nháp và không đổi LoaiDon sau khi tạo (contract
của sp_SuaThongTinHoaDon không sửa kênh). Đơn đã lưu tiếp tục autosave từng dòng;
nháp trong bộ nhớ không được coi là dữ liệu DB khi in/xem lịch sử.

## Chờ thanh toán và mở lại (16/11/2026, T33/migration032)

HOADON.TrangThai có 4 giá trị: Chua thanh toan (sửa được), Cho thanh toan (đã chốt
món, còn thu/hủy), Da thanh toan, Huy. Chuỗi DB giữ không dấu; UI dịch “Chờ thanh
toán”. Chuyển Chua→Cho qua sp_YeuCauThanhToan (receipt SalesRequestPay, 52108 nếu
đơn 0 dòng/0 ₫), Cho→Chua qua sp_MoLaiHoaDon (chỉ ADMIN, receipt SalesReopen, 52109
nếu không phải Cho); Cho→Da/Huy dùng checkout/hủy hiện có. Ghi trực tiếp HOADON vẫn
bị DENY, nên chỉ procedure đổi trạng thái.

Hai cổng tách biệt: sp_SalesCheckOpen chỉ nhận Chua (sửa dòng/header, 52102 nếu
khác); sp_SalesCheckPayable nhận Chua hoặc Cho (preview/checkout/hủy, cùng kiểm
revision 52103 và chưa có thanh toán/tiêu hao). TR_CTHD_TongTien đã chặn dòng món
khi khác Chua nên không sửa; TR_CTTT_KiemTra, TR_DiscountSnapshot, TR_TieuHao_Kho
chấp nhận cả Cho trong transaction checkout. Không khóa GiamGia bằng trigger vì
sp_ThanhToanHoaDon cập nhật giảm giá khi header còn Cho trước khi chuyển Da; khóa
sửa dựa vào procedure gate + DENY.

UX_HD_BanMo và vw_TrangThaiBan tính cả Chua lẫn Cho (bàn đang chờ vẫn bận và hiện
“Cho thanh toan”). sp_SalesReadCore/SalesOpenOrders/SalesOrderGet trả đơn Cho với
payable preview; dashboard OpenOrders, orders.open và nguồn analytics orders đếm cả
hai trạng thái, còn doanh thu vẫn chỉ từ Da thanh toan. Lỗi mới 52108/52109; stale
52103, thiếu quyền 51605. Acceptance SQL của T33 nằm tại
database/tests/g04/acceptance-pending-payment.sql, chỉ chạy trong đợt test được duyệt.

## Phiên bàn (16/11/2026, T34/migration034)

Đơn tại bàn được gắn `HOADON.MaPhien` khi tạo (`sp_TaoHoaDon`/`sp_TaoHoaDonTuNhap`
tự mở phiên nếu bàn chưa có phiên mở). `sp_SalesReadCore` trả thêm `MaPhien` và
`SessionRevision` cuối header (27 cột), nên `SalesOrderHeader` có `SessionCode`,
`SessionRevision`. Đơn mang đi/giao hàng có `MaPhien NULL`, không tạo phiên.

Thu tiền hoặc hủy đơn **không** đóng phiên và không làm bàn trống: sau khi mọi hóa
đơn của phiên đã trả/hủy, bàn vẫn `Dang phuc vu` với `Retained=1` (UI “Đã trả · giữ
bàn”). Đơn mới trên bàn đó nối phiên hiện tại và tạo hóa đơn mới, không gộp tiền,
không backfill đơn. Đóng phiên bằng `sp_TraBan` (điểm 2/3 trong
`docs/database/table-contract.md`), lỗi 52110/52111/52103; receipt `TableClose`.
Không sửa trigger/checksum của 001–030 ngoài phạm vi 034. Acceptance SQL:
`database/tests/g04/acceptance-table-sessions.sql`; hai connection chạy ở đợt
concurrency riêng, ca UI thuộc đợt kiểm thủ công.

