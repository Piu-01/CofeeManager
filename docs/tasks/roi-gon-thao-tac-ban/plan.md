# Plan — Rơi gọn thao tác Bàn / Khu vực và chọn bàn ở POS

- Dựa trên: `requirements.md` (đã duyệt 20/11/2026)
- Quyết định bổ sung khi lập plan (người dùng chốt 20/11/2026):
  - Sơ đồ: **chỉ sửa/xóa ở chế độ Danh sách**; click card vẫn mở POS như cũ.
  - POS: chọn bàn trong ô chọn bàn khi kênh đang Mang đi/Giao hàng → **tự chuyển sang Tại quán** rồi mở bàn (vẫn hỏi xác nhận nếu có nháp chưa lưu).
- Không đổi SQL/procedure/contract. Không chạy đợt test chức năng cho tới khi được duyệt riêng.

## Bước 1 — Hạ tầng lưới dùng chung (icon thùng rác + double-click)

File:
- `src/CafeManager.WinForms/UI/OrbitDataGridView.cs`
- `src/CafeManager.WinForms/UI/OrbitRecordListControl.cs`

Việc làm:

1. `OrbitDataGridView.ApplyMetrics`: nhánh mặc định (profile `Tables`, `Areas`…) — nếu `Name`/`DataPropertyName` là `"Delete"` thì đặt header rỗng, căn giữa, bề rộng ~40px (dùng `Px`), style không sort.
2. `OrbitDataGridView.OnCellPainting` (nhánh chung `RowIndex >= 0 && ColumnIndex >= 0`): thêm trường hợp cột `"Delete"` → vẽ icon `trash` bằng `OrbitManagementTheme.DrawIcon`, màu `AppTheme.Error` khi hover dòng (dùng `hovered`/`hoveredColumn` đã có), còn lại `OrbitManagementTheme.Muted`. Không ảnh hưởng profile `OrderLines` (giữ `PaintOrderLineCell`).
3. `OrbitRecordListControl`:
   - Thêm `protected void AddRowDeleteColumn()` tạo `DataGridViewTextBoxColumn { Name = "Delete", HeaderText = "", DataPropertyName = "" }`, gắn một lần (chống trùng).
   - Thêm hook use-case chung: `protected Action<object>? RowDeleteRequested;` và `protected Action<object>? RowActivated;`.
   - Thêm handler dùng chung trong constructor:
     - `Grid.CellMouseClick`: nếu cột là `"Delete"`, không `busy`, có subscriber → `Grid.CurrentCell = row.Cells[0]` rồi `RowDeleteRequested(item)`.
     - `Grid.CellDoubleClick`: nếu `RowIndex >= 0`, cột không phải `"Delete"`, có subscriber → `RowActivated(item)`.
   - Không chép handler riêng vào từng màn.

Verify:
- `dotnet build` 0 error.
- Probe kỹ thuật (ngoài repo, như probe T34-02): dựng `OrbitRecordListControl` với 2 dòng fixture, gọi `AddRowDeleteColumn`, assert lưới có cột `Delete` bề rộng đúng, `CellMouseClick` cột Delete phát `RowDeleteRequested` đúng item, `CellDoubleClick` cột khác phát `RowActivated`; `busy` thì không phát.

## Bước 2 — Màn Bàn / Khu vực

File: `src/CafeManager.WinForms/Modules/Tables/TablesControl.cs`

Việc làm:

1. `TableList`:
   - Bỏ `AddAction("Chọn bàn", …)`, `AddAction("Trả bàn", …)`, `AddAction("Sửa", …)`, `AddAction("Xóa", …)`; giữ `AddAction("Thêm", …, ADMIN)`.
   - Xóa `SelectCurrent()`, `CloseTable()`, `IsActionAvailable(...)` (chỉ còn phục vụ "Trả bàn").
   - ADMIN: gọi `AddRowDeleteColumn()`; gán `RowDeleteRequested = item => _ = Delete((TableRow)item)` (bọc guard lỗi như `OrbitRecordListControl.Register` hiện có: `UserOperationException`/`UnauthorizedAccessException` hiển thị `State.Text`); gán `RowActivated = item => _ = Edit((TableRow)item)`.
   - Refactor `Edit(bool create)` → `Edit(TableRow? row)` (null = Thêm) và `Delete()` → `Delete(TableRow row)`; giữ nguyên nội dung dialog/normalize/reconcile.
   - Giữ card Sơ đồ: click vẫn gọi `SelectCode(row.Code)`; không thêm double-click trên card.
2. `AreaList`:
   - Bỏ `AddAction("Sửa")`, `AddAction("Xóa")`; giữ "Thêm".
   - `AddRowDeleteColumn()` + `RowDeleteRequested` → `Delete(AreaRow)`; `RowActivated` → `Edit(AreaRow)`; refactor Edit/Delete nhận tham số row.

Verify:
- Build 0 error.
- Probe/functional: ADMIN thấy đúng 1 action "Thêm" + cột icon; double-click dòng mở dialog Sửa đúng dòng; bấm icon thùng rác mở dialog Xóa đúng dòng; nhân viên (non-ADMIN) không có cột Delete, double-click không phản ứng, chỉ click card mở POS; click card vẫn mở POS với đúng mã bàn.

## Bước 3 — POS tự mở bàn khi chọn trong ô chọn bàn

