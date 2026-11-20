# Plan T34 — Phiên bàn và trả bàn

Ngày 16/11/2026. Trạng thái: **đã duyệt; đã triển khai, chạy đợt test 17/11/2026, sửa
T34-01 bằng migration 035 và chạy TS04 + UI 18/11/2026; đủ điều kiện nghiệm thu** —
TS01/02/03/04/05/06/07/08/09/10/11 PASS và TS12 PASS phần SQL; còn NOT RUN có ghi rõ
(nhánh lỗi đóng phiên UI, in lại UI) ([test-results.md](test-results.md),
[bug-report.md](bug-report.md)). Đầu vào là T33 (trạng thái `Cho thanh toan`) đã triển khai.
Nguồn: [requirements-cho-thanh-toan-phien-ban.md](../requirements-cho-thanh-toan-phien-ban.md)
mục 4. Giữ nguyên 56 ID T14–T18, 325 ID toàn dự án và lịch sử test.

## 1. Hiện trạng đã đối chiếu source

- Bàn không có cột trạng thái; trạng thái dẫn xuất từ `vw_TrangThaiBan`
  (`database/DB_Schema.sql:498-503`), hiện chỉ join đơn `Chua thanh toan`.
- `UX_HD_BanMo` bảo đảm một đơn mở/bàn (`:425-426`); sau T33 mở rộng cho `Cho`.
- Tạo đơn khóa dòng `BAN` và kiểm 52104 trong `sp_TaoHoaDon`
  (`database/migrations/015_sales_read_normalization.sql:38-42`) và
  `sp_TaoHoaDonTuNhap` (`030_draft_order_flush.sql:34-38`); chưa có khái niệm phiên.
- `sp_TableList`/`sp_TableSelectCurrent` trả `Code,Name,Area,Status,OrderCode,
  Revision,ReadAt` (`006_tables_areas.sql:22-38`); `TableRow` tương ứng
  (`src/CafeManager.Models/Dtos/Tables/TableRows.cs:3`);
  `TableRepository.Map` đọc 7 cột (`TableRepository.cs:20`).
- `TableService` bọc đọc bằng `AppPermission.Sales`, ghi bàn/khu vực bằng
  `CatalogAdmin` (`TableService.cs:17-27`); `TableRepository.ReceiptAsync` dùng lại
  receipt G03 (`:19`).
- POS mở bàn theo `TableRow.OrderCode` (`SalesControl.OpenTableAsync`, `:465-475`);
  bàn không có đơn mở thì tạo nháp; `TableForNewOrder` coi `OrderCode is null` là trống
  (`:322-326`); sau checkout/cancel gọi `LoadTables` và reset POS
  (`FinishCheckout`, `:653-687`).
- Màn Bàn hiển thị card từ `TableRow` (`TablesControl.cs:36-55`);
  `OrbitTableCard` coi `Status != "Trong"` là bận (`OrbitTableCard.cs:25,33-36`).
- Không có bảng/job đóng phiên; yêu cầu cấm tự đóng theo giờ.

## 2. Quyết định triển khai

1. **`PHIEN_BAN` do SQL sở hữu**; app không ghi trực tiếp (DENY). Mỗi bàn tối đa một
   phiên mở bằng unique filtered index.
2. **Tự mở phiên** trong transaction tạo đơn (đơn thường và đơn nháp flush) khi bàn
   chưa có phiên; đơn được gắn `MaPhien`. Nháp chưa ghi SQL vẫn không chiếm bàn.
3. **Trả bàn là hành động thủ công** (POS và màn Bàn), chỉ khi phiên không còn đơn
   `Chua`/`Cho`. Không tự đóng sau thanh toán/hủy, chỉ **hỏi** "Trả bàn ngay?" mặc
   định Có.
4. **Gọi thêm sau trả trước**: khi phiên mở nhưng không còn đơn mở, tạo đơn mới sẽ
   tự nối phiên hiện tại (hóa đơn mới, receipt riêng, không gộp). Bàn vẫn bận.
