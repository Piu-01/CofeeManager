# Kế hoạch — Sửa/xóa trực tiếp trên dòng danh sách

## Bối cảnh

Hiện mọi màn master data dùng mô hình "chọn dòng → Sửa mở bảng → Xóa hỏi xác nhận" (hoặc
`OperationEditorDialog`). Người dùng chốt đổi sang **sửa tại ô, xóa tại dòng** cho 8 màn:
Menu sản phẩm, Danh mục nền, Bàn/Khu vực, Nguyên liệu, Khách hàng, Nhân viên, Tài sản,
Hạng thành viên. Bỏ nút Sửa/Xóa, không để khoảng trống bố cục. **Tài sản cũng sửa inline**;
khi ghi rơi vào "kết quả chưa xác định" (timeout) thì **khóa đúng dòng đó** và hiện nút
**Đối chiếu ở giữa dòng**, các dòng khác vẫn dùng được. Khuyến mãi và Chấm công ngoài phạm vi.

Không đổi nghiệp vụ, SQL, DAL/BLL, contract dữ liệu hay phân quyền. Chỉ thêm tầng tương tác
UI dùng chung, gọi đúng service/Normalize/revision/requestId hiện có.

## Quyết định thiết kế

1. DTO là positional record immutable, `OrbitDataGridView` ReadOnly và tự paint → không dùng
   chế độ edit native. Dùng **editor overlay** đè lên ô, commit bằng service rồi reload và
   chọn lại dòng theo mã. Mẫu có sẵn: `OrderPanel.PositionNoteEditor` / `OrderNoteEditor`
   (`Modules/Sales/OrderPanel.cs:98-141, 284-336`).
2. Một engine dùng chung cho cả màn kế thừa `OrbitRecordListControl` lẫn 2 màn Designer
   (`CatalogsControl`, `MaterialsControl`); màn chỉ cấp delegate, engine không chứa SQL/nghiệp vụ.
3. Một editor mở tại một thời điểm. Click ô khác = commit ô hiện tại trước rồi mở ô mới;
   Enter/Tab commit, Esc hủy; focus rời editor cũng commit (có guard chống gửi lặp).
   `OrbitField.ApplyInputBehavior` gắn select-all cho input động.
4. Lỗi validate/SQL thường: giữ editor + giá trị đã nhập, báo ở dòng status, không ghi một phần.
   Chỉ `UserOperationException.Error.RequiresReconciliation` mới khóa dòng và hiện "Đối chiếu".
5. Ô checkbox (Hỗ trợ thủ công): click là ghi luôn (checkbox chính là editor), cùng cơ chế
   busy/unknown; PT010 vẫn bị service chặn.
6. Hạng thành viên: inline chỉ Ngưỡng điểm/Giảm %; commit chạy `PolicyAsync` → thay 1 hạng →
   `PolicyPreviewAsync` → hộp xác nhận ngắn "X khách sẽ đổi hạng. Lưu?" → `PolicySaveAsync`.
   Giữ preview + xác nhận tác động thay vì bỏ an toàn cũ; xóa hạng vẫn chạy preview rồi xác nhận.
7. `CatalogService.SaveAsync/DeleteAsync` **không có requestId** (xác minh: 003_reference_catalogs.sql
   chỉ ghi receipt khi tạo ProductCategory; repository không truyền `@RequestId`). Với 2 thao tác
   này, "Đối chiếu" dùng `GetAsync` + so `Revision` (update) / còn tồn tại (delete). Ghi nhận
   khoảng trống này trong tài liệu, không thêm migration ngoài phạm vi.
8. Nhân viên/Tài sản inline **không ghi file chờ** (`IPendingOperationStore`) để tránh đè
   file của nhiều dòng; "Đối chiếu" dùng `ReceiptAsync(requestId)` rồi reload. Sau restart,
   DB là nguồn đúng và revision cũ bị SQL từ chối nên không nhân đôi. Ghi chú tradeoff.

## Tệp mới

- `src/CafeManager.WinForms/UI/OrbitCellEditor.cs` — panel overlay chứa TextBox/ComboBox
  (DropDownList, owner-draw theo theme) cho một ô; API `Begin(kind, text, options, align)`,
  `Text`, `Checked`, `FocusInput()`, event `CommitRequested`, `CancelRequested`; Enter/Tab/Esc,
  border Lilac, kích thước/DPI qua `OrbitManagementTheme.Px`; guard `sending`.
