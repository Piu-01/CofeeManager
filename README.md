# CoffeeManager — Quản lí quán cafe

C# WinForms .NET 10 và SQL Server, triển khai theo
[kế hoạch T01–T32](docs/KeHoach_QuanLiQuanCafe.md).

## Chạy development bằng Visual Studio — cập nhật 18/10/2026

Máy hiện tại đã có DB cố định `CoffeeManager_Development` trên `localhost`.
Mở đúng `CafeManager.slnx` trong checkout này, đặt `CafeManager.WinForms` làm Startup
Project, chọn launch profile **CoffeeManager.Development**, rồi F5.

| Tên đăng nhập | Mật khẩu demo | Vai trò |
|---|---|---|
| admin | Admin@123 | ADMIN — quản trị |
| thungan | Cash@123 | CASH — thu ngân |
| kho | Store@123 | STORE — thủ kho |

Nhập tên và mật khẩu trên Login rồi bấm **Đăng nhập**. Không có nút chọn vai trò,
không cần terminal để nhận mật khẩu. Xem [file dữ liệu mẫu dành cho người dùng](database/demo/README.md)
và [fixture tài khoản](database/demo/accounts.json). Đây là thông tin mock công khai,
không phải credential SQL/Windows hoặc tài khoản sử dụng thật.

Để tái lập trên máy development có SQL Server Mixed Mode và operator có quyền tạo
DB/login (không tự bật Mixed Mode):

```powershell
dotnet restore tools/CafeManager.DevelopmentSetup/CafeManager.DevelopmentSetup.csproj
& .\tools\database\Initialize-Development.ps1
```

Công cụ chỉ nhận DB cố định có marker của nó; chạy lại không reset tài khoản/dữ liệu.
Schema chạy DDL trước, seed cùng marker commit trong transaction của seed; sau đó
áp dụng migration 001–003 nguyên checksum. Không chạy trực tiếp seed lần hai.

Profile chỉ chứa cờ `CAFEMANAGER_USE_DEVELOPMENT=1`. Connection được mã hóa DPAPI
CurrentUser trong `%LOCALAPPDATA%\CoffeeManager\development\connection.dpapi`;
ba mật khẩu ứng dụng và bí mật provision trong `accounts.dpapi` cùng thư mục.
`CAFEMANAGER_APP_CONNECTION` nếu đã đặt vẫn ưu tiên. Chạy exe ngoài profile không
tự chọn DB development. Không copy hai file DPAPI sang tài khoản Windows khác.

Thay đổi máy của bước này: một DB development, một SQL login runtime chỉ thuộc
`CafeManagerApp`, hai file DPAPI. SQL login setup tạm được thu hồi sau bootstrap.
Không tạo Windows user. Theo duyệt bổ sung 18/10/2026, đã xóa 112 DB test cũ, SQL Windows login liên quan và Windows user `CafeManagerApp`; DB development được giữ nguyên.
Không có đăng ký công khai; quản lý/cấp tài khoản đầy đủ thuộc T24, home CASH/STORE
vẫn theo phạm vi module đã triển khai. Không đánh dấu toàn bộ G02 hoàn tất từ bước này.

Chi tiết kiểm chứng: [bàn giao development](docs/tasks/g02-truy-cap-ung-dung-cau-hinh-nen/development-handoff-results.md).

## Trạng thái hiện tại

**G01 (T01–T05) đã nghiệm thu kỹ thuật — 45/45 ca bắt buộc PASS**,
cùng kiểm chứng bổ sung Designer, DPI 100/125/150%, bàn phím và runner khi test
thất bại. B01–B10 đã xử lý theo duyệt. Build Debug/Release **0 warning, 0 error**.
Người dùng đã duyệt commit bàn giao vào repo GitHub ngày 09/10/2026.
Chi tiết: [báo cáo G01](docs/tasks/g01-nen-tang-ky-thuat-du-lieu/report.md).
**G02 (T06–T08) đã viết đầu ra triển khai theo bộ plan được duyệt**, gồm setup
admin một lần, login/session/quyền, shell/theme và7tab danh mục. Chờ duyệt đợt
nghiệm thu27ca; chưa đánh dấu Hoàn tất. [Hồ sơ G02](docs/tasks/g02-truy-cap-ung-dung-cau-hinh-nen/plans.md).

Mở `CafeManager.slnx` để xem solution hiện tại. Build bằng
`dotnet build CafeManager.slnx -c Debug` hoặc `-c Release`.
Ứng dụng hiện chạy Login/MainForm từ `src/CafeManager.WinForms`; cấu hình kết nối qua
`CAFEMANAGER_APP_CONNECTION` với tài khoản ứng dụng riêng. Các hướng dẫn
Environment Probe bên dưới giữ làm tham chiếu kiểm chứng T01.

## Môi trường

