# Biên bản T14 — hoàn tất SQL24/10/2026, schema016

[Manifest v4](../runs/sql-completion_20261024_v4/final-manifest.json),
[lượt SQL chính](../runs/sql-completion_20261024_v4/manifest.json),
[quan sát actor](../runs/sql-completion_20261024_v4/actor-followup.json),
[policy/recipe cả hai thứ tự](../runs/sql-completion_20261024_v4/ordering2-manifest.json).
Ca kếthừa xem [biên bản v3](../runs/sql-completion_20261024_v4/previous-test-results-t14.md).
Source/schema giữ cố định;không sửa sản phẩm. Giữ lịch sử FAIL instrumentation và kết quả follow-up,không xóa.

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T14-K01 | [I] Tạo đúng 3 loại đơn; tại quán bắt buộc bàn, mang đi không bàn/đối tác, giao có đủ người nhận. | Ba kênh đúng; sai bàn/ship và7biên người nhận/tên101/SDT16/chữ/địa chỉ251/trắng bị52101, không header/receipt, transaction0. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T14-K02 | [I] Khách vãng lai NULL hợp lệ; mã bàn/khách/nhân viên sai hoặc nhân viên nghỉ bị chặn. | NULLkhách hợp lệ,MaNV lấy từactor;FK bàn/khách sai52105;actor không có/stamp cũ/khóa/nghỉ51505;4mode khôngheader/receipt,snapshot nhưtrước,transaction0:0. Quan sát riêng xác minh assertion inlineState nhiễu. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T14-K03 | [I] Header khởi tạo tổng/giảm 0, thêm món trigger cập nhật đúng. | Trigger test quan sát header ban đầu tổng/giảm0 trong transaction; sau món đầu tổng25.000 đúng. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T14-K04 | [I] Hai dòng cùng món khác ghi chú giữ hai MaDong; thao tác theo MaDong không sửa nhầm dòng/đơn. | Hai MaDong khác ghi chú giữ riêng; foreign MaDong bị chặn52101. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T14-K05 | [I] Tạo dòng lấy giá DB; sửa số lượng giữ giá snapshot; số lượng 0/âm/quá int bị chặn. | Sửa giữ25.000; thêm sau đổi giá lấy30.000.0/âm/intMax+1 bị chặn. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T14-K06 | [I] Bớt/xóa món đưa giảm cũ về hợp lệ và tính lại trong một transaction, không vướng CHECK trung gian. | Giảm99%: bớt từ50.000→25.000, giảm24.750; xóa về0/0, transaction thành công. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T14-K07 | [I] Sản phẩm ngừng bán/không có công thức không được thêm theo hợp đồng. | Add/tăng món ngừng bị chặn; giảm/xóa được. Thiếu recipe: add52105,checkout52204, toàn snapshot không đổi. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T14-K08 | [I] Sửa đơn đã trả/hủy bị chặn; trạng thái form cũ không bypass kiểm tra DB. | Sửa đơn paid/canceled bị52102; snapshot DB không đổi; stale version52103. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T14-K09 | [I] Hai session tạo cùng bàn: một đơn mở duy nhất, session còn lại tải đơn có thật. | Hai lượt hai session,đảo thứ tự phát lệnh:0/52104. Chính connection thua đọcTableSelectCurrent lấy đúngMaHD thắng vàSalesOrderGet đúng1dòng;không chèn món sang đơn thắng;session0:0. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T14-K10 | [I] Hai session sửa cùng đơn: version cũ bị báo xung đột hoặc được tuần tự hóa theo chính sách, không ghi đè im lặng. | Hai kết nối cùng revision:52103/0; không ghi đè im lặng. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T14-K11 | [I] Lỗi giữa header/chi tiết/ưu đãi rollback mọi thay đổi; mất kết nối xác định lại theo mã cấp. | Fault sauưuđãi rollback toàn snapshot;timeout trướccommit-2 khôngheader/receipt;đóngconnection saucommit vàtimeout saucommit đọc lại1receipt/1header/1line;replay khôngghiđôi,đổiqty/actor51608,transaction0:0. Faultheader/line/receipt kếthừa v1. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T14-K12 | [I] Xóa hết dòng thành đơn rỗng mở, có thể hủy nhưng không checkout. | Xóa hết giữ đơn mở0; checkout51102; hủy thành công không tiêu hao. | PASS | V4JSON/script hoặc biên bản kếthừa |

Bốn ca SQL trướcđó ghiBLOCKED đã kiểm vàPASS;ghiBLOCKED khi agentvẫn kiểmđược làkhôngchínhxác.
Policy/recipe đồngthời haichiều vàtimeout trướccommit,legacy-invalid đã chạy v4,khôngcòn ghiNOT RUN cácnhánh này.
GUI chức năng vẫnchưađủ bằngchứng vì côngcụ click/focus;hìnhthứcSKIPPED;DPI100/150%NOT RUN.
Không suyPASS GUI từSQL hoặcfixture in-memory;khôngtick checklist gốc T01–T32.

