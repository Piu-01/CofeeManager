# Bug: ô chọn loại báo cáo hiển thị tên enum tiếng Anh thay vì nhãn tiếng Việt

- Trạng thái: Đã sửa B01–B04 + test chức năng 15/15 PASS (xem `test-results.md`); chờ người dùng kiểm hình thức trên app đang chạy.
- Ngày báo: 15/11/2026
- Người báo: người dùng (ảnh màn hình Báo cáo, đang ở ngôn ngữ tiếng Việt)
- Màn hình: Báo cáo (`ReportsControl`), dòng công cụ đầu tiên.

## 1. Hiện tượng

Danh sách xổ xuống chọn loại báo cáo hiển thị nguyên tên enum:
`SalesDay, SalesChannel, Products, Payments, Inventory, Debts, CashFlow, Labor, Assets`
và ô đã chọn hiển thị `CashFlow`.

Trong khi cùng màn hình, mô tả phía trên lại đúng tiếng Việt:
`Dòng tiền quản trị · 16/10/2026 → 15/11/2026 · Thu bán: ngày thanh toán; thu/chi khác: ngày phiếu`.
Nghĩa là lớp dịch đang chạy bình thường; chỉ phần chữ của ComboBox không đi qua nhãn.

## 2. Nguyên nhân

`src/CafeManager.WinForms/Modules/Reports/ReportsControl.cs:32`

```csharp
kind.Items.AddRange(Enum.GetValues<ReportKind>().Cast<object>().ToArray());
kind.SelectedIndex=0;
kind.Format+=(_,e)=>{if(e.ListItem is ReportKind k)e.Value=Label(k);};
```

`ComboBox.Format` chỉ có tác dụng khi ComboBox đang **data-bound**. Với ComboBox
nạp item thủ công bằng `Items.Add/AddRange` (`DataSource` null), sự kiện `Format`
không tham gia vào việc lấy chuỗi hiển thị; control dùng `ToString()` của item,
nên hiện `ReportKind.CashFlow` → `"CashFlow"`.

Bằng chứng: probe riêng trên .NET 10 (`net10.0-windows`, WinForms) tái hiện đúng
ca này — ComboBox `DropDownList` + `FlatStyle.Flat`, item là enum, có gắn handler
`Format` đổi sang `"VI:" + k`, nhưng cả `GetItemText()` và ảnh render đều ra
`CashFlow` / `SalesDay`, không ra `VI:...`.
Kết quả probe (trích):
```
0 GetItemText='SalesDay' ... 6 GetItemText='CashFlow'
ảnh render combo: "CashFlow" (không phải "VI:CashFlow")
```

## 3. Cùng cơ chế, phát hiện khi rà soát (chưa chạy UI, chỉ đọc code)

| # | Vị trí | Vấn đề |
|---|---|---|
| B02 | `ReportsControl.cs:50` — combo `option` (bộ lọc Tất cả / phương thức / NCC / nhân viên / nguyên liệu / tài sản) | `option.DataSource = ...` nhưng **không đặt `DisplayMember`**, nên chữ hiển thị lấy `ReportOption.ToString()` = `"Mã · Tên"` thay vì chỉ `Name`. Không phải raw record vì `ReportOption` đã override `ToString`. |
| B03 | `Modules/HumanResources/EmployeesControl.cs:36-37` — bộ lọc `position` | Item thêm bằng `Items.Add(p)` + `position.Format` → cùng lỗi kiểu B01; `HumanResourcesLookup` **không** override `ToString` nên hiện nguyên `HumanResourcesLookup { Code = CV001, Name = ..., Allowance = ... }`. |
| B04 | `Modules/Finance/DebtPaymentDialog.cs:34` (`method`), `Modules/Finance/FinanceEntryDialog.cs:59` (`category`), `Modules/Inventory/PurchaseEditorDialog.cs:56` (`supplier`, `material`), `Modules/Inventory/StockIssueEditorDialog.cs:38` (`material`) | `DataSource` không có `DisplayMember` → lấy `ToString()` của `InventoryLookup`/`FinanceCategory` (đã override thành `"Mã — Tên (Đơn vị)"` / `"Tên (Loại)"`), không phải chỉ tên. |

B01 (được báo) và B02 nằm cùng màn Báo cáo; B03 cùng cơ chế `Format`; B04 cùng
cơ chế thiếu `DisplayMember`.

**Đính chính sau khi chạy probe (16/11/2026):** dự đoán ban đầu ở mục này ghi B02/B04
sẽ hiện raw record — sai. Hai loại lookup đó đã override `ToString` nên trước khi sửa
vẫn đọc được, chỉ khác nhãn (có thêm mã/loại/đơn vị). Lỗi nặng "raw value" thực tế chỉ
có B01 (tên enum) và B03 (`HumanResourcesLookup { ... }`).

## 4. Cách sửa đề xuất (chờ duyệt, chưa sửa)

B01 — `ReportsControl` dòng 32 và nơi đọc `kind.SelectedItem` (dòng 49, 55):
- Nạp item là chuỗi nhãn: `kind.Items.AddRange(Enum.GetValues<ReportKind>().Select(k => (object)Label(k)).ToArray())`, bỏ handler `Format`.
- Đọc lại loại bằng chỉ số: `var selected = (ReportKind)kind.SelectedIndex;` (thứ tự `Enum.GetValues` trùng thứ tự item; không phụ thuộc giá trị số của enum), có chặn `SelectedIndex < 0`.
- Cách khác tương đương: bọc `record KindChoice(ReportKind Kind, string Display)` + `DisplayMember`, nhưng dài hơn vì phải đổi mọi chỗ cast.

