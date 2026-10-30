# Biên bản T17 — regression24/10/2026

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
[biên bản trước](../runs/regression_20261024_testfix_v2/previous-test-results-t17.md).

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T17-K01 | [I/M] Lọc theo ngày/trạng thái/kênh/khách trả đúng đơn, không bỏ giờ cuối ngày. | Fixture ngày trước chốt, actual ngàyTT; Created/Paid2/1,cuối ngày23:59:59 có mặt,filter kết hợp1,paging51đơn50/1, raw IDs không trùng. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T17-K02 | [I] Tiền và số lượng lấy snapshot đã lưu, không menu hoặc tỷ lệ KM hiện tại. | Menu đổi giá không đổi tiền cũ/giá dòng25.000; detail4resultset trả dữ liệu snapshot. | PASS | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T17-K03 | [M] Preview/in đủ đơn nhiều dòng, ghi chú tiếng Việt, phần giảm/ship/tổng/phương thức; phân trang không mất dòng. | Preview trong bộ nhớ100dòng/210entry;9trang đầu, không kiểm hình thức. Chưa đủ UI preview/in thật và đối chiếu nội dung theo trang. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T17-K04 | [M] Ghi đúng tiền dòng trước giảm và số phải trả, không nhầm tiền khách đưa. | Có nội dung print từ snapshot; chưa thao tác/đối chiếu output nghiệp vụ đầy đủ. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T17-K05 | [M] In lại không bịa tiền đưa/thối khi DB chưa lưu; nhãn in lại rõ. | Chưa kiểm UI/print in lại với tiền đưa/thối chưa lưu. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T17-K06 | [M/I] Lỗi máy in/hủy print sau commit không thay tiền/kho/điểm. | Không gửi job in; nhánh hủy/lỗi máy in sau commit chưa chạy. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T17-K07 | [M/I] In lại nhiều lần không gọi checkout; không tạo thanh toán/hóa đơn mới. | Hai Print() trong bộ nhớ hoàn tất, cursor210; controller tái dùng trả9rồi18previewpages tích lũy. Chưa thao tác UI in lại và đối chiếuDB, không claim18là trang mới. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T17-K08 | [M/I] Không có nút sửa tài chính đơn chốt; gọi service sửa vẫn bị chặn. | SQL sửa closed bị chặn; nút/dialog/service qua UI chưa kiểm. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |
| T17-K09 | [M] Xem đơn không tồn tại/xóa chưa chốt thông báo đúng, không crash. | SQL detail mã thiếu trả4set rỗng, không lỗi; thông báo UI chưa chạy. | BLOCKED | v2JSON/script Id tương ứng hoặc biên bản kế thừa |

Chức năng GUI đầy đủ/phiên/login/revoke, hai thứ tự policy/recipe đồng thời, timeout
trước commit, preflight legacy-invalid/rerun/checksum chưa đủ bằng chứng: NOT RUN
ở các subcase tương ứng. Không nâng task Hoàn tất từ PASS vài subcase.
Hình thức UI/bản in SKIPPED theo phân công; DPI100/150%NOT RUN, agent không đổi Scale.
Các vấn đề mới G04-TEST-06/G04-CONC-01 chờ duyệt bổ sung tại additional-bug-batch.md.


