# Plan T35 — Tương thích, tài liệu và regression

Ngày 16/11/2026. Trạng thái: **đã duyệt; đã rà soát/sửa điểm thiếu, cập nhật tài liệu,
chạy regression 17/11/2026 và UI 18/11/2026; đủ điều kiện nghiệm thu** — CP05 PASS;
CP06 PASS thêm UI ADMIN/CASH (STORE NOT RUN); CP08 PASS chức năng 1280×720/1366×768
ở 125%; hình thức/DPI 100–150% người dùng tự kiểm
([test-results.md](test-results.md)). T35 chạy sau T33/T34; không
mở lại nghiệp vụ đã chốt. Nguồn:
[requirements-cho-thanh-toan-phien-ban.md](../requirements-cho-thanh-toan-phien-ban.md)
mục 6. Giữ nguyên 56 ID T14–T18, 325 ID toàn dự án và lịch sử test.

## 1. Mục tiêu

Rà toàn bộ nơi còn hiểu "đơn mở = `Chua thanh toan`" hoặc "bàn trống = không có đơn
`Chua`" sau T33/T34; sửa nhãn/màu/lọc còn thiếu; cập nhật UX và contract; chạy
regression theo ảnh hưởng thực tế. **Không thêm migration** trong điều kiện bình
thường: các thay đổi SQL đã nằm ở 032/033. Nếu phát hiện lỗ hổng SQL, dừng và xin
duyệt bổ sung (tạo migration version mới, không sửa migration đã áp).

## 2. Phạm vi rà soát (đã khoanh vùng từ source)

### 2.1. C# — trạng thái đơn/bàn

| File | Điểm cần kiểm |
|---|---|
| `src/CafeManager.Models/Dtos/Sales/SalesOrderDto.cs:9` | `CanEdit`/`IsPayable`/`IsOpen`: xác nhận không caller nào dùng `IsOpen` với nghĩa "còn sửa". |
| `src/CafeManager.Models/States/SalesStates.cs:5-11` | Nhãn `Cho thanh toan`, nhãn trạng thái bàn/phiên nếu có. |
| `src/CafeManager.WinForms/Modules/Sales/SalesControl.cs:179-193,629-651` | `SetBusy`, F8, hủy, yêu cầu TT/mở lại/trả bàn đúng điều kiện; không cho sửa khi `Cho`. |
| `src/CafeManager.WinForms/Modules/Sales/OrderPanel.cs:55-56` | Màu/nhãn badge cho `Cho`; đơn đã trả giữ Lilac. |
| `src/CafeManager.WinForms/Modules/Sales/CheckoutDialog.cs` | `ReloadPreview`/`Complete` dùng `IsOpen`/status `Da`; đơn `Cho` preview được. |
| `src/CafeManager.WinForms/Modules/Sales/InvoicesControl.cs:19` | Bộ lọc trạng thái đủ 4 giá trị. |
| `src/CafeManager.WinForms/Modules/Sales/InvoiceDetailDialog.cs:33` | `Quay lại POS` dùng `IsPayable`. |
| `src/CafeManager.WinForms/UI/OrbitRecordListControl.cs:58` | Nhãn dùng chung thêm `Cho thanh toan`. |
| `src/CafeManager.WinForms/UI/OrbitDataGridView.cs:433-435` | Badge: `Cho` nhóm chờ; `Cho thanh toan` không bị xếp "đã thu" hoặc "hủy". |
| `src/CafeManager.WinForms/UI/OrbitTableCard.cs:25,33-36` | Nhãn `Cho`/`Retained`; `busy` vẫn theo `Status != "Trong"`. |
| `src/CafeManager.WinForms/Modules/Tables/TablesControl.cs:27,36-40` | Cột `OrderCode`/trạng thái/session; card nhận `Retained`; action Trả bàn. |
| `src/CafeManager.WinForms/Modules/Tables/TableList.cs` (nếu tách) | Đồng bộ với `TablesControl`. |

Cách làm: `grep` toàn `src/` cho `Chua thanh toan`, `Da thanh toan`, `IsOpen`,
`TrangThai=N`, `Status ==`; lập bảng từng điểm và kết luận giữ/sửa. Không đổi logic
ngoài phạm vi trạng thái/phiên.

