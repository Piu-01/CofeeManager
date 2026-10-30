# Biên bản T18 — bổ sung24/10/2026, schema016

Người dùng đã duyệt sửa G04-TEST-06/G04-CONC-01; đã xử lý và regression.
[68ca SQL](../runs/regression_20261024_concurrency_v3/After/manifest.json), [plan trước/sau](../runs/regression_20261024_concurrency_v3/plan-comparison.json),
[clean/rerun/checksum](../runs/regression_20261024_concurrency_v3/Empty/manifest.json).
Các ca kế thừa xem [biên bản v2](../runs/regression_20261024_concurrency_v3/previous-test-results-t18.md).
Giữ expected/checklist gốc; không nâng task Hoàn tất khi cònBLOCKED.

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T18-K01 | [I/M] Chỉ đơn Giao hang có màn hình và dữ liệu người nhận hợp lệ. | SQL DeliveryGet non-delivery bị52501; màn hình/biên người nhận chưa đủ. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T18-K02 | [I] Cho giao → Dang giao → Da giao được; nhảy lùi/nhảy sai trình tự bị chặn. | Cho giao→Dang giao→Da giao được; nhảy bước/lùi bị52502. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T18-K03 | [I] Chuyển trạng thái giao của đơn đã trả không đổi tiền/đối tác/khách/ngày thanh toán. | Đơn paid chuyển giao giữ tổng/giảm/ship/phí/ngàyTT; không tạo payment mới. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T18-K04 | [I/M] Đơn mở chưa trả hủy giao/hủy đơn đúng; không dùng hủy giao để hoàn tiền đơn đã trả. | SQL hủy mở→Huy/Huy giao không tiêu hao; paid cancel52102. UI chưa kiểm. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T18-K05 | [I] Hai session cập nhật từ trạng thái cũ: phát hiện xung đột, không ghi đè kết quả mới. | V3 dùng revision typed,4lượt hai session cùng revision đều0/52103; một transition/receipt,không payment. Validator có negative control102/102 và count. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T18-K06 | [U/M] CASH/ADMIN được cập nhật; STORE bị chặn. | Unit5case đạt; SQL ADMIN/CASH đổi được, STORE51605 và không receipt. Quyền/menuUI chưa kiểm đầy đủ. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T18-K07 | [M] Bộ lọc tìm theo mã/SDT/trạng thái, refresh sau thao tác và reload còn dữ liệu. | SQL lọc mã/SDT/Da giao trả đúng1dòng; refresh/reload màn hình chưa chạy. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T18-K08 | [M] Giao thất bại đơn đã trả có hướng dẫn xử lý thủ công, không hiển thị đã hoàn tiền tự động. | Chưa thao tác hướng dẫn giao thất bại paid; không suy PASS từ đọc source. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |

GUI còn giới hạn click/focus như hồ sơ v2; chưa kiểm đủ POS/keyboard/nháp/unknown/refresh/phiên/in.
Hình thức UI/bản inSKIPPED theo phân công;100/150%NOT RUN, không đổiWindowsScale.
Timeout trước commit,policy/recipe đồng thời hai chiều,legacy-invalid vẫn chưa đủ bằng chứng.
016clean/rerun/checksum và18cặp concurrency đã chạy; không suy thành PASS các nhánh chưa chạy.
Nguồn/expected/actual/phiên/cleanup trong manifest; test/source giữ cố định trong lượt After.