5. **View có fallback**: đơn cũ `MaPhien IS NULL` vẫn làm bàn bận như hiện tại; không
   backfill phiên, không bịa giờ vào.
6. **Không thêm cột trạng thái vào `BAN`**, không đổi `UX_HD_BanMo` (đã mở rộng ở T33).
7. Migration **034 tạm thời** (033 đã thuộc `033_category_diacritics.sql`), cùng quy
   tắc dời số như T33.

## 3. Các bước triển khai

### Bước 1 — Migration 034: bảng phiên, cột liên kết, view, procedure đọc

Files: `database/migrations/034_sales_table_sessions.sql` (mới),
`docs/database/migrations.md`, `docs/database/table-contract.md`,
`docs/database/sales-contract.md`.

- Precondition `SchemaMigrations Version=32` (hoặc số T33 thực tế); preflight không
  có đơn mở trùng bàn; rollback nguyên trạng nếu sai.
- `CREATE TABLE dbo.PHIEN_BAN(MaPhien int IDENTITY PRIMARY KEY, MaBan char(5) NOT NULL
  REFERENCES dbo.BAN(MaBan), GioVao datetime2(0) NOT NULL DEFAULT SYSDATETIME(),
  GioRa datetime2(0) NULL, MaNVMo int NOT NULL REFERENCES dbo.NHANVIEN(MaNV),
  MaNVTra int NULL REFERENCES dbo.NHANVIEN(MaNV), Revision rowversion,
  CHECK(GioRa IS NULL OR GioRa>=GioVao));`
- `CREATE UNIQUE INDEX UX_PHIEN_BAN_Mo ON dbo.PHIEN_BAN(MaBan) WHERE GioRa IS NULL;`
- `DENY INSERT,UPDATE,DELETE ON dbo.PHIEN_BAN TO CafeManagerApp;`
- `ALTER TABLE dbo.HOADON ADD MaPhien int NULL CONSTRAINT FK_HD_Phien REFERENCES
  dbo.PHIEN_BAN(MaPhien);` + `CREATE INDEX IX_HD_Phien ON dbo.HOADON(MaPhien);`
  Không backfill.
- `ALTER VIEW vw_TrangThaiBan`: thêm `MaPhien`, `SessionRevision`,
  `Retained` (bit: có phiên mở nhưng không còn đơn mở) sau các cột cũ; `Status`:
  - không đơn mở và không phiên mở → `Trong` (kể cả đơn cũ không phiên);
  - có đơn `Cho` (không còn `Chua`) → `Cho thanh toan`;
  - có đơn mở (`Chua`/`Cho`) hoặc phiên mở → `Dang phuc vu`;
  - phiên mở, mọi hóa đơn đã trả/hủy → `Dang phuc vu` + `Retained=1`.
  Dùng `OUTER APPLY TOP(1)` cho đơn mở (ưu tiên `Chua` nếu có) và phiên mở để tránh
  nhân dòng; giữ `MaHD` là đơn mở (NULL khi chỉ còn phiên).
- `ALTER sp_TableList`/`sp_TableSelectCurrent`: thêm
  `v.MaPhien SessionCode,v.SessionRevision,v.Retained` sau cột cũ; giữ quyền
  `ADMIN,CASH`, lọc/phân trang như hiện tại.

Verify: áp 034 trên DB test; bảng/index/deny đúng; view trả đúng 3 nhóm trạng thái và
fallback đơn cũ; hai lần mở phiên cùng bàn bị unique index chặn; `sp_TableList` trả
đủ cột mới.

### Bước 2 — Gắn phiên khi tạo đơn và procedure trả bàn

Files: `database/migrations/034_sales_table_sessions.sql` (tiếp),
`docs/database/table-contract.md`.

