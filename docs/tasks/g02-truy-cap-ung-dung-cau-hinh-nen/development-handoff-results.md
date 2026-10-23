# Bàn giao development — 18/10/2026

Phạm vi khắc phục và regression đã được duyệt. Chỉ DB `localhost/CoffeeManager_Development`.
Không thay baseline SQL, migration 001–003 hoặc source Business/DataAccess/Models.

| Ca | Thao tác / expected | Actual | Kết quả |
|---|---|---|---|
| DEV01 | Build tool, app Debug và Release | Cả ba build 0 warning / 0 error, output app tại bin tương ứng | PASS |
| DEV02 | Khởi tạo schema, seed, migration | Marker ready, AppSetupState Completed; 001–003 đúng checksum | PASS |
| DEV03 | AuthService login ba account đúng role | admin ADMIN; thungan CASH; kho STORE | PASS |
| DEV04 | CatalogAdmin chỉ ADMIN; revoke session chặn sử dụng | Cả ba account đúng kết quả | PASS |
| DEV05 | Runtime không sysadmin/db_owner/setup, không đọc thẳng TAIKHOAN/gọi setup | Membership đúng; hai lệnh bị SQL229 | PASS |
| DEV06 | Chạy lại initializer không reseed/reset | Migrations AlreadyApplied; SecurityStamp, role, active và số dòng mọi bảng không đổi | PASS |
| DEV07 | Config DPAPI đúng connection runtime | Tool --verify đọc DevelopmentConnection.Read và so sánh trong memory, không xuất credential | PASS |
| DEV08 | Không tăng DB test, thu hồi setup login | Vẫn 112 DB test; chỉ còn SQL login development runtime, sysadmin=0 | PASS |
| DEV09 | Người dùng F5 profile và nhập từng tài khoản, logout trên GUI | Chưa thao tác sau cập nhật profile; service PASS không thay thế GUI | NOT RUN |
| DEV10 | DPI/Designer/resize đầy đủ, toàn bộ bộ test G02 | Ngoài đợt khắc phục giới hạn này | NOT RUN |

## Lệnh và bằng chứng

- `dotnet build tools/CafeManager.DevelopmentSetup/CafeManager.DevelopmentSetup.csproj --no-restore -m:1 -p:UseSharedCompilation=false -o artifacts/development-setup`
- `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj --no-restore -c Debug -m:1 -p:UseSharedCompilation=false` và Release.
- `& ./tools/database/Initialize-Development.ps1` lần đầu và chạy lại; `dotnet artifacts/development-setup/CafeManager.DevelopmentSetup.dll --verify`.
- Snapshot trước/sau tại `runs/DEVELOPMENT_HANDOFF_20261018/before.txt`, `after.txt`;
  chỉ tên, role, active, SecurityStamp và số dòng; không hash/mật khẩu.
- File source SHA256 trong cùng thư mục. SQL Server local 17.0.1000.7, .NET SDK theo global.json.

## Sự cố trong lúc chuẩn bị công cụ

Hai lần đầu lỗi SQL1205 do outer transaction bọc DDL tạo TVP rồi seed gọi procedure.
Deadlock XML system_health cho thấy cùng process giữ Sch-M/chờ Sch-S USER_TYPE257.
Rollback được kiểm tra: chỉ còn DevelopmentMarker ready=0, không dữ liệu seed dang dở.
Đã bỏ outer DDL transaction như baseline runner; seed giữ transaction gốc và cập nhật
marker ngay trước COMMIT. Sau sửa, khởi tạo và chạy lại đều PASS. Không tạo DB khác,
không đổi cấu hình server, baseline hoặc expected để né lỗi. Source cố định trong
lượt kiểm chứng cuối; không tự chạy runner tạo fixture.

## Giới hạn bàn giao

Mật khẩu chỉ người dùng xem trực tiếp bằng công cụ --accounts; không gọi qua capture/log.
Không xóa 112 DB test hay Windows account CafeManagerApp; không đổi mật khẩu của account
đã active. Không có public signup, module quản lý account đầy đủ vẫn thuộc T24.
Chưa kết luận toàn bộ G02/UI hoàn tất; còn GUI người dùng và ma trận UI đang theo hồ sơ riêng.

Cập nhật sau duyệt cleanup 18/10/2026: đã xóa 112 DB test và Windows user CafeManagerApp cùng SQL Windows login cũ; các ghi chú chưa xóa phía trên là trạng thái trước cleanup. Verify ba vai trò development sau xóa vẫn PASS; xem report audit môi trường.
