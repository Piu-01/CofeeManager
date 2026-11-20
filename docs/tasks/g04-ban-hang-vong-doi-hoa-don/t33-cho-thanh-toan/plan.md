# Plan T33 — Đơn "Chờ thanh toán"

Ngày 16/11/2026. Trạng thái: **đã triển khai, chạy đợt test 17/11/2026, regression sau
sửa T34-01 và UI 18/11/2026; đủ điều kiện nghiệm thu** — PP01–PP12 PASS (PP09 còn giới
hạn nguồn tổng quan; hình thức/DPI 100–150% người dùng tự kiểm)
([test-results.md](test-results.md)).
Thuộc phần bổ sung G04 đã duyệt requirements tại
[requirements-cho-thanh-toan-phien-ban.md](../requirements-cho-thanh-toan-phien-ban.md).
Giữ nguyên 56 ID T14–T18, 325 ID toàn dự án và lịch sử test; T33 là task mới, không
đánh lại ID cũ.

Ghi nhận triển khai: migration `032_sales_pending_payment.sql` đã tạo và áp thử trên
DB scratch riêng (không phải Development); Models/DAL/BLL/POS đã sửa theo các bước
dưới; `en.json` đã thêm nhãn/nút/thông báo mới; xUnit predicate/nhãn PASS; build
Debug/Release 0 lỗi. Acceptance SQL và milestone kỹ thuật `v16` đã chuẩn bị nhưng
**chưa chạy** — chờ duyệt đợt test. Ghi chú: 033 đã thuộc task khác nên T34 dùng 034.

## 1. Hiện trạng đã đối chiếu source

- `HOADON.TrangThai` chỉ nhận 3 giá trị (`database/DB_Schema.sql:333-334`);
  `CHK_HD_NgayTT` buộc `Da thanh toan` ⇔ có `NgayThanhToan` (`:325-327`).
- `UX_HD_BanMo` chỉ chặn trùng đơn `Chua thanh toan` (`:425-426`);
  `vw_TrangThaiBan` chỉ join đơn `Chua thanh toan` (`:498-503`).
- Một cổng dùng chung cho cả sửa và thu: `sp_SalesCheckOpen`
  (`database/migrations/010_sales_orders.sql:35-41`). Nó đang được gọi từ
  `sp_SuaDongHoaDon`, `sp_SuaThongTinHoaDon`, `sp_SalesCheckoutPreview`
  (`011_checkout.sql:44`), `sp_ThanhToanHoaDon` (`:66`), `sp_HuyHoaDon`
  (`012_sales_cancel.sql:16`).
- `sp_ThanhToanHoaDon` chốt `Da thanh toan` (`011_checkout.sql:105`);
  `TR_CTTT_KiemTra` (`016_payment_guard_point_locks.sql:7-30`) chỉ cho ghi thanh toán
  khi đơn `Chua thanh toan`; `TR_DiscountSnapshot` (`011:16-21`) và
  `TR_TieuHao_Kho` (`DB_Schema.sql:594+`) cũng chỉ cho đơn `Chua thanh toan`.
- `TR_CTHD_TongTien` đã chặn mọi dòng món khi trạng thái khác `Chua thanh toan`
  → tự động chặn `Cho thanh toan`, không cần sửa.
- `TR_HD_Chot` (`DB_Schema.sql:534-565`) chỉ khóa đơn `Da thanh toan`/`Huy`; chuyển
  `Chua → Cho → Da/Huy` không bị chặn. Ghi trực tiếp `HOADON` đã bị `DENY`
  (`001_contract_and_code_generation.sql:69`) nên chỉ procedure mới đổi trạng thái.
- Đọc/đếm đang hard-code `Chua thanh toan`: `sp_SalesReadCore` (`015:5-17`),
  `sp_SalesOpenOrders` (`010:199-206`), điều kiện 52104 trong `sp_TaoHoaDon`
  (`015:41`) và `sp_TaoHoaDonTuNhap` (`030:37`), `sp_ReportDashboard`
  (`026_reports.sql:135`), `orders.open` (`028:67`), nguồn `orders` (`029:31`).
- UI khóa theo `SalesOrderDto.IsOpen => Status == "Chua thanh toan"`
  (`SalesOrderDto.cs:9`), dùng ở `SalesControl.SetBusy` (`:179-193`), hủy (`:629`),
  checkout (`:638`), `CheckoutDialog`, `InvoiceDetailDialog.cs:33`; nhãn trạng thái ở
  `SalesStates.cs:8`, `OrbitRecordListControl.cs:58`, badge ở
  `OrbitDataGridView.cs:433-435`, panel đơn `OrderPanel.cs:55-56`, lọc hóa đơn
  `InvoicesControl.cs:19`.

