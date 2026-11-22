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

Từ migration038 (cộng dồn ưu đãi): fn_CheckoutPreview trả thêm TierPercent/
PromotionPercent, token gồm cả hai tỷ lệ; snapshot Checkout ghi thêm hai cột này và
CHECK buộc PercentDiscount = min(100, hạng+KM). Dòng snapshot Checkout cũ không có
breakdown (NULL) và không backfill; đọc/in vẫn theo PercentDiscount cũ. Preview cũ
mất hiệu lực sau nâng cấp nên đơn đang mở phải xác nhận lại số mới.

Từ migration032 (T33), preview/checkout/hủy dùng sp_SalesCheckPayable nên thu được
đơn Chua thanh toan hoặc Cho thanh toan; đơn Cho vẫn khóa sửa dòng/header qua
sp_SalesCheckOpen. Tiền/kho/điểm/snapshot tính như đơn Chua; trigger thanh toán và
tiêu hao chấp nhận trạng thái Cho trong transaction. Sau commit header mới thành Da
thanh toan và NgayThanhToan được ghi như cũ.

Từ migration039 (ưu đãi tự áp), `fn_CheckoutPreview` trả thêm TierDiscount/
PromotionDiscount/PromotionCount và danh sách `AppliedPromotion`; token gồm digest
danh mục KM + digest ưu đãi đang áp nên sửa bảng con/bật/tạm dừng KM làm preview cũ
trả 52201. `sp_ThanhToanHoaDon` ghi một dòng `HoaDonUuDaiSnapshot` (breakdown, digest)
và N dòng `HoaDonUuDaiApDung`; trigger buộc tổng Discount bằng
`Snapshot.PromotionDiscount`, cấm UPDATE/DELETE. Ship không giảm; điểm tính trên
`TongTien − GiamGia`. Đơn legacy còn `MaKM` được tính như KM phạm vi `ALL`.

Từ migration034 (T34), checkout **không** đóng phiên bàn và không làm bàn trống;
phiên chỉ đóng bằng sp_TraBan khi không còn đơn Chua/Cho. UI hỏi “Trả bàn ngay?”
sau khi đã commit tiền; lỗi trả bàn không rollback thanh toán (đối chiếu rồi bấm
lại). In lại hóa đơn đã trả trong phiên đã đóng vẫn đọc theo MaHD, không phụ thuộc
phiên.

Migration016 (G04-CONC-01): TR_CTTT_KiemTra duyệt tập MaHD affected theo thứ tự,
khóa từng PK_HOADON bằng seek trước kiểm trạng thái. Không range scan các đơn lân
cận khi checkout đã giữ khóa kho. Giữ lỗi51002 cho đơn đóng,51003 cho phân bổ vượt,
hỗ trợ batch nhiều đơn và rollback cả batch. Không đổi chữ ký checkout/TVP, không
retry tự động hoặc cập nhật lại kho/điểm. Schema015 cũ vẫn có nguy cơ deadlock đã
ghi nhận; DB chạy dùng016 sau backup/migration, app không tự nâng schema.