### 2.2. SQL — đọc/đếm và lọc

- `sp_InvoiceList`/`sp_InvoiceDetail` (`013_invoice_queries.sql`): lọc trạng thái theo
  tham số, không hard-code; kiểm danh sách giá trị hợp lệ nếu có whitelist.
- Các đếm đơn mở/báo cáo đã sửa ở 032: `sp_ReportDashboard` (`026:135`),
  `sp_DashboardAnalyticsRead` (`028:67`), `sp_DashboardAnalyticsSources` (`029:31`).
  T35 chỉ **kiểm chứng**, không sửa lại; nếu còn chỗ khác hard-code thì ghi bug và
  xin duyệt migration bổ sung.
- Các chỗ đọc "bàn trống" khác ngoài `vw_TrangThaiBan` (nếu có) phải dùng view/cờ
  `Retained`; không tự suy từ `HOADON` rời.
- Kiểm tra các chỗ dùng `HOADON.MaPhien`/`PHIEN_BAN` chỉ qua procedure; app không đọc
  bảng trực tiếp ngoài contract.

### 2.3. Tài liệu và contract

- `docs/UX_QuanLiQuanCafe.md`: bổ sung luồng "Yêu cầu thanh toán" (khóa sửa, mở lại
  ADMIN), trạng thái bàn theo phiên, "Trả bàn ngay?" sau thu/hủy, gọi thêm sau trả
  trước (nhiều hóa đơn, không gộp), giới hạn không tự đóng phiên/không backfill.
- `docs/database/sales-contract.md`: 4 trạng thái, bảng chuyển, hai cổng, mã 52108/
  52109, quyết định không khóa `GiamGia` ở trigger, giới hạn in chỉ thêm nhãn.
- `docs/database/table-contract.md`: `PHIEN_BAN`, `HOADON.MaPhien`, view + `Retained`,
  `sp_TraBan`, `TableClose`, mã 52110/52111, fallback đơn cũ.
- `docs/database/checkout-contract.md` / `invoice-contract.md`: thu từ `Cho` giữ
  nguyên phân bổ/snapshot; danh sách trạng thái hóa đơn có `Cho`.
- `docs/database/data-contract.md`, `migrations.md`, `write-ownership.md`: bảng/cột/
  quyền mới, thứ tự migration 032/033, không backfill.
- `docs/tasks/g04-ban-hang-vong-doi-hoa-don/requirements.md` + `plans.md`: đánh dấu
  đã duyệt/triển khai theo thực tế, liên kết plan; **không** đổi 56 ID/checklist cũ.
- `docs/tasks/g04-ban-hang-vong-doi-hoa-don/development-guide.md`: cách áp 032/033
  trên DB development sau khi app đóng, backup, preflight, smoke; không tự chạy seed.

### 2.4. Regression G04 và các nhóm liên quan

- T14 tạo/sửa đơn (thường + nháp flush), T15 checkout, T16 POS/hủy, T17 tra cứu/in,
  T18 giao hàng: chạy lại các ca bị ảnh hưởng bởi trạng thái/cổng/view.
- Dashboard/báo cáo đếm đơn mở, doanh thu theo ngày thanh toán; in hóa đơn đã trả;
  phân quyền CASH/ADMIN/STORE ở POS, hóa đơn, bàn.
- Ghi kết quả theo mẫu `test-results.md` cạnh task; không tick ca chưa chạy; hình thức
  SKIPPED, DPI 100/150% NOT RUN theo phân công.

## 3. Các bước triển khai

### Bước 1 — Lập bảng rà soát và sửa điểm thiếu

Files: các file ở mục 2.1/2.2; có thể thêm `docs/tasks/g04-.../t35-.../audit.md`
(bảng điểm rà soát, kết luận giữ/sửa, bằng chứng dòng).

- Chạy grep, ghi bảng `file:dòng — hiện tại — tác động — hành động`.
- Sửa nhãn/màu/lọc còn thiếu (không đổi bố cục, không thêm control mới).
- Nếu phát hiện predicate SQL sai: ghi bug tại T33/T34 tương ứng, **không tự sửa**;
  đề xuất migration bổ sung để duyệt.