## 2. Quyết định triển khai

1. **Bốn trạng thái**: thêm `Cho thanh toan`; giữ nguyên chuỗi DB không dấu, nhãn UI
   "Chờ thanh toán". Không thêm trạng thái theo món/đã ra món.
2. **Tách hai cổng SQL**:
   - `sp_SalesCheckOpen` giữ nguyên nghĩa **còn sửa** (chỉ `Chua thanh toan`) cho
     `sp_SuaDongHoaDon`/`sp_SuaThongTinHoaDon`.
   - `sp_SalesCheckPayable` mới: **còn thu/hủy** (`Chua thanh toan` hoặc
     `Cho thanh toan`), cùng kiểm revision, không có thanh toán/tiêu hao.
3. **Chuyển trạng thái** đúng bảng requirements: `Chua→Cho` (Sales), `Cho→Chua`
   (ADMIN, receipt), `Cho→Da`/`Cho→Huy` (checkout/hủy hiện có), `Chua→Da` giữ nguyên.
4. **Không thêm trigger khóa field cho `Cho`**: `sp_ThanhToanHoaDon` cập nhật `GiamGia`
   khi đơn còn `Cho` trước khi chuyển `Da` (`011:100-105`), nên khóa `GiamGia` ở
   trigger sẽ làm hỏng checkout. Khóa sửa dựa vào procedure gate + `DENY` trực tiếp;
   `TR_CTHD_TongTien` đã chặn dòng món. Ghi rõ quyết định này trong contract.
5. **In**: chỉ thêm nhãn trạng thái; không tạo mẫu "phiếu tạm tính" mới (requirements
   §5). Nút "Yêu cầu thanh toán" có thể mở lại preview hiện có sau khi đổi trạng thái.
6. **Đếm đơn mở/báo cáo** phải gồm `Cho` ngay trong T33, không để dashboard hiểu sai.
7. Migration đánh số **032 tạm thời**; nếu migration cấp quyền cho 031 được tạo trước
   thì dùng số kế tiếp còn trống tại thời điểm triển khai, không sửa migration đã áp.
8. Mã lỗi mới: `52108` (chưa đủ điều kiện yêu cầu thanh toán), `52109` (chỉ mở lại
   đơn đang chờ thanh toán). Quyền thiếu tiếp tục dùng `51605`, stale dùng `52103`.

## 3. Các bước triển khai

### Bước 1 — Migration 032: trạng thái, view, cổng, procedure

Files: `database/migrations/032_sales_pending_payment.sql` (mới),
`docs/database/migrations.md`, `docs/database/sales-contract.md`.

- Precondition `SchemaMigrations Version=31`; `SET NOCOUNT ON; SET XACT_ABORT ON;`
  theo mẫu 011/016. Preflight: không có đơn mở nào đã có thanh toán/tiêu hao;
  không có bàn >1 đơn mở (index cũ đã bảo đảm); rollback nguyên trạng nếu sai.
- `DROP CONSTRAINT CHK_HD_TrangThai`, tạo lại với 4 giá trị.
- `DROP INDEX UX_HD_BanMo`; tạo lại unique filtered
  `WHERE MaBan IS NOT NULL AND TrangThai IN (N'Chua thanh toan',N'Cho thanh toan')`.
- `ALTER VIEW vw_TrangThaiBan`: join đơn mở theo 2 trạng thái; trả `Cho thanh toan`
  khi đơn đang chờ, còn lại `Dang phuc vu`/`Trong`; giữ nguyên tên/độ rộng cột cũ.
- Tạo `sp_SalesCheckPayable @Code,@ExpectedRevision`: khóa header
  `UPDLOCK,HOLDLOCK`, yêu cầu trạng thái ∈ (`Chua`,`Cho`), revision khớp, chưa có
  `CHITIET_THANHTOAN`/`TIEUHAO_HOADON`; lỗi 52102/52103 như cổng cũ.
- `ALTER` ba procedure thu/hủy để dùng `sp_SalesCheckPayable` thay `sp_SalesCheckOpen`:
  `sp_SalesCheckoutPreview` (011 bản đầy đủ), `sp_ThanhToanHoaDon` (011 bản đầy đủ),
  `sp_HuyHoaDon` (012 bản đầy đủ). Không đổi phần tính tiền/kho/điểm.
