# Rà soát luồng nghiệp vụ Tài chính — 24/11/2026

Trạng thái: **báo cáo rà soát, chưa sửa mã nguồn, chưa commit.** Chờ người dùng
duyệt phạm vi đợt sửa trước khi triển khai.

Phạm vi: màn **Thu chi** (`FinanceControl`, `FinanceEntryDialog`, `FinanceDetailDialog`),
màn **Công nợ nhà cung cấp** (`SupplierDebtsControl`, `DebtPaymentDialog`,
`SupplierDebtDetailDialog`) và phần **Dòng tiền quản trị** trong Báo cáo vì cùng đọc
nguồn `PHIEUTHUCHI`. Không rà soát Tài sản, Lương, Nhập hàng ngoài điểm giao nhau.

## 1. Cách kiểm và giới hạn

Đã làm:

- Đọc toàn bộ UI, service, repository, DTO và procedure liên quan (`017_finance_sources.sql`,
  `021_supplier_debts.sql`, `026_reports.sql`).
- Truy vấn **chỉ đọc** trên DB development để đối chiếu số liệu thật.
- Thử hành vi phân tích chuỗi tiền bằng đúng kiểu parse của app.
- Chứng minh một dòng chi hệ thống lưu ghi chú tiếng Anh bằng giao dịch **đã rollback**.

Chưa làm (và không ghi PASS):

- Chưa mở GUI thật để bấm từng màn; chưa kiểm hình thức, spacing, tab, hover.
- Chưa chạy test ghi tiền/giao dịch trên DB test riêng; chưa kiểm hai phiên đồng thời.
- Các kết luận dưới đây là **đọc mã + SQL + phép thử chuỗi**, không phải biên bản GUI.

Số liệu đối chiếu tại thời điểm rà soát (DB development):

| Nguồn | Kết quả |
|---|---|
| Phiếu thu chi 30 ngày gần nhất | 24 phiếu, Thu 0 ₫, Chi 122.554.510 ₫ |
| Doanh thu hóa đơn đã trả cùng 30 ngày | 1.030 hóa đơn, 96.689.470 ₫ |
| Công nợ nhà cung cấp còn mở | 20 nguồn, 62.059.500 ₫ |

## 2. Luồng hiện tại

### 2.1 Vào màn và quyền

- Menu **Tài chính & báo cáo → Thu chi** chỉ hiện với ADMIN. CASH không thấy mục này.
- STORE xem được **Công nợ** nhưng không có nút Trả nợ; ADMIN mới trả được.
- BLL vẫn kiểm quyền (`Finance`, `DebtRead`, `DebtPay`), không chỉ ẩn nút.

### 2.2 Ghi phiếu tay

`Ghi phiếu` → chọn **Danh mục** (danh sách trộn Thu/Chi) → Ngày lập (có giờ) → Số tiền →
Mã tài sản tùy chọn → Ghi chú → xác nhận **chỉ hiện số tiền** → `sp_FinanceCreate`:

1. Mở transaction, khóa theo `RequestId`, kiểm receipt cũ để chống gửi lặp.
2. Sinh mã `TC` từ sequence dùng chung.
3. Ghi `PHIEUTHUCHI` với `OriginKind='Manual'`.
4. Ghi receipt rồi commit; trigger chặn UPDATE/DELETE về sau.

Danh mục hệ thống (`TC005` lương, `TC011` trả nợ, `TC012` trả ngay nhập hàng) bị ẩn khỏi
danh sách chọn tay và được trigger bảo vệ.

### 2.3 Trả nợ nhà cung cấp

Danh sách nợ (hiện tại hoặc số dư tại ngày) → chọn dòng → `Trả nợ` → số trả, ngày, phương
thức → xác nhận → `sp_TraCongNo`:

- Kiểm số trả ≤ nợ gốc − đã trả, ngày trả ≥ ngày nhập nguồn, phương thức được hỗ trợ và
  khác `PT010`.
- Sinh một phiếu chi hệ thống đúng danh mục `TC011` + một dòng `THANHTOAN_CONGNO`, cùng
  transaction và receipt.
