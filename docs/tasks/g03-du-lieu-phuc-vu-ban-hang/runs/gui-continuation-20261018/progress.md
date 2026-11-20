# Tiếp tục GUI G03 — 18/10/2026

**Cập nhật sau ảnh người dùng:** login thủ công bị chặn vì DB fixture Pending. Đã đối chiếu thực tế Pending/ActiveAdmins1; ghi G03-B02 ở taskT09. Chờ duyệt dựng lại fixture qua setup chuẩn; chưa sửa source hoặc AppSetupState. Không yêu cầu thử login lại khi vẫn Pending. Phiên test cũ giữ có tên/marker/expiry để dọn trước thay phiên.

Người dùng chọn tiếp tục G03; đợt nghiệm thu đã được duyệt trước đó vẫn áp dụng. Source sản phẩm/migration/test giữ nguyên. Chưa có ca manual mới đủ điều kiện PASS.

Helper Windows đã trả đúng ảnh Visual Studio CoffeeManager sau khi chọn lại cửa sổ và gọi activate_window, rồi capture lại; lần capture đầu vẫn là Codex, không dùng làm evidence Designer. Hộp Open File xuất hiện khi Ctrl+O, nhưng accessibility modal có lúc null/trễ. Chưa mở/save/đóng/mở lại Designer control G03, không coi ảnh StartupForm là PASS các control G03.

DB demo riêng baseline→009: xem manifest.json; marker xác định ownership. Runtime G03Gui ánh xạ login development đã có, không tạo login mới; chỉ sao hash trong SQL để tài khoản thử dùng cùng mật khẩu, không log hash/mật khẩu. Launcher chỉ đặt kết nối test trong environment tiến trình, không thay file cấu hình development. Lượt launcher đầu dừng trước tạo process vì accessor DbConnectionStringBuilder trong PowerShell không parse được chuỗi; đã sửa script đo dùng set_ConnectionString/get_ConnectionString, không phải lỗi app.

App test PID37428 đã mở, window987674, title CoffeeManager · Đăng nhập; tree/ảnh cùng màn login đúng. Chưa có input username/password hoặc login do agent. Đã nhờ người dùng đăng nhập admin thủ công theo giới hạn skill computer-use (không tự động hóa user authentication dialogs). Không dùng identity injection làm PASS login/UI.

Trong lúc chờ login, không đổi màn hoặc focus từ xa để tránh cản người dùng. DB chỉ giữ cho phiên manual đang hoạt động; hết phiên cần dọn theo tên+marker trong manifest, không dùng wildcard. Hạn giữ19/10/2026. Kết quả47ID chưa đổi:20PASS/27BLOCKED.
