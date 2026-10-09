# Kết quả kiểm chứng T01

**Ngày:** 08/10/2026.  
**Trạng thái task:** Đã nghiệm thu kỹ thuật — 7/7 ca PASS; chờ cho phép commit bàn giao.  
**Source:** Theo plan T01 đã duyệt; repository riêng chưa có commit.  
**Môi trường:** [environment.md](environment.md).  
**DB/schema:** Chưa chạy baseline/seed; chỉ thử kết nối đọc metadata.

Các kiểm tra kỹ thuật đã chạy theo plan; hai ảnh người dùng cung cấp xác nhận
Designer và SSMS thực tế. Đủ bảy ca PASS; còn lưu commit bàn giao theo kế hoạch.

## Ma trận kết quả

| Mã | Expected | Actual | Kết quả |
|---|---|---|---|
| T01-K01 | Form net10.0-windows mở Designer không lỗi | Ảnh người dùng: form và nhãn tiếng Việt hiện đúng trong tab [Design], DPI 125%, không lỗi tải Designer | PASS |
| T01-K02 | Build Debug/Release thành công; form mở/đóng bình thường | Sau fix hai build exit 0, 0 warning/0 error; lần kiểm chứng trước main window Debug có handle/title, đóng bằng CloseMainWindow, exit 0 | PASS — kiểm chứng cửa sổ bằng process, chưa quan sát layout |
| T01-K03 | SDK pin và target .NET 10 khớp thực tế | SDK 10.0.401; MSBuild trả net10.0-windows, UseWindowsForms=true, WinExe | PASS |
| T01-K04 | SSMS kết nối đúng SQL instance, có quyền tạo DB test | Ảnh người dùng: query thành công trong SSMS tại localhost/master; đúng MSSQLSERVER/17.0.1000.7, hai cột quyền tạo DB=1 | PASS |
| T01-K05 | Git root là CoffeeManager | `D:/Workspace/Nam_3/HK1/CoffeeManager` | PASS |
| T01-K06 | Git chỉ xét dự án này; output/secret ignore, source không ignore | Status chỉ có file CoffeeManager; 8 đường dẫn đại diện match ignore; project/Designer/global.json/baseline/skill không ignore | PASS |
| T01-K07 | Source sạch restore/build ở nơi khác được | Bản copy mới sau fix: restore và Release build exit 0, 0 warning/0 error; không bin/obj/.git từ bản gốc | PASS |

## Lệnh và bằng chứng

### T01-K02 — Restore/build/chạy

```powershell
dotnet restore tools/CafeManager.EnvironmentProbe/CafeManager.EnvironmentProbe.csproj
dotnet build tools/CafeManager.EnvironmentProbe/CafeManager.EnvironmentProbe.csproj --configuration Debug --no-restore
dotnet build tools/CafeManager.EnvironmentProbe/CafeManager.EnvironmentProbe.csproj --configuration Release --no-restore
```

Restore thành công. Debug: `Build succeeded`, 1 Warning(s), 0 Error(s), 4,92 giây.
Release: `Build succeeded`, 1 Warning(s), 0 Error(s), 2,04 giây.
Warning chung: CS8669 tại `EnvironmentProbeForm.Designer.cs(5,45)`.

Executable Debug được mở bằng Start-Process. Lần đầu WaitForInputIdle trả về
khi chưa quan sát được main window; không coi lần này là PASS hoặc lỗi app đã
xác định. Lần kiểm chứng sau chờ main window trong tối đa 10 giây:

```text
ProcessId: 11804
HasExited: False
WindowTitle: CoffeeManager - T01 Environment Probe
WindowHandle: 395496
ExitCode: 0
```

Đóng bằng `CloseMainWindow()` và `WaitForExit(10000)`, không Kill process.
Chưa có ảnh hoặc kết quả quan sát trực quan layout.

### T01-K03 — SDK và target

```powershell
dotnet --version
dotnet msbuild tools/CafeManager.EnvironmentProbe/CafeManager.EnvironmentProbe.csproj -getProperty:TargetFramework,UseWindowsForms,OutputType
```

```text
10.0.401
TargetFramework: net10.0-windows
UseWindowsForms: true
OutputType: WinExe
```

### T01-K04 — SQL metadata

```powershell
sqlcmd -S localhost -E -b -l 10 -i docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t01-moi-truong-repository/sql-environment-check.sql -W -s "|"
```

Exit 1, trước khi query chạy:

```text
Microsoft ODBC Driver 18 for SQL Server : SSL Provider:
The certificate chain was issued by an authority that is not trusted.
Client unable to establish connection.
```

Không tạo database hoặc sửa dữ liệu. Chưa dùng `-C`/Trust server certificate.

### T01-K05/K06 — Git

```powershell
git rev-parse --show-toplevel
git status --short
git check-ignore -v tools/CafeManager.EnvironmentProbe/bin/Debug/net10.0-windows/CafeManager.EnvironmentProbe.exe tools/CafeManager.EnvironmentProbe/obj/project.assets.json .vs/settings.json appsettings.local.json appsettings.Development.local.json config/local/connection.json database/local-test.bak test-data/local/fixture.json
```

