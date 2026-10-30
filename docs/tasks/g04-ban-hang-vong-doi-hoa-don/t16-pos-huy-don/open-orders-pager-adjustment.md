# Chỉnh cụm phân trang theo ảnh — 25/10/2026

Người dùng gửi ảnh riêng của cụm mũi tên / số trang / mũi tên và yêu cầu chỉnh
phần này. Thay đổi nhỏ, rõ ràng trong giao diện Đơn đang mở vừa triển khai.

Chỉ OpenOrdersDialog.cs đổi: previous/next bỏ text, giữ AccessibleName Trang
trước/Trang sau; dùng ManagementButtonStyle.Pager đã có (38×30logical), bỏ
MinimumSize72×38 của SalesUi.Button để không chặn kích thước pager. Gom hai nút
và pageInfo trong TableLayoutPanel autosize, anchorNone căn giữa; số trang dùng
fontSmall, margin ngang8 và TextAlignMiddleCenter. Nhóm pager căn giữa cạnh Mở đơn.
Không đổi state Trước/Sau hoặc query/phân trang/dữ liệu/mãđơn/cột/role/SQL.

Build Debug/Release0warning/0error. Chạy lại fixture nội dung/handler cũ7/7PASS
bằng đúngDLL, không viết test mới chỉ để kiểm getter/style. Source giữ cố định
trong lượt regression nhỏ. Không chạy lại hoặc gán98checks cũ cho binary mới;
không có SQLwrites trong lượt này. Agent không chụpảnhQA hoặc nghiệmthu hìnhthức.
Native hìnhthức/căn giữa cuối cùng do người dùng xácnhận trên bản mới đã mở.

Artifacts tại ../runs/open-orders-pager-20261025/: build logs, content-results.json,
source-frozen.csv và final-manifest.json. G04giữ34PASS/0FAIL/22BLOCKED; chỉ cậpnhật
cụm phân trang, không tự chốt ca native/hìnhthức toànmàn hoặc toànG04.
