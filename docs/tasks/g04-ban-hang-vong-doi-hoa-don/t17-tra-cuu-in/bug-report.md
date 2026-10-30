# Lỗi bộ kiểm thử T17 — đợt G04 ngày 23/10/2026

## Cập nhật24/10/2026

G04-TEST-04 đã sửa trong v2: đặt ngày lập trước checkout; dùng ngày thanh toán thực,
prefixHD971 riêng, dateSQL động theo SYSDATETIME, fixture lỗi thì ca phụ thuộc BLOCKED.
Không tắt trigger hoặc sửa paid dates. FixturePASS; Created/Paid2/1,filter1,paging50/1
đạt; raw IDs lưu đầy đủ trong remaining-results.json. Detail4set/snapshot/notfound
và filter giao hàng cũng đạt. Bằng chứngv1 FAIL giữ nguyên, không sửa procedure.

## G04-TEST-04 — Fixture thời gian sửa hóa đơn đã khóa; các assertion sau phụ thuộc

Trạng thái: chờ duyệt sửa fixture, chưa sửa. Không xác nhận lỗi lọc sản phẩm.

`remaining.sql` tạo/chốt HD97000002 rồi UPDATE NgayLap/NgayThanhToan. SQL Server chặn
“Hoa don da TT chi duoc cap nhat trang thai giao hang.” tại bước dựng fixture.
Runner vẫn giữ FAIL và tiếp tục phần độc lập; do batch dừng trước vòng tạo51đơn,
ca paging nhận0/0 thay50/1. Ca ngày dùng prefix HD970 rộng nên còn nhận các đơn
độc lập được tạo sau đó:7/3 thay2/1. Đây là kết quả FAIL kế thừa fixture, không là
bằng chứng query bỏ giờ cuối ngày hoặc phân trang sai. Chi tiết mã/row nằm trong
`../runs/acceptance-wave_20261023_195243_f9876e48/remaining-results.json`.

Đề xuất sau duyệt: dựng ngày lập trước checkout khi đơn còn mở; sử dụng ngày thanh
toán thật/fixture import quản trị đã có, tuyệt đối không tắt trigger bảo vệ chỉ để
sửa đơn đã trả. Dùng prefix riêng cho mỗi nhóm và tách chuẩn bị fixture khỏi assertion;
fixture thất bại thì case phụ thuộc BLOCKED, vẫn giữ lỗi gốc. Chạy lại Created/Paid,
23:59:59, filters/paging51đơn, reload/notfound, snapshot và print/in lại/lỗi máy in.

Rủi ro: fixture phải giữ các bất biến lịch sử và không tạo lối import runtime.
Không cần đổi procedure tra cứu từ bằng chứng này. Phiên bản test cũ và FAIL giữ
nguyên; không sửa lại kết quả hoặc expected hàng cũ. Print probe100dòng/13trang đã
chạy exit0 nhưng không thay nghiệm thu UI preview, hủy/lỗi máy in và in lại.
