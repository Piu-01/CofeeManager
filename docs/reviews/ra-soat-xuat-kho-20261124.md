# Rà soát luồng nghiệp vụ Xuất kho — 24/11/2026

Trạng thái: **báo cáo rà soát, chưa sửa mã nguồn, chưa commit.** Chờ người dùng
duyệt phạm vi đợt sửa trước khi triển khai.

Phạm vi: tab **Kho → Xuất kho**, gồm `StockIssuesControl` (danh sách),
`StockIssueEditorDialog` (lập phiếu), `StockIssueDetailDialog` (chi tiết),
`StockIssueService`, `StockIssueRepository`, `StockIssueTableBuilder`,
`sp_LapPhieuXuat`/`sp_StockIssueList`/`sp_StockIssueGet`/`sp_StockIssueReceipt`
(`database/migrations/019_stock_issues.sql`) và phần liên quan trong báo cáo tồn
(`026_reports.sql`). Không rà soát lại Nhập hàng, Kiểm kê, Công nợ ngoài điểm giao nhau.

## 1. Cách kiểm và giới hạn

Đã làm:

- Đọc toàn bộ UI, service, repository, DTO, procedure, trigger và view liên quan.
- Truy vấn **chỉ đọc** DB development (`CoffeeManager_Development`, schema 45):
  12 phiếu xuất (2 phiếu `New`, 10 phiếu `Legacy`), 13 dòng chi tiết, 20 nguyên liệu.
  Ví dụ dữ liệu thật: `PX26090002` — `Trả hàng` — lý do
  “Trả đưa tươi lỗi cho nhà cung cấp”, nhân viên `NV010`.
- Truy vấn chỉ đọc thêm liên kết NCC–NVL: 10 NCC, `NCC10` chưa từng nhập; 16/20 NVL nhập
  từ 2–4 NCC; `NVL16 Dừa tươi` của `PX26090002` chỉ có `NCC06` nhập trong lịch sử.
- Đối chiếu cùng nghiệp vụ với luồng **Nhập hàng** đã làm lại ở migration 043
  (nháp → chờ nhận → kiểm → chốt → hủy, có in phiếu) để thấy chỗ lệch chuẩn trong
  cùng một module Kho.

Chưa làm (và không ghi PASS):

- Chưa mở GUI thật, chưa bấm từng màn, chưa kiểm hình thức/DPI.
- Chưa chạy test ghi kho trên DB test riêng, chưa kiểm hai phiên đồng thời,
  chưa gọi thử `sp_LapPhieuXuat` bằng giao dịch.
- Bảng “đi thử theo vai trò” dưới đây là **mô phỏng theo mã và dữ liệu**, không phải
  biên bản GUI. Các kết luận cần xác nhận native ở đợt test được duyệt.
- Biên bản T21 (27/10/2026) vẫn còn `T21-K01`, `T21-K08`, `T21-K10` ở trạng thái
  BLOCKED/FAIL; các lượt bổ sung sau chỉ chạy một phần, chưa có bảng cập nhật chốt.

## 2. Luồng hiện tại

### 2.1 Vào màn và quyền

- Menu **Kho** chỉ có ở ADMIN và STORE (`MainForm.cs:100-102`); CASH không vào được.
- Tab **Xuất kho** tạo lazy `StockIssuesControl` (`InventoryControl.cs:16`).
- BLL kiểm `AppPermission.Inventory` (`StockIssueService.cs:17`), procedure kiểm
  `ADMIN,STORE` (`019_stock_issues.sql:29,56,59,62`). Ẩn nút không phải lớp bảo vệ duy nhất.

### 2.2 Danh sách

- Cột: Mã, Ngày xuất, Loại, Lý do (`StockIssuesControl.cs:18`). Không có Người lập,
  Ngày chốt, Trạng thái, Tổng lượng, Nhà cung cấp (khi trả hàng).
- Tìm chỉ theo mã phiếu hoặc lý do (`StockIssuesControl.cs:24`,
  `019_stock_issues.sql:56`); không tìm theo nguyên liệu, người lập, nhà cung cấp.
- Không lọc theo loại xuất/khoảng ngày; phân trang 50 dòng.
- Không có nút In phiếu/Xuất chứng từ (khác hoàn toàn tab Nhập hàng có in — xem 3.3).
- Đúp dòng hoặc Enter mở chi tiết chỉ đọc (không có sửa/xóa, đúng thiết kế bất biến).

