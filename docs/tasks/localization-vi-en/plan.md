# Đa ngôn ngữ VI/EN — plan triển khai

Ngày soạn: 09/11/2026. Người dùng giao triển khai theo plan ngày 10/11/2026.
Trạng thái: **Đang làm — LOC-B01–B03 đã sửa/regression PASS; nghiệm thu native còn thiếu**.
Chi tiết kiểm tra kỹ thuật: `technical-checks.md`; đợt test riêng: `test-plan.md`.
Requirements đã được duyệt ngày 09/11/2026, áp dụng các đề xuất mặc định:

- Áp dụng kiểu (B): đổi ngay ở màn đăng nhập; trong app lưu lựa chọn rồi đăng xuất/khởi
  động lại để áp dụng. Không dựng lại module đang mở trong phiên.
  **Cập nhật 10/11/2026:** thay bằng (A) tại `docs/tasks/localization-live-apply/` —
  đổi ngay trong phiên bằng cách dựng lại shell, dùng lại guard nháp hiện có.
- Dịch cả in hóa đơn/preview và thông báo lỗi người dùng nhìn thấy.
- Giữ định dạng tiền `81.000 ₫`, ngày `dd/MM/yyyy` ở cả hai ngôn ngữ.
- Lưu lựa chọn theo Windows user bằng file local, không thêm migration/DB.
- Chia 2 đợt nội dung trong cùng một lần triển khai: đợt 1 (login/shell/POS/lỗi dùng
  chung) là mốc kiểm được sớm; đợt 2 phần còn lại. Không cắt bớt phạm vi requirements.

## 0. Nguyên tắc bắt buộc cho mọi bước

1. Chỉ bọc chuỗi **hiển thị**. Không bọc chuỗi dùng làm khóa logic, mã trạng thái,
   `DataPropertyName`, `Name` control, mã quyền, mã chứng từ, tên enum.
2. Không đổi SQL, procedure, bảng/cột, migration, contract request/response.
3. Không đổi nghiệp vụ, thứ tự thao tác, phím tắt, validation, quyền, luồng nháp/unknown.
4. Mặc định `vi`; thiếu bản dịch thì trả lại đúng câu tiếng Việt, không trả key/rỗng.
5. Mỗi bước sửa xong phải build được và không làm đổi hành vi đang có.
6. Không commit/push trừ khi người dùng giao riêng.
7. Không chạy đợt test nghiệm thu trong lúc triển khai; test chỉ chạy sau khi người dùng
   duyệt đợt test.

## 1. Kiến trúc localization lõi

### 1.1. Thành phần mới (tầng thấp, không phụ thuộc UI/SqlClient)

Thư mục `src/CafeManager.Models/Localization/`:

- `AppLanguage.cs`: enum `Vietnamese`, `English` + mã `vi`/`en`.
- `LanguageService.cs`: giữ ngôn ngữ hiện tại (mặc định `Vietnamese`), `Set`, `Reset`;
  không đọc `CurrentUICulture` của Windows để test ổn định.
- `L.cs`: API dùng chung:
  - `L.T(string vi)` — tra từ điển, fallback chính chuỗi VI.
  - `L.F(string viFormat, params object?[] args)` — tra format rồi `string.Format`
    với `CultureInfo.InvariantCulture` cho phần số/ngày đã được format sẵn từ trước.
  - Chuẩn hóa `\r\n`/`\n` khi tra để tránh lệch khóa.
- `Translations/en.json`: từ điển `"câu tiếng Việt": "English"`, UTF-8, embedded
  resource trong `CafeManager.Models.csproj` (`LogicalName` cố định). Không thêm NuGet;
  dùng `System.Text.Json` sẵn có.
- `LocalizationAudit` (chỉ dùng cho test/script): đếm key, phát hiện key thiếu/trùng,
  placeholder lệch.

Vì sao đặt ở `Models`: cả `DataAccess`, `Business`, `WinForms` đều tham chiếu `Models`,
không tạo project mới và không phá layering.

### 1.2. Quy ước khóa

- Khóa là **nguyên câu tiếng Việt** đang hiển thị, giữ nguyên dấu, khoảng trắng, `&`
  mnemonic và `\r\n`.
- Chuỗi nội suy đổi sang `L.F("...{0}...", value)`. Không ghép `L.T("...") + biến` nếu
  bản dịch cần đảo trật tự từ.
