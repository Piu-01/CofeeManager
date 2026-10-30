# Biên bản nghiệm thu T17 — đợt23/10/2026

Người dùng đã duyệt cả đợt bằng 'duyệt'. SQL Server localhost thật, DB test riêng
schema001–015; SDK10.0.401. Source đóng băng tại [manifest](../runs/acceptance-wave_20261023_195243_f9876e48/manifest.json),
không sửa sản phẩm/migration/test đã chạy. Chưa nghiệm thu hoàn tất.

Evidence: [SQL subset +18unit](../runs/acceptance_20261023_195253_7d540db5/manifest.json),
[SQL mở rộng](../runs/acceptance-wave_20261023_195243_f9876e48/extended-results.json), [phần còn lại/điều tra](../runs/acceptance-wave_20261023_195243_f9876e48/remaining-results.json),
[native binding/print](../runs/acceptance-wave_20261023_195243_f9876e48/native-checkout-print.json), [34unit regression](../runs/acceptance-wave_20261023_195243_f9876e48/regression/regression.trx).
SQL và dữ liệu/expected/actual từng subcase lưu nguyên trong JSON và script cùng thư mục.
Operator Windows identity không thay bằng chứng login/quyền runtime; ca DML dùng user WITHOUT LOGIN riêng.

PASS dưới đây chỉ cho checklist gốc đã đủ bằng chứng; subcase mở rộng trong plan chưa chạy vẫn nêu rõ.
FAIL fixture không tự chuyển thành PASS của sản phẩm. BLOCKED là phần chưa đủ do bộ kiểm thử/luồng GUI.

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T17-K01 | [I/M] Lọc theo ngày/trạng thái/kênh/khách trả đúng đơn, không bỏ giờ cuối ngày. | Fixture cố UPDATE ngày đơn paid bị chặn; ca ngày/paging phụ thuộc FAIL, chưa chứng nhận filter. G04-TEST-04. | FAIL | JSON/script liên kết trên; lọc Id tương ứng |
| T17-K02 | [I] Tiền và số lượng lấy snapshot đã lưu, không menu hoặc tỷ lệ KM hiện tại. | Menu đổi giá không đổi tiền cũ/giá dòng25.000; detail4resultset trả dữ liệu snapshot. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T17-K03 | [M] Preview/in đủ đơn nhiều dòng, ghi chú tiếng Việt, phần giảm/ship/tổng/phương thức; phân trang không mất dòng. | Native preview100dòng/210entry→13trang exit0. Chưa nghiệm thu UI preview/in bản thật, không kiểm hình thức. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T17-K04 | [M] Ghi đúng tiền dòng trước giảm và số phải trả, không nhầm tiền khách đưa. | Có nội dung print từ snapshot; chưa thao tác/đối chiếu output nghiệp vụ đầy đủ. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T17-K05 | [M] In lại không bịa tiền đưa/thối khi DB chưa lưu; nhãn in lại rõ. | Chưa kiểm UI/print in lại với tiền đưa/thối chưa lưu. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T17-K06 | [M/I] Lỗi máy in/hủy print sau commit không thay tiền/kho/điểm. | Không gửi job in; nhánh hủy/lỗi máy in sau commit chưa chạy. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T17-K07 | [M/I] In lại nhiều lần không gọi checkout; không tạo thanh toán/hóa đơn mới. | Chưa thao tác in lại nhiều lần và đối chiếu DB; probe một lần không thay ca này. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T17-K08 | [M/I] Không có nút sửa tài chính đơn chốt; gọi service sửa vẫn bị chặn. | SQL sửa closed bị chặn; nút/dialog/service qua UI chưa kiểm. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T17-K09 | [M] Xem đơn không tồn tại/xóa chưa chốt thông báo đúng, không crash. | SQL detail mã thiếu trả4set rỗng, không lỗi; thông báo UI chưa chạy. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |

Hình thức UI/bản in: **SKIPPED theo phân công**, người dùng tự kiểm. DPI100/150%: **NOT RUN**.
Native probe DeviceDpi120=125% chỉ có phạm vi binding/bounds/preview; không có thao tác UI từ fixture compile lỗi.
Các nhánh UI/UX,100đổi module/10login-logout, late query, quyền/phiên/revocation, fault ưu đãi, policy/recipe
đồng thời hai thứ tự, preflight legacy-invalid/rerun/checksum và mất kết nối trước commit chưa được chứng nhận.
Không tick checklist gốc trong plan. Các phần thiếu phải chạy bù sau duyệt đợt sửa bộ kiểm thử.