Verify: build Debug/Release 0 error; grep còn lại chỉ là nơi đã kết luận giữ nguyên
có lý do; không có chuỗi trạng thái hard-code trong UI ngoài `L.T`/`SalesStates`.

### Bước 2 — Cập nhật tài liệu/contract

Files: như mục 2.3.

- Viết mô tả nghiệp vụ ngắn, bảng chuyển trạng thái, luồng bàn, giới hạn và cách
  đối chiếu khi lỗi; link tới plan/requirements.
- Đồng bộ thuật ngữ: "Chờ thanh toán" (UI) / `Cho thanh toan` (DB); "Trả bàn" /
  `TableClose`; "phiên bàn" / `PHIEN_BAN`.
- Không sửa lịch sử test/bug cũ; chỉ thêm ghi chú ngày 16/11/2026.

Verify: tài liệu khớp hành vi đã triển khai; không còn câu "bàn trống ngay sau khi
thu tiền" hoặc "chỉ có 3 trạng thái".

### Bước 3 — Regression theo ảnh hưởng

Files: `docs/tasks/g04-ban-hang-vong-doi-hoa-don/t33-cho-thanh-toan/test-results.md`,
`t34-phien-ban/test-results.md`, `report.md` (sau đợt test được duyệt).

- Chạy các ca PP/TS đã PASS ở T33/T34 cộng regression T14–T18 liên quan; ghi rõ
  môi trường, schema 032/033, DB test riêng, commit/hash source.
- Ca hai session, timeout/đối chiếu, in sau commit giữ như đợt test G04.
- Không sửa source/migration trong lượt test; FAIL ghi bug, không chỉnh expected.

Verify: báo cáo nhóm nêu rõ PASS/FAIL/BLOCKED/NOT RUN và phần hình thức do người dùng
tự kiểm.

## 4. Ma trận kiểm chứng dự kiến (chờ duyệt đợt test riêng)

| ID | Ca và expected |
|---|---|
| CP01 | Grep/rà soát: mọi predicate trạng thái đơn/bàn đã tách `CanEdit`/`IsPayable`; không còn UI hiểu `Cho` là sửa được. |
| CP02 | Nhãn/màu: POS, panel đơn, danh sách đơn mở, lịch sử hóa đơn, chi tiết, card bàn hiển thị "Chờ thanh toán"/"Đã trả · giữ bàn" đúng. |
| CP03 | Lọc hóa đơn đủ 4 trạng thái; `Quay lại POS` đúng cho `Chua`/`Cho`, tắt cho `Da`/`Huy`. |
| CP04 | Dashboard/báo cáo đếm đơn mở gồm `Chua`+`Cho`; doanh thu không đổi; nguồn `orders` khớp. |
| CP05 | Regression tạo đơn thường/nháp, checkout từ `Chua` và `Cho`, hủy, in lại, tra cứu: không hồi quy. |
| CP06 | Regression giao hàng T18 và phân quyền CASH/ADMIN/STORE: không mở quyền mới ngoài trả bàn/yêu cầu TT/mở lại đã chốt. |
| CP07 | Tài liệu UX/contract khớp hành vi; requirements/plans cập nhật trạng thái, giữ 56 ID/325 ID. |
| CP08 | UI chức năng 125% tại 1280×720/1366×768 cho các màn bị ảnh hưởng; hình thức SKIPPED, DPI 100/150% NOT RUN. |

## 5. Tiêu chí hoàn tất và rủi ro

Hoàn tất khi: không còn điểm đọc trạng thái/bàn sai ngoài danh sách đã kết luận;
nhãn/lọc/in/dashboard khớp 4 trạng thái và phiên bàn; tài liệu/contract đồng bộ;
regression ảnh hưởng PASS trong đợt test đã duyệt; build Debug/Release sạch. Không
đánh Hoàn tất G04/G08 từ T35; điều kiện gỡ tiện ích đăng nhập TEST ở T32/G08 giữ nguyên.

Rủi ro chính: sửa sót một predicate gây hiển thị sai — xử lý bằng bảng audit + CP01;
phát hiện lỗ hổng SQL muộn — dừng, báo bug và xin duyệt migration mới; tài liệu mô tả
vượt hành vi thực tế — chỉ ghi sau khi đối chiếu source/test; đụng checklist cũ — không
sửa ID/lịch sử.