- `src/CafeManager.WinForms/UI/OrbitRowEditor.cs` — engine dùng chung:
  - `InlineKind { Text, Money, Integer, Decimal, Dropdown, Date, Time, Check }`
  - `InlineColumn(Property, Kind, Read(row), Convert(row,text), Options, CanEdit)`
  - Delegate màn cấp: `Save(row, property, value, requestId, ct)`, `Delete(row, requestId, ct)`
    (tự hiện confirm nếu thuộc nhóm cần), `Reconcile(row, requestId, ct) -> bool?`,
    `Reload()`, `RowKey(row)`, `Status(text, state)`, `CanWrite(row)`.
  - Subscribes `CellMouseClick`/`CellDoubleClick`/`KeyDown(F2)`/`Scroll`/`SizeChanged`/`DpiChangedAfterParent`/
    `DataBindingComplete`/`VisibleChanged`; đặt editor theo `GetCellDisplayRectangle` của Grid
    cộng offset, ẩn khi ô bị cắt; tập trung tại `Workspace.Body`/`management.Body`.
  - Trạng thái unknown theo `RowKey`: khóa dòng (không sửa/xóa, vẫn chọn được), đặt một
    `OrbitButton` "Đối chiếu" căn giữa dòng (đo bằng `TextRenderer`, đặt theo
    `GetRowDisplayRectangle`, cập nhật khi scroll/reload); bấm → `Reconcile`; `true` → reload
    + "Đã đối chiếu"; `false` → reload + "Chưa ghi"; `null` → báo không xác định, giữ khóa.
  - Expose `IsBusy`, `HasUnknown`, `event StateChanged`, `CloseEditor()`.
- `docs/tasks/row-inline-edit-delete/plan.md` (bản kế hoạch này sau khi duyệt).

## Tệp sửa

### Hạ tầng

- `UI/OrbitDataGridView.cs`
  - Thêm hook `Func<DataGridViewCell,bool>? InlineEditable`, `Func<DataGridViewCell,Cursor?>? InlineCursor`,
    `Func<int,bool>? RowBlocked` (mặc định null → không đổi hành vi các màn khác).
  - `CellMouseEnter/Leave`: giữ hover dòng; đặt `Cursor` theo `InlineCursor` (IBeam text/số,
    Hand dropdown/ngày, mặc định), trả Default khi rời; không đổi với ô chỉ đọc.
  - Trong nhánh paint ô (dòng 317-361): nếu đang hover đúng ô và `InlineEditable` → viền inset
    1px (Caramel khi dòng chọn, Lilac khi không) **không** đổi nền dòng đang chọn; nếu
    `RowBlocked` → veil amber nhạt + vạch trái 2px. Cột `Delete` giữ nguyên (đã 44px, icon đỏ hover).
- `UI/OrbitRecordListControl.cs`
  - Thêm `protected bool RowWriteBusy` và cho `UpdateActions` chặn khi bận; `protected bool IsBusy => busy || RowWriteBusy`.
  - Helper `protected OrbitRowEditor CreateInlineEditor(...)` gắn Grid + `Workspace.Body`,
    nối `StateChanged` → `RowWriteBusy`/`UpdateActions`; `RowDeleteRequested`/`RowActivated`
    của các màn sẽ bỏ, engine tự xử lý click.
  - Giữ `AddRowDeleteColumn`, `Reload`; engine tự khôi phục selection theo mã sau reload.

### 8 màn

| Màn / tệp | Cột inline | Xóa | Đối chiếu |
|---|---|---|---|
| Menu `ProductsControl.cs` | Name Text, Category Dropdown (`CategoriesAsync`), Price Money, Status Dropdown (`Dang ban`/`Ngung ban`) | ngay, không confirm; SQL chặn nếu có CHITIET_HD | `ProductService.ReconcileAsync`; save `Normalize(new(...,Recipe:[]))` vì `sp_ProductWrite` Update không đụng CONGTHUC |
| Danh mục `CatalogsControl.cs/.Designer.cs` | Name; SDT (NCC); Chiết khấu; Phụ cấp, Hệ số; Bắt đầu/Kết thúc (Time); Loại (Thu/Chi, trừ mã hệ thống); Hỗ trợ (Check, trừ PT010) — theo tab | confirm NCC + Thu/chi; còn lại ngay; mã hệ thống chặn | `GetAsync` + so Revision/giá trị, hoặc còn tồn tại |
| Bàn/Khu vực `TablesControl.cs` | Bàn: Name, Area dropdown (`AllAreas`); Khu vực: Name | ngay | `TableService.ReconcileAsync`; bỏ `RowActivated` mở dialog |
| Nguyên liệu `MaterialsControl.cs/.Designer.cs` | Name, Unit (Tồn hiện tại/Tồn đầu kỳ chỉ đọc) | confirm | `MaterialService.ReconcileAsync` |
| Khách hàng `CustomersControl.cs` | Name, SDT (Điểm/Hạng/Ưu đãi đọc) — chỉ biến thể đầy đủ | confirm | `CustomerService.ReconcileAsync` |
| Nhân viên `EmployeesControl.cs` | Name, SDT, Hợp đồng, Lương tháng, Lương giờ, Chức vụ (lookup có sẵn) | confirm | `ReceiptAsync(id,"EmployeeUpdate"/"EmployeeDelete")`; tab Tài khoản không đổi |
| Tài sản `AssetsControl.cs` | Name, Ngày mua (dd/MM/yyyy), Nguyên giá (nguyên VND), Tháng KH | confirm | `AssetService.WriteAsync` + `ReceiptAsync`; giữ `AssetEditorDialog` cho Thêm/Yêu cầu chờ/Chi tiết |
| Hạng `LoyaltyControl.cs` (`PolicyList`) | Ngưỡng điểm, Giảm % (Mã đọc) | confirm + preview | `LoyaltyService.ReconcileAsync`; bỏ `RowActivated` |

