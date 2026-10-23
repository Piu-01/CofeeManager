# Triển khai nút bỏ qua đăng nhập TEST

23/10/2026 — **B01 đã sửa/PASS regression**, xem test-results.md. CấpSELECT đúng một bảngG03TestMarker choG03Gui trong DBtest sau khi xác minhmanifest/marker; không đổi schema/procedure/dữ liệu nghiệp vụ/login/password/DBdevelopment. Handler thật mở1MainForm vớiADMIN ở125% trong process fixture riêng; marker sai vẫn chặn. Toàn bộQ01–Q10 chưa được nghiệm thu, giữ giới hạn trong biên bản.

## Đã làm

- Nút “Bỏ qua đăng nhập — TEST” trong partial Debug riêng; không cần tên/mật khẩu, không điền tự động hoặc đọc password/hash.
- DAL helper Debug kiểm connection localhost/catalog chỉ định và marker, dùng ReadState.AdminAccountId + ReadIdentity hiện có để lấy ADMIN thật; không đổi AuthService, migration, tài khoản hoặc SQL permissions.
- Dùng lại event LoggedIn/ApplicationSessionContext; hai nút dùng chung pending/generation/cancellation, kiểm kết quả về muộn và dispose session khi form đã đóng.
- Helper/nút/hook cấu hình có `#if DEBUG`; Release loại tiện ích. Đăng nhập thường vẫn gọi AuthService.LoginAsync, giữ ô nhập và nút hiện có.
- Launcher test chỉ truyền env cho process con, không sửa profileF5 hoặc môi trường toàn máy. Hướng dẫn bật/tắt/gỡ trong README.md.
- AGENTS.md đã cập nhật phân công người dùng tự kiểm hình thức; code vẫn hỗ trợ100/125/150%, kiểm chức năng chỉ125%.

## Build và phạm vi file

- Debug:0warning/0error, runs/build-debug.log.
- Release:0warning/0error, runs/build-release.log.
- Output riêng runs/build/Debug vàRelease, không thay app cũ đang mở. Không đổi DPI toàn máy hoặc phát hành/commit/push.
- Sửa3source: Program.cs, ApplicationSessionContext.cs, Forms/LoginForm.cs. Thêm3source: Repositories/TemporaryTestTarget.cs (DAL), Configuration/TemporaryTestLogin.cs, Forms/LoginForm.TemporaryTestLogin.cs (UI). Thêm tools/ui/Start-TestLogin.ps1 và tài liệu task. Không đổi Business/Models, schema/migrations hoặc launchSettings.json.
- Danh sách/hash trước-sau: runs/source-before.csv, source-implemented.csv, source-changes.csv. File đã chạm chuẩn hóa CRLF tránh cảnh báo mixed line endings.
- CSV ghi thêm một thay đổi ngoài các file agent sửa: `CafeManager.WinForms.csproj.user` thay đổi trong lúc Visual Studio đang mở. Agent không ghi file này, không hoàn tác hoặc đưa nó vào phạm vi tiện ích. Đối chiếu source của task phải phân biệt cấu hình local này.
- Parser PowerShell xác nhận launcher có0lỗi cú pháp; đây là kiểm kỹ thuật tĩnh, chưa chạy launcher hoặc test chức năng.

## Chưa kiểm chứng

### Bổ sung profile F5 theo yêu cầu người dùng

Đã thêm `CoffeeManager.Test` vào launchSettings.json, không sửa nội dung profile
Development. Debug đọc manifest explicit, dùng cùng DB test cho helper và module;
manifest lỗi hoặc chạy Release thì giữ lỗi cấu hình, không chuyển về Development.
Helper tìm gốc solution để đường dẫn manifest tương đối không phụ thuộc working directory
của Visual Studio. Launcher PowerShell bỏ biến manifest có thể kế thừa để tiếp tục dùng
đúng manifest/connection được truyền vào launcher.

README đã hướng dẫn chọn Debug→CoffeeManager.Test→F5; checklist gỡ cuốiG08 có cả
profile Test và nhánh manifest. Q10 được bổ sung vào đợt test chưa duyệt/chưa chạy.
Build Debug/Release bổ sung lưu trong runs/build-debug-profile.log và build-release-profile.log;
chưa khẳng định đã bấm F5 kiểm thực tế trong Visual Studio.

Người dùng chốt bước gỡ tiện ích ở cuối G08/T32: phải gỡ source/hook/launcher, kiểm lại
Debug/Release/publish và đăng nhập thường rồi mới PASS G08. Đã đồng bộ kế hoạch dự án,
AGENTS.md và checklist README. Đây là nghĩa vụ bàn giao tương lai, chưa thực hiện gỡ.

Q01–Q09 **NOT RUN**: cần kiểm marker với runtime DB permissions thật, tạo phiên TEST, bảo toàn đăng nhập thường, khóa gửi lặp/cancellation/logout, Release metadata và chức năng ở125%. Build/preprocessor source review chưa thay thế kết quả test. Không đánh dấu task hoàn tất/T06PASS từ tiện ích.
