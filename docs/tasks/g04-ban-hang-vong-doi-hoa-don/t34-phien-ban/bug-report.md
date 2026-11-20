# Bug T34-01 — `sp_InvoiceDetail` vẫn trả 25 cột header sau khi DAL đọc 27 cột

Trạng thái: **đã sửa và regression PASS ngày 17/11/2026** (migration 035; run
`technical_20261117_020349_e6dedcdf` typed+native Success, run
`acceptance_20261117_020633_de5483fe` 5/5 SQL subset PASS, unit 23/23).
Mức độ: chặn nghiệm thu (chi tiết hóa đơn/in lại/giao hàng lỗi runtime).

## Hiện tượng

- Technical/typed smoke v17 (`Invoke-G04Technical.ps1 -ThroughVersion 17`, run
  `technical_20261117_001148_f2533692`) dừng với `Success=false`.
- `typed-smoke.json`: `System.IndexOutOfRangeException` tại
  `SalesRepository.ReadOrder` dòng 89 (`r.IsDBNull(26)`), gọi từ
  `InvoiceRepository.ReadDetail` dòng 23.
- Đợt acceptance (`Test-G04.ps1 -ApprovedAcceptance`, run
  `acceptance_20261117_001407_0ae1ea62`) dừng ở typed smoke với cùng lỗi, nên
  unit test không chạy trong lượt đó.

## Nguyên nhân (bằng chứng)

- T34 thêm hai cột `MaPhien`/`SessionRevision` vào `SalesOrderHeader` và
  `SalesRepository.ReadOrder` đọc chỉ số 25/26 (`src/CafeManager.DataAccess/Repositories/SalesRepository.cs:86-94`).
- `sp_SalesReadCore` (migration 034) đã trả 27 cột, nhưng `sp_InvoiceDetail`
  (migration 013, dòng 26) vẫn là câu SELECT 25 cột cũ, không có `MaPhien`.
  Kiểm trên DB scratch schema034:
  `OBJECT_DEFINITION(sp_InvoiceDetail) LIKE '%MaPhien%'` → `no MaPhien`.
- Vì `InvoiceRepository.ReadDetail` dùng chung `ReadOrder`, mọi luồng chi tiết
  hóa đơn (lịch sử, xem/in lại, chi tiết giao hàng qua `sp_DeliveryGet` →
  `sp_InvoiceDetail`) sẽ ném `IndexOutOfRangeException`.

## Đề xuất sửa

1. Migration mới `035_sales_invoice_detail_sessions.sql`: `ALTER PROCEDURE
   dbo.sp_InvoiceDetail` để header trả đủ 27 cột, thêm
   `h.MaPhien, sessionRow.Revision SessionRevision` với
   `OUTER APPLY(SELECT TOP(1) s.Revision FROM dbo.PHIEN_BAN s WHERE s.MaPhien=h.MaPhien)`.
   Không đổi tiền/snapshot/cột khác; 034 đã áp nên không sửa 034.
2. Cập nhật runner test: `ThroughVersion 18` → `SchemaVersion 35`
   (`Test-G04.ps1`, `Invoke-G04Technical.ps1`), thêm bước dependency cho 035.
3. Chạy lại typed smoke + acceptance batch; bổ sung unit test map header 27 cột
   nếu chưa có.
4. Đính chính `t35-tuong-thich-tai-lieu/audit.md` (kết luận "không cần migration"
   là thiếu trường hợp `sp_InvoiceDetail`).

## Phạm vi ảnh hưởng / rủi ro

- Chỉ đọc; không đổi dữ liệu tiền/kho/điểm/phiên.
- Ảnh hưởng: lịch sử hóa đơn (chi tiết), in lại, chi tiết giao hàng.
- Không sửa trong lượt test hiện tại; cần duyệt bổ sung trước khi áp migration 035.

## Kết quả regression sau sửa