### 2.3 Lập phiếu

- Hộp **Xuất nguyên liệu — Chưa ghi kho**: chọn Loại (`Hủy do hỏng` / `Trả hàng`),
  ngày, lý do bắt buộc, thêm từng nguyên liệu và lượng
  (`StockIssueEditorDialog.cs:24,38,52`).
- Danh sách nguyên liệu lấy từ `sp_PurchaseLookups` — **chỉ mã/tên/đơn vị, không có tồn**
  (`018_purchase_receipts.sql:102`). Tab Nhập hàng dùng `sp_PurchaseMaterialLookup`
  có cả tồn và giá nhập gần nhất (`043_purchase_receiving.sql:292-298`).
- “Thêm dòng” thêm dòng lượng 0 và **không tự đưa focus vào ô lượng**; chỉ khi chọn
  trùng nguyên liệu mới nhảy tới ô lượng của dòng cũ (`StockIssueEditorDialog.cs:28`).
- Bấm **Chốt xuất** → kiểm tra hợp lệ → hộp xác nhận chỉ nêu **số dòng**
  (“Chốt xuất N nguyên liệu?”), không liệt kê mã/lượng/lý do/ngày
  (`StockIssueEditorDialog.cs:56`). Không có bước “xem lại chứng từ” đúng nghĩa.
- Lỗi nghiệp vụ giữ input và hiện ở dòng trạng thái; rời hộp khi đã nhập thì hỏi bỏ nháp
  (`StockIssueEditorDialog.cs:58,65`). Nháp chỉ tồn tại trong bộ nhớ dialog, không lưu DB
  (dựng request khi bấm Chốt, `StockIssueEditorDialog.cs:46-63`).

### 2.4 Ghi sổ

`sp_LapPhieuXuat` (`019_stock_issues.sql:21-50`):

1. Kiểm loại/ngày/lý do (không kiểm ngày tương lai), mở transaction.
2. `sp_FinanceBegin` theo `RequestId` để chống gửi lặp, sinh mã `PX…` từ sequence.
3. Kiểm dòng: rỗng/trùng/mã 5 ký tự/lượng ≤ 0/quá 4 số thập phân.
4. Khóa từng nguyên liệu theo mã tăng dần (`UPDLOCK,HOLDLOCK`), kiểm đủ tồn **toàn phiếu**
   trước khi ghi; thiếu một dòng rollback cả phiếu.
5. Ghi header `DaChot=0` → ghi chi tiết (trigger `TR_CTPX_Kho` trừ tồn) → `DaChot=1`,
   `NgayChot=SYSDATETIME()` → `sp_FinanceFinish` (receipt).
6. Trigger chặn sửa/xóa phiếu và chi tiết đã chốt; runtime `DENY INSERT,UPDATE,DELETE`
   cho principal app (`019_stock_issues.sql:10-19`).

Kết quả: phiếu xuất một bước, nguyên tử, bất biến, không âm kho, không trừ hai lần khi retry.

### 2.5 Đọc lại và báo cáo

- Chi tiết chỉ đọc: loại, ngày (luôn `00:00` vì ô chọn chỉ có ngày), lý do, các dòng
  mã/tên/đơn vị/lượng (`StockIssuesControl.cs:53-78`); không có người lập, ngày chốt,
  tổng lượng, giá trị, nút in.
- Báo cáo tồn ghi nhận `Issue` theo **thời điểm receipt** và chỉ cộng **số lượng**
  (`026_reports.sql:7-13, 36, 106-128`); drill-down từ báo cáo mở đúng chi tiết phiếu
  (`ReportSourceNavigation.cs:22`).

## 3. Đi thử theo vai trò (mô phỏng theo mã + dữ liệu)