B02 — thêm `DisplayMember = nameof(ReportOption.Name)`, `ValueMember = nameof(ReportOption.Code)` trước khi gán `option.DataSource`; code đọc `o.Code` giữ nguyên.

B03 — đổi sang data-bound: `position.DisplayMember = "Name"; position.ValueMember = "Code"; position.DataSource = [new HumanResourcesLookup("", L.T("Mọi chức vụ"), 0), ..list];` bỏ handler `Format`; điều kiện `position.SelectedItem is HumanResourcesLookup p ? p.Code : null` vẫn đúng với item "Mọi chức vụ" (Code rỗng).

B04 — thêm `DisplayMember`/`ValueMember` đúng tên thuộc tính (`Name`/`Code`) cho từng ComboBox, hoặc dùng helper chung nếu muốn gom.

Ba hướng B01–B03 đều là sửa hiển thị, không đổi contract dữ liệu, không đổi request gửi SQL.

## 5. Phạm vi ảnh hưởng và rủi ro

- Không ảnh hưởng nghiệp vụ/SQL: chỉ đổi cách ComboBox lấy chuỗi hiển thị và cách đọc item đã chọn.
- Rủi ro chính ở B01 là map sai loại báo cáo nếu thứ tự item và enum lệch nhau; vì item được sinh trực tiếp từ `Enum.GetValues` nên an toàn, nhưng phải kiểm đủ 9 loại.
- B02/B04: nếu `ValueMember` đặt sai tên thuộc tính thì `SelectedValue`/`SelectedItem` đọc ra null → phải kiểm lại đường lấy `Code` hiện có (B02 dùng `SelectedItem`, B04 dùng `SelectedItem`/`SelectedValue` tùy chỗ).
- Không đụng tới tiền/kho/nợ/transaction.

## 6. Ca kiểm chứng đề xuất (sau khi được duyệt sửa)

- UI (người dùng tự kiểm hình thức theo phân công 22/10/2026):
  1. Mở Báo cáo ở tiếng Việt, mở combo loại: 9 dòng đều là nhãn tiếng Việt, ô đã chọn cũng tiếng Việt.
  2. Chuyển sang English: 9 dòng là nhãn tiếng Anh.
  3. Chọn `Phương thức thanh toán` / `Công nợ tại ngày` / `Công, chốt và chi lương` / `Đối chiếu tồn kho` / `Tài sản`: combo bộ lọc phụ hiện `Tất cả` và tên đối tượng, không hiện `ReportOption { ... }`.
  4. Hồ sơ nhân sự: bộ lọc Chức vụ hiện tên chức vụ; lọc vẫn đúng.
- Chức năng (agent):
  5. Với từng loại báo cáo, bấm `Xem báo cáo` và đối chiếu `Metadata.Kind` + mô tả khớp loại đã chọn (đủ 9 loại × 2 ngôn ngữ), đặc biệt sau khi đổi sang đọc theo chỉ số.
  6. Bộ lọc phụ: chọn một đối tượng cụ thể, kiểm request gửi đi có đúng `Code` như trước khi sửa.
  7. Hồ sơ nhân sự: chọn chức vụ cụ thể và `Mọi chức vụ`, kiểm kết quả lọc như trước.
- Chưa chạy bất kỳ ca nào; đây là kế hoạch test, cần duyệt riêng theo `post-change-test-workflow`.

## 7. Đã sửa (15/11/2026, phạm vi B01–B04 được duyệt)

| # | File | Thay đổi |
|---|---|---|
| B01 | `src/CafeManager.WinForms/Modules/Reports/ReportsControl.cs` | Item combo loại báo cáo là chuỗi `Label(kind)` theo thứ tự `Enum.GetValues`; bỏ handler `Format`; thêm `SelectedKind => (ReportKind)kind.SelectedIndex` (mặc định `SalesDay` nếu chưa chọn) và dùng ở `ChooseOptions`/`CaptureFilter`. |
| B02 | cùng file | `option.DisplayMember = nameof(ReportOption.Name)`, `option.ValueMember = nameof(ReportOption.Code)` trước/không đổi cách đọc `SelectedItem`. |
| B03 | `src/CafeManager.WinForms/Modules/HumanResources/EmployeesControl.cs` | Bỏ `position.Format`; item là chuỗi `Name` (dòng đầu "Mọi chức vụ"), giữ danh sách `positionLookups` song song để lấy `Code` theo `SelectedIndex`; cờ `positionBuilding` chặn reload khi nạp lại danh sách. |
| B04 | `Modules/Finance/DebtPaymentDialog.cs`, `Modules/Finance/FinanceEntryDialog.cs`, `Modules/Inventory/PurchaseEditorDialog.cs`, `Modules/Inventory/StockIssueEditorDialog.cs` | Thêm `DisplayMember`/`ValueMember` (`Name`/`Code`) trước khi gán `DataSource`; cách đọc `SelectedItem` giữ nguyên. |

Chưa đổi SQL, BLL, contract request hay layout. Kiểm chứng đã chạy: `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj` (Debug) — PASS, 0 warning/0 error. Chưa chạy UI, chưa chạy test chức năng.
