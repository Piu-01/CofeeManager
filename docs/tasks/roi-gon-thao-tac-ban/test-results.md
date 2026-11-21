# Test results — Rơi gọn thao tác Bàn / Khu vực và chọn bàn ở POS

- Ngày: 20/11/2026
- Môi trường: Windows, .NET SDK 10.0.401, DPI hệ thống 125% (DeviceDpi 120)
- Source: working tree `main` sau triển khai `docs/tasks/roi-gon-thao-tac-ban/` (chưa commit)
- Phạm vi đợt này: **chỉ compile + probe kỹ thuật**. Đợt kiểm chức năng/GUI thật (DB, phiên bàn, POS)
  **chưa chạy** và cần người dùng duyệt riêng theo post-change-test-workflow.

## Kết quả compile

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| RG-B01 | `dotnet build CafeManager.slnx` (BaseOutputPath tạm vì bin đang bị app/VS khóa) | PASS — 0 error, 2 warning có sẵn (WebView2/WindowsBase) | `runs/row-action-20261120/build.log` |

## Probe kỹ thuật (không thay thế kiểm chức năng)

Chạy `RowActionProbe.exe` (project ngoài repo `C:/Users/Lenovo/.qoder/tmp/cm-rowaction-probe/`,
ProjectReference tới `CafeManager.WinForms`). Probe không gọi SQL, không chụp hình, không kiểm hình thức.

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| RG-P01 | `OrbitRecordListControl.AddRowDeleteColumn` tạo cột `Delete` không tiêu đề, rộng 55px ở DPI 125% | PASS | `probe.json`: `ProbeDeleteColumn=true`, `ProbeDeleteWidth=55`, `ProbeDeviceDpi=120` |
| RG-P02 | Click ô `Delete` của dòng 0 phát `RowDeleteRequested` đúng item; double-click ô Delete không phát `RowActivated` | PASS | `DeleteRequests=["B0001"]`, `Activations=["B0002"]`, `DeleteOnlyFromIcon=true`, `DoubleClickIgnoredOnDeleteCell=true` |
| RG-P03 | Double-click ô thường phát `RowActivated` đúng item dòng 1 | PASS | `Activations=["B0002"]` |
| RG-P04 | `TableList` (ADMIN): không còn nút Chọn bàn/Trả bàn/Sửa/Xóa, giữ action Thêm bàn, có cột Delete và 2 hook dòng | PASS | `RemovedActionsGb=true`, `AddKept=true`, `TableDeleteColumn=true`, `TableRowDeleteHook=true`, `TableRowActivateHook=true` |
| RG-P05 | `SalesControl`: không còn nút "Chọn bàn", vẫn còn "Trả bàn" | PASS | `SalesHasNoSelectTable=true`, `SalesKeepsCloseTable=true` |

Ghi chú phạm vi probe: chưa kiểm paint icon thùng rác (chỉ kiểm cột/tương tác), chưa kiểm
`AreaList` runtime và luồng tự mở bàn ở POS vì cần bàn liệu SQL thật.

## Đợt bug RG-01 / RG-02 (fix 20/11/2026)

Đã sửa theo `bug-report.md` (đợt bug được duyệt):
- RG-01: `OrbitDataGridView` dùng `Property(column)` (fallback `Name` khi `DataPropertyName` rỗng)
  thay vì đọc thẳng `DataPropertyName`, nên cột `Delete` vào đúng nhánh vẽ icon.
