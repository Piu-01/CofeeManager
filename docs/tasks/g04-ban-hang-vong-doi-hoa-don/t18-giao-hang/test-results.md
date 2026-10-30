# Biên bản T18 — hoàn tất SQL24/10/2026, schema016

[Manifest v4](../runs/sql-completion_20261024_v4/final-manifest.json),
[lượt SQL chính](../runs/sql-completion_20261024_v4/manifest.json),
[quan sát actor](../runs/sql-completion_20261024_v4/actor-followup.json),
[policy/recipe cả hai thứ tự](../runs/sql-completion_20261024_v4/ordering2-manifest.json).
Ca kếthừa xem [biên bản v3](../runs/sql-completion_20261024_v4/previous-test-results-t18.md).
Source/schema giữ cố định;không sửa sản phẩm. Giữ lịch sử FAIL instrumentation và kết quả follow-up,không xóa.

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T18-K01 | [I/M] Chỉ đơn Giao hang có màn hình và dữ liệu người nhận hợp lệ. | Sau duyệt sửa UI06, regression40/40 dialog thật đạt; user trên bản mới xác nhận tên/SDT/địa chỉ sai báo đúng ô/focus, bỏ thay đổi đúng, tìm421 Mang đi trong Giao hàng không có dòng. SQL6/6 negative từ chối người nhận sai và hai kênh không giao;474 giữ dữ liệu hợp lệ, chưa trả/Đang giao,0payment/0tiêu hao. Giữ FAIL trước sửa trong bug-report. | PASS | ../runs/recipient-validation-fix-20261026/results.json; after-native-474.json; ../runs/g05-dependency-check-20261026/recipient-results.json; phản hồi người dùng |
| T18-K02 | [I] Cho giao → Dang giao → Da giao được; nhảy lùi/nhảy sai trình tự bị chặn. | Cho giao→Dang giao→Da giao được; nhảy bước/lùi bị52502. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T18-K03 | [I] Chuyển trạng thái giao của đơn đã trả không đổi tiền/đối tác/khách/ngày thanh toán. | Đơn paid chuyển giao giữ tổng/giảm/ship/phí/ngàyTT; không tạo payment mới. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T18-K04 | [I/M] Đơn mở chưa trả hủy giao/hủy đơn đúng; không dùng hủy giao để hoàn tiền đơn đã trả. | 26/10 native hủy473 từ POS: Huy/Huy giao, không đổi payment/tiêu hao/kho/điểm. User xác nhận detail472 đã trả không có sửa/hủy tài chính và khóa Quay lại POS. SQL negative kế thừa paid cancel52102; không có đường hoàn tiền qua giao hàng. | PASS | ../runs/g05-dependency-check-20261026/verdict-473.json; verdict-472-checkout.json; phản hồi người dùng; SQL v4 kế thừa |
| T18-K05 | [I] Hai session cập nhật từ trạng thái cũ: phát hiện xung đột, không ghi đè kết quả mới. | V3 dùng revision typed,4lượt hai session cùng revision đều0/52103; một transition/receipt,không payment. Validator có negative control102/102 và count. | PASS | V4JSON/script hoặc biên bản kếthừa |
| T18-K06 | [U/M] CASH/ADMIN được cập nhật; STORE bị chặn. | Unit5case và SQL ADMIN/CASH chuyển được, STORE51605 không receipt kế thừa. 26/10 native CASH chuyển472; người dùng đăng nhập kho xác nhận không có Bán hàng/Giao hàng/Lịch sử hóa đơn, ADMIN chuyển474 Chờ giao→Đang giao thành công. SQL474 vẫn chưa trả81000, không payment/tiêu hao. | PASS | ../runs/g05-dependency-check-20261026/after-admin-474.json; after-472.json; phản hồi người dùng; SQL/unit kế thừa |
| T18-K07 | [M] Bộ lọc tìm theo mã/SDT/trạng thái, refresh sau thao tác và reload còn dữ liệu. | 26/10 người dùng tìm mã472/473, chuyển giao472 và hủy473, danh sách cập nhật đúng. Tìm SDT0900000026 trả472+473; lọc Đã giao chỉ472 chưa trả, Hủy giao chỉ473 đã hủy; tải lại giữ bộ lọc và473. SQL xác nhận trạng thái. | PASS | ../runs/g05-dependency-check-20261026/after-filter-orders.json; phản hồi người dùng |
| T18-K08 | [M] Giao thất bại đơn đã trả có hướng dẫn xử lý thủ công, không hiển thị đã hoàn tiền tự động. | 26/10 người dùng mở đúng HD20250403 và xác nhận hướng dẫn liên hệ quản lý/app không tự hoàn tiền, Đã giao/Đã thanh toán, khóa bước tiếp. SQL trước/sau không đổi đơn/payment/tiêu hao/kho/điểm. | PASS | ../runs/g05-dependency-check-20261026/after-403.json; verdict-403.json; phản hồi người dùng |

Bốn ca SQL trướcđó ghiBLOCKED đã kiểm vàPASS;ghiBLOCKED khi agentvẫn kiểmđược làkhôngchínhxác.
Policy/recipe đồngthời haichiều vàtimeout trướccommit,legacy-invalid đã chạy v4,khôngcòn ghiNOT RUN cácnhánh này.
GUI chức năng vẫnchưađủ bằngchứng vì côngcụ click/focus;hìnhthứcSKIPPED;DPI100/150%NOT RUN.
Không suyPASS GUI từSQL hoặcfixture in-memory;khôngtick checklist gốc T01–T32.


