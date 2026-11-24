# Plan — Sửa master data bằng double-click, tái dùng engine dòng

Trạng thái: **plan đã duyệt — đã triển khai bước 1–11, build Debug/Release 0 lỗi**;
đợt test bước 12 đã chạy 23/11/2026 (23/24 ca, không còn NOT RUN), kết quả tại
[test-results.md](test-results.md); F-01/F-02 phát hiện kèm đã sửa và hồi quy PASS.

Requirements: [requirements.md](requirements.md) đã được duyệt.

## 0. Nguyên tắc triển khai

- Không đổi SQL, migration, service, request/DTO hay quyền.
- `OrbitRowEditor` vẫn là engine duy nhất của thao tác dòng; chỉ bỏ phần mở/ghi
  editor tại ô, giữ xóa, khóa dòng, Đối chiếu, reload và trạng thái bận.
- Hộp sửa dùng lại dialog hiện có của từng màn. Không tạo dialog sửa dòng tổng quát,
  không viết lại control nhập.
- `OrbitCellEditor` được giữ nguyên trong source để dùng chung làm ô nhập một dòng;
  đợt này không gắn nó làm overlay lên lưới.
- Không chạm các thay đổi đang có ngoài phạm vi trong worktree (`en.json`,
  `PurchasesControl`, `StockIssuesControl`, `StocktakesControl`, `OrbitDataGridView`,
  `docs/bugs/kho-nhap-xuat-kiem-ui/`).

## 1. Rút gọn `OrbitRowEditor` và thêm đường kích hoạt sửa

File: `src/CafeManager.WinForms/UI/OrbitRowEditor.cs`.

Việc làm:

- Bỏ field `editor`, `activeKey`, `activeProperty`, `armedKey`; bỏ đăng ký
  `CommitRequested`/`MoveRequested`/`CancelRequested`, `Scroll`/`SizeChanged`
  reposition editor.
- Bỏ `Save` delegate và toàn bộ luồng ghi ô: `OnCellClick` hai bước, `OpenByKeysAsync`,
  `OpenAsync`, `CommitAsync`, `WriteAsync`, `OpenNeighbourAsync`, `PositionEditor`,
  `CommitOpenEditorAsync`, `Display`.
- Giữ `OnCellClick` chỉ để xử lý cột `Delete`; click ô dữ liệu không làm gì ngoài
  chọn dòng mặc định của lưới.
- Thêm `public Func<object, Task>? Edit { get; set; }`.
- Đăng ký `grid.CellDoubleClick`: bỏ qua ô `Delete`, dòng không hợp lệ, dòng đang khóa
  (`unknown`), người dùng không có `CanWrite`; nếu dòng có ít nhất một cột sửa được
  thì gọi `Edit(item)` qua một nhánh riêng có cờ `activating` chống mở lặp.
- **Không đặt `sending`/`RowWriteBusy` trong suốt thời gian hộp sửa mở**: modal đã
  chặn lưới, và màn như `CatalogsControl` cần `IsBusy == false` để `RefreshRows()` chạy
  sau khi hộp đóng. Chỉ dùng `sending` cho xóa/đối chiếu như hiện tại.
- `OnGridKeyDown` F2: lấy dòng hiện tại, kiểm tra khóa/quyền rồi gọi cùng nhánh
  `Edit(item)`, không phụ thuộc cột hiện tại.
- Giữ `RunAsync` cho xóa/đối chiếu nhưng bỏ `editor.EndCommit()`.
- `EditableCell`: giữ điều kiện `ColumnFor` + `Editable` + `CanWrite` + chưa khóa.
- `CursorFor`: chỉ trả `Cursors.Hand` cho ô sửa được; bỏ `IBeam`.
- Giữ nguyên `Delete`, `Reconcile`, `ReloadAndSelect`, `Lock`, `EnsureButton`,
  `SyncButtons`, `IsBlockedRow`, `RowKey`, `CanWrite`, `Token`, `StateChanged`.