- Chuỗi nhiều dòng giữ nguyên xuống dòng; helper chuẩn hóa khi tra.
- Tên riêng/mã/thương hiệu không đưa vào từ điển: `CoffeeManager`, `ADMIN/CASH/STORE`,
  `PT001`, `HD...`, `Full-time/Part-time`, `Thu/Chi` (enum), mã trạng thái DB.
- Câu trùng nhau nhưng khác ngữ cảnh: ghi chú trong `en.json` bằng khóa hậu tố
  `#ngữ-cảnh` nếu buộc phải tách; mặc định dùng chung nếu nghĩa giống.

### 1.3. Verify bước 1

- Unit test: `T` trả VI khi chưa có bản dịch; trả EN khi có; `F` giữ đúng số placeholder;
  `\r\n` và `\n` tra cùng một khóa; đổi ngôn ngữ rồi trả lại VI.
- Build `CafeManager.Models` + toàn solution pass.
- Chưa thay đổi bất kỳ chuỗi UI nào ở bước này.

## 2. Lưu lựa chọn ngôn ngữ + nạp lúc khởi động

### 2.1. File/khu vực

- Mới: `src/CafeManager.WinForms/Configuration/LanguagePreference.cs`
  - Đọc/ghi `%LocalAppData%\CoffeeManager\language.txt` (`vi`/`en`), ghi atomic
    (temp + replace), không chứa credential.
  - File thiếu/hỏng/quyền lỗi → trả `vi`, không crash; lỗi ghi trả kết quả để UI báo.
  - Có đường dẫn injectable để unit test không đụng `%LocalAppData%` thật.
- Sửa `src/CafeManager.WinForms/Program.cs`: đọc preference trước khi tạo form đầu tiên,
  `LanguageService.Set(...)`. Không đổi luồng `--setup`, connection, quyền.
- Sửa `src/CafeManager.WinForms/ApplicationSessionContext.cs`: mỗi lần `ShowLogin()`
  (khởi động và sau logout) đọc lại preference và set `LanguageService` trước khi tạo
  `LoginForm`. Như vậy lựa chọn đã lưu được áp dụng ở lần đăng nhập kế tiếp.

### 2.2. Verify bước 2

- Test đọc/ghi/roundtrip, file hỏng, thư mục không ghi được.
- Chạy app: chưa có file → VI; ghi `en` thủ công → login hiện EN; xóa file → VI.
- Không có thay đổi DB/network.

## 3. Điểm chọn ngôn ngữ trên UI

### 3.1. Màn đăng nhập

- `Forms/LoginForm.cs` + `LoginForm.Designer.cs`: thêm nút chuyển `VI | EN` (control
  Orbit, không đổi bố cục chính, giữ tab order và Enter/không tự đăng nhập).
- Khi đổi: ghi preference, `LanguageService.Set`, gọi `LoginForm.Relocalize()` cập nhật
  text tại chỗ (không dựng lại form, không mất username đang gõ, không tự submit).
- Toàn bộ chuỗi login dùng `L.T/L.F`.

### 3.2. Trong app

- `Modules/System/SystemInfoControl.cs`: thêm mục "Ngôn ngữ / Language" với lựa chọn
  VI/EN và nút Áp dụng.
- Áp dụng: ghi preference; **không đổi ngôn ngữ UI đang mở**. Hiện thông báo:
  "Đã lưu. Ngôn ngữ áp dụng khi đăng xuất hoặc mở lại ứng dụng." kèm nút
  "Đăng xuất ngay" / "Để sau".
- "Đăng xuất ngay" đi qua luồng logout hiện có (`MainForm.Logout`, hủy phiên, quay lại
  LoginForm); không tự đóng app, không bỏ qua kiểm tra nháp/busy của module đang mở.
- Không đặt nút đổi ngôn ngữ trong dialog đang nhập dữ liệu.

### 3.3. Verify bước 3

- Đổi VI→EN→VI ở login: chữ đổi đúng, username/password giữ nguyên, không submit.
- Lưu EN trong app → đăng xuất → login EN; mở lại app → EN.
- Hủy "Để sau" → phiên hiện tại vẫn VI, không lẫn EN ở dialog mới mở.
- Không thay đổi quyền/phiên/logout hiện có.

## 4. Tách "chữ hiển thị" khỏi "khóa logic" (làm trước khi dịch)

Đây là bước bắt buộc để tránh dịch xong làm sai nghiệp vụ. Các điểm đã xác định:

