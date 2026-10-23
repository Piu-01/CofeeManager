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

## Development cố định (18/10/2026)

Ngoại lệ development được duyệt: `Initialize-Development.ps1` dùng Windows operator
để provision SQL login runtime `CoffeeManager_Development_Runtime` vào đúng DB
`CoffeeManager_Development`, chỉ role `CafeManagerApp`; không tạo Windows account.
SQL login setup riêng tạm thời gọi BootstrapService/contract hiện có rồi bị thu hồi.
Chỉ công cụ provision development kích hoạt hai placeholder CASH/STORE chính xác;
bootstrap của ứng dụng thông thường không tự bật chúng. Không reset account đã active.
Không đổi migration hoặc contract đăng nhập. Cấu hình và bí mật lưu DPAPI CurrentUser;
profile F5 không chứa credential. Xem README và biên bản development-handoff-results.md
trong hồ sơ G02. Quy trình `--setup` thông thường giữ nguyên.

## Chuỗi G03 (004–008)

Chạy `Apply-Migration.ps1` lần lượt Version4..8 với file tương ứng:
004_inventory_materials.sql →005_products_recipes.sql →006_tables_areas.sql →
007_customers.sql →008_promotions_loyalty.sql. Không sửa001–003/baseline/seed.
Preflight không hợp lệ dừng, không tự sửa dữ liệu hoặc ghi đèchecksum.

Development cố định: `tools/database/Update-DevelopmentG03.ps1` kiểmmarker,
backupCOPY_ONLY CHECKSUM khi đang dưới008, áp dụng migration, đối chiếu tồn/
điểm+hạng/tài chính hóađơn. Khôngseed/resetaccount/ghi fixture; app phảiđóng.
Backup giữ tới khi G03 nghiệm thu và người dùng chấp nhận, đường dẫn trong manifest.
Initialize-Development gọi cập nhật này sau003; app runtime không tựchạy migration.
Contract: material-contract.md, product-contract.md, table-contract.md,
customer-contract.md, loyalty-contract.md. Technicalsmoke không là nghiệm thu.

009_promotion_legacy_codes.sql thuộc T13, chạy sau008: cho phép sửa/xóa mã seed cũ
như KM24BF, giữ mãPK; tạo mới vẫn KM+8số. Development/runnerG03 hiện đến009.
