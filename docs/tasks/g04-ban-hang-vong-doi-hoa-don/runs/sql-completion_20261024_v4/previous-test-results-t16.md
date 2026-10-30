# Biên bản T16 — bổ sung24/10/2026, schema016

Người dùng đã duyệt sửa G04-TEST-06/G04-CONC-01; đã xử lý và regression.
[68ca SQL](../runs/regression_20261024_concurrency_v3/After/manifest.json), [plan trước/sau](../runs/regression_20261024_concurrency_v3/plan-comparison.json),
[clean/rerun/checksum](../runs/regression_20261024_concurrency_v3/Empty/manifest.json).
Các ca kế thừa xem [biên bản v2](../runs/regression_20261024_concurrency_v3/previous-test-results-t16.md).
Giữ expected/checklist gốc; không nâng task Hoàn tất khi cònBLOCKED.

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T16-K01 | [M] Tạo/mở lại đơn tại quán, mang đi, giao hàng; thông tin khách/ghi chú còn sau đóng mở app. | POS tạo/mở lại ba kênh/reload app chưa thao tác. Fixture compile đã sửa; công cụ click/focus chưa hoạt động. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T16-K02 | [M] Thêm/sửa/xóa nhiều món; chọn đúng dòng và tổng/ưu đãi tự refresh từ DB. | SQL dòng món đạt; handler/binding POS sửa/xóa/refresh chưa thao tác đầy đủ. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T16-K03 | [M] 81.000 VND, khách đưa 100.000: lưu phân bổ 81.000 và hiển thị thối 19.000. | Control thật SetValue100.000 cho bill81.000 hiển thị thối19.000; chưa click commitDB nên chưa chứng nhận lưu phân bổ từGUI. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T16-K04 | [M] Nhập tiền không đủ/ký tự/quá lớn, bỏ phương thức, đóng dialog: không chốt hoặc ghi dở. | Control thật báo thiếu khi80.000, báo nguyênVND khiabc; click Confirm trả geometry unavailable, chưa đủ biên/handler/đóng dialog. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T16-K05 | [M] Nhấn double-click/Enter nhiều lần chỉ gửi một request; thao tác xong/lỗi mở nút đúng. | SQL replay đạt. Return không tạo thay đổi; Tab không đổi focus nên không đủ bằng chứng phím đến app; không đánh PASS keyboard/doubleclick. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T16-K06 | [M/I] Cửa sổ khác chốt đơn: POS cũ tải lại và không tự sửa/thu thêm. | SQL stale/closed chặn đúng; POS cũ tải lại chưa kiểm. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T16-K07 | [M/I] Mất kết nối/timeout sau gửi: đọc lại MaHD, hiển thị đã chốt hoặc chưa rõ; không tự retry trả tiền. | Timeout sau commit-2 và receipt đạt; UI unknown/reconcile/khóa gửi lại chưa kiểm. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T16-K08 | [M] Hủy đơn mở trả bàn trống; hủy đơn đã trả bị từ chối và không hoàn kho. | SQL hủy mở/paid refusal đạt; handler hủy và bàn refresh chưa kiểm. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T16-K09 | [M] Đơn rỗng không TT; thiếu kho/công thức hiển thị lý do và giữ đơn mở để sửa. | SQL empty/thiếu kho/recipe đạt; thông báo/giữ input POS chưa kiểm. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T16-K10 | [M] Không chọn PT010 hoặc menu ngừng bán; quyền STORE không mở thao tác bán trái phép. | Unit/SQL STORE bị chặn; menu/PT010 trên POS chưa thao tác. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T16-K11 | [M] DPI/resize, tìm kiếm, nhiều dòng, bàn nhiều: không che nút hoặc khóa UI khi query. | Native DeviceDpi120=125%,1row/3columns; chưa đủ resize/tìm/nhiều bàn/dòng. Không đổi Scale; hình thức SKIPPED. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T16-K12 | [M/I] Bán xong refresh bàn/khách/tồn từ DB, không tự trừ/cộng lần nữa phía app. | SQL không nhân kho/điểm đạt; refresh bàn/khách/tồn từ POS chưa thao tác. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |

GUI còn giới hạn click/focus như hồ sơ v2; chưa kiểm đủ POS/keyboard/nháp/unknown/refresh/phiên/in.
Hình thức UI/bản inSKIPPED theo phân công;100/150%NOT RUN, không đổiWindowsScale.
Timeout trước commit,policy/recipe đồng thời hai chiều,legacy-invalid vẫn chưa đủ bằng chứng.
016clean/rerun/checksum và18cặp concurrency đã chạy; không suy thành PASS các nhánh chưa chạy.
Nguồn/expected/actual/phiên/cleanup trong manifest; test/source giữ cố định trong lượt After.