| File | Vấn đề hiện tại | Cách sửa |
|---|---|---|
| `UI/OrbitDialogLayout.cs` | `button.Text is "Lưu" or "Xác nhận"` quyết định nút primary | Thêm cờ `IsPrimary`/style khi tạo nút; không so text |
| `Forms/MainForm.cs` | `button.Text switch` chọn icon menu | Chọn icon theo `NavigationTarget`/Tag |
| `UI/OrbitManagementLayout.cs` | `button.Text.Length > 5` / `Text is "Mã số","Số dư"` quyết định layout rộng | Dùng cờ `Wide` hoặc `Tag` do caller đặt |
| `Forms/CustomerPickerDialog.cs` | tìm nút Đóng theo text | Giữ tham chiếu nút hoặc `Name` cố định |
| `UI/OrbitRecordListControl.cs` | `text == "Thêm"` đổi caption | Truyền cờ `IsCreate` riêng |
| `Forms/CatalogEditorDialog.cs` | `Enum.Parse<ExpenseType>(typeInput.Text)` | Item combo là wrapper `Code+Label`; parse `Code` |
| `Modules/Finance/FinanceControl.cs` | gửi `type.Text` ("Thu"/"Chi") vào filter | Map `SelectedIndex` → `"Thu"/"Chi"` |
| `Modules/HumanResources/EmployeesControl.cs` | gửi `contract.Text`, `role.Text` | Map index/code; không dịch item mã (`ADMIN`, `Full-time`) |
| `Modules/Inventory/MaterialsControl.cs` | sentinel "Tất cả đơn vị" trong combo đơn vị | Giữ index 0 là sentinel, không dùng text làm giá trị |
| `Modules/Inventory/StockIssueEditorDialog.cs` | index → mã `"Huy do hong"/"Tra hang"` (đang đúng) | Giữ nguyên, chỉ dịch nhãn hiển thị |
| `Modules/Loyalty/LoyaltyControl.cs`, `Modules/Menu/ProductsControl.cs` | index → mã trạng thái (đang đúng) | Giữ nguyên, chỉ dịch nhãn |
| `Modules/Reports/ReportsControl.cs` | kênh map theo index (đang đúng); `ReportKind` hiển thị qua `Label` | Dịch `Label`, giữ giá trị enum |
| `Forms/BootstrapForm.cs` | mode theo `SelectedIndex` (đang đúng) | Chỉ dịch item hiển thị |

Thêm helper nhỏ `UI/ChoiceItem` (hoặc tương đương) gồm `Code` + `Label`, dùng cho combo
có cả hiển thị và giá trị; chỉ tạo khi thực sự cần, không làm framework chung.

### Verify bước 4

- Unit test/kiểm tra thủ công: filter Thu/Chi, role ADMIN/CASH/STORE, contract
  Full-time/Part-time, loại xuất Hủy/Trả, trạng thái bán/ngừng bán, kênh báo cáo vẫn
  gửi đúng mã như trước khi đổi ngôn ngữ.
- Nút Lưu/Xác nhận vẫn là primary; icon menu không đổi; layout nút không đổi khi chữ VI.

## 5. Localization shell + control dùng chung (đợt 1)

### 5.1. File

- `Forms/MainForm.cs`, `MainForm.Designer.cs`, `Navigation/NavigationController.cs`
  (menu, nhóm, breadcrumb, title, trạng thái, confirm rời nháp).
- `UI/` các control có chữ: `OrbitSidebar`, `OrbitHeader`, `OrbitEmptyState`,
  `OrbitNotice`, `OrbitPagination`, `OrbitListWorkspace`, `OrbitRecordListControl`,
  `OrbitDataGridView`, `OrbitSearchBox`, `OrbitFilterBox`, `OrbitSectionTabs`,
  `OrbitManagementTabs`, `OrbitViewSwitch`, `OrbitDialogLayout`, `OrbitDialogHost`,
  `OrbitDialogField`, `OrbitDateField`, `OrbitDateRangeField`, `OrbitTimeInput`,
  `OrbitTableCard`, `OrbitSummaryPanel`, `OrbitSurface` (nếu có chữ), `SalesUi`,
  `G05Display`, `DocumentLinks`.
- `Forms/OrbitDialogForm.cs`, `OrbitConfirmationDialog.cs`, `OperationEditorDialog.cs`,
  `Forms/InvoicePrintPreviewForm.cs` (phần caption/nút chung).

