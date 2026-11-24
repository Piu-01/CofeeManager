# Bug: UI 3 tab Nhập hàng / Xuất kho / Kiểm kê trong màn Kho lệch chuẩn danh sách chung

- Trạng thái: Đã sửa theo hướng được duyệt (23/11/2026). Biên dịch PASS; chưa chạy
  đợt test chức năng (chờ duyệt), hình thức do người dùng kiểm trên app.
- Ngày báo: 23/11/2026
- Người báo: người dùng (2 ảnh màn hình Kho → Nhập hàng, kèm yêu cầu bỏ nút
  Tìm/Tải lại và Xem chi tiết, sửa ô tìm kiếm, bỏ khoảng trống, sửa hộp chi tiết)
- Màn hình: `Modules/Inventory/InventoryControl.cs` → `PurchasesControl`,
  `StockIssuesControl`, `StocktakesControl` và 2 hộp `PurchaseDetailDialog`,
  `StockIssueDetailDialog`.

## 1. Hiện tượng

1. Bảng danh sách chỉ chiếm khoảng 60% bề ngang card; phần còn lại là vùng trống
   lớn bên phải (ảnh 2). Cột `MÃ` bị cắt thành `PN2…` dù mã phiếu chỉ ~10 ký tự.
2. Card có một dải trống cao ~46–49px nằm giữa ô tìm kiếm và dòng tiêu đề bảng
   (chỗ đặt 2 nút chữ `Tìm / Tải lại`, `Xem chi tiết`).
3. Ô tìm kiếm dùng placeholder mặc định “Tìm tên hoặc mã nguyên liệu…” cho cả 3
   tab (sai nghiệp vụ), không có kính lúp chuẩn, Enter đọc thẳng text đang gõ.
4. Hộp “Xem chi tiết” phiếu nhập/phiếu xuất đổ toàn bộ nội dung vào một TextBox
   nhiều dòng có cả 2 thanh cuộn: chữ trần, không có bảng dòng món, không có
   khối thông tin có nhãn → khó đọc, lệch hẳn style dialog chung.
5. (Phụ) Tab Kiểm kê hiện cột `ĐÃ ĐIỀU CHỈNH` là `True/False` thay vì nhãn
   “Đã điều chỉnh / Chưa điều chỉnh”.

## 2. Nguyên nhân (đã đối chứng bằng source)

**B01 — Cột bảng bị ép width cố định, không giãn hết card**

`src/CafeManager.WinForms/UI/OrbitDataGridView.cs:20-21` để `Profile` mặc định
= `OrbitGridProfile.Inventory`. Với profile này, `ApplyMetrics` (`:169-182`)
đặt `AutoSizeMode = None` cho mọi cột không tên `Name` và gán
`Width = ColumnWidth(property)` (`:295-306`): `Code` 76px, còn lại 116px.
Khai báo `AutoSizeMode = AllCells` ở 3 control bị ghi đè khi `DataBindingComplete`.
Kết quả: tổng width cột < bề ngang card (phần còn lại trống), và `MÃ` 76px
(trừ padding 2×13px) không đủ chỗ → `PN2…`.

**B02 — Hàng hành động chữ không đúng chuẩn, tạo dải trống**

`OrbitListWorkspace.ArrangeStackedTools` (`UI/OrbitListWorkspace.cs:111-116`)
luôn chừa `metaHeight = max(46, actionsHeight+12)` khi `ShowMeta = true`, kể cả
khi `RowActions` rỗng. Ba control đang nhét 2 nút chữ `Tìm / Tải lại` +
`Xem chi tiết` vào `RowActions` (`PurchasesControl.cs:17-19`,
`StockIssuesControl.cs:16-18`, `StocktakesControl.cs:16-18`). Chuẩn danh sách
(`docs/ui/HuongDan_UI_Orbit_Cafe.md` §11.1/§11.2) quy định hàng 1 = ô tìm
giãn hết + kính lúp + làm mới dạng icon, hàng hành động chỉ dành cho hành động
trên dòng. Hai nút chữ này vừa trùng chức năng với ô tìm vừa tạo dải trống.

