# T07-HOME-20261110 — Bỏ Trang phiên cho CASH/STORE

**Ngày:** 10/11/2026
**Hướng đã được duyệt:** CASH/STORE không còn nút Trang phiên, vào thẳng Bán
hàng/Kho; ADMIN giữ nút home với nhãn "Tổng quan"; SessionHomeControl giữ làm
fallback, không xóa file.

## Thay đổi source

| File | Nội dung |
|---|---|
| `src/CafeManager.WinForms/Forms/MainForm.Designer.cs` | Nhãn `homeButton` đổi "Trang phiên" → `L.T("Tổng quan")`. |
| `src/CafeManager.WinForms/UI/OrbitSidebar.cs` | Thêm `HomeVisible` (mặc định true); khi false thì ẩn nút home và đặt chiều cao hàng menu đầu về 0. |
| `src/CafeManager.WinForms/Forms/MainForm.cs` | `sidebar.HomeVisible = session.Identity.Role == "ADMIN";`; màn đích khi mở: CASH→Sales, STORE→Inventory, còn lại→Home (tôn trọng `initialTarget`). |
| `src/CafeManager.WinForms/Program.cs` | Home còn ADMIN→AdminDashboardControl, role khác→SessionHomeControl (fallback khi bị điều hướng Home từ nơi khác). |
| `src/CafeManager.Models/Localization/Translations/en.json` | Thêm `"Tổng quan": "Overview"` theo thứ tự ordinal sẵn có, cùng các key còn thiếu của AlertRow/POS mà audit phát hiện (" · Mở ", câu chọn bàn trống, "Đã hiểu"). |

Không đổi quyền BLL, NavigationController, SQL/DAL, module nghiệp vụ hoặc
cơ chế xác nhận nháp.

## Kiểm kỹ thuật

| Mã | Kiểm | Kết quả |
|---|---|---|
| H01 | Build Release `CafeManager.WinForms` sau thay đổi | PASS — 0 warning, 0 error |
| H02 | `en.json` parse hợp lệ, có key "Tổng quan" = "Overview" | PASS — node JSON.parse OK |
| H03 | Build Debug | BLOCKED — file `bin\Debug\net10.0-windows\CafeManager.Models.dll` bị khóa bởi CafeManager.WinForms (PID 33760) và Microsoft Visual Studio (PID 36512); không kill tiến trình theo AGENTS.md |
| H04 | `tools/localization/audit.ps1` | PASS — 1527 used / 1527 dictionary key, 0 violation (trước khi thêm key còn 8 missing) |

## Kiểm native (chờ người dùng)

| Mã | Thao tác | Expected | Kết quả |
|---|---|---|---|
| R01 | Đăng nhập ADMIN | Sidebar có nút "Tổng quan" và mở AdminDashboardControl | NOT RUN |
| R02 | Đăng nhập CASH | Không có nút home; mở thẳng Bán hàng; menu còn Bán hàng, Lịch sử hóa đơn, Giao hàng, Khách hàng, Tồn phục vụ bán | NOT RUN |
| R03 | Đăng nhập STORE | Không có nút home; mở thẳng Kho; menu còn Kho, Nguyên liệu / Tồn, Menu / Công thức, Công nợ | NOT RUN |
| R04 | CASH/STORE: Tab/Shift+Tab trong menu | Không dừng ở nút home đã ẩn; nút đầu tiên của nhóm đầu nhận focus | NOT RUN |
| R05 | ADMIN chuyển ngôn ngữ EN | Nút hiển thị "Overview" | NOT RUN |

- Môi trường dự kiến: WinForms thật, DB development schema029, DPI 125%, 1280×720
  và 1366×768.
- DPI 100%/150%: NOT RUN, không đổi Windows Scale.
- Hình thức do người dùng tự kiểm; hồ sơ này không nghiệm thu visual.
- H03 chưa xác nhận Debug build sau khi đóng app; cần chạy lại `dotnet build -c Debug`
  trước khi bàn giao Debug binary.

## Rủi ro

- Ẩn nút home không xóa route `NavigationTarget.Home`; nếu module khác điều hướng
  về Home thì CASH/STORE vẫn thấy SessionHomeControl (fallback có chủ đích).
- `HomeVisible` thay đổi sau khi sidebar dựng nhóm; không được gọi lại
  `InitializeNavigation` sau đó vì sẽ dựng lại control.
