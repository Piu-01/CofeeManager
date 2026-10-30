# Biên bản T17 — hoàn tất SQL24/10/2026, schema016

[Manifest v4](../runs/sql-completion_20261024_v4/final-manifest.json),
[lượt SQL chính](../runs/sql-completion_20261024_v4/manifest.json),
[quan sát actor](../runs/sql-completion_20261024_v4/actor-followup.json),
[policy/recipe cả hai thứ tự](../runs/sql-completion_20261024_v4/ordering2-manifest.json).
Ca kếthừa xem [biên bản v3](../runs/sql-completion_20261024_v4/previous-test-results-t17.md).
Source/schema giữ cố định;không sửa sản phẩm. Giữ lịch sử FAIL instrumentation và kết quả follow-up,không xóa.

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T17-K01 | [I/M] Lọc theo ngày/trạng thái/kênh/khách trả đúng đơn, không bỏ giờ cuối ngày. | Fixture ngày trước chốt, actual ngàyTT; Created/Paid2/1,cuối ngày23:59:59 có mặt,filter kết hợp1,paging51đơn50/1, raw IDs không trùng. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T17-K02 | [I] Tiền và số lượng lấy snapshot đã lưu, không menu hoặc tỷ lệ KM hiện tại. | Menu đổi giá không đổi tiền cũ/giá dòng25.000; detail4resultset trả dữ liệu snapshot. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T17-K03 | [M] Preview/in đủ đơn nhiều dòng, ghi chú tiếng Việt, phần giảm/ship/tổng/phương thức; phân trang không mất dòng. | Preview trong bộ nhớ100dòng/210entry;9trang đầu, không kiểm hình thức. Chưa đủ UI preview/in thật và đối chiếu nội dung theo trang. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T17-K04 | [M] Ghi đúng tiền dòng trước giảm và số phải trả, không nhầm tiền khách đưa. | Người dùng chốt421/kháchđưa100000 rồi in MicrosoftPrinttoPDF; output1trang có1×81000=81000, tổng/phải trả/thu81000, không in100000 thành doanh thu. SQL1PT00181000, điểm+81/kho khớp; nguồn phương thức Tien mat đúng danh mục. | PASS | ../runs/pdf-print-20261025/final-manifest.json; ../runs/pdf-print-20261025/G04-HD20250421.pdf |
| T17-K05 | [M] In lại không bịa tiền đưa/thối khi DB chưa lưu; nhãn in lại rõ. | Chưa kiểm UI/print in lại với tiền đưa/thối chưa lưu. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T17-K06 | [M/I] Lỗi máy in/hủy print sau commit không thay tiền/kho/điểm. | Không gửi job in; nhánh hủy/lỗi máy in sau commit chưa chạy. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T17-K07 | [M/I] In lại nhiều lần không gọi checkout; không tạo thanh toán/hóa đơn mới. | Hai Print() trong bộ nhớ hoàn tất, cursor210; controller tái dùng trả9rồi18previewpages tích lũy. Chưa thao tác UI in lại và đối chiếuDB, không claim18là trang mới. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T17-K08 | [M/I] Không có nút sửa tài chính đơn chốt; gọi service sửa vẫn bị chặn. | SQL sửa closed bị chặn; nút/dialog/service qua UI chưa kiểm. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T17-K09 | [M] Xem đơn không tồn tại/xóa chưa chốt thông báo đúng, không crash. | SQL detail mã thiếu trả4set rỗng, không lỗi; thông báo UI chưa chạy. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |

Bốn ca SQL trướcđó ghiBLOCKED đã kiểm vàPASS;ghiBLOCKED khi agentvẫn kiểmđược làkhôngchínhxác.
Policy/recipe đồngthời haichiều vàtimeout trướccommit,legacy-invalid đã chạy v4,khôngcòn ghiNOT RUN cácnhánh này.
GUI chức năng vẫnchưađủ bằngchứng vì côngcụ click/focus;hìnhthứcSKIPPED;DPI100/150%NOT RUN.
Không suyPASS GUI từSQL hoặcfixture in-memory;khôngtick checklist gốc T01–T32.