- RG-02: `SalesControl` giữ `loading` của lần nạp gần nhất; thêm `OpenTableAfterLoadAsync` chờ
  `loading` xong rồi mới mở bàn; `MainForm.OpenSalesTableAsync` dùng method mới.

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| RG-B02 | Rebuild sau 2 fix (`dotnet build src/CafeManager.WinForms`, BaseOutputPath tạm) | PASS — 0 error, 1 warning có sẵn (WebView2/WindowsBase) | `runs/row-action-20261120/build-rg.log` |
| RG-P06 | Cột `Delete` phân giải property = "Delete" (fallback Name), cột thường vẫn = "Name" | PASS | `probe-rg.json`: `DeleteColumnProperty="Delete"`, `NameColumnProperty="Name"` |
| RG-P07 | Gọi thẳng nhánh `OnCellPainting` cho ô cột Delete trên bitmap: icon thùng rác được vẽ (không còn trống) | PASS | `DeleteIconPainted=true`, `DeleteIconPixels=23`, ảnh `runs/row-action-20261120/probe-delete-cell.png` |
| RG-P08 | `OpenTableAfterLoadAsync` chờ `loading` chưa xong (không hoàn tất trước gate) rồi hoàn tất sau khi gate xong, không lỗi, không gọi SQL (unknown=true) | PASS | `OpenWaitsForLoading=true`, `OpenCompletedAfterLoad=true`, `OpenFaulted=false` |
| RG-P09 | Toàn bộ probe cũ vẫn PASS sau fix (RG-P01–P05) | PASS | `TechnicalSuccess=true`, exit code 0 |

Lưu ý: RG-P07 là kiểm kỹ thuật "có vẽ icon", **không** thay nghiệm thu hình thức; màu/hover/khoảng cách
vẫn thuộc phần người dùng tự kiểm (RG-F07 SKIPPED).

## Đợt test chức năng RG-F01–F06 (21/11/2026)

- Môi trường: app build tạm `C:/Users/Lenovo/.qoder/tmp/cm-build-roi-gon2/Debug/net10.0-windows/CafeManager.WinForms.exe`,
  kết nối `CAFEMANAGER_APP_CONNECTION` → SQL Server thật, DB test riêng
  `CoffeeManager_Test_RG_20261121_a_ui` (bản copy `CoffeeManager_Development`, schema 36, tài khoản active).
- Tài khoản: `admin/Admin@123` (ADMIN), `thungan/Cash@123` (CASH). DPI hệ thống 125%.
- Điều khiển UI qua UIA (thư mục script tạm `C:/Users/Lenovo/.qoder/tmp/cm-rg-tests/`), không sửa source trong lúc test.
- Bằng chứng trong `runs/ui_20261121/` (dump UIA, danh sách control/text, giá trị ô nhập).

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| RG-F01 | POS: chọn bàn trống trong ô chọn bàn → vào đúng bàn, kênh Tại quán | PASS — chọn "Ban 02 · Trống", state "Đã chọn Ban 02 (trống). Thêm món đầu để tạo đơn." | `03-sales.txt`, `05-f01-select-ban02.txt` |
| RG-F02 | POS: chọn bàn đang có đơn → mở đơn; có nháp → hỏi xác nhận bỏ nháp | PASS (có ghi nhận RG-03) — combo chọn Ban 03 mở đúng HD26093335; bấm thẻ bàn B0008 mở đúng HD26093450; có ghi chú dở, chọn bàn khác hiện hộp "Bỏ input chưa ghi để tiếp tục?", Hủy → giữ đơn/ghi chú | `06-f02-select-ban08.txt`, `09-f02-card-ban08.txt`, `23-f02b-open-ban03.txt`, `24-f02b-draft-confirm.txt`, `25-f02b-cancel-kept-order.txt`, `25b-f02b-combo-state.txt` |
| RG-F03 | POS: Đơn mới/đổi kênh/tải lại/sau trả bàn không tự mở bàn lặp | PASS — bấm "Đơn mới" 2 lần + "Làm mới" + đổi kênh Mang đi/Tại quán: HOADON giữ 3454, POS vẫn "Đã chọn Ban 01 (trống)", không tạo đơn rỗng, không mở lặp | `21-f03-neworder-reload.txt` |
| RG-F04 | Bàn: double-click dòng mở Sửa đúng dòng; icon thùng rác mở Xóa + xác nhận; nhân viên CASH không có icon/không sửa | PASS — ADMIN double-click dòng mở "Sửa bàn" đúng B0001; icon thùng rác mở "Xóa bàn" đúng dòng; CASH không có phân hệ "Bàn / Khu vực" nên không có icon/sửa | `12-f04-edit-dialog.txt`, `12b-edit-values.txt`, `13-f04-delete-dialog.txt`, `17-cash-login.txt`, `18-cash-tables.txt` |
| RG-F05 | Khu vực: double-click/icon thùng rác đúng dòng; chặn xóa khu vực còn bàn | PASS — double-click mở "Sửa khu vực" đúng KV002/San vuon; icon mở "Xóa khu vực" đúng dòng; xác nhận → báo "Dữ liệu không thỏa ràng buộc…", DB vẫn còn KV002 và 2 bàn | `15-f05-area-edit.txt`, `15b-area-edit-values.txt`, `16-f05-area-delete.txt`, `16c-f05-area-delete-blocked.txt` |
| RG-F06 | Regression T34: trả bàn từ POS, "Trả bàn ngay?" sau thanh toán | PASS — Trả bàn khi còn đơn chưa chốt bị chặn "Bàn còn đơn chưa chốt; thu hoặc hủy trước khi trả bàn."; thanh toán HD26093455 → hỏi in hóa đơn → từ chối → hỏi "Trả bàn ngay?" → Có: PHIEN_BAN 8 đóng (GioRa), đơn "Da thanh toan", B0002 "Trong", POS về đơn mới | `26-f06-traban-confirm.txt`, `26b-f06-traban-blocked.txt`, `27-f06-post-payment-prompt.txt`, `28-f06-closed-session.txt` |
| RG-F07 | Hình thức UI (màu icon, hover, spacing) | SKIPPED theo phân công — người dùng tự kiểm | — |

