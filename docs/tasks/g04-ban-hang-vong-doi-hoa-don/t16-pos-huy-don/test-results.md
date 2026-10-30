# Biên bản T16 — hoàn tất SQL24/10/2026, schema016

Tiến độmanual25/10: đã tạo Mang điHD20250411 sau G04-UI01; agent đối chiếu
SP001×1,giá81000,ghi chú đúng,giảm/ship0,0payment. Nhánh này đạt, nhưngT16-K01
còn đủ3kênh/mở lại nên vẫnBLOCKED. Xemheader-fix-test-results.md/evidence tương ứng.

[Manifest v4](../runs/sql-completion_20261024_v4/final-manifest.json),
[lượt SQL chính](../runs/sql-completion_20261024_v4/manifest.json),
[quan sát actor](../runs/sql-completion_20261024_v4/actor-followup.json),
[policy/recipe cả hai thứ tự](../runs/sql-completion_20261024_v4/ordering2-manifest.json).
Ca kếthừa xem [biên bản v3](../runs/sql-completion_20261024_v4/previous-test-results-t16.md).
Source/schema giữ cố định;không sửa sản phẩm. Giữ lịch sử FAIL instrumentation và kết quả follow-up,không xóa.

| ID | Expected gốc | Actual | Trạng thái | Bằng chứng |
|---|---|---|---|---|
| T16-K01 | [M] Tạo/mở lại đơn tại quán, mang đi, giao hàng; thông tin khách/ghi chú còn sau đóng mở app. |25/10phát hiệnFAILG04-UI01;đã duyệt/sửa và13/13header componentPASS, cả3kênh Apply trả đúng; giữFAIL cũ. Chưa đủ thao tácchuột/bànphím vàtạo/mở lạiapp3kênh trênPOS,không suyPASS toànca. | BLOCKED | header-fix-test-results.md; ../runs/header-fix-20261025/; lịch sử ../runs/header-investigation-20261025/ |
| T16-K02 | [M] Thêm/sửa/xóa nhiều món; chọn đúng dòng và tổng/ưu đãi tự refresh từ DB. | Người dùng xác nhận đủ thêmSP002×1→111000,sửaSP001×2→192000,xóaSP002→162000,sửaSP001×1→81000,đúng dòng/tổng. Fixture vãng lai/khôngKM,giảm0. Agent đọcSQL cuối:chỉSP001×1,giá81000,ghi chúgiữ,giảm/ship0,chưaTT,0payment. Giá trị trung gian do người dùng quan sát,không giả là agent đã snapshot từng bướcSQL. | PASS | ../runs/header-fix-20261025/manual-order-HD20250411-edit.json |
| T16-K03 | [M] 81.000 VND, khách đưa 100.000: lưu phân bổ 81.000 và hiển thị thối 19.000. | Sau sửa UI05, người dùng chốt qua nút thật: không lỗi, khách đưa100000/thối19000. Agent SQL xác nhận Da thanh toan/payable81000, đúng1payment PT001=81000; tiêu hao/kho NVL01−0.018 và NVL03−0.010 khớp; source207file không đổi. Tiền đưa/thối do người dùng quan sát, DB không lưu. Giữ FAIL trước sửa trong bug-report. | PASS | checkout-submit-test-results.md; ../runs/checkout-submit-fix-20261025/manual-checkout-verdict.json |
| T16-K04 | [M] Nhập tiền không đủ/ký tự/quá lớn, bỏ phương thức, đóng dialog: không chốt hoặc ghi dở. | Control thật báo thiếu khi80.000, báo nguyênVND khiabc; click Confirm trả geometry unavailable, chưa đủ biên/handler/đóng dialog. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T16-K05 | [M] Nhấn double-click/Enter nhiều lần chỉ gửi một request; thao tác xong/lỗi mở nút đúng. | SQL replay đạt. Return không tạo thay đổi; Tab không đổi focus nên không đủ bằng chứng phím đến app; không đánh PASS keyboard/doubleclick. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T16-K06 | [M/I] Cửa sổ khác chốt đơn: POS cũ tải lại và không tự sửa/thu thêm. | SQL stale/closed chặn đúng; POS cũ tải lại chưa kiểm. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T16-K07 | [M/I] Mất kết nối/timeout sau gửi: đọc lại MaHD, hiển thị đã chốt hoặc chưa rõ; không tự retry trả tiền. | Timeout sau commit-2 và receipt đạt; UI unknown/reconcile/khóa gửi lại chưa kiểm. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T16-K08 | [M] Hủy đơn mở trả bàn trống; hủy đơn đã trả bị từ chối và không hoàn kho. | SQL hủy mở/paid refusal đạt; handler hủy và bàn refresh chưa kiểm. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T16-K09 | [M] Đơn rỗng không TT; thiếu kho/công thức hiển thị lý do và giữ đơn mở để sửa. | SQL empty/thiếu kho/recipe đạt; thông báo/giữ input POS chưa kiểm. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T16-K10 | [M] Không chọn PT010 hoặc menu ngừng bán; quyền STORE không mở thao tác bán trái phép. | Unit/SQL STORE bị chặn; menu/PT010 trên POS chưa thao tác. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |
| T16-K11 | [M] DPI/resize, tìm kiếm, nhiều dòng, bàn nhiều: không che nút hoặc khóa UI khi query. | UI03 đã sửa theo duyệt25/10;60/60 layout/binding Darkmode125%,32/32 component,7/7 SQL bridge PASS. Cash/notice/grid/footer tiếp cận được; còn tìm kiếm/nhiều bàn/món/loading POS chưa kiểm. Giữ FAIL UI03 trước sửa, hình thức người dùng tự kiểm. | BLOCKED | display-fix-test-results.md; ../runs/display-fix-20261025-r2/; lịch sử UI03 ở bug-report.md |
| T16-K12 | [M/I] Bán xong refresh bàn/khách/tồn từ DB, không tự trừ/cộng lần nữa phía app. | SQL không nhân kho/điểm đạt; refresh bàn/khách/tồn từ POS chưa thao tác. | BLOCKED | V4JSON/script hoặc biên bản kếthừa |

Bốn ca SQL trướcđó ghiBLOCKED đã kiểm vàPASS;ghiBLOCKED khi agentvẫn kiểmđược làkhôngchínhxác.
Policy/recipe đồngthời haichiều vàtimeout trướccommit,legacy-invalid đã chạy v4,khôngcòn ghiNOT RUN cácnhánh này.
GUI chức năng vẫnchưađủ bằngchứng vì côngcụ click/focus;hìnhthứcSKIPPED;DPI100/150%NOT RUN.
Không suyPASS GUI từSQL hoặcfixture in-memory;khôngtick checklist gốc T01–T32.


