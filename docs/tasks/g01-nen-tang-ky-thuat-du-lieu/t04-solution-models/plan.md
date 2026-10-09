# Plan T04 — Solution ba tầng và Models dùng chung

**Trạng thái:** 7/7 PASS — source UI B07 đã lưu/đóng/mở lại Designer bằng VS18/DTE và có ảnh/bằng chứng; chưa nghiệm thu toàn nhóm.  
**Phụ thuộc:** T03, triển khai theo đầu ra contract T03; nghiệm thu trong đợt test.  
**Nguồn:** Kế hoạch 3.2/T04, UX mục 15–17, UI guide README/index/preview.

## 1. Phạm vi và đầu ra

Tạo solution với bốn project src và project tests; namespace/reference đúng,
composition root và Form khởi động tối thiểu mở được bằng Designer. Chỉ tạo
DTO cần dùng cho mẫu query T05, không tạo entity/repository cho mọi bảng.
Probe T01 được giữ; không làm Login T06, MainForm/theme/menu T07 trong T04.

## 2. Bước triển khai

| Bước | File/khu vực | Đầu ra cần kiểm chứng trong đợt test |
|---|---|---|
| 1 | `CafeManager.slnx`, `src/*/*.csproj`, `tests/CafeManager.Tests/*.csproj` | Bốn project đúng framework; test xUnit, SDK pin hiện có |
| 2 | ProjectReference của các project | Models độc lập; DAL → Models; BLL → DAL/Models; UI → BLL/Models và DAL tại composition root; tests tham chiếu đúng phần cần kiểm thử |
| 3 | `src/CafeManager.Models/Dtos/TableStatusDto.cs` | MaBan/TenBan/MaKV/TrangThai/MaHD typed theo view T03; nullable đúng, không SqlClient/WinForms |
| 4 | `src/CafeManager.WinForms/Program.cs`, `Forms/StartupForm.cs`, `.Designer.cs`, `.resx` | ApplicationConfiguration.Initialize, composition root; Form hiển thị trạng thái nền tối thiểu |
| 5 | `README.md`, `.gitignore` nếu cần, hồ sơ task | Lệnh restore/build/run từ gốc; không đường dẫn tuyệt đối/secret; source sạch build được |

- Thư viện target `net10.0`; WinForms `net10.0-windows`, UseWindowsForms.
  Tests target tương thích Windows khi cần; package xUnit/test SDK phiên bản
  tương thích .NET 10 được pin tường minh khi triển khai.
- StartupForm có constructor không kết nối DB để Designer dùng được; runtime
  nhận dependency qua Program khi T05 có service thật. T04 chỉ nối thành phần
  hiện có; không tạo interface/service rỗng để lấp tầng.
- Form native, Segoe UI 10–11pt, nền kem/chữ nâu theo guide, Dock/Anchor;
  `.Designer.cs` chỉ layout/control. Theme chung/AppTheme triển khai tại T07.
  Form T05 có thể bổ sung thao tác kiểm chứng nền vào Form này; không lấy HTML
  preview làm chứng cứ Designer/WinForms.
- Computed/số dư không có setter ghi DB; DTO chỉ phục vụ đọc view trong scope.

## 3. Kiểm thử sau duyệt đợt test G01

| Ca | Lệnh/thao tác | Expected và bằng chứng |
|---|---|---|
| T04-K01 | `dotnet restore CafeManager.slnx`; `dotnet build CafeManager.slnx -c Debug` và `-c Release` | Build thành công, không vòng reference; lưu logs/warnings/errors |
| T04-K02 | Kiểm tra csproj/reference và namespace/package | Models không UI/SqlClient; BLL không WinForms, references đúng tầng |
| T04-K03 | Review Form/Designer, tìm SQL/SqlConnection ngoài Program | Form không SQL/connection; chỉ gọi service, Designer không business |
| T04-K04 | Chạy WinForms từ project, mở/đóng Form | Program nối đúng dependency, mở StartupForm đúng; không giả đã đăng nhập |
| T04-K05 | Mở StartupForm bằng VS Designer, save/reopen | Không lỗi namespace/resource, không cần SQL để mở Designer |
| T04-K06 | Copy source sạch không bin/obj/local config sang thư mục test, restore/build | Không cần đường dẫn máy tác giả; lưu lệnh và log copy-build |
| T04-K07 | Review source/diff | Chỉ DTO/thành phần cần dùng, không generic repository/UnitOfWork hoặc lớp rỗng hàng loạt |

Đợt test kiểm tra Form thật ở 1280×720/1366×768 và DPI 100/125/150% theo guide
cho phần layout tối thiểu; thiếu GUI/bằng chứng ghi BLOCKED, gom manual cho người
dùng. Không viết unit test getters/DTO. Chạy kiểm chứng sau khi duyệt test, không
coi việc tạo project/file là PASS.

## 4. Hồ sơ và xử lý bug

Kết quả tại `test-results.md` cạnh plan, ghi schema/source commit hoặc hash,
expected/actual và log/manual evidence. Bug T04 tại `bug-report.md`, liên kết
report G01; chờ duyệt cả đợt bug trước debug/regression. Triển khai xong T04
chuyển T05 theo bộ plan đã duyệt; chờ duyệt test khi xong toàn phần còn lại G01.
