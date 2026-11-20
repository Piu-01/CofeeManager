# Kiểm tra kỹ thuật triển khai VI/EN

Ngày 10/11/2026. Đây là **lịch sử kiểm kỹ thuật trước nghiệm thu**. Đợt test sau đó
đã được duyệt và chạy, còn FAIL/BLOCKED, xem `report.md` và `bug-report.md`.
Các kết quả bên dưới thuộc bước 9/11 của plan, không nghiệm thu thay
WinForms thật, integration SQL hoặc G08/T31–T32.

## Source và phạm vi

- Repo root đã kiểm: `D:/Workspace/Nam_3/HK1/CoffeeManager`. HEAD `8a38f86`, working
  tree có thay đổi sẵn và cập nhật song song; không commit/push/reset/clean.
- Snapshot cuối: `runs/source-manifest.csv`, 272 file source/project/resource của
  app, test và audit. Đối chiếu sau lượt unit cuối: **0 file trong snapshot thay đổi**.
- SDK .NET 10.0.401, Windows, target app `net10.0-windows`. Unit dùng .NET 10 và
  VSTest 18.7.0. Không kết nối SQL, không ghi fixture vào DB development/DB đang dùng.

## Nội dung đã triển khai

- Models có `AppLanguage`, `LanguageService`, `L.T`/`L.F` và resource JSON nhúng;
  mặc định VI, fallback nguyên câu gốc, lookup chuẩn hóa CRLF/LF, format bất biến.
  Các giá trị số/ngày có format được định dạng `vi-VN` trước khi đưa vào câu dịch.
- File `%LocalAppData%/CoffeeManager/language.txt` theo Windows user; ghi qua file
  tạm + rename, lỗi đọc mặc định VI, lỗi ghi được trả về UI; path injectable cho test.
- Program đọc lựa chọn trước tạo form; mỗi lần quay về login đọc lại file. Login có
  VI/EN, cập nhật chữ tại chỗ và giữ input, khóa chọn ngôn ngữ khi đang xác thực.
- Thông tin hệ thống lưu ngôn ngữ cho phiên kế tiếp; Để sau giữ ngôn ngữ đang chạy,
  Đăng xuất ngay dùng guard/logout hiện có, không tự tạo lại module hoặc bỏ nháp.
- Dịch shell, dialog, native owner workflow, mọi phân hệ, dashboard/chart, preview/in,
  lỗi BLL/DAL, nhãn trạng thái. `SqlErrorMapper` lấy bản dịch tại lúc map lỗi thay vì
  cache ngôn ngữ từ lần khởi tạo static đầu tiên.
- Tách chữ khỏi logic: nav theo Tag/NavigationTarget, primary dialog theo IsPrimary,
  command của danh sách giữ giá trị riêng, nút đóng picker theo Name, Thu/Chi theo
  index→enum/code, cột công thức dùng khóa nội bộ ổn định. Role/hợp đồng/kênh/loại xuất
  và các mã request giữ nguyên. Chỉ cột trạng thái/loại được map nhãn; không dịch tên
  khách/món/ghi chú trùng chữ của mã trạng thái.
- Metadata báo cáo từ SQL (DateBasis/Note/Coverage) và nhãn nhóm thu bán được dịch
  tại lớp hiển thị qua danh sách mẫu rõ ràng; dữ liệu nghiệp vụ không đưa qua dịch.
- Không sửa SQL/migration/contract vì localization, không đổi định dạng VND/ngày,
  không đổi framework hoặc thêm dependency runtime/NuGet.

## Lệnh và kết quả cuối

| Kiểm tra | Lệnh | Kết quả / bằng chứng |
|---|---|---|
| Debug | `dotnet build CafeManager.slnx --no-restore -m:1 -c Debug -v:q` | PASS, 0 warning / 0 error; `build-debug.log` |
| Release | `dotnet build CafeManager.slnx --no-restore -m:1 -c Release -v:q` | PASS, 0 warning / 0 error; `build-release.log` |
| Build unit riêng | `dotnet build tests/CafeManager.Tests/CafeManager.Tests.csproj --no-restore -m:1 -o artifacts/localization-vi-en/unit -v:q` | PASS, 0 warning / 0 error; `build-unit.log` |
| Unit thuần | Lệnh bên dưới | **152 PASS, 0 FAIL, 0 SKIPPED**, gồm 10 ca mới; `runs/unit-final.trx` |
| Audit | `./tools/localization/audit.ps1` | **1.519 key dùng / 1.519 key EN, 0 vi phạm**; `audit.log`, `inventory.csv` |