File: `src/CafeManager.WinForms/Modules/Sales/SalesControl.cs`

Việc làm:

1. Bỏ field `selectTable`, dòng `selectTable = Action(...)`, và `selectTable` khỏi `tableHost.Controls.AddRange`.
2. Thêm cờ chống đệ quy: `private bool suppressTableOpen;` + helper `private void SetTableSelection(string? code)` gán `table.SelectedValue` trong `try/finally suppressTableOpen`.
3. Thay mọi chỗ gán bàn bằng code sang `SetTableSelection`: `LoadTables` (khôi phục lựa chọn), nhánh đổi kênh (`SelectedIndex = -1` / `SelectedValue = targetTable`), `StartNewOrderAsync` (`table.SelectedValue = free.Code`), `OpenTableAsync` (`table.SelectedValue = code`).
4. Thêm handler `table.SelectedValueChanged` bổ sung (cạnh handler `UpdateTableStatus` hiện có, gọi sau):
   - Bỏ qua nếu `suppressTableOpen || !initialized || busy || unknown || disposed`.
   - Bỏ qua nếu giá trị chọn null/không phải string.
   - Bỏ qua nếu trùng bàn hiện tại: `order?.Header.TableCode == code || (order is null && context?.TableCode == code && channel = Tại quán)`.
   - Có cờ `openingTable` chống chạy chồng; `SetBusy(true)` trong lúc mở rồi trả lại qua `Guard`.
   - Gọi `OpenTableAsync(code)`: nếu bàn có đơn → mở đơn; nếu trống → `ConfirmInputAsync` (giữ hộp hỏi nháp) rồi đặt context Tại quán — tức là tự chuyển kênh sang Tại quán như nút "Chọn bàn" cũ.
   - Không bỏ `public Task OpenTableAsync(string code)` (MainForm/TablesControl còn gọi).
5. Cập nhật chuỗi 3 chỗ còn "bấm Chọn bàn" (dòng 466, 477, 534) → "Chọn bàn trống khác hoặc chọn bàn đang có đơn trong ô chọn bàn để mở đơn hiện có."
6. `src/CafeManager.Models/Localization/Translations/en.json`: sửa key `"Chọn bàn trống khác rồi bấm Chọn bàn; nháp được giữ."` (dòng 124) theo nghĩa mới; thêm key cho chuỗi mới nếu tách thành 2 biến thể; key `"Chọn bàn"` giữ lại chỉ nếu còn dùng (kiểm tra sau khi xóa nút — nếu không còn dùng thì xóa key).

Verify:
- Build 0 error; grep không còn `selectTable` và không còn chuỗi "bấm Chọn bàn" trong `src/`.
- Functional (đợt test riêng): chọn bàn trống → POS vào đúng bàn, kênh Tại quán; chọn bàn đang có đơn → mở đơn; đang có nháp → hỏi xác nhận; đổi kênh Mang đi làm combo bỏ chọn và **không** tự mở bàn; bấm "Đơn mới" tự chọn bàn trống và **không** hỏi nháp; sau trả bàn/reset không tự mở lại.

## Bước 4 — Cập nhật tài liệu trong phạm vi

File:
- `docs/UX_QuanLiQuanCafe.md` (mục luồng màn Bàn/Khu vực và POS liên quan)
- `docs/tasks/g03-du-lieu-phuc-vu-ban-hang/t11-ban-khu-vuc/plan.md` (ghi chú thay đổi hành vi sau T34)
- `docs/tasks/roi-gon-thao-tac-ban/` thêm `test-results.md` khi tới đợt test

Việc làm: ghi ngắn gọn hành vi mới (không còn nút Chọn/Trả bàn ở màn Bàn; sửa/xóa qua dòng; POS tự mở bàn), đánh dấu ngày và trạng thái. Không sửa lịch sử test cũ.

Verify: đọc lại mục đã sửa, không mâu thuẫn với mô tả T34.

## Bước 5 — Build và bàn giao phần triển khai

- Build Debug (ưu tiên) qua `BaseOutputPath` tạm nếu bin bị app/VS khóa; ghi lại cách build.
- Không chạy đợt test chức năng/GUI cho tới khi người dùng duyệt riêng (post-change-test-workflow).
- Báo cáo: file đã đổi, kết quả compile, phần chưa kiểm (functional, DPI 125%, hình thức do người dùng tự kiểm), và nhắc phần UI/regression T34-02 còn NOT RUN vì bin bị khóa.

## Rủi ro và cách xử lý

- **Tự mở bàn lặp/hỏi nháp vô ích**: chặn bằng `suppressTableOpen` quanh mọi gán code + `openingTable` + so bàn hiện tại; verify riêng ca "Đơn mới", "đổi kênh", "LoadTables sau trả bàn".
- **Nuốt nháp khi chuyển bàn**: giữ `ConfirmInputAsync` như nút cũ; không có đường nào bỏ nháp im lặng.
- **Cột icon làm hẹp bảng**: cột Delete bề rộng cố định ~40px, các cột còn lại giữ nguyên; nếu tràn ngang, lưới vốn đã có scroll ngang.
- **Double-click vô tình kích hoạt khi click icon thùng rác hai lần nhanh**: handler Delete không đăng ký `RowActivated` cho cột Delete (đã loại trừ ở Bước 1).
- **Quyền**: icon và double-click chỉ gắn khi ADMIN; BLL vẫn chặn cuối.