- Lịch sử trả nợ bất biến; trigger chặn sửa/xóa.

### 2.4 Đọc danh sách và báo cáo

- Tổng Thu/Chi tính bằng window function trên toàn bộ tập đã lọc (không phải chỉ 50 dòng
  đang hiện) — đúng.
- Tìm kiếm chỉ trên **mã phiếu** và **ghi chú**; không tìm theo danh mục, người lập, nhà
  cung cấp.
- `sp_ReportCashFlow` gộp **thu bán hàng** + toàn bộ phiếu thu chi, nhóm theo
  `OriginKind + ' · ' + Loại`.

## 3. Đi thử theo vai trò

Các dòng dưới là hành vi suy ra từ mã và dữ liệu thật, không phải biên bản bấm GUI.

| # | Vai trò / thao tác | Người dùng thấy gì | Đánh giá |
|---|---|---|---|
| U01 | Quản lý mở Thu chi, để mặc định 30 ngày | 24 phiếu chi, dòng trạng thái “Thu 0 ₫ · Chi 122.554.510 ₫” | Dễ hiểu sai là quán không có thu, trong khi POS bán 96,7 triệu cùng kỳ |
| U02 | Gõ số tiền `15.000` | Được chấp nhận thành **15 ₫**, hộp xác nhận “Ghi phiếu 15 ₫?” | Lỗi số liệu nghiêm trọng, phiếu bất biến nên phải tạo phiếu đảo thủ công |
| U03 | Gõ số tiền `2.500.000` | Bị từ chối “không dấu phân cách” | Cùng một thói quen gõ mà lúc sai âm thầm, lúc báo lỗi |
| U04 | Đổi Loại sang “Thu” | Lưới **không đổi** cho tới khi bấm Tìm/Tải lại | Người dùng tưởng bộ lọc hỏng |
| U05 | Đổi Nguồn sang “Chi lương” | Như trên; các ô lọc còn không có nhãn | Khó biết ô nào là Từ ngày/Đến ngày/Loại/Nguồn |
| U06 | Trả nợ 10.000 với ngày **mai** | Được ghi; “Còn nợ hiện tại” giảm ngay hôm nay; phiếu không nằm trong danh sách mặc định đến hôm nay | Sổ quỹ và công nợ lệch nhau |
| U07 | Mở lịch sử nợ | Cột `Id / Code / Date / Amount / Method`, phương thức `PT001` | Lộ tiếng Anh và mã kỹ thuật giữa màn hình tiếng Việt |
| U08 | Ghi phiếu xong | Hộp thoại đóng ngay; mã phiếu chỉ thoáng hiện trong dòng trạng thái rồi mất | Không kịp ghi mã để lưu hồ sơ; màn chính không xác nhận |
| U09 | Tìm “trả nợ” trong Thu chi | Không thấy phiếu chi do hệ thống sinh (ghi chú thật là `SupplierDebt`) | Tìm bằng ngôn ngữ người dùng thất bại |
| U10 | Mở báo cáo Dòng tiền quản trị | Các nhóm `SupplierDebt · Chi`, `Payroll · Chi`, `PurchaseImmediate · Chi` | Chỉ nhóm “Thu bán hàng” được dịch |
| U11 | Nhìn cột Số tiền | `122554510` (có phân cách, không có ₫) | Khác với ô tổng và báo cáo đều có ₫ |
| U12 | DB lỗi khi tải lại | Dòng trạng thái báo lỗi nhưng lưới **vẫn giữ số cũ** | Dễ đọc số cũ như số mới |
| U13 | Trả nợ, không chạm Phương thức | Ô phương thức tự chọn sẵn “Tiền mặt” | Có thể ghi sai phương thức mà không biết |
| U14 | Gõ mã tài sản sai | Nhận lỗi chung “Nguồn hoặc trạng thái thu chi không hợp lệ” | Không chỉ đúng ô cần sửa |
| U15 | Thu ngân muốn ghi 50.000 ₫ mua đá | Không có màn Thu chi | Đúng phân quyền đã chốt nhưng lệch vận hành quán nhỏ |

