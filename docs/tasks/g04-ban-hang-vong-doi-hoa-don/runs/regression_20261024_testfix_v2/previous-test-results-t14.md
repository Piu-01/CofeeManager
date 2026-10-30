# Biên bản nghiệm thu T14 — đợt23/10/2026

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
| T14-K01 | [I] Tạo đúng 3 loại đơn; tại quán bắt buộc bàn, mang đi không bàn/đối tác, giao có đủ người nhận. | Ba kênh tạo đúng; tổ hợp sai bàn/ship rollback. Chưa đủ biên tên/SDT/địa chỉ. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T14-K02 | [I] Khách vãng lai NULL hợp lệ; mã bàn/khách/nhân viên sai hoặc nhân viên nghỉ bị chặn. | NULL và FK khách sai đã kiểm; actor/stamp/nghỉ việc chưa phủ đủ. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T14-K03 | [I] Header khởi tạo tổng/giảm 0, thêm món trigger cập nhật đúng. | Tổng sau thêm đúng; chưa quan sát header0 trước trigger trong transaction. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T14-K04 | [I] Hai dòng cùng món khác ghi chú giữ hai MaDong; thao tác theo MaDong không sửa nhầm dòng/đơn. | Hai MaDong khác ghi chú giữ riêng; foreign MaDong bị chặn52101. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T14-K05 | [I] Tạo dòng lấy giá DB; sửa số lượng giữ giá snapshot; số lượng 0/âm/quá int bị chặn. | Sửa giữ25.000; thêm sau đổi giá lấy30.000.0/âm/intMax+1 bị chặn. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T14-K06 | [I] Bớt/xóa món đưa giảm cũ về hợp lệ và tính lại trong một transaction, không vướng CHECK trung gian. | Giảm99%: bớt từ50.000→25.000, giảm24.750; xóa về0/0, transaction thành công. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T14-K07 | [I] Sản phẩm ngừng bán/không có công thức không được thêm theo hợp đồng. | Ngừng bán: add/tăng bị chặn, giảm/xóa được, checkout bị chặn. Thiếu recipe chưa chạy riêng. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T14-K08 | [I] Sửa đơn đã trả/hủy bị chặn; trạng thái form cũ không bypass kiểm tra DB. | Sửa đơn paid/canceled bị52102; snapshot DB không đổi; stale version52103. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T14-K09 | [I] Hai session tạo cùng bàn: một đơn mở duy nhất, session còn lại tải đơn có thật. | Hai kết nối:0/52104; đúng một header/một dòng. Chưa kiểm loser tải mã thật qua TableSelectCurrent. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T14-K10 | [I] Hai session sửa cùng đơn: version cũ bị báo xung đột hoặc được tuần tự hóa theo chính sách, không ghi đè im lặng. | Hai kết nối cùng revision:52103/0; không ghi đè im lặng. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T14-K11 | [I] Lỗi giữa header/chi tiết/ưu đãi rollback mọi thay đổi; mất kết nối xác định lại theo mã cấp. | Ba fault header/line/receipt rollback toàn snapshot, transaction0. Timeout classifier mất mã exception: G04-TEST-05. | FAIL | JSON/script liên kết trên; lọc Id tương ứng |
| T14-K12 | [I] Xóa hết dòng thành đơn rỗng mở, có thể hủy nhưng không checkout. | Xóa hết giữ đơn mở0; checkout51102; hủy thành công không tiêu hao. | PASS | JSON/script liên kết trên; lọc Id tương ứng |

Hình thức UI/bản in: **SKIPPED theo phân công**, người dùng tự kiểm. DPI100/150%: **NOT RUN**.
Native probe DeviceDpi120=125% chỉ có phạm vi binding/bounds/preview; không có thao tác UI từ fixture compile lỗi.
Các nhánh UI/UX,100đổi module/10login-logout, late query, quyền/phiên/revocation, fault ưu đãi, policy/recipe
đồng thời hai thứ tự, preflight legacy-invalid/rerun/checksum và mất kết nối trước commit chưa được chứng nhận.
Không tick checklist gốc trong plan. Các phần thiếu phải chạy bù sau duyệt đợt sửa bộ kiểm thử.


