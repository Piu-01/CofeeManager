# G05 — Ghi chú đo kiểm29/10

Các source sản phẩm được sửa trong pha debug đã duyệt rồi chốt source-after-debug.csv trước regression. Probe/tài liệu ở thư mục run là instrumentation, không thay source sản phẩm hoặc expected. source-freeze-check/final-audit xác nhận0 thay đổi trong regression.

Build/probe ban đầu trong sandbox báo Build FAILED nhưng0warning/0error; build elevated sau đó thành công, không tính lần sandbox thành lỗi sản phẩm. Layout-before đo trước sửa bằng helper thật, hai cấu hình ghép control và DPI120; không screenshot/so sánh hình thức. Component-results là kiểm public controls/bounds/handler PerformClick, tách với native chuột qua cua_repl.

HashRegressionV1 dừng vì gọi SafeGetValue trên statement wrapper; V2 đọc StringConstantExpressionAst.Value nhưng harness tách suffix thiếu1 ký tự. Các bản được giữ, chưa dùng làm kết luận sản phẩm. HashRegression.ps1 cuối đọc đúng literal source và suffix; hash-regression.json PASS. Không đổi moneySql/expected trong các lần sửa instrumentation này, không chạy script nâng development.

GUI fixture riêng được cấp account ngẫu nhiên trong DB test, credential chỉ lưu DPAPI ngoài repo; state setup được chuẩn bị và ghi gui-setup.json, không là Bootstrap acceptance. Lần launch Hidden đầu không tạo được phiên tương tác đã chọn; phiên Normal PID18692 đăng nhập thường thành công, stderr không có lỗi. Không dừng process của người dùng. Không log password/hash đăng nhập. Mọi screenshot chỉ dùng định vị control để thao tác chức năng; bằng chứng lưu là AX/bounds, không nghiệm thu hình thức.

Dialog-footer-diagnostic là ca tái hiện lỗi mới B05, FAIL vì hai nút có vùng client tiếp cận cao0; source helper chưa sửa, không chỉnh expected để làm PASS. B05 cần duyệt bổ sung. Dữ liệu native đọc trong phiên không đổi hash nguồn tài chính; cuối lượt đã dọn2DB/credential/GUI theo resource-audit.json, không xóa pending thật, không ghi development/đụng backup.