**B03 — Ô tìm kiếm không dùng hành vi chuẩn**

Ba control tự gắn `search.Input.KeyDown` và đọc `search.Input.Text` trực tiếp
(`PurchasesControl.cs:19`), không có “từ khóa đã submit” như
`OrbitRecordListControl.UseStackedSearch` (`UI/OrbitRecordListControl.cs:138-152`),
không có nút kính lúp/làm mới icon, không đặt `PlaceholderText` theo nghiệp vụ
(đang ăn placeholder mặc định của `OrbitSearchBox.cs:7`).

**B04 — Hộp chi tiết dùng TextBox trần**

`PurchaseDetailDialog` (`PurchasesControl.cs:47`) và `StockIssueDetailDialog`
(`StockIssuesControl.cs:46`) dựng `TextBox{ReadOnly, Multiline, ScrollBars.Both}`
chứa chuỗi `\r\n` gồm header và danh sách dòng món. TextBox này không qua
`OrbitManagementTheme.DialogGrid`, không có cột/format tiền, không có nhãn
trường; các dialog chi tiết khác (ví dụ `SupplierDebtDetailDialog`,
`FinanceDetailDialog`) và chuẩn §11.3 dùng khối thông tin + bảng có header.

**B05 — Cột bool của Kiểm kê**

`StocktakesControl.cs:15` bind cột `Closed` kiểu `bool`; `G05Display.Grid`
(`UI/G05Display.cs:9-32`) không map bool nên grid vẽ `True/False`.

## 3. Hướng sửa đề xuất (chờ duyệt)

1. **Bảng** — thêm 3 profile giãn cột dùng chung trong `OrbitDataGridView`
   (`Purchases`, `StockIssues`, `Stocktakes`) theo đúng cơ chế ledger đã có của
   `Finance`/`SupplierDebts`: đo nội dung từng cột, `AutoSizeMode = Fill`,
   `FillWeight = MinimumWidth`; căn phải cho `Total/Immediate/Debt`. Không đổi
   hành vi `Finance`/`SupplierDebts` hiện có.
2. **Bỏ 2 nút chữ** `Tìm / Tải lại`, `Xem chi tiết` khỏi cả 3 tab; đặt
   `ShowMeta = false` để không còn dải trống. Ô tìm giãn hết hàng 1, bên phải là
   **kính lúp + làm mới dạng icon** trong `SearchActions` (đúng §11.1/§11.2),
   có tooltip/AccessibleName.
3. **Tìm kiếm** — tách “từ khóa nháp” và “từ khóa đã submit”: Enter/kính lúp mới
   submit, đổi trang/làm mới dùng từ khóa đã submit; placeholder riêng từng tab
   (mã phiếu + NCC / mã phiếu + lý do / mã phiếu kiểm kê). Thêm chuỗi tiếng Anh
   tương ứng trong `Translations/en.json`.
4. **Xem chi tiết** — vẫn mở bằng nhấn đúp dòng (đã có sẵn) và thêm Enter trên
   dòng đang chọn; hộp chi tiết dựng lại: khối thông tin có nhãn
   (`OrbitSummaryPanel`) + `OrbitDataGridView` chỉ đọc liệt kê dòng món
   (Mã, Nguyên liệu, Đơn vị, Lượng, Đơn giá/Thành tiền), giữ các nút liên kết
   “Phiếu chi/Công nợ nguồn”. Không đổi contract dữ liệu.
5. **Kiểm kê** — format cột `Closed` thành “Đã điều chỉnh / Chưa điều chỉnh”.
6. Thêm trạng thái rỗng dùng `OrbitEmptyState` thay vì bảng trống.

Không đổi: service/procedure/SQL, quyền, luồng lập phiếu, nút `Lập phiếu nhập`
/ `Lập phiếu xuất` / `Lấy số liệu tồn`, tab Kiểm kê mở `StocktakeEditorDialog`.

## 4. Tối ưu ngoài nguyên nhân (optional, không bắt buộc làm chung)