## 4. Đối chiếu cách làm của hệ thống lớn

Tham chiếu cách tổ chức công việc của ERP/POS doanh nghiệp (SAP B1, NetSuite, Dynamics BC,
Toast, Square, KiotViet, Sapo), không sao chép giao diện.

| Hạng mục | Chuẩn phổ biến | Hiện tại | Kết luận |
|---|---|---|---|
| Chứng từ trung tâm | Mã, ngày, người lập, trạng thái, người chốt, lịch sử | Có mã/ngày/nguồn; **thiếu người lập trên danh sách**; không trạng thái/người chốt | Thiếu truy vết cơ bản |
| Sổ quỹ | Số đầu kỳ, thu bán, chi, tồn quỹ, đối soát két | Chỉ danh sách phiếu + tổng lọc; doanh thu nằm ở nguồn khác | Chưa phải sổ quỹ; nhãn gây hiểu nhầm |
| Sửa sai | Chứng từ đảo/điều chỉnh có liên kết, không sửa lịch sử | Bất biến, nhưng phải tự tạo phiếu ngược ghi tay, không liên kết | Đúng nguyên tắc, thiếu công cụ |
| Duyệt và khóa kỳ | Duyệt trước khi ghi sổ, khóa kỳ | Ghi sổ ngay, không duyệt, không khóa kỳ | Chấp nhận được với quán nhỏ, cần ghi rõ |
| Phân tách nhiệm vụ | Thu ngân tạm ứng/chi vặt trong ca; quản lý duyệt | Chỉ ADMIN ghi thu chi | Hẹp hơn thực tế vận hành |
| Phương thức, đối tượng | Tiền mặt/chuyển khoản, người nhận, chứng từ kèm | Trả nợ có phương thức; phiếu tay **không có** | Khó đối soát két |
| In/xuất chứng từ | In phiếu thu/chi riêng | Báo cáo có CSV/PDF; phiếu lẻ không in | Thiếu đầu ra chứng từ |
| Drill-down | Từ báo cáo về chứng từ gốc | Có link nguồn và nguồn dòng tiền | Tốt |
| Chống gửi lặp | Idempotency, khóa chứng từ | Có receipt + app lock + pending store | Tốt |
| Bất biến | Đã chốt là khóa | Trigger chặn sửa/xóa | Tốt |

## 5. Phát hiện

### P0 — sai tiền hoặc lệch số liệu

#### F01. Nhập `15.000` thành 15 ₫

- **Bằng chứng:** `FinanceEntryDialog.Save` và `DebtPaymentDialog.Pay` dùng
  `decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)`
  (`src/CafeManager.WinForms/Modules/Finance/FinanceEntryDialog.cs:81`,
  `src/CafeManager.WinForms/Modules/Finance/DebtPaymentDialog.cs:46`).
  Phép thử: `15.000` → `15.000m` (được nhận), `1.000.000` → `false` (bị từ chối).
- **Ảnh hưởng:** phiếu chi/trả nợ nhỏ đi 1.000 lần; phiếu bất biến nên phải tạo phiếu đảo;
  báo cáo và công nợ sai theo. Người dùng càng dễ mắc vì chính app hiển thị `81.000 ₫`.
- **Đề xuất:** chuẩn hóa chuỗi theo vi-VN (bỏ `.`/khoảng trắng phân cách nghìn, chỉ nhận
  số nguyên VND; không nhận phần thập phân), hiển thị lại số đã hiểu trong hộp xác nhận;
  thêm unit test cho `15.000`, `15 000`, `15000`, `1.234.567`, `15,5`, chuỗi rác.
- **Phạm vi:** UI/helper, không đổi schema.

#### F02. Ngày tương lai làm lệch công nợ và sổ quỹ