Ghi chú phạm vi:
- Đơn cũ có `HOADON.MaPhien` NULL (vd. HD26093335 của seed/copy) thì nút "Trả bàn" disabled
  do thiết kế không backfill phiên — kiểm "Trả bàn" dùng đơn tạo mới qua UI (HD26093455), không phải lỗi.
- Ca RG-F02 phát hiện thêm lỗi nhất quán ô chọn bàn/nhãn bàn, ghi tại `bug-report.md` mục RG-03,
  **chưa sửa** trong lượt test này (đã sửa và regression ở mục dưới).

## Đợt regression RG-03 (21/11/2026)

- Fix được duyệt: `SalesControl.LoadOrder` đồng bộ ô chọn bàn theo bàn của đơn; `OpenTableAsync` giữ
  bàn thực của ngữ cảnh và trả lại khi người dùng Hủy bỏ nháp; helper `ActiveTableCode()`.
- Môi trường: build tạm mới `C:/Users/Lenovo/.qoder/tmp/cm-build-roi-gon3/Debug/net10.0-windows/CafeManager.WinForms.exe`
  (xác nhận path tiến trình đang chạy), cùng DB test `CoffeeManager_Test_RG_20261121_a_ui`, `admin/Admin@123`,
  DPI 125%. Không sửa source trong lúc test.

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| RG-B03 | Build sau fix RG-03 | PASS — 0 error, 1 warning có sẵn (WebView2/WindowsBase) | `29-build-rg3.log` |
| RG-F08 | Màn Bàn: bấm thẻ "Ban 08 · Đang dùng" → POS mở HD26093450 và ô chọn bàn/nhãn bàn hiển thị đúng Ban 08 (trước fix bị kẹt Ban 02) | PASS — combo VALUE `Ban 08 · HD26093450`, nhãn "Ban 08 · Đang dùng", header "Bàn B0008" | `30-rg3-tables.txt`, `31-rg3-a-open-ban08.txt` |
| RG-F09 | Mở Ban 03 (HD26093335), mở ghi chú dòng, chọn Ban 09 rồi Hủy ở hộp "Dữ liệu chưa lưu" → giữ đơn Ban 03 và ô chọn bàn trả lại Ban 03 | PASS — sau Hủy: order vẫn HD26093335/B0003, combo VALUE `Ban 03 · HD26093335`, nhãn "Ban 03 · Đang dùng", ghi chú vẫn mở | `32-rg3-b-open-ban03.txt`, `33-rg3-b-note-editor.txt`, `34b-rg3-b-cancel-restored.txt` |
| RG-F10 | Sanity nghiệp vụ liên quan: chọn bàn trống Ban 09 → ngữ cảnh đơn mới đúng bàn; chọn lại "Ban 03 · HD26093335" → mở đơn; "Làm mới" giữ đơn; "Đơn mới" chọn bàn trống Ban 01 | PASS — lần lượt combo/nhãn `Ban 09 · Trống`, `Ban 03 · HD26093335` (giữ sau Làm mới), `Ban 01 · Trống` sau Đơn mới | `35-rg3-c-select-empty-ban09.txt`, `36-rg3-c-combo-open-ban03.txt`, `37-rg3-c-reload.txt`, `38-rg3-c-neworder.txt` |

