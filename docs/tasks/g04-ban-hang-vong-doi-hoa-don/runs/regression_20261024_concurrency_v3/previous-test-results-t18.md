# Biên bản T18 — regression24/10/2026

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
[biên bản trước](../runs/regression_20261024_testfix_v2/previous-test-results-t18.md).

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T18-K01 | [I/M] Chỉ đơn Giao hang có màn hình và dữ liệu người nhận hợp lệ. | SQL DeliveryGet non-delivery bị52501; màn hình/biên người nhận chưa đủ. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T18-K02 | [I] Cho giao → Dang giao → Da giao được; nhảy lùi/nhảy sai trình tự bị chặn. | Cho giao→Dang giao→Da giao được; nhảy bước/lùi bị52502. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T18-K03 | [I] Chuyển trạng thái giao của đơn đã trả không đổi tiền/đối tác/khách/ngày thanh toán. | Đơn paid chuyển giao giữ tổng/giảm/ship/phí/ngàyTT; không tạo payment mới. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T18-K04 | [I/M] Đơn mở chưa trả hủy giao/hủy đơn đúng; không dùng hủy giao để hoàn tiền đơn đã trả. | SQL hủy mở→Huy/Huy giao không tiêu hao; paid cancel52102. UI chưa kiểm. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T18-K05 | [I] Hai session cập nhật từ trạng thái cũ: phát hiện xung đột, không ghi đè kết quả mới. | Fixture nội suy rowversion sai→SQL102/102, Pair ghi PASS sai. Audit hạ FAIL; chưa chứng nhận concurrency delivery. G04-TEST-06 chờ duyệt. | FAIL | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T18-K06 | [U/M] CASH/ADMIN được cập nhật; STORE bị chặn. | Unit5case đạt; SQL ADMIN/CASH đổi được, STORE51605 và không receipt. Quyền/menuUI chưa kiểm đầy đủ. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T18-K07 | [M] Bộ lọc tìm theo mã/SDT/trạng thái, refresh sau thao tác và reload còn dữ liệu. | SQL lọc mã/SDT/Da giao trả đúng1dòng; refresh/reload màn hình chưa chạy. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T18-K08 | [M] Giao thất bại đơn đã trả có hướng dẫn xử lý thủ công, không hiển thị đã hoàn tiền tự động. | Chưa thao tác hướng dẫn giao thất bại paid; không suy PASS từ đọc source. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |

Chức năng GUI đầy đủ/phiên/login/revoke, hai thứ tự policy/recipe đồng thời, timeout
trước commit, preflight legacy-invalid/rerun/checksum chưa đủ bằng chứng: NOT RUN
ở các subcase tương ứng. Không nâng task Hoàn tất từ PASS vài subcase.
Hình thức UI/bản in SKIPPED theo phân công; DPI100/150%NOT RUN, agent không đổi Scale.
Các vấn đề mới G04-TEST-06/G04-CONC-01 chờ duyệt bổ sung tại additional-bug-batch.md.