- Thanh cuộn dọc của grid đang là scrollbar native sáng màu trên nền tối;
  thuộc hành vi của `DataGridView` dùng chung, mọi màn danh sách đều vậy.
  Chỉ sửa nếu người dùng muốn đổi toàn bộ màn danh sách.

## 5. Rủi ro

- `OrbitDataGridView` là control dùng chung: thay đổi chỉ thêm nhánh profile mới
  và mở rộng tập cột căn phải (`Total/Immediate/Debt`) — không ảnh hưởng màn
  khác vì các cột này không có ở `Finance`/`SupplierDebts`.
- Bỏ nút “Xem chi tiết” làm giảm khả năng khám phá; bù bằng nhấn đúp/Enter và
  chú thích ngắn trên dòng trạng thái. Nếu người dùng muốn giữ một điểm vào rõ
  ràng, có thể thay bằng hành động từng dòng thay vì nút chữ.
- Hộp chi tiết mới dùng cùng dữ liệu `GetAsync`, không đổi truy vấn.

## 6. Ca kiểm chứng đề xuất (chạy sau khi được duyệt)

| Mã | Nội dung |
|---|---|
| UI01 | 3 tab: bảng phủ hết bề ngang card, không còn vùng trống phải; mã phiếu hiện đủ, không `…` |
| UI02 | Không còn nút chữ Tìm/Tải lại, Xem chi tiết; không còn dải trống giữa ô tìm và header bảng |
| UI03 | Enter/kính lúp mới tìm; gõ nhưng chưa submit rồi chuyển trang/làm mới vẫn dùng từ khóa cũ; placeholder đúng từng tab |
| UI04 | Rỗng/lỗi/tải: có trạng thái rõ, không hiện tổng 0 giả |
| UI05 | Nhấn đúp và Enter mở hộp chi tiết; khối thông tin + bảng dòng món hiển thị đúng, tiền căn phải, lượng N4 |
| UI06 | Kiểm kê: cột điều chỉnh hiện chữ, ngày snapshot đúng `dd/MM/yyyy HH:mm` |
| UI07 | Phân trang `SetPage` vẫn đúng tổng thật; đổi trang không mất từ khóa đã submit |
| UI08 | Mở rộng 1280×720 và 1366×768 ở DPI 125%: card/bảng/footer không chồng, ô tìm không bị cắt (kiểm thủ công bởi người dùng; phần 100/150% ghi NOT RUN) |

## 7. Đã triển khai (23/11/2026)

| File | Thay đổi |
|---|---|
| `src/CafeManager.WinForms/UI/OrbitDataGridView.cs` | Thêm profile `Purchases`, `StockIssues`, `Stocktakes` dùng chung nhánh ledger (cột Fill theo nội dung đo được); căn phải thêm `Total/Immediate/Debt`. Màn Finance/SupplierDebts giữ nguyên. |
| `Modules/Inventory/PurchasesControl.cs` | Bỏ 2 nút chữ; `ShowMeta=false`; ô tìm giãn hết hàng 1 + kính lúp + làm mới icon có tooltip; từ khóa nháp/đã submit; placeholder riêng; trạng thái rỗng; nhấn đúp/Enter mở chi tiết; dựng lại `PurchaseDetailDialog` (khối thông tin + bảng dòng món có định dạng). |
| `Modules/Inventory/StockIssuesControl.cs` | Cùng chuẩn danh sách; dựng lại `StockIssueDetailDialog`. |
| `Modules/Inventory/StocktakesControl.cs` | Cùng chuẩn danh sách; cột `Closed` hiện “Đã điều chỉnh / Chưa điều chỉnh”. |
| `CafeManager.Models/Localization/Translations/en.json` | Bổ sung 16 khóa tiếng Anh cho chuỗi mới (placeholder, nhãn chi tiết, cột in hoa, dòng trạng thái). |

Kiểm chứng đã chạy: `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj` → 0 error
(build vào thư mục tạm vì app đang chạy khóa `bin`; đã xóa thư mục tạm).
Chưa chạy: test chức năng trong app, kiểm thị giác, DPI 100/150%.