| # | Vai trò / thao tác | Người dùng thấy gì | Đánh giá |
|---|---|---|---|
| U01 | STORE mở Kho → Xuất kho | 12 phiếu, cột ngày hiện `02/11/2026 00:00` | Giờ `00:00` gây thắc mắc; không biết ai lập phiếu |
| U02 | Bấm Lập phiếu xuất | Chọn loại/ngày/lý do; **không thấy tồn hiện tại** | Phải nhớ hoặc mở tab khác để tra tồn |
| U03 | Chọn nguyên liệu → Thêm dòng | Danh sách xổ xuống toàn bộ NVL, thêm dòng lượng 0 rồi phải click ô lượng | Thiếu bước focus; nhập 5 dòng tốn nhiều thao tác |
| U04 | Nhập 9999 cho NVL chỉ còn 2 | Chốt → hộp xác nhận chỉ nêu số dòng → báo “Không đủ nguyên liệu; toàn phiếu chưa chốt.” | Không nêu **nguyên liệu nào, còn bao nhiêu**; phải dò từng dòng |
| U05 | Xuất đúng lượng rồi chốt | Hộp thoại đóng ngay; mã `PX…` thoáng hiện rồi mất; danh sách tải lại | Không kịp ghi mã; không có xác nhận ở màn chính; **không in được phiếu** |
| U06 | Mở chi tiết phiếu | Thấy loại/lý do/dòng; không thấy người lập, giờ chốt, tổng lượng, giá trị | Thiếu thông tin chứng từ cơ bản |
| U07 | Chọn “Trả hàng”, nhập lý do trả NCC | Chỉ giảm kho; **không chọn được NCC, không giảm công nợ** | Nợ NCC vẫn còn; kế toán phải nhớ và xử lý ngoài hệ thống |
| U08 | Chọn ngày mai (hoặc ngày tương lai) | Được chấp nhận; tồn giảm ngay hôm nay; phiếu mang ngày tương lai | Chứng từ đi trước thời điểm phát sinh |
| U09 | CASH đăng nhập | Không thấy mục Kho | Đúng phân quyền đã chốt |
| U10 | STORE lập phiếu hủy | Ghi được; BLL/procedure cho phép ADMIN,STORE | Đúng yêu cầu T21 |
| U11 | Từ báo cáo tồn mở nguồn “Xuất” | Mở đúng chi tiết phiếu xuất | Truy vết số lượng tốt |
| U12 | Tìm “sữa” trong ô tìm | Không ra phiếu dù phiếu có dòng sữa | Tìm bằng ngôn ngữ người dùng thất bại |
| U13 | Nhập lý do 201 ký tự | Bị chặn với lỗi rõ ở dòng trạng thái | Tốt |
| U14 | Đóng hộp khi đã nhập dở | Hỏi “Bỏ nháp xuất chưa ghi kho?” | Tốt |
| U15 | Mất mạng/timeout khi chốt | Giữ yêu cầu, hiện “Kiểm tra kết quả”, gửi lại cùng `RequestId` | Tốt, không trừ kho hai lần |

## 4. Đối chiếu cách làm của hệ thống lớn

Tham chiếu cách tổ chức công việc của ERP/POS doanh nghiệp (SAP B1, NetSuite,
Dynamics BC, Toast, Square, Lightspeed, KiotViet, Sapo), không sao chép giao diện.

| Hạng mục | Chuẩn phổ biến | Hiện tại | Kết luận |
|---|---|---|---|
| Chứng từ trung tâm | Mã, ngày chứng từ, người lập, người nhận/chứng kiến, trạng thái, người chốt, lịch sử | DB có `MaNV`, `NgayChot` nhưng UI **không hiển thị ai lập/chốt**; không có người nhận | Thiếu truy vết cơ bản trên chứng từ |
| Vòng đời | Nháp → chờ duyệt → chốt → hủy/điều chỉnh | Chốt một bước, không nháp DB, không duyệt, không hủy/điều chỉnh | Khác biệt có chủ đích nhưng lệch thực tế với hàng hủy |
| Định giá tồn | Giá vốn bình quân/đích danh; bút toán giảm tồn có giá trị | Chỉ lưu **số lượng** (`CHITIET_PX.SoLuongXuat`); báo cáo chỉ có số lượng | Không trả lời được “tháng này hủy bao nhiêu tiền” |
| Xuất trả NCC | Gắn NCC, sinh chứng từ trả/credit note, giảm công nợ | Không có cột NCC; chỉ ghi lý do tự do; nợ không đổi | **Lệch nghiệp vụ lớn** (đang ngoài phạm vi đã chốt) |
| Lý do | Danh mục lý do (hết hạn/hư hỏng/rơi vỡ/khác) + ghi chú, có thể theo dòng | Một ô chữ tự do ở header | Khó tổng hợp hao hụt theo nguyên nhân |
| In/xuất chứng từ | In phiếu xuất/biên bản hủy có chữ ký | **Không có** in/xuất phiếu (Nhập hàng có in) | Thiếu đầu ra chứng từ |
| Kiểm soát nội bộ | Người lập ≠ người duyệt với hàng hủy | Một người lập và chốt | Chấp nhận được với quán nhỏ, cần ghi rõ |
| Tồn khả dụng khi lập | Hiện tồn/cam kết và cảnh báo vượt tồn ngay trên dòng | Không hiện tồn; chỉ báo lỗi chung sau khi chốt | Kém an toàn thao tác |
| Xem lại trước chốt | Màn xem lại đầy đủ dòng/lý do/ngày | Hộp xác nhận chỉ nêu số dòng | Dễ chốt nhầm |
| Chống gửi lặp | Idempotency, khóa chứng từ | `RequestId` + receipt + pending store + nút kiểm tra | Tốt |
| Bất biến | Đã chốt là khóa | Trigger + DENY + rollback toàn phiếu | Tốt |
| Đồng thời | Khóa theo thứ tự, không âm kho | Khóa NVL tăng dần, kiểm đủ tồn trước khi ghi | Tốt |
| Báo cáo | Hao hụt theo kỳ/NVL/lý do/giá trị, drill-down | Có số lượng trong báo cáo tồn và drill-down; không có báo cáo hao hụt riêng | Đủ số lượng, thiếu phân tích |
| Lô/hạn dùng | Lô, HSD, FEFO | Không có | Ngoài phạm vi, ghi nhận có chủ đích |

