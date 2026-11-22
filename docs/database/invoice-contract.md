# Tra cứu/in — migration013 / T17

sp_InvoiceList có DateBasis Created/Paid, hai ngày bao gồm ngày cuối, trạng thái,
kênh, khách, tìm mã/tên/SDT và Skip50. SQL dùng khoảng ngày nửa mở, sắp ngày+MaHD;
COUNT OVER trên tập hóa đơn, không join món×payment để cộng tiền.

sp_InvoiceDetail đọc một transaction ngắn: header stored, dòng snapshot, payment,
metadata snapshot/điểm. Không tính ưu đãi hiện tại cho đơn đã trả. Từ migration038,
metadata có thêm tỷ lệ hạng/tỷ lệ KM để in breakdown; đơn cũ (hoặc LegacyUnknown)
thiếu cột này trả NULL và in như trước. Tên sản phẩm là
nhãn danh mục hiện tại, không giả snapshot tên. LegacyUnknown không có nguồn/tỷ lệ.
ADMIN/CASH được đọc/in, STORE bị chặn Business và SQL.

PrintDocument nhận DTO sau đọc, connection đã đóng. Preview/in lại không tham
chiếu checkout, in sau commit; không tiền đưa/thối bịa lại, không lộ phí đối tác.
State phân trang reset BeginPrint, giữ cursor qua PrintPage; khổ quá nhỏ báo lỗi
thay vì cắt dòng. Chưa có máy in thì chi tiết vẫn đọc; không tự cài/đổi printer.

Chưa nghiệm thu ca in hoặc hình thức; hình thức do người dùng kiểm theo phân công.

Từ migration039, `sp_InvoiceDetail` trả header 32 cột (thứ tự
…, ReadAt, MaPhien, SessionRevision, PromotionCount, TierPercent,
PromotionPercent, TierDiscount, PromotionDiscount) và **5 result set**: header,
dòng snapshot, payment, danh sách ưu đãi `AppliedPromotion` đọc từ
`HoaDonUuDaiApDung` (set 4, giống `sp_SalesReadCore`), rồi metadata snapshot
(Kind/Source/tỷ lệ/giảm/điểm/RecordedAt). Đơn cũ/LegacyUnknown không có dòng con
thì danh sách rỗng; metadata thiếu (NULL) như trước và UI vẫn in theo
`PercentDiscount`; in liệt kê từng KM (tên, %, tiền) khi có.

Từ T33/T34, bộ lọc trạng thái hóa đơn có đủ Chua thanh toan / Cho thanh toan /
Da thanh toan / Huy (lọc qua tham số sp_InvoiceList, không hard-code); danh sách
đơn giao cũng có mục “Chờ thanh toán”. Hóa đơn đã trả trong một phiên bàn đã đóng
vẫn tra cứu/in lại bình thường; chi tiết/in không đọc PHIEN_BAN và không đổi số
snapshot. “Quay lại POS” chỉ bật cho đơn Chua/Cho; in hóa đơn chính chỉ cho Da.