- Tạo `sp_YeuCauThanhToan @Actor,@Stamp,@Code,@ExpectedRevision,@RequestId`:
  transaction + `sp_SalesBegin(...,'SalesRequestPay',...)`; khóa
  `SalesOrder:<MaHD>`; `sp_SalesCheckOpen`; yêu cầu `TongTien>0` và có ≥1 dòng
  (nếu không `THROW 52108`); `UPDATE HOADON SET TrangThai=N'Cho thanh toan'`;
  `sp_SalesFinish`; trả receipt.
- Tạo `sp_MoLaiHoaDon @Actor,@Stamp,@Code,@ExpectedRevision,@RequestId`:
  transaction; `sp_G03Demand @Actor,@Stamp,'ADMIN'`; receipt operation
  `SalesReopen`; khóa order + header; yêu cầu trạng thái `Cho` và revision khớp
  (nếu không `THROW 52109`); chưa có tiền/tiêu hao; đặt lại `Chua thanh toan`;
  `sp_SalesFinish`; trả receipt.
- `ALTER sp_SalesReconcile`: thêm `SalesRequestPay`,`SalesReopen` vào whitelist
  operation; vẫn kiểm Actor/receipt.
- `ALTER` các đọc/đếm để gồm `Cho thanh toan`: `sp_SalesReadCore` (015 bản đầy đủ,
  3 CASE + 2 tham số `fn_DiscountPreview`), `sp_SalesOpenOrders` (010),
  `sp_TaoHoaDon` (015, điều kiện 52104), `sp_TaoHoaDonTuNhap` (030, điều kiện 52104),
  `sp_ReportDashboard` (026), `sp_DashboardAnalyticsRead` (028 mục `orders.open`),
  `sp_DashboardAnalyticsSources` (029 mục `@Kind='orders'`).
  Không đổi doanh thu: chỉ đơn `Da thanh toan` mới vào doanh thu.
- `ALTER TRIGGER TR_CTTT_KiemTra` (bản 016) và `TR_DiscountSnapshot` (bản 011) và
  `TR_TieuHao_Kho` (bản schema) cho phép ghi khi trạng thái ∈ (`Chua`,`Cho`).
  Không sửa `TR_CTHD_TongTien`, `TR_HD_Chot`.
- `GRANT EXECUTE` hai procedure mới cho `CafeManagerApp`; không cấp quyền bảng.
- Ghi `SchemaMigrations` theo runner hiện có; checksum 001–031 giữ nguyên.

Verify: áp migration trên DB test schema 031; insert trực tiếp trạng thái `Cho` bị
`DENY`; yêu cầu TT đơn rỗng bị 52108; đơn có dòng chuyển `Cho` đúng một lần;
revision cũ bị 52103; CASH gọi mở lại bị 51605; ADMIN mở lại đúng; thu tiền và hủy
từ `Cho` vẫn ghi đủ tiền/kho/điểm; `vw_TrangThaiBan` trả `Cho thanh toan`; không
tạo được đơn thứ hai trên bàn đang `Cho`; `@@TRANCOUNT=0`, `XACT_STATE=0` sau lỗi.

### Bước 2 — Models, DAL, BLL

Files: `src/CafeManager.Models/Dtos/Sales/SalesOrderDto.cs`,
`src/CafeManager.Models/States/SalesStates.cs`,
`src/CafeManager.Models/Requests/Sales/SalesRequests.cs`,
`src/CafeManager.DataAccess/Repositories/SalesRepository.cs` (và partial mới nếu cần),
`src/CafeManager.Business/Services/SalesService.cs` (và partial),
`src/CafeManager.DataAccess/SqlErrorMapper.cs`.

- `SalesOrderHeader`: thêm `CanEdit => Status == "Chua thanh toan"` và
  `IsPayable => Status is "Chua thanh toan" or "Cho thanh toan"`;
  đổi `IsOpen => IsPayable` (giữ tên để không sửa hàng loạt caller, ghi comment
  "còn mở = còn thu/hủy"). Không đổi thứ tự tham số record đang map.
- `SalesStates.Label`: thêm `"Cho thanh toan" => L.T("Chờ thanh toán")`.
- Request mới `SalesRevisionRequest(string Code, byte[] Revision, Guid RequestId)`.
- `SalesRepository.RequestPaymentAsync`/`ReopenAsync`: dùng `Write(...)` với
  `sp_YeuCauThanhToan`/`sp_MoLaiHoaDon`; `SalesService.RequestPaymentAsync` kiểm
  revision/mã; `ReopenAsync` kiểm thêm `session.Identity.Role == "ADMIN"` trước khi
  gọi SQL (SQL vẫn là nơi quyết định quyền).