**Kết luận ngắn:** phần **ghi sổ tồn** đã chắc và đúng hơn mức đồ án thông thường
(nguyên tử, chống âm, chống lặp, bất biến). Phần **chứng từ, định giá, trả NCC, in ấn
và hỗ trợ người dùng khi lập** còn cách khá xa cách doanh nghiệp lớn vận hành.

## 5. Phát hiện

### P0 — sai/lệch số liệu, cần quyết định nghiệp vụ

#### F01. “Trả hàng” không gắn nhà cung cấp và không giảm công nợ

- **Bằng chứng:** `PHIEUXUAT` không có cột `MaNCC` (`database/DB_Schema.sql:160-170`);
  UI chỉ có hai loại, không chọn NCC (`StockIssueEditorDialog.cs:24`);
  `sp_LapPhieuXuat` chỉ ghi `LoaiXuat`, `LyDo` (`019_stock_issues.sql:43-44`).
  Kế hoạch ghi rõ “Xuất trả chưa tự xử lý hoàn tiền/cấn trừ nợ trong phạm vi này”
  (`docs/KeHoach_QuanLiQuanCafe.md:187`). Dữ liệu thật: `PX26090002` lý do
  “Trả đưa tươi lỗi cho nhà cung cấp”.
- **Ảnh hưởng:** kho giảm đúng nhưng **công nợ NCC vẫn giữ nguyên**. Hệ thống không có
  công cụ giảm nợ do trả hàng (chỉ có trả tiền), nên số nợ sai vĩnh viễn trừ khi hủy
  nguyên phiếu nhập — chỉ làm được khi phiếu chưa chốt. Không biết trả cho NCC nào.
- **Đề xuất (cần chọn):**
  - **A (khuyến nghị):** thêm NCC cho loại Trả hàng + giảm công nợ theo giá trị trả
    (migration mới, procedure sở hữu transaction, liên kết chứng từ).
  - **B:** giữ xuất kho thuần, thêm chứng từ “Giảm công nợ do trả hàng” riêng có liên kết.
  - **C:** giữ nguyên, đổi nhãn thành “Trả nguyên liệu cho NCC (chỉ giảm kho)” và ghi
    quy trình điều chỉnh nợ thủ công; chấp nhận nợ lệch có kiểm soát.
  - Cả A và B đều cần F01b (liên kết NCC–NVL) để chọn đúng NCC và đúng NVL của NCC đó.
- **Phạm vi:** vượt T21 đã chốt, cần duyệt requirements riêng.

#### F01b. Danh mục nguyên liệu không gắn NCC — không biết NCC cung cấp NVL nào

Bổ sung 24/11/2026 theo quan sát của người dùng: “nguyên liệu hiện tại không gắn với
NCC, nên không xác định rõ NCC đó đã nhập những nguyên liệu nào”.