### 5.2. Nội dung

- Dịch tiêu đề/menu/nhóm/eyebrow/description/empty state/status/loading/confirm/
  placeholder/tooltip/AccessibleName do các control này tạo.
- Giữ nguyên `DataPropertyName`, `Name`, mã profile enum, icon key.
- Với `AccessibleName` đang lấy từ `Text`: sau khi dịch, accessible name theo ngôn ngữ
  hiện tại; không hardcode lại tiếng Việt.
- `OrbitRecordListControl` profile strings (Menu/Tables/Customers/Promotions/...) dịch
  theo profile; không đổi logic profile.

### 5.3. Verify bước 5

- Build pass; mở shell ở EN: sidebar, topbar, empty state, pagination, dialog xác nhận
  hiển thị EN; ở VI không lệch so với hiện tại.
- Không đổi kích thước cố định gây cắt chữ ở 125% (kiểm ở bước 12).
- Dialog confirm vẫn chặn phím/nháp như cũ.

## 6. Đợt 1 — Đăng nhập, bootstrap, lỗi dùng chung, POS/thanh toán, in

### 6.1. Đăng nhập/bootstrap/startup

- `Forms/LoginForm.cs/.Designer.cs`, `Forms/DevelopmentQuickLoginDialog.cs`,
  `Forms/BootstrapForm.cs/.Designer.cs`, `Forms/StartupForm.cs/.Designer.cs`.
- `StartupForm` hiện không được `Program` khởi tạo; vẫn dịch để đủ phạm vi, chỉ verify
  biên dịch + rà soát, không tính là màn chạy thực tế trong biên bản.

### 6.2. Lỗi dùng chung

- `src/CafeManager.DataAccess/SqlErrorMapper.cs`: 141 thông báo → `L.T`/`L.F`.
- `src/CafeManager.Business/**` các `throw new ArgumentException/UserOperationException`
  và message trả về UI → `L.T`/`L.F`.
- `src/CafeManager.Models/States/SalesStates.cs` + các label trạng thái → `L.T`.
- Không dịch message log kỹ thuật nội bộ không hiển thị; rà từng chỗ trước khi bọc.
- Giữ nguyên mã lỗi SQL, `Number`, `Kind`, cờ `writing/sent`; chỉ đổi phần chữ.

### 6.3. POS/thanh toán/hóa đơn

- `Modules/Sales/SalesControl.cs`, `OrderPanel.cs`, `ProductCardControl.cs`,
  `SalesUi.cs`, `CheckoutDialog.cs`, `CheckoutDialog.Layout.cs`,
  `InvoicesControl.cs`, `InvoiceDetailDialog.cs`, `OpenOrdersDialog.cs`,
  `OrderHeaderDialog.cs`, `DeliveryControl.cs`, `CustomersControl.cs`,
  `TablesControl.cs`, `LoyaltyControl.cs`, `CustomerPickerDialog.cs`.
- `Modules/Home/SessionHomeControl.cs`, `Modules/Home/AdminDashboardControl.cs`
  (thuộc shell/đợt 1 vì là màn mở đầu).
- `Printing/InvoicePrintDocument.cs`, `InvoicePrinting.cs`, `Forms/InvoicePrintPreviewForm.cs`.
  Dịch tiêu đề, cột, ghi chú in, "IN LẠI", "Trang", lỗi giấy; giữ định dạng tiền/ngày,
  giữ font Segoe UI và khổ A4.

### Verify bước 6

- Ở EN: login, menu, POS thêm món/sửa số lượng/ghi chú, F3/F4/F8, checkout, xác nhận,
  in/preview hiển thị EN; không lẫn VI do app sở hữu.
- Ở VI: luồng và chữ như hiện tại.
- Gửi filter/mã trạng thái vẫn đúng sau bước 4.
- Không đổi phím tắt, không tự chốt tiền bằng Enter.

## 7. Đợt 2A — Danh mục, menu/công thức, kho, thu chi, công nợ

### File

- `Modules/Catalogs/CatalogsControl.cs/.Designer.cs`, `Forms/CatalogEditorDialog.cs/.Designer.cs`.
- `Modules/Menu/ProductsControl.cs`.
- `Modules/Inventory/MaterialsControl.cs/.Designer.cs`, `InventoryControl.cs`,
  `PurchasesControl.cs`, `PurchaseEditorDialog.cs`, `StockIssuesControl.cs`,
  `StockIssueEditorDialog.cs`, `StocktakesControl.cs`, `StocktakeEditorDialog.cs`.
