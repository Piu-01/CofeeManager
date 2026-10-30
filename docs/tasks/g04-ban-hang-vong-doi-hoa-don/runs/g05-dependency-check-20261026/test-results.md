# Ưu tiên G04 để mở đường G05 — 26/10/2026

Người dùng yêu cầu kiểm trước các ca BLOCKED mà G05 cần. Theo kế hoạch,
G05/T19 phụ thuộc T18; chưa tuyên bố toàn G04 PASS hoặc bỏ phụ thuộc.

Ưu tiên: T18-K01/K04/K06/K07/K08 (còn thiếu GUI/quyền); kế thừa bằng chứng SQL
T18-K02/K03/K05 đã PASS. Sau đó T16-K08/K12 và nhánh giao hàng T16-K01,
đối chiếu tiền/kho sau hủy và giao; không ưu tiên ca in lại lúc này.

Source cố định, không sửa sản phẩm. DB/marker/schema theo manifest, dùng lại DB
hiện có. Chuẩn bị hai đơn giao mở qua AuthService/SalesService; không thanh toán.
Chuẩn bị fixture không đồng nghĩa nghiệm thu PASS. Hình thức SKIPPED theo phân công.

Đang chờ người dùng kiểm HD20250403: detail người nhận, khóa bước tiếp,
hướng dẫn thất bại paid và tìm/refresh giữ đúng Đã giao / Đã thanh toán.

## Ca HD20250403 — đã kiểm

Người dùng ban đầu mở473 (Chờ giao/Chưa thanh toán); đã hướng dẫn lại đúng403.
Sau đó người dùng xác nhận đúng ca403: Đã giao/Đã thanh toán, bước tiếp khóa,
có hướng dẫn giao thất bại paid liên hệ quản lý, không tự hoàn tiền.
SQL after-403.json so với before-manual.json: Orders/Payments/Consumption/Stock/
Points không đổi (verdict-403.json). T18-K08 PASS; K01/K07 chỉ bổ sung bằng chứng
nhánh này, chưa thay bằng nghiệm thu toàn ca. Chưa chuyển trạng thái472/hủy473.

## Ca HD20250472 — chuyển giao chưa trả

Người dùng xác nhận Chờ giao→Đang giao→Đã giao qua nút thật và xác nhận,
tải lại thấy Đã giao/Chưa thanh toán. SQL after-472.json: TrangThaiGiao=Da giao,
TrangThai=Chua thanh toan, ThanhTien81000, NgayThanhToan NULL. Toàn bộ payment,
tiêu hao, tồn kho và điểm khách hàng bằng baseline; verdict-472.json PASS.
Bổ sung nhánh native cho transition T18-K02 đã PASS, không cộng thêm một ID.
T18-K07 còn cần lọc SDT/trạng thái; chưa đánh PASS toàn ca từ chỉ tìm mã.

## Ca HD20250473 — hủy đơn giao mở

Người dùng xác nhận trạng thái hủy đúng. SQL after-473.json: LoaiDon=Giao hang,
TrangThai=Huy, TrangThaiGiao=Huy giao, chưa có ngày thanh toán; payment/tiêu hao/
tồn kho/điểm toàn DB không đổi baseline (verdict-473.json). Nhánh hủy giao mở đạt.
Người dùng đồng thời báo chữ Mang đi; đang xác định vị trí nhãn/thời điểm hiển thị,
chưa kết luận lỗi ánh xạ hoặc thay đổi source, chưa đóng toàn T18-K04/T16-K08.

## T18-K07 — PASS native tìm/lọc/reload

Người dùng xác nhận bốn bước: tìm SDT0900000026 trả472+473; Đã giao chỉ472
Chưa thanh toán; Hủy giao chỉ473 Đã hủy; tải lại giữ filter và473. Trước đó
đã tìm theo mã và refresh sau transition/hủy đúng. SQL after-filter-orders.json
xác nhận trạng thái cuối. Kết hợp bằng chứng SQL kế thừa, T18-K07 PASS.
Tổng nhóm37PASS/0FAIL/19BLOCKED. Các ID còn thiếu không tự nâng PASS.

## HD20250472 — thanh toán sau khi giao

Người dùng xác nhận mở lại đơn Đã giao/Chưa thanh toán ở POS, phải trả81000,
khách đưa100000, chốt rồi chọn Không in và POS trống. SQL after-472-checkout.json:
Đã thanh toán/Đã giao, đúng một PT00181000; NVL01 giảm0,018 và NVL03 giảm0,010,
khớp tiêu hao, các nguyên liệu khác không đổi. StockMatches/OnePayment81000=true.
Chỉ checkout làm giảm kho; các bước giao/hủy473 trước đó không làm giảm/hoàn kho.
Không suy PASS toàn T16-K12 vì chưa kiểm đầy đủ refresh bàn/khách/tồn trên POS.

Người dùng yêu cầu báo khi đủ điều kiện triển khai G05. Chưa xác nhận đủ: T18
còn K01/K04/K06 thiếu bằng chứng; nghiệm thu toàn G04 còn19BLOCKED. Giữ nguyên
phụ thuộc và yêu cầu duyệt requirements/bộ plan G05, chưa tự triển khai G05.

## T18-K04 — PASS hủy mở / khóa tài chính paid

User xác nhận detail472 paid khóa Quay lại POS, không có sửa/hủy tài chính.
Kết hợp native hủy473 và SQL bất biến, SQL paid cancel52102 kế thừa: T18-K04 PASS.
Tổng38PASS/0FAIL/18BLOCKED. T18 còn K01 (kênh/người nhận), K06 (UI ba role).

## T18-K06 — PASS ba role

CASH chuyển472 qua native đã đạt. Người dùng xác nhận đăng nhập kho không có
Bán hàng/Giao hàng/Lịch sử hóa đơn; đăng nhập admin chuyển474→Đang giao, vẫn
chưa thanh toán. SQL after-admin-474.json đúng chưa trả81000/Đang giao, không
payment hoặc tiêu hao. Kết hợp unit5case và SQL STORE51605 kế thừa: T18-K06 PASS.
Tổng39PASS/0FAIL/17BLOCKED. T18 chỉ còn K01 chưa đủ bằng chứng biên người nhận.
User hỏi ADMIN vẫn có nghiệp vụ nhân viên: UX mục2 cho ADMIN toàn bộ nghiệp vụ,
PermissionService.Allows ADMIN=true. Đây phù hợp ma trận hiện tại, không tự đổi quyền.

## T18-K01 — FAIL G04-UI06

User xác nhận lỗi ba trường đều gắn X tại tên; source Save catch luôn chọn
recipient. SQL6/6 negative vẫn PASS, không bị ghi đè. Ghi FAIL và bug ownerT16;
chờ duyệt hướng sửa/đợt regression. Tổng39PASS/1FAIL/16BLOCKED. Chưa chốt T18/G05.
