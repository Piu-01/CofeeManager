# Bổ sung bàn giao development — được duyệt 18/10/2026

Người dùng duyệt sau khi đã mở đúng source và được thông báo phần còn lại là DB
development cố định cùng tài khoản ADMIN/CASH/STORE. Phạm vi này không bao gồm
xóa112DBtest hoặc Windowsuser CafeManagerApp.

## Đích và đầu ra cụ thể

- SQL localhost, DB mới duy nhất **CoffeeManager_Development**. Không đổi DB cũ
  QuanLyCafe_ERP hoặc DB test. Nếu tên đích đã tồn tại mà không có marker của công
  cụ thì từ chối; không DROP/reset DB để thử lại.
- Schema/seed gốc → migration001→002→003, giữ nguyên checksum của migration.
  Seed giữ dữ liệu mẫu nghiệp vụ; chỉ kích hoạt3account đã seed trong DBdev này.
- ADMIN admin/NV001 qua BootstrapService và procedurefirst-admin hiện có;
  thungan/CASH/NV004 và kho/STORE/NV010 qua thao tác provisiondev của operator,
  chỉ thay inactive placeholder. Không thêm account editor T24 hoặc public signup.
- SQLlogin CoffeeManager_Development_Runtime: khôngsysadmin/db_owner/setup,
  chỉ role CafeManagerApp trên DBdev. SQLlogin setup riêng tạm thời, bỏ user/login
  sau bootstrap. Không tạo account Windows hoặc đổi SQL authentication mode.
- Mật khẩu ngẫu nhiên mỗi máy, không mật khẩu mặc định/qua chat/CLIargument.
  Connection và thông tin3account lưu2file DPAPI CurrentUser ở LOCALAPPDATA,
  không ghi vào repo/log. Công cụ --accounts chỉ cho hiển thị tại terminal trực
  tiếp, từ chối redirect. Đây là công cụ development, không recovery/reset account.
- F5 dùng profile rõ tên CoffeeManager.Development, cờ không bí mật trong
  launchSettings.json. Connection env explicit vẫn ưu tiên; ngoài profile không
  tự rơi vào developmentDB. Fileconfig lỗi báo lỗi, không fallback admin/sa.

## File và thứ tự

1. tools/database/Initialize-Development.ps1: guard, schema DDL autocommit rồi seed + marker atomic, migrations.
2. tools/CafeManager.DevelopmentSetup: provision, bootstrap, DPAPI, verify3role.
3. WinForms/Configuration/DevelopmentConnection.cs, Program.cs,
   Properties/launchSettings.json: localconfig/profileF5, không đổi LoginForm.
4. README và auth-contract: phân biệt cách development với cài đặt thông thường.

## Kiểm chứng trong đợt khắc phục được duyệt

Build công cụ/app; setup trên đúngDBdev; verify3account bằng AuthService,
permission CatalogAdmin, revoke session, principalSQL; chạy lại không reset/mất
dữ liệu; đối chiếu112DBtest không tăng, setup login đã thu hồi. Không gọi runner
tạo fixture hay chạy lại484test. GUI login thực tế do người dùng nhập tại máy;
không tự động hóa hộp xác thực. Mọi giới hạn ghi rõ trong biên bản cuối.