- `Modules/Finance/FinanceControl.cs`, `FinanceEntryDialog.cs`, `FinanceDetailDialog.cs`,
  `SupplierDebtsControl.cs`, `DebtPaymentDialog.cs`.
- `Forms/OperationEditorDialog.cs` phần dùng chung cho các editor (nếu chưa làm ở bước 5).

### Verify bước 7

- Ở EN: mọi bảng, filter, editor, confirm, validation, empty/loading/error của các màn
  này hiển thị EN; tên danh mục/nguyên liệu/món/nhà cung cấp lấy từ DB vẫn nguyên.
- Ở VI: không lệch.
- Các filter gửi đúng mã (`Thu/Chi`, loại xuất, trạng thái) sau bước 4.
- Mở/sửa/nháp/hủy/đóng dialog giữ nguyên hành vi.

## 8. Đợt 2B — Nhân sự, lương, chấm công, tài sản, báo cáo, hệ thống

### File

- `Modules/HumanResources/HumanResourcesControl.cs`, `EmployeesControl.cs`,
  `AttendanceControl.cs`, `PayrollControl.cs`, `PayrollDetailDialog.cs`,
  `HrEditorDialog.cs`.
- `Modules/Assets/AssetsControl.cs`, `AssetEditorDialog.cs`, `AssetDetailDialog.cs`,
  `AssetPickerDialog.cs`.
- `Modules/Reports/ReportsControl.cs`, `ReportSourceDialog.cs`,
  `Modules/Reports/ReportSourceNavigation.cs` (nếu có chữ).
- `Modules/System/SystemInfoControl.cs` (mục ngôn ngữ + thông tin schema).

### Verify bước 8

- Ở EN: bảng lương/chấm công/tài sản/báo cáo/nguồn chứng từ hiển thị EN; tiêu đề cột
  theo `DataPropertyName` vẫn map đúng sang nhãn EN.
- Ở VI: không lệch.
- Giá trị enum/tháng/năm/mã nhân viên/mã chứng từ không bị dịch.
- Báo cáo vẫn gửi đúng `ReportKind`, kênh, mã nguồn.

## 9. Hoàn tất từ điển + audit

### 9.1. Inventory

- Sinh `docs/tasks/localization-vi-en/inventory.csv` từ source: file, dòng, câu VI,
  bản EN, trạng thái (`translated`, `keep`, `db-data`, `comment`).
- Quy tắc `keep`: tên riêng/mã/enum/khóa logic; ghi lý do từng nhóm.
- Không đưa comment vào inventory.

### 9.2. Audit

- Script `tools/localization/audit.ps1` (chỉ đọc): quét `.cs` trong `src`, bỏ comment
  (`//`, `///`), tìm literal còn dấu tiếng Việt; xuất bảng theo file/dòng. Mục tiêu
  cuối: **0** dòng hiển thị còn tiếng Việt ngoài allowlist.
- Allowlist tối thiểu: `CoffeeManager`, mã ASCII, tên riêng DB, chuỗi test.
- So khớp inventory với `en.json`: không key chết, không key thiếu, không value rỗng,
  placeholder `{n}` khớp giữa VI/EN.

### Verify bước 9

- Audit 0 vi phạm ngoài allowlist (hoặc danh sách allowlist được duyệt rõ).
- Unit test parity pass.
- Không còn `L.T` trả key/rỗng ở các màn đã liệt kê.

## 10. Layout kỹ thuật cho tiếng Anh (125%)

- Rà các chỗ dùng chiều dài chữ cố định: menu 168px sidebar 200px, nút dialog,
  `OrbitManagementLayout` wide, grid cột `AutoSizeMode` cố định, status/notice một dòng.
- Cho phép `AutoEllipsis`/`AutoSize`/tăng chiều cao hàng ở chỗ cần; không đổi theme/màu.
- Kiểm ở 1280×720 và 1366×768, DPI 125% (theo phân công agent); 100%/150% ghi NOT RUN,
  không đổi Windows Scale.
- Không chấp nhận nút bị cắt chữ, dialog tràn WorkingArea, text chồng lên nhau ở EN.

### Verify bước 10