- Thu gọn `InlineColumn` còn metadata cần cho hover/quyền:
  `Property`, `Kind` và `Editable`; bỏ `Read`, `Parse`, `Options`, `RightAligned`
  vì không còn nhập tại ô.

Verify:

- Grep không còn tham chiếu `OrbitCellEditor` trong `OrbitRowEditor`.
- Build project sau khi cập nhật các màn (bước 10) không lỗi.
- Hover ô sửa được vẫn có màu Caramel và con trỏ Hand; ô đọc/vùng trống không đổi.
- Double-click và F2 chỉ mở hộp sửa, không mở editor tại ô.

## 2. Giữ helper tạo engine ở base

File: `src/CafeManager.WinForms/UI/OrbitRecordListControl.cs`.

Việc làm:

- Giữ `CreateInlineEditor()` và `RowWriteBusy`/`IsBusy` để các màn kế thừa dùng
  chung; cập nhật comment mô tả engine là “mở hộp sửa/xóa dòng”, không còn “sửa tại ô”.
- Không đổi `DispatchRowAction` (thùng rác) và `DispatchRowActivated` (hiện chưa màn
  nào dùng); engine tự nhận double-click nên không cần nối thêm `RowActivated`.
- Không đổi cơ chế `Reload`, `UpdateActions` hay `RowDeleteRequested`.

Verify:

- Các màn kế thừa vẫn dùng `CreateInlineEditor`; không có handler double-click trùng
  gây mở hộp sửa hai lần.

## 3. Menu sản phẩm

File: `src/CafeManager.WinForms/Modules/Menu/ProductsControl.cs`.

Việc làm:

- Đổi `Edit(bool create)` thành `Edit(bool create, ProductRow? row = null)`; khi sửa
  dùng `row`, khi thêm dùng `null`; giữ `OperationEditorDialog`, cách chuẩn hóa và
  reload/chọn dòng hiện có.
- Gỡ `inline.Save`, `SaveInline`, `Column` cũ, `CategoryName`, `CategoryCode`, `Money`.
- `inline.ColumnFor` chỉ trả metadata cho `Name`, `Category`, `Price`, `Status` để
  hover/quyền; `inline.Edit = item => Edit(false, (ProductRow)item)`.
- Giữ `inline.Delete`, `Reconcile`, `Reload`, `CanWrite`.

Verify:

- Click một lần chỉ chọn dòng; double-click mở hộp sửa đúng sản phẩm; F2 mở hộp sửa.
- Lưu đổi tên/giá/danh mục/trạng thái qua dialog, reload và chọn lại dòng; Hủy không ghi.
- Hover 4 cột sửa được; cột Mã không hover.

## 4. Danh mục nền

File: `src/CafeManager.WinForms/Modules/Catalogs/CatalogsControl.cs`.

Việc làm:

- Gỡ `inline.Save`, `SaveInline`, `Request`, `Column` cũ, `Money`, `Number`, `Clock`.
- `inline.ColumnFor` chỉ còn metadata để hover/quyền theo loại dòng:
  `Name`, `Phone`, `Ratio`, `Allowance`, `Factor`, `StartTime`, `EndTime`, `Type`,
  `Supported`; giữ điều kiện `Editable` của loại Thu/Chi hệ thống và `PT010`.
- `inline.Edit = item => Edit((CatalogRow)item)`.
- Giữ `Edit(CatalogRow?)` dùng `CatalogEditorDialog` và `RefreshRows`, xóa/đối chiếu.

Verify:

- Double-click ở từng tab mở đúng `CatalogEditorDialog` của tab đó.
- Sửa tên/SĐT/tỷ lệ/phụ cấp/hệ số/giờ/loại/hỗ trợ lưu đúng; Hủy không ghi.
- Hover chỉ ở cột sửa được; dòng hệ thống/PT010 không hover ở ô bị khóa.
- Đổi tab khi đang mở hộp sửa bị chặn như hiện tại.

## 5. Nguyên liệu