- `Invoke-G04Technical.ps1 -ThroughVersion 18` — run
  `technical_20261117_020349_e6dedcdf`: `Succeeded`, schema **035**, typed smoke
  `Success=true` (`InvoiceRows=1`, `PaidCode=HD20250412`) và native smoke
  `Success=true` (`InvoiceRows=1`, `DeliveryRows=3`, DPI 120); `SourceUnchanged=true`.
- `Test-G04.ps1 -ApprovedAcceptance` — run
  `acceptance_20261117_020633_de5483fe`: 5/5 SQL subset PASS
  (`orders`, `checkout`, `delivery`, `pending-payment`, `table-sessions`), typed smoke
  `Success=true` (`PaidCode=HD20250427`), unit Sales **23/23 PASS**,
  `SourceUnchanged=true`.
- `technical-v18.sql` kiểm trực tiếp: `sp_InvoiceDetail` trả `MaPhien`/`SessionRevision`
  đúng phiên (~`OUTER APPLY` trên `PHIEN_BAN`), gọi được cho hóa đơn tại quán, đơn
  hủy/phiên đóng sạch transaction.

## Ghi chú phụ (không phải bug sản phẩm)

`sp_DashboardAnalyticsSources` từ chối khi `@@TRANCOUNT<>0`; `INSERT…EXEC` mở
transaction ngầm nên SQL subset không thu được resultset nguồn. Đây là hành vi
có chủ đích của contract tổng quan; script PP09 chuyển sang kiểm
`sp_ReportDashboard` và ghi phần nguồn là giới hạn.

# Bug T34-02 — Nút "Trả bàn" trên POS bị nút "F3 · Khách / Ưu đãi" che mất

Trạng thái: **đã sửa source ngày 20/11/2026, chờ duyệt đợt test**.
Mức độ: chặn thao tác trả bàn thủ công trên POS (không mất dữ liệu).

## Hiện tượng

- Màn Bán hàng, chọn bàn phiên đã trả ("Đã trả · giữ bàn"): khung đơn chỉ hiển thị
  "F3 · Khách / Ưu đãi" và "Hủy đơn mở"; không thấy nút "Trả bàn" dù bàn có phiên
  và nút đã bật theo điều kiện.

## Nguyên nhân (bằng chứng)

- `SalesControl.cs:100-103` thêm ba nút vào `orderPanel.HeaderActions` theo thứ tự
  `[header, closeTable, cancel]`.
- `OrderPanel.OnLayout` (trước sửa, dòng 233-247) chỉ `SetBounds` cho
  `headerButtons[0]` và `headerButtons[^1]`; nút giữa (`closeTable` = "Trả bàn")
  giữ vị trí mặc định `(0,0)` nên bị vẽ chồng dưới nút đầu (z-order: control thêm
  trước nằm trên; `OrbitButton.PaintManagement` tô nền đục che kín).
- Bố cục "nút đầu trái, nút cuối phải" có từ thiết kế 2 nút; commit `5673f51` thêm
  `closeTable` vào giữa nhưng không cập nhật `OnLayout`.
- Điều kiện bật (`SalesControl.cs:205-206`) và `CloseTable()` đúng; chỉ lỗi hiển thị.

## Đề xuất sửa / đã sửa

- `OrderPanel.OnLayout`: bố trí mọi nút của `HeaderActions` — nút đầu sát trái, các
  nút còn lại dồn sát phải và xếp từ phải sang trái, cách nhau 8 logic px; nút đầu
  co lại khi chật. Không đổi vị trí panel, nút thanh toán hoặc dòng món.

## Phạm vi ảnh hưởng / rủi ro

- Chỉ bố cục vùng header khung đơn POS. Không đổi SQL, quyền, transaction, dữ liệu.
- Panel hẹp: nhãn nút đầu bị ellipsis nhiều hơn trước.
- Chưa chạy test (chờ duyệt đợt test); hình thức do người dùng tự kiểm.
