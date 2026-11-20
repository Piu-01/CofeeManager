# Đăng nhập nhanh — DEV

09/11/2026. Đã triển khai và sửa DQ-B01 theo duyệt;57/57 subcase regression,142unit PASS. Debug đường F5 đã build lại đúng DLL được kiểm. Chức năng chưa nghiệm thu toàn bộ vì native còn thiếu; xem bug-regression-results.md.

**Bổ sung sau xử lý G07-DEV01:** DB development đã đồng bộ027 theo duyệt; native nút ADMIN mở được Tổng quan production và đọc số liệu đúng. [Biên bản](../g07-tai-san-bao-cao-hieu-nang-du-lieu/t28-bao-cao-doi-chieu/development-upgrade-results-20261109.md). Bằng chứng này chỉ bổ sung luồng ADMIN→Home, không nâng PASS mọi nhánh modal/role khác còn thiếu ở đợt trước.

1. Chọn cấu hình **Debug**, profile **CoffeeManager.Development**, chạy F5. Target phải là `localhost/CoffeeManager_Development` và cấu hình kết nối development hiện có phải hợp lệ.
2. Trên Login, bấm **Cấu hình đăng nhập nhanh**. Chọn ADMIN/CASH/STORE, nhập tài khoản và mật khẩu thật của role đó, bấm **Lưu**. Lặp lại cho các role cần dùng. Không cần cấu hình đủ ba role cùng lúc.
3. Đóng hộp cấu hình, bấm nút role để đăng nhập. Bấm role chưa cấu hình sẽ mở hộp cấu hình chọn sẵn role đó; lưu xong cần bấm role một lần nữa.
4. Đăng xuất để về Login và chọn role khác. Mỗi lần đều xác thực thật; tài khoản khóa/nghỉ việc, đổi mật khẩu/role cần cập nhật cấu hình.

Không tạo tài khoản hay reset mật khẩu. Nếu thiếu tài khoản, sử dụng chức năng quản trị tài khoản hiện có. Không gửi mật khẩu vào chat.

## Cấu hình local

Chỉ bật khi Debug và hai cờ `CAFEMANAGER_USE_DEVELOPMENT=1`, `CAFEMANAGER_DEV_QUICK_LOGIN=1`. Profile F5 đã chứa hai cờ; Release không tạo tiện ích dù có cờ. Chạy exe Debug thường không có cờ sẽ dùng Login thường.

Credential được mã hóa DPAPI CurrentUser dưới `%LOCALAPPDATA%/CoffeeManager/development/quick-login/<SHA256-server-database>.dpapi`. Không copy file vào repo/package. Thao tác “Xóa cấu hình role” xóa mục của role đang chọn. Muốn bỏ hết thì xóa lần lượt ba role; file mã hóa có thể còn map rỗng. Khi file hỏng/không giải mã được, đóng hộp rồi xóa đúng file `.dpapi` của tiện ích trong thư mục `quick-login` và cấu hình lại; không xóa `connection.dpapi` hoặc các file pending nghiệp vụ.

Không đưa đường dẫn cá nhân, plaintext hoặc hash mật khẩu vào bằng chứng test. Hộp cấu hình xác thực bằng AuthService nhưng chỉ lưu credential, không mở MainForm. AuthService vẫn có thể rehash mật khẩu hợp lệ theo cơ chế hiện có.

## Checklist gỡ trước bàn giao cuối T32

- [ ] Gỡ `Configuration/DevelopmentQuickLogin.cs`, `Forms/DevelopmentQuickLoginDialog.cs`.
- [ ] Gỡ nút/handler/dependency trong LoginForm, ApplicationSessionContext và Program.
- [ ] Gỡ entry point `OrbitDialogHost.ShowLoginDialog` nếu chỉ còn phục vụ tiện ích này.
- [ ] Gỡ cờ `CAFEMANAGER_DEV_QUICK_LOGIN` khỏi profile/launcher và hướng dẫn bật trong tài liệu vận hành; giữ hồ sơ lịch sử.
- [ ] Dọn đúng file credential của tiện ích trên máy dev sau kết thúc sử dụng; không đụng file connection/pending.
- [ ] Build Debug/Release/publish cuối, kiểm đủ ba role, login sai/đúng, logout, khóa/thu hồi/đổi role, callback muộn và quyền với các cờ cũ vẫn được đặt.
- [ ] Lưu bằng chứng tại T32/report G08; không chốt G08 từ việc ẩn nút hoặc compile guard.