- Ảnh chụp/ghi chú kỹ thuật lưu trong `docs/tasks/localization-vi-en/` (chỉ là bằng chứng
  layout kỹ thuật, không phải nghiệm thu hình thức).
- Không đổi hành vi co giãn/DPI hiện có.

## 11. Build + unit test cuối

- `dotnet build` Debug và Release toàn solution; `dotnet test` project
  `tests/CafeManager.Tests` (unit, không cần SQL) pass.
- Thêm test:
  - `LocalizationTests`: fallback, EN, format, newline, parity, placeholder.
  - `LanguagePreferenceTests`: roundtrip, file hỏng, đường dẫn không ghi được.
  - `CodeMappingTests` (nếu tách helper combo): mã `Thu/Chi`, role, contract, loại xuất.
- Không sửa test cũ để né lỗi; test cũ mặc định chạy VI nên không đổi expected.
- Ghi log build/test vào `docs/tasks/localization-vi-en/technical-checks.md`.

### Verify bước 11

- Build 0 error; unit test pass; không cảnh báo mới do thay đổi này (trừ cảnh báo
  NuGet/network đã có).
- Không có migration/SQL file mới.

## 12. Chuẩn bị đợt test (chưa chạy)

Tạo `docs/tasks/localization-vi-en/test-plan.md` gồm:

- Ma trận màn hình × VI/EN × trạng thái (loading/rỗng/lỗi/xác nhận/kết quả).
- Ca chức năng trọng yếu: login/logout/quyền; POS nháp/F3/F4/F8/checkout/in; kho
  nhập/xuất/kiểm kê; thu chi/trả nợ; nhân sự/lương/chấm công; tài sản; báo cáo.
- Ca chuyển ngôn ngữ: login, lưu preference, logout, mở lại app, file hỏng.
- Ca "không lẫn VI": mở từng màn EN và rà chuỗi app sở hữu.
- Ca layout 125% tại 1280×720/1366×768.
- Ghi rõ 100%/150% DPI NOT RUN và hình thức do người dùng tự kiểm.
- Chờ người dùng duyệt đợt test riêng trước khi chạy; không tự chạy nghiệm thu.

## 13. Rủi ro và cách xử lý

| Rủi ro | Xử lý |
|---|---|
| Dịch làm đổi giá trị gửi service (Thu/Chi, role, contract, loại xuất) | Bước 4 tách code/label trước; test mapping; ca hồi quy riêng |
| Chuỗi trùng nghĩa khác ngữ cảnh | Inventory + ghi chú ngữ cảnh; tách khóa hậu tố khi cần |
| Bỏ sót chuỗi trong Designer/nội suy | Audit script + rà từng màn; test "không lẫn VI" |
| Tiếng Anh dài làm vỡ layout 125% | Bước 10; ưu tiên AutoSize/AutoEllipsis, không đổi theme |
| Lẫn ngôn ngữ trong một phiên do đổi giữa chừng | Kiểu B: chỉ áp dụng lúc login/khởi động; không đổi `LanguageService` khi đang mở module |
| Lỗi DAL/BLL bị dịch sai làm test cũ fail | Mặc định VI; test cũ không đổi expected; kiểm riêng bản EN |
| In hóa đơn sai font/khổ | Giữ Segoe UI/A4; kiểm preview EN/VI, không đổi số liệu |
| Bằng chứng G08 cũ không còn hiệu lực | Ghi rõ trong hồ sơ; chạy lại regression ở đợt test riêng sau duyệt |

## 14. Đầu ra bàn giao của task

- Source đã triển khai theo các bước trên (không commit).
- `docs/tasks/localization-vi-en/requirements.md` (đã duyệt), `plan.md` (file này),
  `inventory.csv`, `technical-checks.md`, `test-plan.md`.
- Từ điển `Translations/en.json` + test localization.
- Ghi chú các điểm chưa làm được/blocker (nếu có), không tự đánh PASS nghiệm thu.

## 15. Thứ tự thực hiện và điểm dừng

1. Bước 1 → 2 → 3 → 4 (nền tảng, chưa dịch module).
2. Bước 5 → 6 (đợt 1).
3. Bước 7 → 8 (đợt 2).
4. Bước 9 → 10 → 11 (audit, layout, build/test).
5. Bước 12: dừng, báo cáo và **chờ duyệt đợt test**. Không chạy test nghiệm thu,
   không sửa bug ngoài phạm vi đã duyệt.