- `ALTER sp_TaoHoaDon` (bản 015 đã sửa ở T33) và `sp_TaoHoaDonTuNhap` (bản 030):
  sau khi khóa dòng `BAN` và kiểm 52104:
  ```sql
  DECLARE @session int;
  SELECT @session=MaPhien FROM dbo.PHIEN_BAN WITH(UPDLOCK,HOLDLOCK)
   WHERE MaBan=@TableCode AND GioRa IS NULL;
  IF @session IS NULL
  BEGIN
   DECLARE @staff int; SELECT @staff=MaNV FROM dbo.TAIKHOAN WHERE MaTK=@Actor;
   INSERT dbo.PHIEN_BAN(MaBan,MaNVMo) VALUES(@TableCode,@staff);
   SET @session=SCOPE_IDENTITY();
  END;
  ```
  Chèn `HOADON` kèm `MaPhien=@session`. Kênh không có bàn (`Mang di`,`Giao hang`)
  để `NULL`. Không đổi payload receipt (phiên là hệ quả tất định của việc tạo đơn).
- Tạo `sp_TraBan @Actor,@Stamp,@MaPhien,@ExpectedRevision,@RequestId`:
  transaction; `sp_G03Demand @Actor,@Stamp,'ADMIN,CASH'`; receipt operation
  `TableClose` với `EntityCode=CONVERT(varchar(10),@MaPhien)`, payload hash gồm
  MaPhien/revision; khóa phiên `UPDLOCK,HOLDLOCK`; yêu cầu `GioRa IS NULL` và
  revision khớp (đóng rồi → 52111, stale → 52103); nếu còn đơn `Chua`/`Cho` gắn
  phiên → `THROW 52110` "Còn đơn chưa chốt; thu hoặc hủy trước khi trả bàn.";
  cập nhật `GioRa=SYSDATETIME(), MaNVTra` từ actor; `sp_SalesFinish`; trả receipt.
- `GRANT EXECUTE` cho `sp_TraBan`; mã lỗi 52110/52111 thêm vào `SqlErrorMapper`
  (BLL ở bước 3).

Verify: tạo đơn tại quán tự mở phiên đúng một lần; tạo đơn thứ hai sau khi đơn đầu
đã trả nối đúng phiên; đơn mang đi/giao không tạo phiên; trả bàn khi còn đơn mở bị
52110 và không đổi `GioRa`; trả bàn khi sạch thành công + receipt; gọi lặp cùng
RequestId không đóng hai lần; hai connection tạo đơn cùng bàn chỉ sinh một phiên.

### Bước 3 — Models, DAL, BLL

Files: `src/CafeManager.Models/Dtos/Tables/TableRows.cs`,
`src/CafeManager.Models/Dtos/Sales/SalesOrderDto.cs`,
`src/CafeManager.Models/Requests/Tables/TableRequests.cs`,
`src/CafeManager.DataAccess/Repositories/TableRepository.cs`,
`src/CafeManager.DataAccess/Repositories/SalesRepository.cs`,
`src/CafeManager.Business/Services/TableService.cs`,
`src/CafeManager.DataAccess/SqlErrorMapper.cs`.

- `TableRow`: thêm `int? SessionCode, byte[]? SessionRevision, bool Retained`
  ở cuối record; cập nhật `TableRepository.Map` đọc thêm 3 cột (nullable đúng thứ tự).
- `SalesOrderHeader`: thêm `int? SessionCode, byte[]? SessionRevision` ở cuối record;
  `sp_SalesReadCore` trả thêm `h.MaPhien` và revision phiên qua `OUTER APPLY`
  (bản ALTER trong 034); cập nhật mapper `GetAsync`.