- `SqlErrorMapper`: thêm 52108 "Đơn cần có món trước khi yêu cầu thanh toán.",
  52109 "Chỉ mở lại được đơn đang chờ thanh toán."

Verify: build; unit test predicate `CanEdit`/`IsPayable` và nhãn; gọi service với
revision sai/Guid rỗng bị chặn trước SQL; CASH gọi reopen bị `UnauthorizedAccessException`.

### Bước 3 — POS, panel đơn và màn hình liên quan

Files: `src/CafeManager.WinForms/Modules/Sales/SalesControl.cs`,
`src/CafeManager.WinForms/Modules/Sales/OrderPanel.cs`,
`src/CafeManager.WinForms/Modules/Sales/InvoicesControl.cs`,
`src/CafeManager.WinForms/Modules/Sales/InvoiceDetailDialog.cs`,
`src/CafeManager.WinForms/UI/OrbitTableCard.cs`,
`src/CafeManager.WinForms/UI/OrbitRecordListControl.cs`,
`src/CafeManager.WinForms/UI/OrbitDataGridView.cs`,
`src/CafeManager.Models/Localization/Translations/en.json`.

- Thêm nút `requestPay` ("Yêu cầu thanh toán") và `reopen` ("Mở lại đơn") vào
  `PaymentActions`: `[requestPay, pay, reopen, reconcile]`. `requestPay` chỉ bật khi
  `CanEdit`, có dòng, preview hợp lệ, không có ghi chú dở; `reopen` chỉ bật khi
  `Status == "Cho thanh toan"` và vai trò ADMIN; các nút khác giữ logic.
- `SetBusy`: products/header/orderPanel chỉ tương tác khi `CanEdit` (hoặc nháp);
  `pay`/`cancel` dùng `IsPayable`; `requestPay`/`reopen` theo điều kiện trên.
  F8 giữ nguyên checkout; không thêm phím tắt mới.
- `RequestPayment()`: xác nhận rõ "Sau bước này không sửa món; quản lý có thể mở lại";
  `Write(code,"SalesRequestPay",...)`; thành công hỏi "In tạm tính?" Yes/No, dùng
  `invoices.DetailAsync` + `InvoicePrinting.PreviewAsync` hiện có (lỗi in chỉ báo,
  trạng thái đã đổi vẫn giữ); thất bại giữ nguyên đơn và input.
- `ReopenOrder()`: xác nhận, gọi `Write(code,"SalesReopen",...)`, tải lại đơn.
- `OrderPanel.ShowOrder`: nhãn `SalesStates.Label` đã có; màu `Cho thanh toán` dùng
  màu chờ (Caramel); đơn `Cho` vẫn hiện tổng/phải trả bình thường.
- `InvoicesControl`: thêm `new("Cho thanh toan", L.T("Chờ thanh toán"))` vào bộ lọc
  trạng thái; `InvoiceDetailDialog`: `pos.Enabled = h.IsPayable` thay `IsOpen`.
- Nhãn bàn: `OrbitTableCard` hiển thị "Chờ thanh toán" khi `Status == "Cho thanh toan"`
  (vẫn bận, vẫn không cho đơn mới); `OrbitRecordListControl` thêm nhãn; badge
  `OrbitDataGridView` xếp `Cho thanh toan` vào nhóm chờ.
- Chuỗi mới vào `en.json`; không hard-code ngoài `L.T`.
- Không đổi bố cục tổng thể, không thêm dialog mới ngoài xác nhận hiện có.

Verify: đơn mở có dòng → bấm Yêu cầu TT → trạng thái `Cho`, nút sửa món/F3 khóa,
F8 thu được, Hủy được; CASH không thấy/không bấm được Mở lại; ADMIN mở lại rồi sửa
lại được; bàn hiện "Chờ thanh toán"; lọc hóa đơn thấy đơn `Cho`; đơn `Da` giữ nguyên.

### Bước 4 — Test và tài liệu của task

Files: `database/tests/g04/acceptance-pending-payment.sql` (mới),
`tools/database/Test-G04.ps1`, `tools/database/Invoke-G04Technical.ps1`,
`tests/CafeManager.Tests/Sales/SalesRulesTests.cs`,
`docs/database/sales-contract.md`, `docs/database/checkout-contract.md`,
`docs/database/migrations.md`, `docs/UX_QuanLiQuanCafe.md` (phần trạng thái đơn).

