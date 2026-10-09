# Áp dụng migration và tách principal

1. Cài mới: DB_Schema.sql → seed một lần nếu cần demo → migrations tăng version.
   Seed chưa được chạy lại; không chạy CREATE DATABASE vào DB đã có.
2. Đóng app khi bootstrap sequence; runner khóa migration và bảng nguồn, tính
   max suffix hiện có. Không có writers bên ngoài lấy MAX+1 trong thời gian này.
3. Migration principal có quyền DDL/DML setup; app dùng Windows login chuyên
   biệt ánh xạ role CafeManagerApp, không sa/sysadmin/db_owner.
4. `Apply-Migration.ps1 -Database <DB đã chọn> -Version 1` đọc SHA256 bytes SQL,
   transaction cùng connection qua GO và sp_getapplock; ghi version sau DDL.
5. Version/checksum trùng → AlreadyApplied; checksum khác → từ chối; dùng
   version mới thay vì sửa migration đã phát hành. Lỗi rollback DDL/version.
6. Connection runtime tách `CAFEMANAGER_APP_CONNECTION` (app) và
   `CAFEMANAGER_MIGRATION_CONNECTION` (test setup); không lưu trong Git/log.
   Runner PowerShell migration dùng Windows identity quản trị của process.
7. Local trust certificate chỉ localhost development; LAN phải cấu hình chứng
   chỉ hợp lệ. Mẫu `database/security/provision-app-user.sql` nhận tham số
   WindowsLogin/DatabaseUser, login đã được quản trị tạo; không password mặc định.

App hiện chưa tự bootstrap/login (T06). Program chỉ cấu hình nền/query view.
Thiếu cấu hình báo rõ, không tự fallback sang administrator hoặc DB test.

## Kiểm chứng sau duyệt test

`Test-ContractMigration.ps1` tạo DB T03 mới, seed và empty, áp dụng migration,
đối chiếu rows/FK/version, hai session cấp mã, quyền user WITHOUT LOGIN.
`Test-DataAccess.ps1` tạo DB T05 mới, migration và SQL test-only fixtures,
chạy xUnit; không dùng DB ứng dụng. Test/bug ở task con trong G01, giữ các lượt.
G01 đã duyệt triển khai nhưng **chưa duyệt đợt test**: các lệnh trên chưa chạy.
