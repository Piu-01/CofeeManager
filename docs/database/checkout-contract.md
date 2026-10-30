# Checkout — migration011 / T15

sp_SalesCheckoutPreview nhận actor/stamp/MaHD/version; trả số chính thức và token
binary32 từ version đơn/chính sách/khách/KM/đối tác, số tiền/giảm. sp_ThanhToanHoaDon
chỉ nhận actor/stamp/code/version/request/token và TVP3cột, không nhận ngày/giảm.
Chữ ký baseline cũ không còn là API runtime sau011. Seed baseline chỉ chạy trên
schema rỗng trước001–011, không reseed hoặc gọi chữ ký cũ sau nâng cấp.

Khóa Policy→Recipes→Order→header→khách/danh mục→kho theo MaNVL. SQL tính lại ưu đãi,
so token, kiểm support PT, tồn và công thức, group tiêu hao, ghi snapshot/tiền/chốt/
điểm/receipt trong cùng transaction. Trigger trừ kho/cộng điểm đúng một lần.
TVP rỗng hợp lệ cho bill0₫; không payment0. PT010 bị chặn ở SQL và BLL.

HoaDonUuDaiSnapshot bất biến, KindCheckout cho giao dịch mới; LegacyUnknown cho đơn
cũ với nguồn/tỷ lệNULL, không dựng lại lịch sử. Phí đối tác giữ snapshot header.
Tiền đưa/thối chỉ trong UI phiên hiện tại, không phải số thu và không được bịa khi in lại.

Timeout sau gửi phải đối chiếu request/MaHD, không retry checkout tự động. Lỗi522xx
được dịch an toàn;52103 xung đột,52201 yêu cầu preview mới. Chưa nghiệm thu G04.

Migration016 (G04-CONC-01): TR_CTTT_KiemTra duyệt tập MaHD affected theo thứ tự,
khóa từng PK_HOADON bằng seek trước kiểm trạng thái. Không range scan các đơn lân
cận khi checkout đã giữ khóa kho. Giữ lỗi51002 cho đơn đóng,51003 cho phân bổ vượt,
hỗ trợ batch nhiều đơn và rollback cả batch. Không đổi chữ ký checkout/TVP, không
retry tự động hoặc cập nhật lại kho/điểm. Schema015 cũ vẫn có nguy cơ deadlock đã
ghi nhận; DB chạy dùng016 sau backup/migration, app không tự nâng schema.