- **Bằng chứng:**
  - `NGUYENVATLIEU` chỉ có mã, tên, ĐVT, tồn (`DB_Schema.sql:91-99`). NCC chỉ nằm trên
    **đầu phiếu nhập** (`PHIEUNHAP.MaNCC`, `:134-145`); `CHITIET_PN` không có NCC
    (`:147-158`), nên liên kết NCC–NVL chỉ suy ra được từ lịch sử nhập.
  - Lookup NVL **không lọc theo NCC**: nhập hàng trả toàn bộ danh mục
    (`018_purchase_receipts.sql:98-104`); `sp_PurchaseMaterialLookup` trả thêm tồn và
    `LastPrice` nhưng `LastPrice` lấy từ **bất kỳ NCC nào**
    (`043_purchase_receiving.sql:292-298`).
  - Truy vấn chỉ đọc DB development (schema 45): 10 NCC nhưng `NCC10` chưa từng có phiếu
    nhập; **16/20 NVL được nhập từ 2–4 NCC** (`NVL02 Sữa tươi` 4 NCC, `NVL03 Đường`
    4 NCC). `PX26090002` trả `NVL16 Dừa tươi`, lịch sử chỉ `NCC06` nhập NVL này, nhưng
    phiếu xuất không ghi NCC nên không chứng minh được đã trả cho ai.
- **Ảnh hưởng:**
  1. Lập phiếu nhập chọn NCC xong vẫn thấy toàn bộ NVL → dễ ghi nhập NVL mà NCC đó không
     bán; khi lập phiếu trả hàng không có cơ sở chọn đúng NCC/NVL.
  2. Không trả lời được ngay “NCC này đang cung cấp, tháng này nhập những NVL gì” trên
     màn hình hay báo cáo; chỉ truy vấn SQL tay qua `CHITIET_PN`–`PHIEUNHAP`.
  3. Tồn là **kho chung theo NVL** (`SoLuongTon`), không theo NCC/lô. Vì một NVL nhập từ
     nhiều NCC, không biết phần còn trong kho thuộc NCC nào → trả hàng chỉ dựa trên số
     lượng, không dựa trên lô; `LastPrice` cho F03 cũng có thể là giá của NCC khác.
  4. Đây là nguyên nhân gốc khiến F01 khó làm đúng: thiếu liên kết NCC–NVL ở danh mục,
     không chỉ thiếu cột `MaNCC` trên phiếu xuất.
- **Đề xuất (cần chọn):**
  - **A (khuyến nghị, phạm vi nhỏ):** không đổi bảng; thêm truy vấn đọc
    `sp_SupplierMaterials` (NCC → NVL đã nhập, lần nhập cuối, giá gần nhất của chính NCC
    đó) dùng để: lọc NVL sau khi chọn NCC ở phiếu nhập/phiếu trả hàng, thêm cột “NCC gần
    nhất” trong lookup NVL. Chỉ đọc lịch sử, không giả định NCC cố định; vẫn cho chọn
    NVL ngoài lịch sử (món mới/NCC mới) nhưng cảnh báo “NCC này chưa từng nhập NVL này”.
  - **B:** thêm bảng danh mục `NVL_NCC` (NCC–NVL, cờ NCC chính, giá thỏa thuận) + màn
    gán và migration; lọc NVL theo bảng này. Đúng chuẩn ERP nhưng là yêu cầu mới (mã,
    quyền, màn hình, dữ liệu khởi tạo).
  - **C:** giữ nguyên, ghi rõ giới hạn “không quản lý NCC–NVL” và chấp nhận phiếu trả
    hàng không xác định NCC.
  - Quản lý **lô/giá vốn theo lô** để biết tồn của từng NCC là phạm vi lớn hơn, tách riêng.
- **Phạm vi:** danh mục + Nhập hàng + Trả hàng; vượt T21/T22 đã chốt, cần requirements
  riêng. Không gộp vào fix layout G05-B08.

#### F02. Cho phép ngày xuất tương lai, tồn giảm ngay

- **Bằng chứng:** `DateTimePicker` không đặt `MaxDate` (`StockIssueEditorDialog.cs:16`);
  `StockIssueService.Normalize` không kiểm ngày (`StockIssueService.cs:9-14`);
  `sp_LapPhieuXuat` chỉ kiểm `@Date IS NULL` (`019_stock_issues.sql:25`).
- **Ảnh hưởng:** phiếu ngày mai vẫn trừ kho hôm nay; danh sách hiện ngày tương lai,
  người đọc tưởng kho chỉ giảm vào ngày đó. Khác với Kiểm kê đã chặn ngày.