File: `src/CafeManager.WinForms/Modules/Inventory/MaterialsControl.cs`.

Việc làm:

- Thêm `Edit(MaterialRow? row)` dùng lại `OperationEditorDialog` và bố cục Tên/Đơn vị
  của `Create()`; khi sửa không thêm trường tồn đầu kỳ/nguồn tồn.
- Lưu bằng `MaterialService.Normalize(new(row.Code, name, unit))` +
  `service.SaveAsync(session, request, row.Revision, id)`; khi thêm giữ `Create()`.
- Gỡ `inline.Save`, `SaveInline`, `Column` parse; `inline.ColumnFor` chỉ metadata
  `Name`/`Unit`; `inline.Edit = item => Edit((MaterialRow)item)`.
- Giữ xóa có xác nhận, reconcile, reload, quyền ADMIN/STORE.

Verify:

- Double-click mở hộp sửa Tên/Đơn vị, không có tồn đầu kỳ; lưu reload đúng dòng.
- F2, Hủy, hover, thùng rác và Đối chiếu hoạt động như trước.
- Không đổi tồn kho khi sửa hồ sơ.

## 6. Khách hàng

File: `src/CafeManager.WinForms/Modules/Customers/CustomersControl.cs`.

Việc làm:

- Đổi `Edit(bool create)` thành `Edit(bool create, CustomerRow? row = null)`; dùng
  `row` thay vì `Selected` khi sửa; giữ `OperationEditorDialog`, note điểm/hạng và
  reload/chọn dòng.
- Gỡ `inline.Save`, `SaveInline`, `Column` parse; `inline.ColumnFor` chỉ metadata
  `Name`/`Phone`; `inline.Edit = item => Edit(false, (CustomerRow)item)` ở constructor
  quản trị, không nối ở constructor picker F3.
- Giữ xóa có xác nhận, reconcile, quyền ADMIN.

Verify:

- Chế độ quản trị: double-click/F2 mở hộp sửa; lưu/Hủy đúng; hover chỉ Tên/SĐT.
- Chế độ picker F3: double-click không mở sửa, không có thùng rác; chọn khách vẫn chạy.

## 7. Nhân viên

File: `src/CafeManager.WinForms/Modules/HumanResources/EmployeesControl.cs`.

Việc làm:

- Đổi `Edit(bool create)` thành `Edit(bool create, EmployeeRow? row = null)`; khi sửa
  lấy bản ghi mới theo `row.Code` qua `service.GetAsync` (giữ freshness như `Current`),
  nếu không còn thì báo lỗi; giữ `HrEditorDialog`, `CaptureCommand`, pending/reconcile.
- Gỡ `inline.Save`, `SaveInline`, `Column` parse, `Money`, `PositionCode` nếu không
  còn dùng; `inline.ColumnFor` chỉ metadata các cột sửa được; đặt
  `inline.Edit = item => Edit(false, (EmployeeRow)item)` chỉ ở tab Nhân viên.
- Giữ `Current()` cho Nghỉ/Đi làm lại và tài khoản; giữ xóa có xác nhận và reconcile.

Verify:

- Tab Nhân viên: double-click/F2 mở hộp sửa; lưu qua file chờ/reconcile như cũ.
- Tab Tài khoản không có engine double-click sửa hồ sơ.
- Hover, xóa, quyền ADMIN và dòng khóa vẫn đúng.

## 8. Tài sản

File: `src/CafeManager.WinForms/Modules/Assets/AssetsControl.cs`.

Việc làm:

- Đổi `Edit(bool create)` thành `Edit(AssetRow? row)`; truyền `row` vào
  `new AssetEditorDialog(service, session, pending, row, false)`. Nút Thêm/Yêu cầu chờ
  gọi `Edit(null)`.
- Gỡ `inline.Save`, `SaveInline`, `Column` parse, `Date`, `Money`, `Months`.
- `inline.ColumnFor` chỉ metadata `Name`/`PurchasedOn`/`Value`/`Months`;
  `inline.Edit = item => Edit((AssetRow)item)`.