```powershell
dotnet vstest artifacts/localization-vi-en/unit/CafeManager.Tests.dll '--TestCaseFilter:FullyQualifiedName!~CafeManager.Tests.Integration&FullyQualifiedName!~CafeManager.Tests.G02' '--Logger:trx;LogFileName=unit-final.trx' '--ResultsDirectory:docs/tasks/localization-vi-en/runs'
```

Filter loại các lớp integration/G02 dùng SQL; vẫn chạy các rules thuần của
Auth/Catalog/G03/Sales/Finance/Inventory/HumanResources/Assets/Reports và 10 ca mới.

Audit phân tích cú pháp, không đếm comment; inventory hiện có 133 file source.
132 literal được giữ có lý do (command/profile, pattern so sánh, định danh control,
code dữ liệu); không dùng bản dịch làm giá trị gửi SQL. Audit phát hiện key thiếu,
thừa, trùng, bản dịch trống và placeholder lệch; **không chứng nhận runtime không
còn chuỗi động hoặc chứng nhận layout native**.

`git diff --check` trên working tree chung còn báo một dòng trắng EOF tại
`src/CafeManager.WinForms/UI/OrbitDataGridView.cs:398`; không thay source sau snapshot
unit chỉ để dọn whitespace. Các cảnh báo LF→CRLF là cấu hình working tree Git,
không phải cảnh báo biên dịch. Không ghi nhận diff-check là PASS.

## Sự cố môi trường / thay đổi song song

- Testhost trong sandbox không kết nối được sau 90 giây; lượt đó **ABORTED**, không
  tính FAIL nghiệp vụ hay PASS. Sau khi chạy testhost ngoài sandbox, 10 ca mới PASS
  và lượt unit cuối 152/152 PASS. Bằng chứng chính là `runs/unit-final.trx`;
  `runs/sandbox-testhost-aborted.trx` giữ kết quả môi trường cũ.
- Build đầu thấy `OrbitTabControl.cs:19` dùng `Padding.Empty` bị hiểu thành Point.
  Đã thông báo và người dùng đồng ý sửa một dòng. Trước khi agent sửa, source được
  cập nhật song song thành `new Padding(0)` và build hết lỗi; giữ cách sửa tương đương
  này, không ghi nhận là agent tự áp dụng fix. Dashboard/POS cập nhật song song cũng
  được giữ và ghép bản dịch; không ghi đè bằng bản source cũ.

## Phần chưa chạy / bước tiếp

- **NOT RUN**: nghiệm thu chức năng WinForms VI/EN, login/logout/native modal, giao
  dịch SQL thật/đồng thời/timeout, in thực tế và bounds/layout kỹ thuật DPI 125% tại
  1280×720/1366×768. Đã chuẩn bị ma trận trong `test-plan.md`, chờ duyệt một đợt.
- **SKIPPED theo phân công**: hình thức UI do người dùng tự kiểm; agent không chụp
  ảnh đối chiếu, test hover/spacing hoặc ghi PASS thị giác.
- **NOT RUN**: DPI 100%/150%; không đổi Windows Scale, không suy kết quả từ source.
- `StartupForm` không phải form được Program mở: đã compile/dịch source, chưa chạy
  thực tế. Native Print/File dialogs còn dùng ngôn ngữ Windows; chỉ chữ do app tạo
  thuộc từ điển VI/EN.
- Không dùng PASS cũ của G08 để chứng nhận source hoặc package sau localization.
  Bản publish cũ chưa chứa tính năng; publish/regression cuối vẫn thuộc bàn giao G08.

## Cách dùng và bảo trì

Chọn VI/EN ở login để áp dụng ngay. Trong app vào Thông tin hệ thống → Ngôn ngữ →
Áp dụng → Đăng xuất ngay hoặc Để sau. Không đổi ngôn ngữ theo Windows tự động.
Muốn thêm chữ mới: dùng `L.T`/`L.F`, cập nhật resource EN, chạy audit và unit kỹ thuật;
không bọc mã logic/tên người dùng nhập. Xem `tools/localization/README.md`.
