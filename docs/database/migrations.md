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

G02 đã có code setup/login/shell/danh mục; đợt nghiệm thu riêng chưa chạy.
Thiếu cấu hình báo rõ, không fallback administrator/DBtest. Contract:
[auth-contract.md](auth-contract.md), [catalog-contract.md](catalog-contract.md).

## Chuỗi migration G02

```powershell
& .\tools\database\Apply-Migration.ps1 -Database <DB_da_chon>
& .\tools\database\Apply-Migration.ps1 -Database <DB_da_chon> -Version 2 -File .\database\migrations\002_auth_and_bootstrap.sql
& .\tools\database\Apply-Migration.ps1 -Database <DB_da_chon> -Version 3 -File .\database\migrations\003_reference_catalogs.sql
```

002 tạo role/setupstate/loginKey/stamp/procedure;003 tạo catalogrevision,
systemexpense/payment-support/receipt/CRUD/lookup. Clean không cần seeddemo;
demo chỉseed trước001 trên schema rỗng. Preflightconflict rollback, giữ dữ liệu
và báo người cài; không reset cấu hình để bỏ qua xung đột. Rerun qua runner
version/checksum, không chạy SQL003 trực tiếp nhiều lần. Các DB smoke có bản
migration tạm trong triển khai được giữ làm lịch sử; chạy bản cuối trên DB
test mới, không sửa checksum đã ghi để ép rerun.

## Kiểm chứng sau duyệt test

`Test-ContractMigration.ps1` tạo DB T03 mới, seed và empty, áp dụng migration,
đối chiếu rows/FK/version, hai session cấp mã, quyền user WITHOUT LOGIN.
`Test-DataAccess.ps1` tạo DB T05 mới, migration và SQL test-only fixtures,
chạy xUnit; không dùng DB ứng dụng. Test/bug ở task con trong G01, giữ các lượt.
G01 đã nghiệm thu45/45; lịch sử tại reportG01. RunnerG02/collectorUI chỉ chạy
sau duyệt đợt testG02; build và dependency smoke được ghi riêng trong triển khai.