- Request `CloseSessionRequest(int SessionCode, byte[] Revision, Guid RequestId)`.
- `TableRepository.CloseSessionAsync` gọi `sp_TraBan`, trả `OperationResult`.
- `TableService.CloseSessionAsync(session, request)` bọc `Run(write:false ...
  AppPermission.Sales)` như đọc bàn (CASH/ADMIN đều trả bàn được), chuẩn hóa
  `SessionCode>0`, revision 8 byte, RequestId khác rỗng; lỗi map qua
  `UserOperationException`.
- `SqlErrorMapper`: 52110 "Bàn còn đơn chưa chốt; thu hoặc hủy trước khi trả bàn.",
  52111 "Phiên bàn đã đóng hoặc không còn mở."

Verify: build; đọc bàn/đơn map đúng cột mới; gọi trả bàn với revision sai bị chặn;
không đổi quyền đọc bàn hiện có.

### Bước 4 — POS, màn Bàn và hộp "Trả bàn ngay?"

Files: `src/CafeManager.WinForms/Modules/Sales/SalesControl.cs`,
`src/CafeManager.WinForms/Modules/Sales/OrderPanel.cs`,
`src/CafeManager.WinForms/Modules/Tables/TablesControl.cs`,
`src/CafeManager.WinForms/UI/OrbitTableCard.cs`,
`src/CafeManager.Models/Localization/Translations/en.json`.

- POS giữ `TableRow` đang chọn (đã có `tableRows` + `table`); thêm trạng thái
  `currentSession` (mã + revision) lấy từ `TableRow` khi chọn bàn/`OpenTableAsync`
  hoặc từ `order.Header.SessionCode` khi mở đơn.
- Nút `closeTable` ("Trả bàn") đặt cạnh Hủy đơn trong `HeaderActions` (không đặt ở
  footer thanh toán để tránh bấm nhầm khi thu tiền). Chỉ bật khi có phiên và đơn
  hiện tại không `Chua`/`Cho`; nếu bàn còn đơn khác, SQL trả 52110 và UI hiển thị
  thông điệp, tải lại bàn.
- `CloseTableAsync()`: xác nhận "Trả bàn <mã>? Bàn sẽ trống cho khách mới."; gọi
  `tables.CloseSessionAsync`; thành công reset POS, `LoadTables`, báo mã phiên; lỗi
  stale/52110 tải lại bàn và giữ đơn hiện tại.
- Sau `FinishCheckout` (đã commit tiền) và sau hủy thành công: hỏi
  "Trả bàn ngay?" Yes/No mặc định **Yes**; chỉ gọi đóng phiên khi có `SessionCode`;
  lỗi trả bàn **không** làm mất kết quả thu/hủy — hiển thị "Đã thu/hủy; chưa trả
  bàn được" + lý do và cho bấm lại. Không tự retry.
- Bàn có phiên mở nhưng không còn đơn (`Retained`): khi chọn bàn, POS tạo nháp mới
  nhưng hiện banner "Bàn đang giữ · khách đã trả. Thêm món sẽ tạo hóa đơn mới trong
  cùng phiên; trả bàn khi khách về." Không tự mở hóa đơn đã trả. `TableForNewOrder`
  giữ quy tắc `OrderCode is null` là dùng được (đã đúng cho gọi thêm).
- Màn Bàn: thêm action "Trả bàn" (Sales) bật khi `Selected.SessionCode` có giá trị;
  xác nhận rồi gọi service, reload; lỗi hiển thị ở `State`. Không đổi action
  Thêm/Sửa/Xóa của ADMIN.
- `OrbitTableCard`: thêm thuộc tính `Retained`; nhãn:
  `Trong` → "Trống"; `Cho thanh toan` → "Chờ thanh toán";
  `Retained` → "Đã trả · giữ bàn"; còn lại → "Đang dùng". Vẫn `busy = Status != "Trong"`.
  `TablesControl` truyền `Retained` và accessible name tương ứng.
- Chuỗi mới vào `en.json`; không hard-code.

