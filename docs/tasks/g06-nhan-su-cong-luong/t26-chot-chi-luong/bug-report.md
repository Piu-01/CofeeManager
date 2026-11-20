# T26 — Liên kết bug chung G06

31/10/2026. G06-B01 và G06-B02 do T24 sở hữu, [báo cáo chi tiết](../t24-nhan-vien-tai-khoan/bug-report.md). PayrollRepository dùng chung helper Money có rủi ro overflow tiền lớn; PayrollControl dùng cùng format gây literalN. Chưa tự sửa hoặc nhân đôi bug. Đợt đề nghị gồm cả regression payroll preview/chốt/chi, định dạng text và nguồn phiếu chi. Native payroll handler còn BLOCKED do công cụ input modal, không quy kết bug sản phẩm.

31/10/2026: người dùng đã duyệt đợt; G06-B01/B02 đã sửa và regression PASS. Xem [biên bản](../regression-b01-b02.md), giữ lịch sử/bằng chứng trước sửa. Native còn BLOCKED.
