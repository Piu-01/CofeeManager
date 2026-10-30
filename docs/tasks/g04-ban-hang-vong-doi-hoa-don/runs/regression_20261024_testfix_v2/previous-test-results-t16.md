# Biên bản nghiệm thu T16 — đợt23/10/2026

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
| T16-K01 | [M] Tạo/mở lại đơn tại quán, mang đi, giao hàng; thông tin khách/ghi chú còn sau đóng mở app. | Chưa thao tác tạo/mở lại qua POS; fixture GUI compile lỗi G04-TEST-03. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T16-K02 | [M] Thêm/sửa/xóa nhiều món; chọn đúng dòng và tổng/ưu đãi tự refresh từ DB. | SQL dòng món đạt subcase; chưa kiểm binding/handler POS. G04-TEST-03. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T16-K03 | [M] 81.000 VND, khách đưa 100.000: lưu phân bổ 81.000 và hiển thị thối 19.000. | Unit CashChange đạt; chưa nhập100.000 qua dialog và commit81.000. G04-TEST-03. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T16-K04 | [M] Nhập tiền không đủ/ký tự/quá lớn, bỏ phương thức, đóng dialog: không chốt hoặc ghi dở. | Unit biên tiền đạt; GUI invalid/đóng dialog chưa chạy. G04-TEST-03. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T16-K05 | [M] Nhấn double-click/Enter nhiều lần chỉ gửi một request; thao tác xong/lỗi mở nút đúng. | Replay SQL đạt; double-click/Enter/F8 và enable nút chưa thao tác. G04-TEST-03. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T16-K06 | [M/I] Cửa sổ khác chốt đơn: POS cũ tải lại và không tự sửa/thu thêm. | SQL stale/closed chặn đúng; POS cũ refresh chưa kiểm. G04-TEST-03. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T16-K07 | [M/I] Mất kết nối/timeout sau gửi: đọc lại MaHD, hiển thị đã chốt hoặc chưa rõ; không tự retry trả tiền. | Receipt/readback có dữ liệu; timeout fixture FAIL05, UI unknown chưa kiểm. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T16-K08 | [M] Hủy đơn mở trả bàn trống; hủy đơn đã trả bị từ chối và không hoàn kho. | SQL hủy mở/paid refusal đạt; UI/bàn refresh chưa kiểm. G04-TEST-03. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T16-K09 | [M] Đơn rỗng không TT; thiếu kho/công thức hiển thị lý do và giữ đơn mở để sửa. | SQL empty/thiếu kho/ngừng bán đạt; thông báo/giữ input UI chưa kiểm. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T16-K10 | [M] Không chọn PT010 hoặc menu ngừng bán; quyền STORE không mở thao tác bán trái phép. | Unit/SQL STORE bị chặn; menu/PT010 trên UI chưa thao tác. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T16-K11 | [M] DPI/resize, tìm kiếm, nhiều dòng, bàn nhiều: không che nút hoặc khóa UI khi query. | Native125%1row/3columns, CTA trong client; chưa đủ resize/tìm/bàn/nhiều dòng. Hình thức SKIPPED. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |
| T16-K12 | [M/I] Bán xong refresh bàn/khách/tồn từ DB, không tự trừ/cộng lần nữa phía app. | SQL trừ/cộng một lần đạt; refresh bàn/khách/tồn từ POS chưa thao tác. | BLOCKED | JSON/script liên kết trên; lọc Id tương ứng |

Hình thức UI/bản in: **SKIPPED theo phân công**, người dùng tự kiểm. DPI100/150%: **NOT RUN**.
Native probe DeviceDpi120=125% chỉ có phạm vi binding/bounds/preview; không có thao tác UI từ fixture compile lỗi.
Các nhánh UI/UX,100đổi module/10login-logout, late query, quyền/phiên/revocation, fault ưu đãi, policy/recipe
đồng thời hai thứ tự, preflight legacy-invalid/rerun/checksum và mất kết nối trước commit chưa được chứng nhận.
Không tick checklist gốc trong plan. Các phần thiếu phải chạy bù sau duyệt đợt sửa bộ kiểm thử.