Chi tiết wiring từng màn:
- Products: bỏ 2 `AddAction` (dòng 25-26), thêm `AddRowDeleteColumn()`; engine với `CanWrite`
  = ADMIN; save build request từ row + 1 trường; delete dùng `row.Revision`.
- Customers: chỉ constructor biến thể đầy đủ (dòng 18, 34) — **không** đụng picker (25-32);
  `CanWrite` = ADMIN.
- Employees: chỉ nhánh `!accountTab`; Position dropdown map Name↔Code từ `positionLookups`;
  save truyền đủ 6 trường đã Normalize; delete `DeleteAsync`; giữ Nghỉ/Đi làm lại, Yêu cầu chờ.
- Assets: bỏ `Sửa`/`Xóa`, giữ Thêm/Yêu cầu chờ/Chi tiết; save gọi thẳng
  `WriteAsync(new(id,"Update",row.Code,row.Revision,new AssetRequest(...)))`; validate ngày
  `DateOnly.TryParseExact("dd/MM/yyyy")`, nguyên giá nguyên dương, tháng > 0 (dùng `AssetService.Normalize`).
- Catalogs: xóa `editButton/deleteButton` khỏi Designer (field, tạo, RowActions, Click) và
  `Edit_Click/Delete_Click`, `unknownDelete`; thêm cột "Delete"; `TabChanged` đóng editor/ clear
  unknown; `RefreshRows` thay bằng reload của engine; `IsWriting` gồm `engine.IsBusy || HasUnknown`.
  Rebuild `CatalogRequest` đúng subtype từ row + trường đổi (`PaymentMethodRequest` cho Supported).
- Materials: bỏ `edit/remove` khỏi Designer + handlers + `UpdateRowActions`; thêm cột "Delete";
  delete thay `OperationEditorDialog` bằng `OrbitConfirmationDialog` rồi `DeleteAsync`;
  save `Normalize(new(Code,Name,Unit))`; reload = `LoadRows`.
- Tables/Areas/Ranks: bỏ `RowDeleteRequested`/`RowActivated`, engine nhận click/F2; giữ
  `AddRowDeleteColumn` và map bàn.
- Ranks save/delete: lấy `PolicyAsync`, dựng `TierInput[]`, preview, confirm khi sửa/xóa,
  `PolicySaveAsync(...,id)`; giữ luật hạng ngưỡng 0 và ≥1 hạng.

### Khác

- `src/CafeManager.Models/Localization/Translations/en.json`: thêm key literal mới: "Đối chiếu",
  "Đang ghi…", "Đã ghi {0}.", "Chưa ghi · {0}", "Đã đối chiếu: đã ghi.", "Đối chiếu: chưa ghi; có thể sửa lại.",
  "Không xác định được kết quả đối chiếu.", "Đã xóa {0}.", "Bấm để sửa {0}", tooltip/trash "Xóa {0}."
  (dùng `L.F`; giữ nguyên thứ tự alphabet).
- `docs/tasks/row-inline-edit-delete/requirements.md`: sửa mục 2.1/4/6 — Tài sản inline, bỏ
  ngoại lệ + Q1; bổ sung khóa dòng + nút Đối chiếu giữa dòng; ghi chú catalog không có requestId.
- `docs/tasks/.../plan.md`: bản kế hoạch đã duyệt.
- `docs/UX_QuanLiQuanCafe.md`: thay mô tả Sửa/Xóa của 8 màn bằng click-to-edit, trash từng dòng,
  ma trận xác nhận, luồng unknown/Đối chiếu.