| Thành phần | Phiên bản đã phát hiện |
|---|---|
| .NET SDK | 10.0.401, pin trong `global.json` |
| Windows Desktop Runtime | 10.0.13 x64 hoặc bản vá .NET 10 LTS mới hơn |
| Visual Studio | Community 2026, 18.10.2, workload .NET desktop đã cài |
| SSMS | 22.9.2 |
| SQL Server | Default instance local `MSSQLSERVER`, engine 17.0.1000.7, Enterprise Developer Edition (64-bit) |
| Git | 2.51.0.windows.1 |

Máy phát triển cần Windows, SDK đã pin và Visual Studio có workload **.NET desktop
development** để mở Designer. SDK khác phiên bản sẽ không được chọn ngầm vì
`rollForward` là `disable`.

Runtime phải được cập nhật riêng với SDK: PKCS có trong shared Windows Desktop
Runtime, nên audit NuGet sạch không chứng minh binary đang chạy đã được vá.
DataAccess pin `System.Security.Cryptography.Pkcs 10.0.13`; kiểm assembly thực tế
khi triển khai. Không dùng Desktop Runtime 10.0.12 cho bản bàn giao này.

## Restore, build và chạy form kiểm chứng

Mở PowerShell tại gốc CoffeeManager:

```powershell
dotnet --version
dotnet restore tools/CafeManager.EnvironmentProbe/CafeManager.EnvironmentProbe.csproj
dotnet build tools/CafeManager.EnvironmentProbe/CafeManager.EnvironmentProbe.csproj -c Debug --no-restore
dotnet build tools/CafeManager.EnvironmentProbe/CafeManager.EnvironmentProbe.csproj -c Release --no-restore
dotnet run --project tools/CafeManager.EnvironmentProbe/CafeManager.EnvironmentProbe.csproj -c Debug --no-build
```

Form có tiêu đề **CoffeeManager — T01 Environment Probe** và thông tin kiểm chứng
WinForms .NET 10. Đóng bằng nút X để kết thúc. Project này kiểm tra toolchain;
solution nghiệp vụ được tạo ở T04.

### T01-K01 — Mở Designer

1. Trong Visual Studio chọn **File → Open → Project/Solution**, mở
   `tools/CafeManager.EnvironmentProbe/CafeManager.EnvironmentProbe.csproj`.
2. Trong Solution Explorer chọn `EnvironmentProbeForm.cs` → **View Designer**
   (hoặc Shift+F7).
3. Xác nhận form và nhãn hiện đúng, không có lỗi tải Designer.
4. Gửi ảnh/kết quả thực tế để cập nhật biên bản T01-K01. Nếu có lỗi, gửi nguyên
   thông báo; không đánh dấu PASS chỉ vì project build được.

### T01-K04 — Xác nhận SQL Server qua SSMS

1. Mở SSMS; chọn Database Engine, server `localhost`, Windows Authentication.
   Bật **Trust server certificate** cho kết nối local này, theo hướng xử lý đã duyệt.
2. Kết nối và mở script
   `docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t01-moi-truong-repository/sql-environment-check.sql`.
3. Chạy script chỉ đọc trong `master`, đối chiếu tên máy/instance, phiên bản
   engine, login và các cột quyền tạo database.
4. Cần có quyền tạo DB qua `CanCreateAnyDatabase=1` hoặc
   `CanCreateDatabaseInMaster=1`; các cột vai trò hỗ trợ đối chiếu. Giá trị NULL
   không phải bằng chứng có quyền.
5. Gửi kết quả hoặc ảnh để nghiệm thu; che thông tin định danh nếu cần.

Lần kết nối ban đầu với ODBC Driver 18 bị lỗi trust certificate. Người dùng đã
duyệt trust certificate cho SQL local; lệnh sau đã kết nối thành công và xác nhận
quyền tạo DB test:

```powershell
sqlcmd -S localhost -E -C -b -l 10 -i docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t01-moi-truong-repository/sql-environment-check.sql -W -s "|"
```

`-C` chỉ dùng cho kết nối phát triển local đã duyệt, không được đặt thành mặc định
cho runtime/LAN. Script chỉ đọc; không tạo DB, sửa schema hoặc chạy seed.

## Cấu trúc và cấu hình local

- `tools/CafeManager.EnvironmentProbe/`: project kiểm chứng T01.
- `database/DB_Schema.sql`, `database/Insert_Data.sql`: baseline và seed đính kèm;
  được kiểm chứng trên DB riêng ở T02. Thứ tự schema trước, seed sau, seed một lần.
- `docs/`: kế hoạch, UX, UI guide và hồ sơ task.
- `.agents/skills/`, `AGENTS.md`: quy trình làm việc của agent.

App đọc chuỗi kết nối runtime từ môi trường process CAFEMANAGER_APP_CONNECTION. Cấu hình local,
dùng `config/local/`, `appsettings.local.json` hoặc `appsettings.*.local.json`,
đã được Git ignore; không lưu credential vào source, README hay log.
Output bin/obj, state Visual Studio, backup SQL và dữ liệu test local cũng bị ignore.