Root đúng CoffeeManager. Tám đường dẫn trên match `.gitignore`. Kiểm tra riêng
Designer.cs, csproj, global.json, DB_Schema.sql và karpathy SKILL.md: không ignore.
`git status` chỉ liệt kê nội dung dự án; chưa stage, commit hay tạo remote.

### T01-K07 — Bản source sạch

Vị trí thử:
`C:/Users/Lenovo/AppData/Local/Temp/opencode/coffee-t01-clean-20ab941e0448425e825042469b0dae99/`.
Chỉ copy `global.json` và các file top-level của project; không output hoặc cấu
hình local. Chạy version/restore/build từ gốc bản copy, SDK 10.0.401, Release
build thành công trong 1,79 giây, 1 warning CS8669, 0 error.

## Phần cần tiếp tục

Đủ bảy ca PASS. Chờ người dùng cho phép commit mốc bàn giao T01; chuẩn bị
requirements T02, chỉ triển khai sau điểm duyệt của task tương ứng.

## Kiểm chứng sau xử lý được duyệt — 08/10/2026

Đã thêm một dòng `#nullable enable` trước namespace của Designer. Không thay logic.

| Kiểm tra | Actual |
|---|---|
| Debug build --no-restore | Exit 0; 0 warning, 0 error; 4,47 giây |
| Release build --no-restore | Exit 0; 0 warning, 0 error; 1,27 giây |
| Copy sạch mới: version/restore/Release build | SDK 10.0.401; exit 0; 0 warning, 0 error; build 2,04 giây |

Bản copy mới:
`C:/Users/Lenovo/AppData/Local/Temp/opencode/coffee-t01-clean-fixed-bdae858673264828a22f3c6463ffc6e3/`.

Lệnh SQL được người dùng duyệt:

```powershell
sqlcmd -S localhost -E -C -b -l 10 -i docs/tasks/g01-nen-tang-ky-thuat-du-lieu/t01-moi-truong-repository/sql-environment-check.sql -W -s "|"
```

Exit 0, kết quả:

```text
ServerName / MachineName: LAPTOP-79UNRIRC
InstanceName: MSSQLSERVER
ProductVersion: 17.0.1000.7
Edition: Enterprise Developer Edition (64-bit)
OriginalLogin / CurrentLogin: LAPTOP-79UNRIRC\Lenovo
CanCreateAnyDatabase: 1
CanCreateDatabaseInMaster: 1
IsSysadmin: 1
IsDbcreator: 1
```

Query chỉ đọc, không tạo DB hoặc chạy schema/seed. Đủ bằng chứng CLI về instance
và quyền. Khi kiểm chứng CLI, còn thiếu SSMS; ảnh manual bên dưới đã bổ sung đủ.

## Bằng chứng manual người dùng cung cấp — 08/10/2026

Nguồn: hai ảnh đính kèm trong hội thoại nghiệm thu T01; chưa lưu file ảnh cục bộ.

### T01-K01 — Designer

Ảnh Visual Studio lúc khoảng 19:28 ngày 08/10/2026: project
`CafeManager.EnvironmentProbe` đang mở, tab `EnvironmentProbeForm.cs [Design]`
hiển thị cửa sổ và nhãn tiếng Việt đầy đủ. Properties nhận đúng lớp Form;
Toolbox WinForms có sẵn. Banner cho biết main display ở DPI 125% và gợi ý
DPI-Unaware Mode, là thông tin scaling, không phải lỗi tải Designer.
Không đổi DPI hoặc setting Designer chỉ vì banner này. Expected/actual khớp.

### T01-K04 — SSMS

Ảnh SSMS lúc khoảng 19:29: script metadata đã chạy trong `master`, status
**Query executed successfully**, trả một dòng. Thanh kết nối ghi `localhost`.

- ServerName/MachineName: `LAPTOP-79UNRIRC`.
- InstanceName: `MSSQLSERVER`; ProductVersion: `17.0.1000.7`.
- Edition: `Enterprise Developer Edition (64-bit)`.
- OriginalLogin/CurrentLogin: `LAPTOP-79UNRIRC\Lenovo`.
- CanCreateAnyDatabase=1, CanCreateDatabaseInMaster=1, IsSysadmin=1.

Ảnh không hiển thị cột IsDbcreator ở phần bên phải, nhưng hai cột quyền tạo DB
đã nhìn rõ và đủ để nghiệm thu; không suy đoán giá trị cột ngoài ảnh.

### Kiểm chứng source sau thao tác Designer

VS đã thêm `EnvironmentProbeForm.resx` vào project; giữ nguyên file người dùng
tạo và không đưa `.csproj.user`/`.vs` vào source. Designer.cs vẫn giữ nullable
directive và layout đã tạo. Build lại source hiện tại:

- Debug: 0 warning, 0 error, 3,29 giây.
- Release: 0 warning, 0 error, 1,20 giây.
- Copy sạch gồm csproj/cs/resx và global.json, không file .user: restore và
  Release build thành công, 0 warning, 0 error, 1,18 giây.

Vị trí copy:
`C:/Users/Lenovo/AppData/Local/Temp/opencode/coffee-t01-designer-clean-a061c98f0bf14a1c989ce770bf03b7da/`.