- **Bằng chứng:** DateTimePicker hai hộp thoại không đặt `MaxDate`; `FinanceService.Normalize`
  không kiểm ngày; `sp_FinanceCreate` không kiểm ngày; `sp_TraCongNo` chỉ kiểm
  `@Date >= ngày nhập nguồn`; `sp_DebtList` khi xem hiện tại dùng `vw_CongNoNCC` cộng **mọi**
  khoản trả, không lọc theo ngày. Màn Thu chi mặc định `Đến ngày = hôm nay`.
- **Ảnh hưởng:** một khoản trả nợ ghi ngày mai làm “Còn nợ hiện tại” giảm hôm nay, nhưng
  phiếu chi không xuất hiện trong danh sách mặc định → quản lý thấy công nợ giảm mà sổ chi
  không có. Phiếu chi tương lai cũng tương tự.
- **Đề xuất (cần chốt):**
  - **A (khuyến nghị):** chặn ngày > thời điểm hiện tại ở UI, BLL và procedure, thông báo
    “Ngày không được sau hôm nay; chọn ngày đã phát sinh”.
  - **B:** cho phép nhưng tách “ngày chứng từ” và “ngày hạch toán”, mọi số dư/công nợ lọc
    theo ngày hạch toán — thay đổi contract, lớn hơn nhiều.
- **Phạm vi:** migration kế tiếp + BLL/UI; cần duyệt vì đổi miền giá trị ngày được phép.

### P1 — hiểu sai, khó dùng, thiếu truy vết

#### F03. Màn “Thu chi” không nói rõ không gồm doanh thu bán hàng

- **Bằng chứng:** dữ liệu 30 ngày: Thu 0 ₫ trong khi POS 96.689.470 ₫; doanh thu chỉ vào
  báo cáo **Dòng tiền quản trị**, không tạo phiếu thu (đúng quy tắc chống nhân đôi).
- **Ảnh hưởng:** quản lý mở màn tên “Thu chi” thấy “Thu 0 ₫”, kết luận sai về dòng tiền.
- **Đề xuất:** đổi nhãn thành **“Phiếu thu chi”**, phụ đề “Chỉ gồm phiếu thu/chi nội bộ;
  doanh thu bán hàng xem Báo cáo → Dòng tiền quản trị”; đổi ô tổng thành “Thu phiếu · Chi
  phiếu”. Không thêm dữ liệu bán hàng vào sổ phiếu, không tạo phiếu thu thứ hai.

#### F04. Bộ lọc không nhãn và không tự áp dụng

- **Bằng chứng:** `FinanceControl` thêm trực tiếp `from, to, type, origin` vào `Filters`,
  không Label; chỉ nút “Tìm / Tải lại”/Enter mới gọi `LoadRows`.
- **Ảnh hưởng:** đổi Loại/Nguồn/Ngày mà lưới giữ nguyên; người dùng tưởng lỗi hoặc đọc
  nhầm dữ liệu cũ.
- **Đề xuất:** thêm nhãn ngắn (Từ ngày, Đến ngày, Loại, Nguồn) và tự tải khi đổi (debounce
  nhẹ) hoặc nút “Lọc” đặt cạnh; giữ Enter ở ô tìm.

#### F05. Danh sách thiếu Nguồn, Người lập; không lọc/tìm theo danh mục

- **Bằng chứng:** `FinanceRow` không có `MaNV/TenNV`; `sp_FinanceList` không select `MaNV`;
  `WHERE` chỉ `MaTC LIKE` hoặc `GhiChu LIKE`. Chi tiết cũng không hiện người lập.
- **Ảnh hưởng:** không trả lời được “ai lập, từ nghiệp vụ nào” trên danh sách; muốn tìm
  “chi lương tháng 10” phải mở từng phiếu.
- **Đề xuất:** thêm cột **Nguồn** (dùng `OriginText`) và **Người lập**; lọc theo danh mục;
  mở rộng tìm kiếm sang tên danh mục và tên nhân viên. Sửa `sp_FinanceList` + DTO + UI,
  không đổi bảng.

#### F06. Mất mã phiếu ngay sau khi ghi/trả nợ

- **Bằng chứng:** `FinanceEntryDialog.Complete` đặt trạng thái rồi đóng; `FinanceControl`
  bỏ qua `ResultCode` (`if (...==DialogResult.OK) await LoadRows();`). Tương tự
  `SupplierDebtsControl` với kết quả trả nợ.
