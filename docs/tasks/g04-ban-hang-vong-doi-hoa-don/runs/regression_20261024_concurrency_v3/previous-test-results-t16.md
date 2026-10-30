# Biên bản T16 — regression24/10/2026

Người dùng đã duyệt sửa5lỗi bộ kiểm thử và chạy bù ngày24/10. V1 giữ nguyên lịch sử;
source sản phẩm/schema015 không đổi. Các nhánh mới dưới đây thay trạng thái hiện
tại theo evidence, không sửa expected/checklist T01–T32.

[Manifest v2](../runs/regression_20261024_testfix_v2/manifest.json),
[SQL extended](../runs/regression_20261024_testfix_v2/extended-results.json),
[audit raw concurrency](../runs/regression_20261024_testfix_v2/concurrency-audit.json),
[SQL còn lại](../runs/regression_20261024_testfix_v2/remaining-results.json),
[SQL chạy bù](../runs/regression_20261024_testfix_v2/supplement-results.json),
[52unit TRX](../runs/regression_20261024_testfix_v2/unit/g04-regression.trx),
[GUI control actions](../runs/regression_20261024_testfix_v2/gui-actions.json).
SQL/script/JSON lưu đầy đủ dữ liệu, lệnh, expected và actual của subcase. Chỉ dựa
raw actual để kết luận, không dựa PASS sai của Pair. Kết quả kế thừa v1 xem
[biên bản trước](../runs/regression_20261024_testfix_v2/previous-test-results-t16.md).

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T16-K01 | [M] Tạo/mở lại đơn tại quán, mang đi, giao hàng; thông tin khách/ghi chú còn sau đóng mở app. | POS tạo/mở lại ba kênh/reload app chưa thao tác. Fixture compile đã sửa; công cụ click/focus chưa hoạt động. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T16-K02 | [M] Thêm/sửa/xóa nhiều món; chọn đúng dòng và tổng/ưu đãi tự refresh từ DB. | SQL dòng món đạt; handler/binding POS sửa/xóa/refresh chưa thao tác đầy đủ. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T16-K03 | [M] 81.000 VND, khách đưa 100.000: lưu phân bổ 81.000 và hiển thị thối 19.000. | Control thật SetValue100.000 cho bill81.000 hiển thị thối19.000; chưa click commitDB nên chưa chứng nhận lưu phân bổ từGUI. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T16-K04 | [M] Nhập tiền không đủ/ký tự/quá lớn, bỏ phương thức, đóng dialog: không chốt hoặc ghi dở. | Control thật báo thiếu khi80.000, báo nguyênVND khiabc; click Confirm trả geometry unavailable, chưa đủ biên/handler/đóng dialog. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T16-K05 | [M] Nhấn double-click/Enter nhiều lần chỉ gửi một request; thao tác xong/lỗi mở nút đúng. | SQL replay đạt. Return không tạo thay đổi; Tab không đổi focus nên không đủ bằng chứng phím đến app; không đánh PASS keyboard/doubleclick. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T16-K06 | [M/I] Cửa sổ khác chốt đơn: POS cũ tải lại và không tự sửa/thu thêm. | SQL stale/closed chặn đúng; POS cũ tải lại chưa kiểm. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T16-K07 | [M/I] Mất kết nối/timeout sau gửi: đọc lại MaHD, hiển thị đã chốt hoặc chưa rõ; không tự retry trả tiền. | Timeout sau commit-2 và receipt đạt; UI unknown/reconcile/khóa gửi lại chưa kiểm. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T16-K08 | [M] Hủy đơn mở trả bàn trống; hủy đơn đã trả bị từ chối và không hoàn kho. | SQL hủy mở/paid refusal đạt; handler hủy và bàn refresh chưa kiểm. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T16-K09 | [M] Đơn rỗng không TT; thiếu kho/công thức hiển thị lý do và giữ đơn mở để sửa. | SQL empty/thiếu kho/recipe đạt; thông báo/giữ input POS chưa kiểm. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T16-K10 | [M] Không chọn PT010 hoặc menu ngừng bán; quyền STORE không mở thao tác bán trái phép. | Unit/SQL STORE bị chặn; menu/PT010 trên POS chưa thao tác. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T16-K11 | [M] DPI/resize, tìm kiếm, nhiều dòng, bàn nhiều: không che nút hoặc khóa UI khi query. | Native DeviceDpi120=125%,1row/3columns; chưa đủ resize/tìm/nhiều bàn/dòng. Không đổi Scale; hình thức SKIPPED. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T16-K12 | [M/I] Bán xong refresh bàn/khách/tồn từ DB, không tự trừ/cộng lần nữa phía app. | SQL không nhân kho/điểm đạt; refresh bàn/khách/tồn từ POS chưa thao tác. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |

Chức năng GUI đầy đủ/phiên/login/revoke, hai thứ tự policy/recipe đồng thời, timeout
trước commit, preflight legacy-invalid/rerun/checksum chưa đủ bằng chứng: NOT RUN
ở các subcase tương ứng. Không nâng task Hoàn tất từ PASS vài subcase.
Hình thức UI/bản in SKIPPED theo phân công; DPI100/150%NOT RUN, agent không đổi Scale.
Các vấn đề mới G04-TEST-06/G04-CONC-01 chờ duyệt bổ sung tại additional-bug-batch.md.