- **Đề xuất:** chặn ngày > hôm nay ở UI + BLL + SQL (khuyến nghị), hoặc tách
  “ngày chứng từ” và “ngày hạch toán” nếu muốn nhập trước — thay đổi lớn hơn.
- **Phạm vi:** UI/BLL + migration nhỏ.

### P1 — thiếu truy vết, thiếu chứng từ, dễ dùng sai

#### F03. Không lưu/không báo giá trị thiệt hại

- **Bằng chứng:** `CHITIET_PX` chỉ có `SoLuongXuat` (`DB_Schema.sql:172-181`);
  view và báo cáo chỉ trả `Quantity` (`019_stock_issues.sql:52-53`,
  `026_reports.sql:12-13`); không có cột giá xuất/giá vốn.
- **Ảnh hưởng:** quản lý không biết tháng này hủy bao nhiêu tiền, không so sánh được
  chi phí hao hụt giữa các kỳ; báo cáo lãi/lỗ thiếu chi phí hủy.
- **Đề xuất:** snapshot `DonGiaXuat` tại thời điểm chốt (giá nhập gần nhất hoặc bình quân
  theo quy tắc đã chốt) + cột giá trị trong view/chi tiết/báo cáo. Không tính lại giá
  khi giá nhập đổi (giữ nguyên tắc snapshot).
- **Phạm vi:** migration + báo cáo; cần chốt quy tắc giá.

#### F04. Không in được phiếu xuất/biên bản hủy

- **Bằng chứng:** thư mục `src/CafeManager.WinForms/Printing/` chỉ có
  `InvoicePrinting.cs`, `PurchasePrinting.cs`; `StockIssuesControl` không có nút In,
  chi tiết phiếu xuất không có In.
- **Ảnh hưởng:** không có chứng từ giấy để ký/xác nhận hủy; trái thông lệ
  “chứng từ là trung tâm” (mục 6.1 AGENTS.md), trong khi cùng module Nhập hàng đã in được.
- **Đề xuất:** báo cáo in phiếu xuất (mã, ngày, người lập, loại, lý do, dòng NVL/lượng/
  đơn vị, chỗ ký người lập – người chứng kiến – người nhận NCC), tái dùng FastReport
  và mẫu của phiếu nhập.

#### F05. Danh sách/chi tiết thiếu người lập, giờ chốt, người nhận

- **Bằng chứng:** `vw_StockIssueRead` không select `MaNV`, `NgayChot`
  (`019_stock_issues.sql:52-53`); `StockIssueRow` không có các trường này;
  cột danh sách chỉ 4 cột (`StockIssuesControl.cs:18`); chi tiết không có.
- **Ảnh hưởng:** không trả lời “ai hủy, lúc nào, ai nhận hàng trả” — thiếu truy vết cơ bản.
- **Đề xuất:** thêm Người lập + Giờ chốt vào view/DTO/UI; thêm “Người nhận/Chứng kiến”
  nếu chốt phương án F01.

#### F06. Không thấy tồn khi lập; lỗi thiếu tồn không chỉ đúng dòng

- **Bằng chứng:** editor dùng lookup không có tồn (`StockIssueEditorDialog.cs:38`),
  trong khi Nhập hàng dùng lookup có `Stock,LastPrice` (`043_purchase_receiving.sql:292-298`);
  SQL báo chung `52803` → “Không đủ nguyên liệu; toàn phiếu chưa chốt.”
  (`SqlErrorMapper.cs:125`), không nêu mã/lượng còn.
- **Ảnh hưởng:** người dùng phải đoán; nhập lại nhiều lần; dễ nhập quá tồn vào giờ cao điểm.
  Nguyên liệu không tồn tại (lookup cũ) cũng báo “không đủ” gây hiểu sai.
- **Đề xuất:** hiện tồn khả dụng theo dòng, cảnh báo khi vượt tồn trước khi gửi; SQL trả
  chi tiết lỗi (mã NVL, còn bao nhiêu) hoặc mã lỗi riêng cho “NVL không tồn tại”.

#### F07. Không có bước xem lại chứng từ trước khi chốt

- **Bằng chứng:** hộp xác nhận chỉ nêu số dòng (`StockIssueEditorDialog.cs:56`).
- **Ảnh hưởng:** dễ chốt nhầm loại/ngày/lý do/lượng; phiếu bất biến nên phải bù bằng
  kiểm kê hoặc lập phiếu nhập.
