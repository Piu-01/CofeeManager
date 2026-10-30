# Lịch sử hóa đơn V4 — báo cáo triển khai26/10/2026

Đã triển khai và chạy đợt test riêng được user duyệt26/10/2026:53/53 functional,
94/94 regression,18/18 xUnit Sales đạt. Chờ người dùng xác nhận thao tác tay/
hình thức; xem [kết quả và giới hạn từng ca](test-results.md).
Không thay trạng thái nghiệm thu T17/G04 hoặc các kết quả native trước đây.

- Ghép bố cục từ mẫu V4 vào InvoicesControl, tái sử dụng workspace/theme/grid,
  bổ sung OrbitDateRangeField ghép hai OrbitDateField hiện có.
- Kính lúp/Enter submit tìm kiếm; đổi bộ lọc tự tải bằng từ khóa đã submit.
  Nội dung đang gõ dở không tự thành từ khóa khi lọc, chuyển trang hoặc làm mới.
- Grid có profile riêng cho mã/ngày đầy đủ, tiền vi-VN và badge có chữ;
  pager dùng TotalCount thật. Chi tiết/In giữ cơ chế Form modal Windows hiện có.
- Tùy chọn của shared controls mặc định giữ hành vi các màn khác. Không sửa
  SQL/DAL/BLL, quyền, checkout, nội dung hóa đơn in hoặc dữ liệu tài chính.

Build Debug và Release: 0 warning/0 error. Smoke kỹ thuật28/28 tại DeviceDpi120
cho client1280×720 và1366×768: bounds toolbar/cụm ngày/action/pager, vùng grid,
độ rộng mã và chế độ readonly. Fixture constructor chỉ dùng session/service null
và dòng DTO trong bộ nhớ; không kiểm đăng nhập, query, quyền hay luồng nghiệp vụ.
Không chụp ảnh hoặc nghiệm thu hình thức. DPI100/150% chưa chạy.

Bằng chứng: [manifest](../runs/invoice-history-v4-implementation-20261026/manifest.json),
[smoke](../runs/invoice-history-v4-implementation-20261026/layout-smoke.json),
[Debug](../runs/invoice-history-v4-implementation-20261026/build-debug-final.log),
[Release](../runs/invoice-history-v4-implementation-20261026/build-release.log),
source-final.csv và dll-hashes.json cùng thư mục. Bản V4 đã mở ở đăng nhập thường
PID23728 trên DBtest cho người dùng kiểm; không gán kết quả binary native cũ sang
binary này. Source không đổi, fixture tự dọn và snapshot tiền/kho/điểm không đổi.

Rủi ro cần kiểm: shared controls, query chậm/lỗi, pager trang cuối đủ50 dòng,
ngày và draft/applied keyword, mở đúng dòng sau lọc. Xem [test-plan](test-plan.md).
Không commit. Không tự chốt/hủy các đơn476–478 hoặc tạo DB mới trong pha triển khai.