- SQL subcase dùng fixture chung G04: tạo đơn mở, chuyển `Cho`, thử các đường sửa
  (dòng/header) bị chặn; thu từ `Cho`; hủy từ `Cho`; ADMIN/CASH mở lại; revision cũ;
  index bàn; view; dashboard/report đếm; rollback sau lỗi.
- `Test-G04.ps1` thêm file vào danh sách SQL; `Invoke-G04Technical.ps1` thêm mốc
  `v16` (áp 032 + smoke contract) — chỉ chạy khi đợt test được duyệt.
- xUnit: thêm ca cho `SalesOrderHeader.CanEdit/IsPayable`, `SalesStates.Label`,
  quy tắc service (revision, ADMIN).
- Tài liệu: mô tả 4 trạng thái, bảng chuyển, hai cổng, mã lỗi, quyết định không khóa
  `GiamGia` ở trigger, phạm vi in chỉ thêm nhãn.

Verify: build Debug/Release 0 error; SQL file chạy được trên DB test schema 032;
không tick PASS nghiệm thu trong pha triển khai.

## 4. Ma trận test dự kiến (chờ duyệt đợt test riêng)

| ID | Ca và expected |
|---|---|
| PP01 | Đơn có ≥1 dòng, Sales: Yêu cầu TT → đúng `Cho thanh toan`, receipt `SalesRequestPay`, revision tăng, `NgayThanhToan` vẫn NULL. |
| PP02 | Đơn 0 dòng/`TongTien=0`: 52108, trạng thái không đổi, transaction sạch. |
| PP03 | Sau `Cho`: thêm/sửa/xóa dòng và sửa header bị 52102; UI khóa đúng; không ghi một phần. |
| PP04 | Thu tiền từ `Cho`: preview/checkout thành công, tiền/kho/điểm/snapshot đúng như từ `Chua`; trigger thanh toán/tiêu hao/snapshot chấp nhận `Cho`. |
| PP05 | Hủy từ `Cho` khi chưa thu/chưa trừ kho thành công; hủy sau khi đã thu bị chặn như hiện tại. |
| PP06 | ADMIN mở lại: về `Chua`, receipt `SalesReopen`, sửa lại được; CASH bị 51605; revision cũ bị 52103; mở lại đơn không phải `Cho` bị 52109. |
| PP07 | Bàn có đơn `Cho`: `vw_TrangThaiBan` trả `Cho thanh toan`; tạo đơn mới cùng bàn bị 52104; index unique chặn dòng thứ hai. |
| PP08 | `sp_SalesReadCore`/`sp_SalesOpenOrders`/danh sách đơn mở/chi tiết hiển thị đơn `Cho`, payable đúng; lọc hóa đơn theo `Cho` đúng. |
| PP09 | Dashboard `orders.open`, báo cáo `OpenOrders`, nguồn `orders` đếm cả `Chua` và `Cho`; doanh thu không đổi. |
| PP10 | Timeout/đối chiếu: receipt `SalesRequestPay`/`SalesReopen` đúng actor; replay cùng RequestId không đổi trạng thái lần hai; lỗi giữa transaction rollback nguyên trạng. |
| PP11 | Regression: tạo đơn (1 món/nháp), checkout trực tiếp từ `Chua`, hủy, in/tra cứu, giao hàng không hồi quy. |
| PP12 | UI chức năng 125%: nút mới, khóa theo trạng thái, focus/Tab/Esc, 1280×720 và 1366×768; hình thức SKIPPED, DPI 100/150% NOT RUN. |

## 5. Tiêu chí hoàn tất và rủi ro

Hoàn tất khi: migration áp được và bảo toàn dữ liệu; khóa sửa ở `Cho` được chứng
minh ở SQL; thu/hủy từ `Cho` đúng bất biến tiền/kho/điểm; ADMIN mở lại có receipt,
CASH bị chặn; đếm đơn mở/báo cáo gồm `Cho`; build Debug/Release sạch; các ca PP
trong phạm vi đợt test đã duyệt PASS. Không đánh Hoàn tất chỉ vì build/migration.

Rủi ro chính: sót một predicate `=N'Chua thanh toan'` khiến đơn `Cho` hiện sai hoặc
không thu được — giảm thiểu bằng rà grep toàn repo ở T35 và test PP04/PP08;
trigger cũ chặn nhầm khi checkout từ `Cho` — test PP04; quên đếm `Cho` ở dashboard —
test PP09; migration đụng checksum cũ — chỉ tạo migration mới.