- Giữ xóa có xác nhận, reconcile, quyền ADMIN/STORE, nút Chi tiết.

Verify:

- Double-click/F2 mở `AssetEditorDialog` ở chế độ Update đúng dòng; lưu/hủy và file
  chờ hoạt động như dialog Thêm.
- Chi tiết vẫn mở `AssetDetailDialog`; hover/xóa/Đối chiếu không đổi.

## 9. Bàn, khu vực và hạng thành viên

Files:

- `src/CafeManager.WinForms/Modules/Tables/TablesControl.cs`
- `src/CafeManager.WinForms/Modules/Loyalty/LoyaltyControl.cs`

Việc làm TableList/AreaList:

- Gỡ `inline.Save`, `SaveInline`, `Column` parse, `AreaCode`, `AreaName`, `AreaNames`.
- `inline.ColumnFor` chỉ metadata `Name`/`Area` cho bàn và `Name` cho khu vực.
- `inline.Edit = item => Edit((TableRow)item)` và
  `inline.Edit = item => Edit((AreaRow)item)`.
- Giữ click card Sơ đồ mở POS; chỉ double-click dòng ở chế độ Danh sách mới sửa.

Việc làm PolicyList:

- Gỡ `inline.Save`, `SaveInline`, `Column`, `Points`, `Percent`; giữ `Input`,
  `ShowEditor`, `DeleteTier`.
- `inline.ColumnFor` chỉ metadata `MinimumPoints`/`Discount`;
  `inline.Edit = item => ShowEditor((TierRow)item)`.
- Giữ bước preview số khách bị ảnh hưởng trước khi lưu và xóa có xác nhận.

Verify:

- Bàn/Khu vực: double-click mở đúng hộp sửa; card Sơ đồ vẫn mở POS; hover/xóa đúng.
- Hạng: double-click/F2 mở `ShowEditor`; bắt buộc xem lại SQL trước lưu; xóa preview.

## 10. Dọn tham chiếu và build

Việc làm:

- Grep toàn source cho `inline.Save`, `SaveInline`, `OpenByKeysAsync`, `CommitAsync`,
  `WriteAsync`, `OpenNeighbour`, `PositionEditor`, `OrbitCellEditor` để bảo đảm không
  còn lời gọi chết ngoài class `OrbitCellEditor` được giữ lại.
- Build Debug và Release bằng output tạm để tránh khóa DLL của app đang mở:
  `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Debug -p:BaseOutputPath=<temp>`.
- Không chạy test chức năng ở bước này.

Verify:

- Build Debug/Release 0 lỗi.
- Không còn cảnh báo đáng kể mới do thay đổi; không sửa file ngoài phạm vi.

## 11. Cập nhật quy tắc UI chung cho màn hiện tại và màn sau

Mục tiêu của bước này không chỉ sửa mô tả 8 màn mà chốt **quy tắc chung** để màn
master data làm sau không quay lại kiểu nhập tại ô.

Files:

- `docs/ui/HuongDan_UI_Orbit_Cafe.md` mục 11.5: đổi tiêu đề/nội dung “sửa tại ô, xóa
  tại dòng” thành “double-click mở hộp sửa, xóa tại dòng”; ghi rõ áp dụng cho **mọi
  màn master data hiện có và màn làm sau**. Thay mục 11.5.2 bằng luồng:
  - click một lần chỉ chọn dòng;
  - double-click ô dữ liệu hoặc F2 mở hộp sửa hiện có của màn;
  - không mở editor overlay, không nhập tại ô, không có con trỏ IBeam;
  - giữ hover Caramel/Hand, thùng rác, ma trận xác nhận xóa, khóa dòng/Đối chiếu;
  - màn mới bắt buộc tái dùng engine + hộp sửa hiện có, không tự dựng inline riêng.
  Cập nhật mục 11.5.1 (câu “sửa tại ô”) và 11.5.5 mô tả engine/hộp sửa.
