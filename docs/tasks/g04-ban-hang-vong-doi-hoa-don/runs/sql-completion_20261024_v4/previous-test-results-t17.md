# Biên bản T17 — bổ sung24/10/2026, schema016

Người dùng đã duyệt sửa G04-TEST-06/G04-CONC-01; đã xử lý và regression.
[68ca SQL](../runs/regression_20261024_concurrency_v3/After/manifest.json), [plan trước/sau](../runs/regression_20261024_concurrency_v3/plan-comparison.json),
[clean/rerun/checksum](../runs/regression_20261024_concurrency_v3/Empty/manifest.json).
Các ca kế thừa xem [biên bản v2](../runs/regression_20261024_concurrency_v3/previous-test-results-t17.md).
Giữ expected/checklist gốc; không nâng task Hoàn tất khi cònBLOCKED.

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T17-K01 | [I/M] Lọc theo ngày/trạng thái/kênh/khách trả đúng đơn, không bỏ giờ cuối ngày. | Fixture ngày trước chốt, actual ngàyTT; Created/Paid2/1,cuối ngày23:59:59 có mặt,filter kết hợp1,paging51đơn50/1, raw IDs không trùng. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T17-K02 | [I] Tiền và số lượng lấy snapshot đã lưu, không menu hoặc tỷ lệ KM hiện tại. | Menu đổi giá không đổi tiền cũ/giá dòng25.000; detail4resultset trả dữ liệu snapshot. | PASS | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T17-K03 | [M] Preview/in đủ đơn nhiều dòng, ghi chú tiếng Việt, phần giảm/ship/tổng/phương thức; phân trang không mất dòng. | Preview trong bộ nhớ100dòng/210entry;9trang đầu, không kiểm hình thức. Chưa đủ UI preview/in thật và đối chiếu nội dung theo trang. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T17-K04 | [M] Ghi đúng tiền dòng trước giảm và số phải trả, không nhầm tiền khách đưa. | Có nội dung print từ snapshot; chưa thao tác/đối chiếu output nghiệp vụ đầy đủ. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T17-K05 | [M] In lại không bịa tiền đưa/thối khi DB chưa lưu; nhãn in lại rõ. | Chưa kiểm UI/print in lại với tiền đưa/thối chưa lưu. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T17-K06 | [M/I] Lỗi máy in/hủy print sau commit không thay tiền/kho/điểm. | Không gửi job in; nhánh hủy/lỗi máy in sau commit chưa chạy. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T17-K07 | [M/I] In lại nhiều lần không gọi checkout; không tạo thanh toán/hóa đơn mới. | Hai Print() trong bộ nhớ hoàn tất, cursor210; controller tái dùng trả9rồi18previewpages tích lũy. Chưa thao tác UI in lại và đối chiếuDB, không claim18là trang mới. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T17-K08 | [M/I] Không có nút sửa tài chính đơn chốt; gọi service sửa vẫn bị chặn. | SQL sửa closed bị chặn; nút/dialog/service qua UI chưa kiểm. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |
| T17-K09 | [M] Xem đơn không tồn tại/xóa chưa chốt thông báo đúng, không crash. | SQL detail mã thiếu trả4set rỗng, không lỗi; thông báo UI chưa chạy. | BLOCKED | V3JSON Id tương ứng hoặc biên bản kế thừa |

GUI còn giới hạn click/focus như hồ sơ v2; chưa kiểm đủ POS/keyboard/nháp/unknown/refresh/phiên/in.
Hình thức UI/bản inSKIPPED theo phân công;100/150%NOT RUN, không đổiWindowsScale.
Timeout trước commit,policy/recipe đồng thời hai chiều,legacy-invalid vẫn chưa đủ bằng chứng.
016clean/rerun/checksum và18cặp concurrency đã chạy; không suy thành PASS các nhánh chưa chạy.
Nguồn/expected/actual/phiên/cleanup trong manifest; test/source giữ cố định trong lượt After.


