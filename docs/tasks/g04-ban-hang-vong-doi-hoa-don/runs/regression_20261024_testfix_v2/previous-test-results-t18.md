# Biên bản nghiệm thu T18 — đợt23/10/2026

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
| T18-K01 | [I/M] Chỉ đơn Giao hang có màn hình và dữ liệu người nhận hợp lệ. | SQL DeliveryGet non-delivery bị52501; màn hình/biên người nhận chưa đủ. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T18-K02 | [I] Cho giao → Dang giao → Da giao được; nhảy lùi/nhảy sai trình tự bị chặn. | Cho giao→Dang giao→Da giao được; nhảy bước/lùi bị52502. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T18-K03 | [I] Chuyển trạng thái giao của đơn đã trả không đổi tiền/đối tác/khách/ngày thanh toán. | Đơn paid chuyển giao giữ tổng/giảm/ship/phí/ngàyTT; không tạo payment mới. | PASS | JSON/script liên kết trên; lọc Id tương ứng |
| T18-K04 | [I/M] Đơn mở chưa trả hủy giao/hủy đơn đúng; không dùng hủy giao để hoàn tiền đơn đã trả. | SQL hủy mở→Huy/Huy giao không tiêu hao; paid cancel52102. UI chưa kiểm. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T18-K05 | [I] Hai session cập nhật từ trạng thái cũ: phát hiện xung đột, không ghi đè kết quả mới. | Stale revision bị52103 trong SQL; hai kết nối delivery riêng chưa chạy. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T18-K06 | [U/M] CASH/ADMIN được cập nhật; STORE bị chặn. | 5unit transition/quyền đạt; chưa đủ ADMIN/CASH/STORE trên UI/runtime SQL delivery. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T18-K07 | [M] Bộ lọc tìm theo mã/SDT/trạng thái, refresh sau thao tác và reload còn dữ liệu. | SQL lọc mã/SDT/Da giao trả đúng1dòng; refresh/reload màn hình chưa chạy. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T18-K08 | [M] Giao thất bại đơn đã trả có hướng dẫn xử lý thủ công, không hiển thị đã hoàn tiền tự động. | Chưa thao tác hướng dẫn giao thất bại paid; không suy PASS từ đọc source. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |

Hình thức UI/bản in: **SKIPPED theo phân công**, người dùng tự kiểm. DPI100/150%: **NOT RUN**.
Native probe DeviceDpi120=125% chỉ có phạm vi binding/bounds/preview; không có thao tác UI từ fixture compile lỗi.
Các nhánh UI/UX,100đổi module/10login-logout, late query, quyền/phiên/revocation, fault ưu đãi, policy/recipe
đồng thời hai thứ tự, preflight legacy-invalid/rerun/checksum và mất kết nối trước commit chưa được chứng nhận.
Không tick checklist gốc trong plan. Các phần thiếu phải chạy bù sau duyệt đợt sửa bộ kiểm thử.