- **Đề xuất:** hộp xác nhận dạng tóm tắt chứng từ (loại, ngày, lý do, danh sách NVL/lượng,
  cảnh báo trả hàng chưa cấn nợ), hoặc bước “Xem lại” trước nút Chốt.

#### F08. Không có nháp DB và không có duyệt (khác biệt có chủ đích)

- **Bằng chứng:** `request` chỉ tạo khi bấm Chốt; nháp nằm trong `BindingList` và mất khi
  đóng (`StockIssueEditorDialog.cs:45-56`); kế hoạch ghi “phiếu chưa chốt chỉ tồn tại
  trong transaction”.
- **Ảnh hưởng:** đếm/hủy nhiều nguyên liệu mà phải làm liền mạch; đổi ca hoặc hết giờ là
  mất; không có người duyệt cho hàng hủy (rủi ro thất thoát).
- **Đề xuất:** ghi nhận là khác biệt có chủ đích cho quán nhỏ; nếu muốn sát thực tế, lập
  task “phiếu nháp + duyệt hủy” (mô hình 2 bước như Nhập hàng 043).

#### F09. Không có hủy/điều chỉnh có liên kết chứng từ

- **Bằng chứng:** chỉ có guard bất biến (`019_stock_issues.sql:15-19`); không có procedure
  hủy/đảo phiếu xuất; đường bù duy nhất là Kiểm kê hoặc lập phiếu nhập.
- **Ảnh hưởng:** lập sai phải bù bằng nghiệp vụ khác, mất liên kết nguyên nhân – chứng từ.
- **Đề xuất:** nếu cần, thêm chứng từ “điều chỉnh/hủy phiếu xuất” có tham chiếu phiếu gốc
  và lý do, không sửa lịch sử (giữ nguyên tắc bất biến).

#### F10. Lý do tự do, không phân loại; không có lý do theo dòng

- **Bằng chứng:** `LyDo nvarchar(200)` ở header; UI một ô `TextBox`
  (`StockIssueEditorDialog.cs:16,25`; `019_stock_issues.sql:6`).
- **Ảnh hưởng:** không tổng hợp được hao hụt theo nguyên nhân; cùng loại “hỏng” nhưng
  mỗi người gõ một kiểu.
- **Đề xuất:** danh mục lý do (Hết hạn, Hư hỏng, Rơi vỡ, Sai pha chế, Khác) +
  ghi chú tự do; cân nhắc lý do theo dòng.

#### F11. Hủy đồ đã pha không liên kết đơn/hóa đơn

- **Bằng chứng:** chỉ có dòng hướng dẫn trên editor
  (`StockIssueEditorDialog.cs:25`); không có trường tham chiếu hóa đơn/đơn.
- **Ảnh hưởng:** dễ ghi kép tiêu hao bán và xuất hủy, hoặc bỏ sót; không phân tích được
  hao hụt theo đơn/món.
- **Đề xuất:** ghi nhận ngoài phạm vi hiện tại; nếu cần, thêm tham chiếu tùy chọn
  (mã đơn/hóa đơn) vào header/chi tiết khi có yêu cầu chính thức.

### P2 — trải nghiệm và nhất quán

#### F12. Danh sách thiếu thông tin và bộ lọc

Chỉ 4 cột, không có tổng lượng/tổng giá trị, không lọc loại/khoảng ngày, không tìm theo
NVL/NCC. Đề xuất bổ sung cột và bộ lọc tối thiểu; không cần xuất toàn bộ dữ liệu thô.

#### F13. Thêm dòng chưa tối ưu thao tác

Dòng mới không được focus vào ô lượng (`StockIssueEditorDialog.cs:28`); danh sách NVL
là ComboBox xổ toàn bộ, không lọc gõ nhanh. Đề xuất focus ô lượng sau khi thêm và cho
gõ để lọc nguyên liệu.

#### F14. Ngày hiển thị `00:00` ở danh sách/chi tiết

`G05Display` format cột `Date` là `dd/MM/yyyy HH:mm` (`G05Display.cs:29`) nhưng ô chọn chỉ
có ngày. Đề xuất hiển thị `dd/MM/yyyy` cho ngày chứng từ, hoặc thêm giờ chốt riêng khi
làm F05.

#### F15. Mã phiếu vừa lập không được giữ lại ở màn chính

`Complete` đặt trạng thái rồi đóng ngay (`StockIssueEditorDialog.cs:63`); danh sách chỉ
tải lại. Đề xuất hiện “Đã chốt PX…” ở dòng trạng thái màn chính, cho mở chi tiết/in ngay;
tránh phải đoán dòng mới nhất.

