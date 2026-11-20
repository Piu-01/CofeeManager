# Đã gỡ tiện ích bỏ qua đăng nhập

**Bổ sung09/11/2026:** Người dùng đã duyệt tiện ích [đăng nhập nhanh theo role DEV](../dev-role-quick-login/README.md) dùng xác thực thật và credential DPAPI local. Đây là tiện ích mới, không khôi phục bypass đã gỡ24/10. Trước bàn giao T32 phải gỡ cả tiện ích mới theo checklist ở hồ sơ đó; lịch sử bên dưới giữ nguyên.

Theo yêu cầu trực tiếp của người dùng ngày24/10/2026, tiện ích được gỡ sớm,
không đợi cuối G08. App chỉ vào phiên bằng đăng nhập thường.

- Đã xóa UI/helper DAL/helper cấu hình tạo phiên test và các hook trong
  Program, ApplicationSessionContext, LoginForm.
- Đã xóa launcher bypass, công cụ cấp quyền marker và profile CoffeeManager.Test.
- Launcher manual G04 chỉ chọn DB test riêng; bắt buộc nhập tài khoản/mật khẩu.
- Không đổi tài khoản/mật khẩu, AuthService, schema hoặc dữ liệu development.

Requirements/plan/report/test-results và runs cũ giữ làm lịch sử,
không còn là hướng dẫn bật tiện ích. Các chương trình chẩn đoán lịch sử tham chiếu
helper đã xóa không dùng với source hiện tại.

Xem [biên bản gỡ](removal-results.md). G08/T32 vẫn phải build/publish cuối và
regression đăng nhập, logout, thu hồi phiên, quyền trên bản bàn giao cuối cùng.
Việc gỡ sớm không đồng nghĩa G04 hoặc G08 đã nghiệm thu PASS.
