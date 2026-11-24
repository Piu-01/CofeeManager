# Bug T20b — Danh sách Nhập hàng: cột “Nợ gốc” để trống ở phiếu đã trả đủ ngay

## Hiện tượng

Người dùng báo trên màn Quản lý / Kho / Nhập hàng (24/11/2026): các phiếu đã chốt và
trả đủ ngay (ví dụ `PN26090017`, `PN26090012`, `PN26090007`) có cột **NỢ GỐC** để trống,
trong khi các phiếu còn nợ hiển thị số tiền. Ô trống trông như lỗi tải dữ liệu.

Ảnh: `C:\Users\Lenovo\.qoder\tmp\D--Workspace-Nam-3-HK1-CoffeeManager\images\ca8604db-9bb3-4c5e-b442-cb0b222eb52d\1ac5ba07-b5c6-4148-bd7c-06839ada4bbe.png`.

## Nguyên nhân và bằng chứng

- `vw_PurchaseRead` lấy `Debt = c.TienNo` từ `CONGNO_NCC` (LEFT JOIN). Phiếu trả đủ ngay
  không có dòng công nợ nên `Debt = NULL`:

  | Code | CutoverKind | Total | Immediate | Debt | TrangThai |
  |---|---|---|---|---|---|
  | PN26090020 | New | 360.000 | 0 | 360.000 | DaChot |
  | PN26090017 | New | 3.305.000 | 3.305.000 | NULL | DaChot |
  | PN26090015 | New | 4.350.000 | 2.175.000 | 2.175.000 | DaChot |
  | PN20250202 | Legacy | 625.000 | NULL | NULL | DaChot |

- `PurchasesControl` bind cột `Debt` trực tiếp; `G05Display.Grid` đặt `Format="N0"`.
  Giá trị `NULL` của `decimal?` hiển thị thành ô trống. Đây là hiển thị, không phải mất dữ liệu:
  DB đúng (không có công nợ khi trả đủ).
- Ô **TRẢ NGAY** của 10 phiếu `CutoverKind='Legacy'` cũng trống vì view trả `Immediate = NULL`
  cho dữ liệu trước chuyển đổi — cùng kiểu “ô trống trông như lỗi”.

## Đề xuất sửa (đã duyệt 24/11/2026, đã áp dụng hiển thị)

1. Chỉ sửa hiển thị tại `PurchasesControl` (CellFormatting): với dòng `CutoverKind="New"`,
   `Status="DaChot"`, `Debt = NULL` thì hiển thị `0` ở cột NỢ GỐC. Không đổi view/DTO để
   không phá fallback “Nợ còn dự kiến” của phiếu Chờ nhận/Đã kiểm trong chi tiết
   (`row.Debt ?? actual - immediate`).
2. (Tùy chọn) Dòng Legacy có `Immediate = NULL` hiển thị `—` thay vì ô trống, để phân biệt
   “không có dữ liệu lịch sử” với số 0.

Phạm vi ảnh hưởng: chỉ cột hiển thị trong danh sách Nhập hàng; không đổi SQL, không đổi
số liệu, không đổi in phiếu. Rủi ro: thấp; cần xem lại danh sách sau sửa (New trả đủ = 0,
New còn nợ giữ nguyên, Chờ nhận/Đã kiểm giữ trống theo thiết kế).

## Trạng thái

Đã sửa hiển thị tại `PurchasesControl.FormatAmounts` (24/11/2026), build Debug thành công.
Bản đầu tiên gán `e.Value=0m` kèm `FormattingApplied=true` làm DataGridView báo
`FormatException: Formatted value of the cell has a wrong type` (ô văn bản yêu cầu
FormattedValue kiểu chuỗi); đã đổi sang chuỗi `"0"` và build lại thành công.
Chưa chạy kiểm thử GUI theo phân công; chờ duyệt đợt test và người dùng kiểm hình thức.

Cần kiểm khi chạy test:
- `PN26090017` (New, trả đủ) hiển thị NỢ GỐC `0`, không còn ô trống.
- `PN26090020`/`PN26090015` (New, còn nợ) giữ nguyên số nợ.
- Phiếu New `ChoNhan`/`DaKiem` vẫn để trống NỢ GỐC (nợ còn dự kiến chỉ hiện trong chi tiết).
- Dòng Legacy (`PN20250202`…) hiển thị TRẢ NGAY `—`, các cột khác không đổi, không lỗi tải.
- Mở chi tiết/in phiếu các dòng trên không thay đổi số liệu.

## Sự cố phát sinh khi mở dialog Lập phiếu nhập sau thiết kế lại

- Triệu chứng: mở dialog báo `ArgumentOutOfRangeException: Minimum width provided to the
  DataGridViewColumn is smaller than 2. Actual value was 0` tại `PurchaseEditorDialog.SizeIndex`.