#### F16. Lỗi tải danh sách vẫn giữ lưới cũ

`LoadRows` khi lỗi chỉ đổi dòng trạng thái, không xóa `DataSource`
(`StockIssuesControl.cs:39-43`). Đề xuất trạng thái lỗi rõ (làm mờ/clear lưới) để không đọc
số cũ như số mới.

#### F17. Nhãn gây hiểu nhầm

Tab “Xuất kho”, tiêu đề “Xuất nguyên liệu”, nút “Trả hàng” không nói rõ **trả NCC**;
cảnh báo “chưa tự hoàn tiền/cấn nợ” đang thay cho giải pháp nghiệp vụ. Đề xuất đổi nhãn
“Trả nguyên liệu cho NCC” và ghi rõ hệ quả trong chứng từ.

#### F18. Phân trang có thể vượt trang cuối

`next.Click` luôn tăng `page` (`StockIssuesControl.cs:26`); SQL trả rỗng. Giống Nhập hàng,
mức thấp; nên chặn khi đã ở trang cuối.

## 6. Điểm đang làm tốt

- Ghi sổ nguyên tử: kiểm đủ **toàn phiếu** trước khi trừ kho; thiếu một dòng rollback cả phiếu.
- Không âm kho: pre-check dưới khóa + check constraint + trigger.
- Khóa nguyên liệu theo thứ tự tăng dần, tương thích checkout và nhập hàng.
- Chống gửi lặp đầy đủ: `RequestId` + `G03OperationReceipt` + pending store + “Kiểm tra kết quả”.
- Bất biến sau chốt: trigger + DENY + UI chỉ đọc.
- Phân quyền kiểm ở BLL và procedure, không chỉ ẩn nút.
- Lý do bắt buộc; lượng dương, tối đa 4 số thập phân; chặn trùng NVL.
- Báo cáo tồn ghi nhận xuất theo thời điểm chốt và drill-down về đúng chứng từ.
- Rời hộp khi có nháp biết hỏi; lỗi giữ dữ liệu đã nhập.

## 7. Đề xuất đợt sửa (chờ duyệt)

**Đợt 1 — đúng số liệu/nghiệp vụ (ưu tiên):**
F02 (chặn ngày tương lai) và F01 (phương án A hoặc B cho trả NCC/công nợ), kèm
F01b (chọn A/B/C cho liên kết NCC–NVL) vì F01 cần biết đúng NCC/NVL trước khi trả.
Gồm migration + BLL + UI; test SQL rollback và native.

**Đợt 2 — chứng từ và truy vết:**
F04 (in phiếu xuất/biên bản hủy), F05 (người lập/giờ chốt/người nhận), F15 (giữ mã phiếu
vừa lập). Chủ yếu view/DTO/UI + mẫu in; không đổi bảng trừ F01/F05.

**Đợt 3 — nhập liệu an toàn:**
F06 (hiện tồn, lỗi theo dòng), F07 (xem lại trước chốt), F13, F14, F16, F17, F18.

**Đợt 4 — mở rộng cần quyết định:**
F03 (giá vốn/giá trị hao hụt), F08 (nháp + duyệt), F09 (điều chỉnh/hủy liên kết),
F10 (danh mục lý do), F11 (liên kết đơn/hóa đơn). Tách task riêng, không gộp vào sửa lỗi.

## 8. Cần người dùng chốt

1. Trả hàng: chọn A (gắn NCC + giảm công nợ), B (chứng từ giảm nợ riêng) hay C (giữ nguyên,
   chỉ đổi nhãn và quy trình thủ công)?
2. Ngày xuất tương lai: chặn hẳn (khuyến nghị) hay tách ngày chứng từ/ngày hạch toán?
3. Có duyệt đợt 1 + 2 để bắt đầu sửa, hay chỉ đợt 1?
4. Có cần giá vốn/hao hụt bằng tiền (F03) trong học kỳ này không?
5. Có cần bước duyệt hủy (F08) hay giữ mô hình một người như hiện tại?
6. Liên kết NCC–NVL (F01b): chọn A (chỉ đọc lịch sử nhập, khuyến nghị), B (bảng danh mục
   `NVL_NCC` có màn gán), hay C (giữ nguyên, ghi giới hạn)?

Không có mã nguồn nào bị thay đổi trong lần rà soát này.