Verify: thu tiền → hộp hỏi trả bàn, Yes đóng phiên, No giữ bàn bận; hủy tương tự;
lỗi trả bàn sau thu không rollback tiền; bàn đã trả còn phiên hiển thị "Đã trả · giữ
bàn" và vẫn không cho người khác dùng; trả bàn từ màn Bàn hoạt động; đơn cũ không
phiên vẫn như trước; bấm trả bàn hai lần không đóng hai lần.

### Bước 5 — Test và tài liệu của task

Files: `database/tests/g04/acceptance-table-sessions.sql` (mới),
`tools/database/Test-G04.ps1`, `tools/database/Invoke-G04Technical.ps1`,
`tests/CafeManager.Tests/Sales/SalesRulesTests.cs` (nếu có quy tắc thuần),
`docs/database/table-contract.md`, `docs/database/sales-contract.md`,
`docs/database/migrations.md`, `docs/UX_QuanLiQuanCafe.md` (luồng bàn/POS).

- SQL subcase: mở phiên tự động (đơn thường + nháp flush), nối phiên khi gọi thêm,
  unique index, trả bàn bị chặn khi còn `Chua`/`Cho`, trả bàn thành công, trả lặp,
  fallback đơn cũ không `MaPhien`, hai session đồng thời trên cùng bàn.
- Thêm file vào `Test-G04.ps1`; mốc `v17` trong `Invoke-G04Technical.ps1` (áp 034 +
  smoke) — chỉ chạy khi đợt test được duyệt.
- Tài liệu: mô tả `PHIEN_BAN`, `HOADON.MaPhien`, view + cờ `Retained`, `sp_TraBan`,
  operation `TableClose`, mã 52110/52111, quy tắc gọi thêm không gộp, không backfill.

Verify: build Debug/Release 0 error; SQL chạy trên DB test schema 034; không tick PASS.

## 4. Ma trận test dự kiến (chờ duyệt đợt test riêng)

| ID | Ca và expected |
|---|---|
| TS01 | Tạo đơn tại quán (thường) tự mở đúng một phiên, `HOADON.MaPhien` gắn đúng, `MaNVMo` đúng actor; đơn mang đi/giao không tạo phiên. |
| TS02 | Flush nháp tại quán tự mở phiên trong cùng transaction; nháp chưa Giữ không tạo phiên/bàn vẫn Trống. |
| TS03 | Bàn có phiên mở, không còn đơn: `vw_TrangThaiBan` = `Dang phuc vu`, `Retained=1`, card "Đã trả · giữ bàn"; tạo đơn mới nối đúng phiên, hóa đơn riêng, không gộp. |
| TS04 | Hai connection tạo đơn cùng bàn đồng thời: một phiên duy nhất, một đơn thắng 52104, không nhân phiên/đơn. |
| TS05 | Trả bàn khi còn đơn `Chua`/`Cho`: 52110, `GioRa` vẫn NULL, đơn/tiền không đổi. |
| TS06 | Trả bàn khi phiên sạch: `GioRa`/`MaNVTra` đúng, receipt `TableClose`, bàn `Trong`; gọi lặp cùng RequestId không đóng lần hai. |
| TS07 | Trả bàn hai lần khác RequestId: lần sau 52111, không ghi đè giờ trả. |
| TS08 | Sau checkout: hộp "Trả bàn ngay?" mặc định Có; Yes đóng phiên; No giữ bàn bận; lỗi trả bàn không rollback tiền và cho bấm lại. |
| TS09 | Sau hủy đơn: cùng cơ chế hỏi trả bàn; hủy đơn cuối rồi trả bàn → bàn Trống. |
| TS10 | Đơn cũ `MaPhien IS NULL`: bàn bận khi còn đơn chưa trả như hiện tại; không sinh phiên giả/không backfill. |
| TS11 | POS/màn Bàn: nút Trả bàn đúng trạng thái, xác nhận, focus/Tab/Esc; checkout/in không hồi quy; 125% tại 1280×720 và 1366×768; hình thức SKIPPED, DPI 100/150% NOT RUN. |
| TS12 | Đếm đơn mở/báo cáo/dashboard không đổi bởi phiên; doanh thu vẫn theo ngày thanh toán; in lại hóa đơn trong phiên đã trả đúng. |