- Nguyên nhân: `SizeIndex` gán `MinimumWidth = 0` cho cột `#`; `DataGridViewColumn` yêu cầu tối thiểu 2.
- Sửa: đặt `MinimumWidth = Px(grid, 40)` trước khi đặt `Width = Px(grid, 44)`.
- Kiểm chứng 24/11/2026: build Debug thành công; probe tạo handle toàn bộ control của dialog
  (không chỉ constructor) và paint thử `PurchasesControl` với 3 dòng New trả đủ / New còn nợ /
  Legacy — không còn exception. Chưa chạy kiểm thử GUI/hình thức theo phân công.

## Đợt 24/11/2026 — Ô ngày, số lượng thực, nút Kiểm tra tổng, dữ liệu thử

Người dùng báo trên dialog Lập phiếu nhập: ô ngày hiển thị lỗi, cần cho nhập số lượng
thực, nút “Kiểm tra tổng” không rõ tác dụng, và thiếu phiếu ở các trạng thái khác để thử.

### Đã sửa trong source

1. **Ô ngày**: thay `DateTimePicker` thô bằng `OrbitDateField` cho cả “Ngày nhập” và
   “Hạn trả dự kiến”; hạn trả có checkbox “Có hạn” (chỉ bật khi phiếu còn nợ và còn sửa).
   Giá trị nhập sai định dạng `dd/MM/yyyy` hoặc ngày tương lai được giữ nguyên dữ liệu,
   báo lỗi tại ô và đưa focus về ô thay vì ném exception. `OrbitDateField` thêm
   `FocusInput()` nội bộ để dialog đặt focus.
2. **Số lượng thực**: grid, dialog nhận hàng và BLL dùng định dạng `0.####`; BLL nhận
   `0 < Số lượng ≤ 99999999999999,9999`, tối đa 4 chữ số thập phân; đơn giá vẫn nguyên VND.
   Thực nhận nhận `0 ≤ x ≤ 99999999999999,9999`, tối đa 4 chữ số thập phân.
   Migration 045 cập nhật procedure tương ứng (xem `docs/database/migrations.md`).
3. **Nút “Kiểm tra tổng”**: gỡ khỏi thanh hành động vì trùng việc — dialog đã tự tính tổng
   theo dòng và nút “Lưu phiếu chờ nhận” vẫn gọi `PreviewAsync` để server kiểm tra trước khi
   tạo yêu cầu. Không đổi contract lưu phiếu.
4. **Dữ liệu thử**: thêm `database/demo/seed-demo-purchase-statuses.sql` với 5 phiếu
   Chờ nhận/Đã kiểm/Đã hủy, số liệu và lý do lệch thực nhận sát vận hành; đã chạy trên
   `CoffeeManager_Development`. Phiếu chưa ghi kho giữ `TongTien = 0` theo bất biến.

### Kiểm chứng đã chạy

- Build WinForms Debug: 0 lỗi (1 cảnh báo WindowsBase sẵn có).
- Migration 045 áp `Applied`; seed chạy không lỗi; `sqlcmd` xác nhận đúng trạng thái,
  dòng hàng, thực nhận/lý do.
- Smoke SQL `sp_PurchaseValidate`: dòng 2,5 tính tổng 4.608.000; 2,50001 bị từ chối 52701.
- Probe reflection (dựng dialog bằng `Show` trên STA, không phải nghiệm thu GUI): xác nhận
  hai ô ngày là `OrbitDateField`, checkbox “Có hạn”, không còn field/nút `preview`,
  định dạng `Quantity`/`Stock` là `0.####`, giá trị ngày round-trip đúng, BLL nhận 2,5 và
  từ chối 2,50001/0. Probe dùng `CreateControl` thô (không `Show`) làm `DataGridView` ném
  `ArgumentOutOfRangeException` lúc `Dispose`; lỗi chỉ tái hiện ở cách dựng headless đó,
  dựng bằng `Show` dispose sạch nên không sửa production.
- **Đợt test SQL 24/11/2026 (đã duyệt): 35/35 PASS**, schema 45, `InventoryUnchanged=true`,
  DB test đã dọn — bằng chứng `runs/acceptance_20261124_171230_15f773d7/`. Lượt đầu
  `acceptance_20261124_171145_7e2a9fd7` FAIL do test fixture giữ revision cũ sau khi ca
  fraction chuyển sang lưu thành công; đã sửa fixture dùng `@revCount` mới, không đổi kỳ vọng
  và không sửa source/migration trong lượt test.
- **Chưa chạy**: kiểm GUI thật end-to-end, kiểm hình thức, DPI 100/150, hai session song song,
  fault injection/timeout. Người dùng tự kiểm hình thức theo phân công; T20b vẫn Đang làm.

### Lưu ý khi thử

- Danh sách Nhập hàng hiển thị `TỔNG = 0` cho phiếu Chờ nhận/Đã kiểm mới vì chưa ghi kho;
  đây là số liệu đúng theo thiết kế, không phải lỗi tải. Nếu muốn danh sách hiện tạm tính
  theo dòng, cần đổi view/DTO và phải duyệt phạm vi riêng.
- Phiếu Đã hủy chỉ xem/lịch sử; không sửa/xóa. Phiếu Đã kiểm mở chi tiết để xem lý do lệch.