- **Quy tắc thiết kế UI dùng chung (bắt buộc, áp cho mọi màn danh sách code sau):**
  - `docs/ui/HuongDan_UI_Orbit_Cafe.md`: sửa bảng 11.1 (hàng hành động: master data không còn
    Sửa/Xóa; chứng từ chỉ còn Chi tiết/In) và thêm mục **11.5 "Sửa/xóa trực tiếp trên dòng —
    22/11/2026"**: ô sửa được hover viền/con trỏ + click mở editor tại ô; ô đọc/vùng trống chỉ
    chọn dòng; Enter/Tab ghi, Esc hủy; thùng rác 44px cuối dòng; ma trận xác nhận kho/tài
    chính/lịch sử; xóa ngay không toast/undo; unknown chỉ khóa dòng + nút "Đối chiếu" giữa dòng;
    bố cục hàng nút dồn phải không chừa khoảng trống; quyền giữ nguyên; ghi rõ mục này thay cách
    hiểu 26/10 cho master data, không đổi quy tắc chứng từ/modal.
  - `docs/ui/WINFORMS_IMPLEMENTATION.md`: thêm mục **2.2 "Sửa/xóa trực tiếp trên dòng"** — mapping
    `OrbitRowEditor` + `OrbitCellEditor`, hook `InlineEditable`/`InlineCursor`/`RowBlocked` của
    `OrbitDataGridView`, `AddRowDeleteColumn`, reload + chọn lại theo mã, contract lỗi/unknown;
    nêu rõ không dùng edit native vì DTO immutable, không đặt logic SQL trong UI.
  - `docs/ui/README.md`: mục dated 22/11/2026 dẫn tới 11.5 và 2.2, nêu phạm vi 8 màn, loại trừ
    Khuyến mãi/Chấm công và chứng từ đã chốt; không tuyên bố hình thức đã nghiệm thu.

## Thứ tự triển khai

1. Chốt tài liệu trước khi code: requirements (làm rõ Tài sản) + **quy tắc thiết kế UI 11.5 /
   2.2 / README dated entry** làm chuẩn cho 8 màn và các màn danh sách sau; đọc lại mục UX/UI liên quan.
2. `OrbitCellEditor` → `OrbitRowEditor` → hook `OrbitDataGridView` → helper `OrbitRecordListControl`.
3. Ranks → Tables/Areas → Materials → Catalogs → Products → Customers → Employees → Assets.
4. Dọn nút/bố cục 2 Designer + các `AddAction`; thêm khóa en.json.
5. Build Debug/Release; rà soát không đụng Khuyến mãi/Chấm công; cập nhật tài liệu.
6. Dừng, xin duyệt **đợt test riêng** trước khi chạy test (theo post-change-test-workflow).

## Kiểm chứng (sau khi được duyệt test)

- `dotnet build` Debug + Release sạch (warnings-as-errors), không lỗi Designer.
- Từng màn: click ô sửa được → editor đúng kiểu, select-all; Enter/Tab/blur ghi + reload +
  chọn lại dòng; Esc không ghi; click ô đọc/vùng trống chỉ chọn dòng; F2/double-click vào ô;
  hover đúng ô (viền + con trỏ), dòng đang chọn giữ màu; thêm/xóa không còn khoảng trống nút.
- Xóa: đúng ma trận confirm; xóa đúng dòng dù chưa chọn; bị chặn (lịch sử/mã hệ thống/khu vực
  còn bàn) → dòng còn, lỗi ở status; không toast/undo.
- Timeout thật trên DB test: session 2 giữ `BEGIN TRAN` khóa dòng, sửa/xóa dòng khác trong app
  → timeout; chỉ dòng đó khóa, nút Đối chiếu giữa dòng, giá trị không tự revert, status có
  request id; `COMMIT` → receipt có, reload; `ROLLBACK` → "Chưa ghi", mở khóa. Lặp cho
  Product/Material/Customer/Employee/Asset/Rank; Catalogs kiểm bằng Revision/GetAsync.
- Ranks: preview + confirm hiện số khách ảnh hưởng; không xóa hạng ngưỡng 0/cuối.
- Regression: Công thức (Menu), Mở sổ/Lịch sử điểm, tab Tài khoản & Nghỉ/Đi làm lại, tạo tài
  sản + Yêu cầu chờ, tạo/thêm danh mục, map bàn, phân trang/tìm/lọc, Khuyến mãi + Chấm công
  không đổi. Ghi `test-results.md`; hình thức do người dùng tự kiểm, agent chỉ kiểm chức năng
  và layout kỹ thuật ở DPI 125%.
- Soát tài liệu: mục 11.5, 2.2 và README dated entry mô tả cùng một ma trận confirm/luồng
  unknown; bảng 11.1 không còn câu "Sửa/Xóa" cho master data; link chéo mở đúng mục.