## 5. Tiêu chí hoàn tất và rủi ro

Hoàn tất khi: phiên mở/đóng đúng một lần; bàn bận theo phiên và fallback đơn cũ;
trả bàn bị chặn khi còn đơn mở; sau thu/hủy hỏi trả bàn nhưng không tự đóng; gọi thêm
sau trả trước tạo hóa đơn mới cùng phiên, không nhân tiền; build Debug/Release sạch;
các ca TS trong phạm vi đợt test đã duyệt PASS.

Rủi ro chính: unique index phiên đụng dữ liệu cũ (không có bảng cũ nên thấp); quên
gắn phiên ở một nhánh tạo đơn → test TS01/TS02; POS giữ `currentSession` cũ sau khi
đổi bàn → reset khi `BeginNewOrder`/đổi bàn; hộp trả bàn sau checkout gây hiểu nhầm
đã rollback tiền → thông điệp tách bạch và test TS08; view `OUTER APPLY` trả nhiều
dòng nếu viết sai → test TS03/TS10.

## 6. Ghi nhận triển khai (16/11/2026)

Đã hoàn tất bước 1–5 theo plan; chưa chạy đợt test và chưa nghiệm thu.

- **SQL:** `database/migrations/034_sales_table_sessions.sql` (PHIEN_BAN + unique
  filtered index + DENY, `HOADON.MaPhien`/FK/index, view 3 cột mới,
  sp_TableList/sp_TableSelectCurrent, mở/nối phiên trong sp_TaoHoaDon +
  sp_TaoHoaDonTuNhap, sp_SalesReadCore 27 cột, sp_TraBan + GRANT). Áp thành công
  trên DB scratch `CoffeeManager_Test_G04_20261116_pendingpay` (schema032 → 034);
  kiểm Version=34, object/index/deny/view, sp_TableList 10 cột, sp_SalesOrderGet 27
  cột. Chưa áp lên Development.
- **C#:** `TableRow`/`SalesOrderHeader` thêm cột phiên; `CloseSessionRequest`;
  `TableRepository.CloseSessionAsync` + map; `TableService.CloseSessionAsync`;
  `SqlErrorMapper` 52110/52111; `SalesRepository` đọc 27 cột.
- **UI:** POS thêm nút **Trả bàn** cạnh Hủy đơn, banner “Đã trả · giữ bàn”, hộp
  “Trả bàn ngay?” sau thu/hủy (lỗi trả bàn không rollback tiền); màn Bàn thêm action
  Trả bàn; `OrbitTableCard.Retained` nhãn “Đã trả · giữ bàn”; chuỗi mới ở `en.json`.
- **Test/docs:** thêm `database/tests/g04/acceptance-table-sessions.sql` (phần SQL
  của TS01–TS03, TS05–TS07, TS10, TS12) và `technical-v17.sql`; `Test-G04.ps1`
  ThroughVersion=17, ánh xạ schema034, thêm case mới; `Invoke-G04Technical.ps1`
  ValidateRange(10,17) + ánh xạ schema034. Cập nhật `docs/database/table-contract.md`,
  `sales-contract.md`, `migrations.md`, `docs/UX_QuanLiQuanCafe.md`.
- **Kiểm chứng đã chạy (không phải nghiệm thu):** build Debug 0 error (2 warning
  WindowsBase có sẵn); 42 unit test Localization/Sales/Printing PASS trên output tạm.
- **Chưa chạy:** TS04 hai connection (đợt concurrency riêng), TS08/09/11 (UI/DPI do
  người dùng kiểm theo phân công), toàn bộ đợt acceptance; chưa commit.
