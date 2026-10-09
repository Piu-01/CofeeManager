# Môi trường thực tế — T01

**Ngày khảo sát:** 08/10/2026.  
**Trạng thái:** Môi trường đã nghiệm thu kỹ thuật; đầy đủ T01-K01–K07 PASS.  
**Schema:** Baseline có trong `database/`; chưa thực thi.  
**Commit:** Repository riêng vừa khởi tạo, chưa có commit.

## Công cụ

| Thành phần | Quan sát thực tế | Nguồn |
|---|---|---|
| OS | Windows 10.0.26200, win-x64 | `dotnet --info` |
| SDK | 10.0.401 | `dotnet --info`, `dotnet --version` |
| MSBuild | 18.9.11+e34a38d2a | `dotnet --info` |
| .NET Desktop Runtime | Microsoft.WindowsDesktop.App 10.0.12; có thêm 8.0.31 | `dotnet --info` |
| Visual Studio | Community 2026 18.10.2, installationVersion 18.10.12217.157 | `vswhere` |
| Workload desktop | ManagedDesktop và ManagedDesktop.Core có trong VS | `vswhere -requires` |
| WinForms template | `winforms`, `winformslib`, `winformscontrollib` có sẵn | `dotnet new list winforms` |
| SSMS | 22.9.2; executable version 22.9.12120.119 | `vswhere`, VersionInfo SSMS.exe |
| Git | 2.51.0.windows.1 | `git --version` |

VS tại `C:/Program Files/Microsoft Visual Studio/18/Community`.
SSMS tại `C:/Program Files/Microsoft SQL Server Management Studio 22/Release`.
Đây là đường dẫn khảo sát, không phải dependency tuyệt đối trong source/project.

## Git và SDK

- Trước T01: Git root `D:/Workspace`.
- Sau `git init -b main` tại CoffeeManager: root
  `D:/Workspace/Nam_3/HK1/CoffeeManager`.
- `.gitignore` đã được đối chiếu với bin/obj, `.vs`, cấu hình local và backup.
- `global.json`: SDK 10.0.401, `rollForward=disable`, `allowPrerelease=false`.
- MSBuild xác nhận `TargetFramework=net10.0-windows`, `UseWindowsForms=true`,
  `OutputType=WinExe` cho project kiểm chứng.

## SQL Server

- Service `MSSQLSERVER`: Running.
- `sqlcmd` có tại `C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/180/Tools/Binn/SQLCMD.EXE`.
- `Invoke-Sqlcmd` được phát hiện từ SQLPS; chưa dùng để thay đổi kết nối.
- Đã thử `localhost`, Windows Authentication, ODBC Driver 18 bằng script chỉ đọc.
- Kết nối ban đầu bị chặn: `The certificate chain was issued by an authority that is not trusted.`
- Sau khi người dùng duyệt trust certificate local, chạy lại `sqlcmd -S localhost
  -E -C ...`: exit 0, query chỉ đọc thành công.
- Server/máy: `LAPTOP-79UNRIRC`, instance `MSSQLSERVER`, engine `17.0.1000.7`,
  `Enterprise Developer Edition (64-bit)`.
- Windows login: `LAPTOP-79UNRIRC\Lenovo`; CanCreateAnyDatabase=1,
  CanCreateDatabaseInMaster=1, IsSysadmin=1, IsDbcreator=1. Đủ quyền tạo DB test.
  Đây là danh tính kiểm chứng/migration hiện tại, chưa phải tài khoản runtime app.
- Ảnh người dùng ngày 08/10/2026 xác nhận SSMS kết nối localhost/master, query
  thành công và hai cột quyền tạo DB=1; T01-K04 PASS.

## Kiểm chứng project

Restore, build Debug/Release và restore/build Release từ source copy sạch ban đầu
thành công nhưng có một warning CS8669. Sau khi người dùng duyệt, thêm
`#nullable enable` vào Designer: Debug/Release và bản copy sạch mới đều có
**0 warning, 0 error**. Bằng chứng trong `test-results.md`.

Chạy executable Debug và quan sát main window qua process: có window handle,
title `CoffeeManager - T01 Environment Probe`; gửi CloseMainWindow, process
kết thúc exit code 0. Ảnh người dùng sau đó xác nhận Designer hiện đúng form/nhãn
ở DPI 125%; T01-K01 PASS. File resx được VS tạo đã được giữ nguyên và source
hiện tại được build/copy sạch lại với 0 warning, 0 error.

Chi tiết expected/actual và ca còn chặn: [test-results.md](test-results.md).