Ghi chú:
- Lần bấm Hủy đầu tiên trong RG-F09 không trúng nút (hộp vẫn mở, combo còn Ban 09); bấm lại đúng nút
  Hủy thì combo trả về Ban 03 — lỗi thao tác UIA của lượt test, không phải lỗi app; bằng chứng chốt là
  `34b-rg3-b-cancel-restored.txt`.
- **Quan sát ngoài phạm vi RG-03:** dòng gợi ý `state` đôi khi còn câu cũ sau khi mở đơn (vd. đang mở
  HD26093335 nhưng gợi ý vẫn "Đã chọn Ban 09. Thêm món đầu để tạo đơn.", thấy ở
  `36-rg3-c-combo-open-ban03.txt`; tương tự `31-rg3-a-open-ban08.txt`). Đã ghi thành bug RG-04 và
  sửa ở mục dưới sau khi được duyệt.

## Đợt dọn RG-04 — gợi ý `state` sau khi mở đơn (21/11/2026)

- Phạm vi: chỉ câu gợi ý ở POS sau khi mở đơn; không đổi đơn/bàn/phiên/tiền. Chi tiết bug:
  `bug-report.md` mục RG-04.
- Môi trường: build tạm `C:/Users/Lenovo/.qoder/tmp/cm-build-roi-gon4/Debug/net10.0-windows/CafeManager.WinForms.exe`,
  DB test `CoffeeManager_Test_RG_20261121_a_ui`, `admin/Admin@123`, DPI 125%. Không sửa source trong lúc test.

| Mã | Nội dung | Kết quả | Bằng chứng |
|---|---|---|---|
| RG-B04 | Build sau fix RG-04 | PASS — 0 error, 1 warning có sẵn | `39-build-rg4.log` |
| RG-F11 | Mở thẻ Ban 08 từ màn Bàn → gợi ý đổi thành "Đã mở đơn HD26093450." (không còn câu "Đã chọn Ban …") | PASS — header B0008, combo/nhãn Ban 08, gợi ý đúng đơn | `40-rg4-card-ban08.txt` |
| RG-F12 | Giữ đúng luồng RG-F09 (nháp + Hủy) và combo: Hủy → combo/nhãn trả Ban 03, gợi ý "Đã mở đơn HD26093335."; chọn Ban 09 rồi mở lại đơn qua combo → gợi ý đúng đơn; "Làm mới" → "Đã tải từ SQL", giữ đơn | PASS — không tái phát RG-03/RG-04 | `41-rg4-cancel-restored.txt`, `42-rg4-combo-open-ban03.txt`, `43-rg4-reload.txt` |

## Tồn đọng từ bug T34-02

Trước đây UI thật/regression T34-02 NOT RUN vì `bin` bị Microsoft Visual Studio/CafeManager.WinForms
khóa. Trong đợt 21/11/2026 đã chạy regression T34 trên build tạm (không dùng `bin` repo) và PASS —
xem RG-F06 ở trên; chi tiết lịch sử vẫn tại `docs/tasks/g04-ban-hang-vong-doi-hoa-don/t34-phien-ban/test-results.md`.