Git root phải là thư mục CoffeeManager:

```powershell
git rev-parse --show-toplevel
git status --short
```

Repository đang ở nhánh `main`, G01 đã commit/push origin; G02 chưa commit. Chỉ commit khi được
người dùng cho phép. T01–T32 được tổ chức thành tám nhóm G01–G08 trong mục 12
của kế hoạch, giữ nguyên chi tiết/325 ca nghiệm thu. Duyệt requirements nhóm một
lần, viết đủ plan rồi duyệt cả bộ một lần → triển khai tuần tự → duyệt đợt test
một lần → test từng task và báo cáo chung → duyệt đợt bug trước debug/regression.
Requirements nhóm đầu tại `docs/tasks/g01-nen-tang-ky-thuat-du-lieu/requirements.md`.
T01 đã nghiệm thu được kế thừa, requirements T02 cũ chỉ là tham chiếu.
Chỉ mục toàn bộ plan G01: [docs/tasks/g01-nen-tang-ky-thuat-du-lieu/plans.md](docs/tasks/g01-nen-tang-ky-thuat-du-lieu/plans.md).

## Solution và nền dữ liệu G01

Đã viết đầu ra và chạy đợt test T03–T05 theo duyệt: restore/build/app/unit chạy
được; B01–B10 đã xử lý và45/45ca bắt buộcPASS. Kết quả chung:
[report.md](docs/tasks/g01-nen-tang-ky-thuat-du-lieu/report.md).
G01 đã nghiệm thu; chi tiết phạm vi ở
[test-plan.md](docs/tasks/g01-nen-tang-ky-thuat-du-lieu/test-plan.md).

```powershell
# Các lệnh kiểm chứng thực hiện sau khi duyệt đợt test
dotnet restore CafeManager.slnx
dotnet build CafeManager.slnx -c Debug
dotnet build CafeManager.slnx -c Release
dotnet run --project src/CafeManager.WinForms/CafeManager.WinForms.csproj
& .\tools\database\Test-ContractMigration.ps1
& .\tools\database\Test-DataAccess.ps1
```

App đọc `CAFEMANAGER_APP_CONNECTION` từ môi trường process, dùng Windows
principal riêng có role CafeManagerApp; không tự chọn DB hoặc fallback admin.
Thiếu cấu hình, Login vẫn mở và hiển thị lý do, nút đăng nhập vô hiệu. Không nhập
connection string chứa secret vào source hoặc đưa vào log.

Contract/quyền/migration: [docs/database/data-contract.md](docs/database/data-contract.md),
[write-ownership.md](docs/database/write-ownership.md), [migrations.md](docs/database/migrations.md).
SQL test tools dùng Windows identity setup riêng của process, tạo DB T03/T05
mới và giữ để tra cứu; fixtures/procedure T03/T05 chỉ tồn tại trong DB test.

## Cài mới / truy cập G02

Schema→seed nếu cần demo (một lần)→migration001→002→003 theo
[migrations.md](docs/database/migrations.md). Provision Windows identity setup
riêng vào CafeManagerSetup. Chạy `dotnet run --project src/CafeManager.WinForms
-- --setup --server localhost --database <DB_da_chon>` bằngidentity đó; nhập
password tại form. Success đóngsetup; thu hồi rolesetup, chạyapp bằngruntime
CafeManagerApp với CAFEMANAGER_APP_CONNECTION. Không lưu password trongGit/chat/
CLIargs/log. Không dùng việc thiếuadminactive làm điều kiện mở lạibootstrap.

[Auth contract](docs/database/auth-contract.md) ·
[Catalog contract](docs/database/catalog-contract.md) ·
[Hướng dẫn nghiệm thuG02](docs/tasks/g02-truy-cap-ung-dung-cau-hinh-nen/manual-test-guide.md).
Runner `tools/database/Test-G02.ps1` và `tools/ui/Verify-G02.ps1` đã chuẩn bị,
chỉ chạy sau duyệt đợt testG02; build/SQLcompile/smoke không thay nghiệm thu.

## G03 — dữ liệu phục vụ bán hàng

Đã có nguyên liệu/mở sổ, menu/công thức, bàn/khu vực, khách/hạng, khuyến mãi và
điều chỉnh điểm; đang chờ duyệt đợt nghiệm thu. Xem
[đường chạy F5 và kịch bản dùng thử](docs/tasks/g03-du-lieu-phuc-vu-ban-hang/development-guide.md)
và [báo cáo triển khai](docs/tasks/g03-du-lieu-phuc-vu-ban-hang/report.md).
Development cố định `localhost/CoffeeManager_Development`, schema009; cập nhật bằng
`tools/database/Update-DevelopmentG03.ps1` khi đã đóng app (backup và đối chiếu trước/
sau, không seed/reset account). Khởi tạo mới giữ baseline→seed tùy chọn→001..009.
G03 chưa có POS/checkout đích, nhập/xuất/kiểm kê hoặc tra cứu/in hoàn chỉnh.