- **Ảnh hưởng:** người dùng không kịp ghi mã để lưu/in; màn chính không xác nhận đã ghi.
- **Đề xuất:** hiện mã ở dòng trạng thái màn chính (giữ tới lần tải sau), cho bấm mở chi
  tiết phiếu vừa tạo; không đóng mà không báo.

#### F07. Trộn Thu/Chi trong một ô “Danh mục”, xác nhận không nêu loại

- **Bằng chứng:** `FinanceCategory.ToString()` nối “(Thu)/(Chi)”; hộp xác nhận chỉ có số tiền.
- **Ảnh hưởng:** chọn nhầm danh mục là đảo chiều thu/chi mà không thấy rõ; phiếu bất biến.
- **Đề xuất:** tách nhóm Thu/Chi hoặc thêm bước chọn “Loại phiếu” trước; xác nhận hiển thị
  “Phiếu chi · Tiền điện · 2.500.000 ₫ · 24/11/2026”.

#### F08. Lịch sử trả nợ lộ header tiếng Anh và mã phương thức

- **Bằng chứng:** `SupplierDebtDetailDialog` để `AutoGenerateColumns=true` trên
  `SupplierDebtPayment(Id, Code, Date, Amount, Method)`; không có `CellFormatting`.
- **Ảnh hưởng:** `Id/Code/Date/Amount/Method` và `PT001` giữa màn tiếng Việt; thiếu tên
  phương thức.
- **Đề xuất:** khai báo cột thủ công (Mã phiếu, Ngày, Số tiền, Phương thức), ẩn `Id`, map
  phương thức từ `sp_DebtMethods` (hoặc bổ sung tên vào lịch sử).

#### F09. Ghi chú hệ thống và nhóm Dòng tiền lưu tiếng Anh

- **Bằng chứng:** `sp_FinanceSystemExpense` ghi `GhiChu = UseCase` (`'Payroll'`,
  `'SupplierDebt'`, `'PurchaseImmediate'`); đã chứng minh bằng insert rollback:
  `TC... | SupplierDebt | 12345 | SupplierDebt`. `sp_ReportCashFlow` đặt tên nhóm
  `OriginKind + N' · ' + Loại`; UI chỉ dịch dòng có `Code='Sales'` (cả CSV cũng vậy).
- **Ảnh hưởng:** ghi chú và báo cáo lộ thuật ngữ; tìm tiếng Việt “trả nợ/lương” không ra
  phiếu hệ thống (dữ liệu demo có ghi chú đẹp nên che lỗi này).
- **Đề xuất:** phiếu hệ thống mới ghi ghi chú tiếng Việt (hoặc để trống và UI hiển thị theo
  Nguồn); dịch tên nhóm Dòng tiền qua bảng map; **không sửa dữ liệu cũ** (bất biến), chỉ
  map khi hiển thị nếu ghi chú trùng giá trị enum.

#### F10. Cột tiền thiếu ký hiệu ₫

- **Bằng chứng:** `G05Display.Grid` đặt `Format="N0"` cho Amount/Original/Paid/Remaining;
  `FinanceControl` lặp lại `N0`. Tổng và báo cáo lại có ₫.
- **Ảnh hưởng:** khó quét nhanh số tiền, không nhất quán trong cùng app.
- **Đề xuất:** dùng `#,##0' ₫'` với provider vi-VN trong `G05Display`, kiểm lại độ rộng cột.

### P2 — độ tin cậy và đề xuất mở rộng

#### F11. Lỗi tải giữ nguyên lưới cũ

`FinanceControl.LoadRows` khi lỗi chỉ đổi dòng trạng thái, không xóa `DataSource`, không
tắt “Xem chi tiết”. Cần trạng thái lỗi rõ (làm mờ/clear lưới) để không đọc số cũ như mới.
`SupplierDebtsControl` cũng chỉ đổi status — kiểm cùng lúc.

