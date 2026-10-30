# Đợt test luồng hỏi in/reset POS — đã duyệt

25/10/2026. Người dùng đã duyệt đợt test bằng “duyệt”. Đang chạy trên source cố
định, xem post-checkout-print-test-results.md. Đây là ca bổ sung củaT16/T17,
không thay56ID/325ID hoặcexpected cũ.
Đóng băng source/binary trước chạy, không sửa sản phẩm trong lượt test.

| Ca bổ sung | Thao tác và expected |
|---|---|
| PC01 Không | Chốt đơn DBtest → hỏi một lần → Không; POS không còn mã/dòng/khách/ưu đãi cũ, có thể tạo đơn tiếp, không mở print/không tạoHD rỗng |
| PC02 Có rồi hủy | Có → hộp chọn máy in đúng chứng từ → Cancel; nháp mới, status đã thanh toán/hủy in, DB payment/kho/điểm không đổi sau commit |
| PC03 Có rồi in | Người dùng chọn máy/đích in; chứng từ lần đầu không có IN LẠI, nội dung snapshot đúng, nháp mới; không gửi job thật trước khi người dùng chọn đích |
| PC04 Lỗi in/đọc sau commit | Giữ thông báo đã thanh toán/chưa in hoặc chưa tải được; nháp mới, in lại từ lịch sử, không gọi checkout; lỗi được tiêm ở component phải ghi rõ, không gọi là fault máy in thật |
| PC05 Hủy/lỗi/unknown trước hoàn tất | Đơn/input được giữ, không hỏi in/reset; modal/busy/Enter/F8 không gửi lặp |
| PC06 Lịch sử/in lại | POS không cònXemIn; lịch sử vẫn có preview/in lại; IN LẠI rõ, không bịa tiền đưa/thối; nhiều lần in không thêm payment/tiêu hao |
| PC07 Hồi quy tiền và nháp mới |81000/100000,mixed/bank/0₫, SelectAll, các nút/dòng mới không thừa dữ liệu cũ; bàn/khách/tồn từDB, không tự cập nhật lần2 |

SQL Server test riêng có marker; agent đối chiếu tiền, tiêu hao/kho, điểm khi có
khách và trạng thái trước/sau. Không chạy ghi vào development hoặc dùng đơn đã
trảHD20250411 để giả chốt lại. Hủy/lỗi in không hoàn tiền/kho.

WinForms thật ở125%, ghi rõ thao tác component và người dùng. Công cụ modal
trước đó có lỗi geometry nên có thể cần người dùng bấm Yes/No/chọn máy in; agent
vẫn chủ động phần component/SQL. Hình thức người dùng tự kiểm, không ảnh QA;
DPI100/150%NOT RUN. Giữ logFAIL, ghi bug mới để duyệt đợt bổ sung trước sửa.

Liên quan T16-K03/K05/K07/K11/K12 vàT17-K03–K08. Kết quả lưu cạnh plan rồi cập
nhật report nhóm, không nâng toànID nếu còn nhánh chưa chạy.
