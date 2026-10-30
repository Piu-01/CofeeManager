# Biên bản T14 — regression24/10/2026

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
[biên bản trước](../runs/regression_20261024_testfix_v2/previous-test-results-t14.md).

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T14-K01 | [I] Tạo đúng 3 loại đơn; tại quán bắt buộc bàn, mang đi không bàn/đối tác, giao có đủ người nhận. | Ba kênh đúng; sai bàn/ship và7biên người nhận/tên101/SDT16/chữ/địa chỉ251/trắng bị52101, không header/receipt, transaction0. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T14-K02 | [I] Khách vãng lai NULL hợp lệ; mã bàn/khách/nhân viên sai hoặc nhân viên nghỉ bị chặn. | NULL và FK khách sai đã kiểm; actor/stamp/nghỉ việc chưa phủ đủ. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T14-K03 | [I] Header khởi tạo tổng/giảm 0, thêm món trigger cập nhật đúng. | Trigger test quan sát header ban đầu tổng/giảm0 trong transaction; sau món đầu tổng25.000 đúng. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T14-K04 | [I] Hai dòng cùng món khác ghi chú giữ hai MaDong; thao tác theo MaDong không sửa nhầm dòng/đơn. | Hai MaDong khác ghi chú giữ riêng; foreign MaDong bị chặn52101. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T14-K05 | [I] Tạo dòng lấy giá DB; sửa số lượng giữ giá snapshot; số lượng 0/âm/quá int bị chặn. | Sửa giữ25.000; thêm sau đổi giá lấy30.000.0/âm/intMax+1 bị chặn. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T14-K06 | [I] Bớt/xóa món đưa giảm cũ về hợp lệ và tính lại trong một transaction, không vướng CHECK trung gian. | Giảm99%: bớt từ50.000→25.000, giảm24.750; xóa về0/0, transaction thành công. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T14-K07 | [I] Sản phẩm ngừng bán/không có công thức không được thêm theo hợp đồng. | Add/tăng món ngừng bị chặn; giảm/xóa được. Thiếu recipe: add52105,checkout52204, toàn snapshot không đổi. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T14-K08 | [I] Sửa đơn đã trả/hủy bị chặn; trạng thái form cũ không bypass kiểm tra DB. | Sửa đơn paid/canceled bị52102; snapshot DB không đổi; stale version52103. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T14-K09 | [I] Hai session tạo cùng bàn: một đơn mở duy nhất, session còn lại tải đơn có thật. | Hai kết nối:0/52104; đúng một header/một dòng. Chưa kiểm loser tải mã thật qua TableSelectCurrent. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T14-K10 | [I] Hai session sửa cùng đơn: version cũ bị báo xung đột hoặc được tuần tự hóa theo chính sách, không ghi đè im lặng. | Hai kết nối cùng revision:52103/0; không ghi đè im lặng. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T14-K11 | [I] Lỗi giữa header/chi tiết/ưu đãi rollback mọi thay đổi; mất kết nối xác định lại theo mã cấp. | Fault header/line/receipt rollback đạt; timeout sau commit ghi đúng-2, đối chiếu1receipt/1header/transaction0. Trước commit/mất kết nối khác chưa chạy đủ. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T14-K12 | [I] Xóa hết dòng thành đơn rỗng mở, có thể hủy nhưng không checkout. | Xóa hết giữ đơn mở0; checkout51102; hủy thành công không tiêu hao. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |

Chức năng GUI đầy đủ/phiên/login/revoke, hai thứ tự policy/recipe đồng thời, timeout
trước commit, preflight legacy-invalid/rerun/checksum chưa đủ bằng chứng: NOT RUN
ở các subcase tương ứng. Không nâng task Hoàn tất từ PASS vài subcase.
Hình thức UI/bản in SKIPPED theo phân công; DPI100/150%NOT RUN, agent không đổi Scale.
Các vấn đề mới G04-TEST-06/G04-CONC-01 chờ duyệt bổ sung tại additional-bug-batch.md.


