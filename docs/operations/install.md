# Cài CoffeeManager trên Windows

Bản G08 ứng viên: .NET10 WinForms, Windows x64, SQL Server riêng. Source pin SDK trong global.json; chạy package chỉ cần Desktop Runtime .NET10 x64 theo release manifest, không cần Visual Studio. Không coi .exe là bộ cài SQL Server. Kiểm phiên bản SQL/runtime thực tế trong manifest trước dùng.

## Cài DB sạch

Operator dùng Windows identity có quyền tạo DB/DDL, chọn đúng SQL instance và **DB mới**. Không dùng DB đang có dữ liệu hoặc system database. Trong thư mục bộ cài có tools/database và database:

```powershell
New-Item -ItemType Directory -Path ./install-evidence
./tools/database/Install-Database.ps1 -Server localhost -Database CafeManager_New -Mode Clean -ManifestPath ./install-evidence/clean.json
```

Thứ tự: DB mới → database/DB_Schema.sql (đổi target trong bộ nhớ, không sửa baseline) → migrations001–027 theo version và SHA256. Clean không chạy Insert_Data.sql. Role ADMIN/CASH/STORE do002 tạo; phương thức tiền mặt/chuyển khoản và chi hệ thống do003 tạo. Operator kiểm manifest Installed và đủ27version trước bootstrap. Không tự tạo khách/tồn/điểm/nhân viên demo.

Nếu cài lỗi, manifest ghi DB đã tạo hay chưa và version đã áp dụng. Không chạy Install lần hai lên DB đó; không tự xóa/seed lại. Operator kiểm marker OperationInstallMarker và dữ liệu trước quyết định tiếp tục migration còn thiếu bằng Apply-Migration.ps1 hoặc bỏ DB mới do chính lượt cài tạo. Không sửa file migration đã có checksum để chạy lại.

## Danh tính và quản trị đầu tiên

Đọc [security.md](security.md), dùng identity setup và runtime riêng. Cấp CafeManagerSetup cho Windows identity setup bằng script database/security/provision-setup-user.sql sau thay placeholder; không cấp role này cho runtime. Mở exe bằng identity setup:

```powershell
./app/CafeManager.WinForms.exe --setup --server localhost --database CafeManager_New
```

Chọn tạo nhân viên quản trị mới, nhập username/mật khẩu riêng và xác nhận trong hộp hiện có. Không password trên commandline/chat/file log. Bootstrap tạo CVADM/nhân viên và account hash trong một transaction, trạng thái Completed bền vững. Đóng rồi mở lại không bootstrap lại. Thu hồi CafeManagerSetup của identity setup sau hoàn tất; app bình thường không tự fallback lên quyền setup.

Runtime dùng Windows identity riêng đã có login SQL, được provision user qua script database/security/provision-app-user.sql bằng tham số @WindowsLogin/@DatabaseUser của operator. Đặt CAFEMANAGER_APP_CONNECTION trong môi trường local tiến trình theo chính sách máy; ví dụ **chỉ dùng placeholder không phải credential thật**:

```powershell
$env:CAFEMANAGER_APP_CONNECTION='Server=YOUR_SQL_HOST;Database=CafeManager_New;Integrated Security=True;Encrypt=True;TrustServerCertificate=False'
./app/CafeManager.WinForms.exe
```

Runtime Windows identity đang chạy app phải là identity đã được cấp CafeManagerApp. Localhost development đã có ngoại lệ trust theo cấu hình; ở máy sử dụng cần chứng chỉ SQL hợp lệ, không tự chuyển trust=true để che lỗi chứng chỉ. Không dùng SQL credential của operator cho runtime. Kiểm kết nối bằng đăng nhập thường; sai/thiếu cấu hình phải hiện lỗi và không chọn DB khác.

## DB demo tách biệt

Chọn DB mới khác, -Mode Demo. Nhánh này chạy baseline → Insert_Data.sql **một lần trên schema rỗng** → migrations001–027. Không chạy seed sau migrations hoặc lên DB sử dụng. Account seed mặc định inactive/placeholder; dùng setup ActivateDemo với admin/NV001 để tạo hash riêng hoặc kiểm LegacyReview nếu dữ liệu cũ đã có hash thật. Không DML đổi AppSetupState để bỏ review.

database/demo/accounts.json là mock công khai cho development, không là credential SQL/Windows hoặc password bản sử dụng. DevelopmentSetup hiện chỉ chấp nhận DB development cố định; không gọi nó trên DB mới. Khi demo package, operator tạo/cấp các role theo chức năng quản lý tài khoản rồi giữ mật khẩu ở máy, không đóng gói file bí mật. Không có chọn role/bypass trên Login.

## Kiểm sau cài

Đăng nhập ADMIN, cấp tài khoản CASH/STORE theo chức năng nhân sự; kiểm role/BLL, logout và đăng nhập lại. Khai báo dữ liệu nền qua module trước bán/nhập, mở sổ tồn theo contract, không UPDATE tồn/điểm trực tiếp. Xem [demo.md](demo.md) để đi luồng. Cài thành công không chứng minh325ca đã PASS: [báo cáo G08](../tasks/g08-van-hanh-nghiem-thu-ban-giao/report.md) công khai phần còn thiếu.