#### F12. Phương thức trả nợ tự chọn sẵn Tiền mặt; không có “trả hết”

`method.DataSource` được gán nhưng không đặt placeholder, nên ComboBox chọn dòng đầu
(`PT001`). Người dùng có thể ghi sai phương thức mà không biết. Nên có mục “Chọn phương
thức” hoặc hiển thị rõ trong xác nhận; thêm nút “Trả toàn bộ còn nợ” (điền số, vẫn xác nhận).

#### F13. Mã tài sản sai trả lỗi chung

`sp_FinanceCreate` gộp lỗi danh mục/tài sản vào 52602; mapper hiện “Nguồn hoặc trạng thái
thu chi không hợp lệ”. Nên kiểm mã tài sản trước khi gửi (hoặc tách mã lỗi) và focus đúng ô.

#### F14. Khác biệt có chủ đích (ghi nhận, chưa đề nghị làm ngay)

- Không có sổ quỹ đầu kỳ/cuối kỳ, đối soát két cuối ca/ngày, chốt kỳ.
- Không có chứng từ điều chỉnh/đảo có liên kết chứng từ gốc.
- Phiếu tay không có phương thức thanh toán, người nhận, chứng từ kèm.
- Không in/xuất riêng từng phiếu thu/chi (báo cáo có CSV/PDF).
- Không có bước duyệt/người duyệt; ghi sổ ngay.
- Chỉ ADMIN ghi thu chi; thu ngân không có quỹ tạm ứng.
- Một dãy mã `TC` chung cho thu và chi (không tách PT/PC).

Các mục này vượt phạm vi T19/T23 đã chốt; nếu muốn làm phải lập requirements riêng.

## 6. Điểm đang làm tốt

- Chứng từ bất biến sau ghi; không sửa/xóa lịch sử.
- Chống gửi lặp đầy đủ: `RequestId` + receipt + app lock + pending store + “Kiểm tra kết quả”.
- Nguồn chi hệ thống được bảo vệ: ẩn khỏi chọn tay, trigger kiểm đúng danh mục/nguồn.
- Liên kết nguồn tốt: phiếu nhập, bảng lương, công nợ, tài sản; báo cáo drill-down được.
- Công nợ có chế độ “tại ngày” và kiểm vượt nợ/ngày nguồn ở SQL.
- Tổng Thu/Chi tính trên toàn tập lọc ở SQL, không cộng lại ở UI.
- Phân quyền kiểm ở BLL/procedure, không chỉ ẩn nút.

## 7. Đề xuất đợt sửa (chờ duyệt)

**Đợt 1 — an toàn số liệu (ưu tiên):** F01, F02 (phương án A).
Gồm helper nhập tiền dùng chung, chặn ngày tương lai ở UI + BLL + migration.
Test: unit parse tiền; SQL rollback cho ngày tương lai; thủ công luồng ghi/trả.

**Đợt 2 — dễ dùng và truy vết:** F03, F04, F05, F06, F07, F08, F09, F10, F11, F12, F13.
Chủ yếu UI + ba procedure (`sp_FinanceList`; `sp_FinanceSystemExpense` cho phiếu mới;
`sp_DebtList` nếu cần tên phương thức). Không đổi bảng, không sửa dữ liệu cũ.

**Đợt 3 — mở rộng (cần quyết định nghiệp vụ):** F14. Tách thành task riêng, không gộp
vào đợt sửa lỗi.

## 8. Cần người dùng chốt

1. Ngày tương lai: chặn hẳn (A) hay tách ngày chứng từ/ngày hạch toán (B)?
2. Có duyệt thêm cột **Nguồn/Người lập** và lọc theo danh mục (sửa `sp_FinanceList`) không?
3. Màn Thu chi: chỉ sửa nhãn + liên kết báo cáo Dòng tiền (khuyến nghị), hay muốn xây sổ
   quỹ hợp nhất có doanh thu (task mới)?
4. Duyệt đợt 1 để bắt đầu sửa, hay duyệt cả đợt 1 + 2?

Không có mã nguồn nào bị thay đổi trong lần rà soát này.