- `docs/ui/WINFORMS_IMPLEMENTATION.md` mục 2.2: cập nhật bảng control/API
  (`OrbitRowEditor` mở hộp sửa, `OrbitCellEditor` là ô nhập dùng chung, không overlay),
  bỏ mô tả commit tại ô, bỏ câu “không thêm bảng sửa cho trường đơn giản đã inline”;
  thêm quy tắc màn master data mới phải dùng hộp sửa, không dùng inline cell editing.
- `docs/ui/README.md`: cập nhật câu trỏ mục 11.5 và hồ sơ task, nêu quy tắc áp dụng
  cho màn làm sau.
- `docs/UX_QuanLiQuanCafe.md` mục 10.1: đổi luồng click-hai-bước/nhập tại ô sang
  double-click/F2 mở hộp sửa; giữ xóa, hover, khóa dòng và Đối chiếu; ghi rõ màn mới
  theo cùng luồng.

Verify:

- Không còn tài liệu UI/UX mô tả click hai bước hoặc nhập tại ô cho master data.
- Có câu quy tắc rõ ràng cho màn master data làm sau: double-click/F2 mở hộp sửa,
  không nhập tại ô.
- Không ghi PASS hình thức/chức năng khi chưa chạy đợt test.

## 12. Đợt test đề xuất (chờ duyệt riêng)

Chỉ chạy sau khi người dùng duyệt đợt test. Môi trường: WinForms thật, SQL Server
test riêng, DPI 125%, source cố định. Lưu kết quả vào
`docs/tasks/row-edit-dialog/test-results.md`.

Ca chính:

1. 8 màn: click một lần chỉ chọn dòng; double-click mở đúng hộp sửa; F2 mở hộp sửa;
   Hủy không ghi; lưu reload và chọn lại dòng.
2. Hover: cột sửa được đổi Caramel/Hand; cột đọc, vùng trống, dòng khóa không đổi.
3. Xóa: ma trận xác nhận/xóa ngay, quyền, chặn FK, dòng khóa, Đối chiếu timeout.
4. Nguyên liệu: sửa Tên/Đơn vị, không lộ tồn đầu kỳ, không đổi tồn kho.
5. Tài sản: `AssetEditorDialog` Update đúng dòng; file chờ/đối chiếu; Chi tiết riêng.
6. Bàn: Danh sách double-click sửa; Sơ đồ click card mở POS.
7. Khách hàng picker F3: double-click không sửa; chọn khách vẫn đúng.
8. Hạng: preview ảnh hưởng bắt buộc trước khi lưu.
9. Quyền: vai trò không có quyền không thấy hover/mở hộp sửa.
10. Regression: POS, Khuyến mãi, Chấm công, chứng từ không đổi.

Không chạy kiểm hình thức/DPI 100%/150%; phần này người dùng tự kiểm theo phân công.

## 13. Rủi ro và cách kiểm soát

- **Double-click trùng handler**: engine và base cùng nhận `CellDoubleClick`; base chỉ
  gọi `RowActivated` nếu màn đăng ký, các màn này không đăng ký nên không mở hai lần.
- **Mất freshness của dòng**: mỗi `Edit` nhận row nhưng dialog/service dùng revision
  hiện có; nhân viên đọc lại bản ghi mới như luồng cũ; lỗi revision hiện ở hộp/status.
- **Hộp sửa đang mở mà engine bận**: dùng cờ `activating` riêng để chặn mở lặp; modal
  chặn thao tác lưới. Không dùng `sending`/`RowWriteBusy` trong lúc hộp mở vì có thể
  làm `CatalogsControl.RefreshRows` bỏ qua reload sau khi lưu.
- **Hành vi cũ còn sót**: xóa `SaveInline`/parse của từng màn và grep lại; giữ
  `OrbitCellEditor` nhưng không gắn overlay.
- **Tài liệu lệch code**: cập nhật cùng đợt, không tuyên bố PASS trước test.
