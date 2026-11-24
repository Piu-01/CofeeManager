# T20b — Nhận hàng 2 bước: biên bản đợt test

Ngày 23/11/2026. Đợt test được người dùng duyệt. Kết quả dưới đây chỉ thuộc phạm vi T20b;
không thay đổi lịch sử T20/T01–T32 và không nâng 325 ID.

## Môi trường và nguồn

- SQL Server thật `localhost`, bản 2025 RTM 17.0.1000.7, xác thực Windows.
- DB test riêng `CoffeeManager_Test_G05_20261123_220006_d9d1cf50_demo`, tạo từ `DB_Schema.sql` +
  `Insert_Data.sql`, áp migration 001–043 bằng `tools/database/Apply-Migration.ps1` trong transaction;
  marker `G05TestMarker` và dọn DB sau chạy (đã xác nhận còn 0 DB test cùng run).
- Schema cuối `SchemaMigrations Max(Version)=43`. Tồn kho JSON trước/sau toàn bộ migration giống hệt
  (`InventoryUnchanged=true`).
- Source app cố định trong lượt chạy; chỉ thêm/cập nhật test asset (`database/tests/g05/*`, xUnit) và
  dọn file test asset tạm sau chạy. Build Debug/Release: 0 lỗi (chỉ cảnh báo MSB3277 WebView2 có sẵn).
- Không chạy migration trên DB sử dụng thật.

Bằng chứng SQL (lượt chạy lại cuối bằng runner đã hoàn thiện):
`runs/acceptance_20261123_220439_02134ca4/` (manifest, migrations.jsonl, sql-results.json).
Probe in/UI: `runs/acceptance_20261123_220006_d9d1cf50/print-probe/`.

## SQL — 35/35 PASS

Lệnh: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/database/Test-T20bReceiving.ps1`
(script mới chỉ dùng DB test G05 có marker, tự dọn DB). File test: `database/tests/g05/t20-purchases.sql`
(viết lại cho luồng 2 bước) và `database/tests/g05/t20b-receiving.sql` (mới).

| ID | Expected | Actual / bằng chứng | Kết quả |
|---|---|---|---|
| T20b-K01 | Lập phiếu Chờ nhận: cấp mã, `TongTien=0`, chưa cộng tồn, chưa chi/nợ; replay cùng RequestId không nhân đôi. | `T20b-K01.SQL-draft`, `T20-K05.SQL-draft` | PASS |
| T20b-K02 | STORE lưu số kiểm: đủ dòng, lệch phải có lý do, không vượt số đặt, số nguyên; không đổi tồn; replay 1 receipt. | `T20b-K02.SQL-*` (missing-line 52701, over-ordered 52704, fraction 52701, diff-reason 52704, cash-denied 51605, count PASS) | PASS |
| T20b-K03 | ADMIN chốt: tồn +thực nhận, tổng/chi/nợ theo thực nhận; replay không nhân đôi; mọi tổng phiếu khớp dòng đã ghi kho. | `T20b-K03.SQL-post` (tồn NVL01 +8, NVL02 +4; tổng 160000; chi 4000; nợ 156000), `SQL-totals-intact`, `T20-K05.SQL-replay` | PASS |
| T20b-K04 | Quyền: STORE/CASH không lập, STORE không chốt, CASH không xem danh sách; `CafeManagerApp` đủ GRANT proc + TVP. | `T20b-K04.SQL-*` (51605 ×4, grants PASS) | PASS |
| T20b-K05 | Revision cũ ở bước kiểm/chốt bị chặn, phải tải lại. | `T20b-K05.SQL-stale-count`, `SQL-stale-post` (52706) | PASS |
| T20b-K06 | Huỷ phiếu chưa chốt: bắt buộc lý do, không đụng tồn/chi/nợ; huỷ lần hai/bill đã chốt bị chặn; chi tiết phiếu huỷ bất biến. | `T20b-K06.SQL-*` (52701/51605/52706/52702) | PASS |
| T20b-K07 | Dòng phiếu cũ (Legacy, đã chốt) được backfill `SoLuongNhan=SoLuong`, `DaGhiKho=1`; tồn không đổi qua migration. | `T20b-K07.SQL-legacy-backfill`, manifest `InventoryUnchanged=true` | PASS |
| T20b-K08 | `vw_ReportStockEvents` lấy số thực nhận và thời điểm `PurchaseReceive`, không lấy thời điểm lập. | `T20b-K08.SQL-stock-report` | PASS |
| T20b-K09 | Chặn số lượng lẻ, đơn giá lẻ, trùng nguyên liệu ở `sp_PurchaseValidate`. | `T20b-K09.SQL-*` (52701 ×3) | PASS |
| T20b-K10 | Chặn ngày tương lai; trả ngay vượt tổng thực nhận ở chốt; trả đủ tạo một chi, không nợ; lọc trạng thái sai bị chặn và trạng thái view đúng. | `T20b-K10.SQL-*` (52701/52703/paid-full/52701) | PASS |

Ghi chú: các ca thiếu dòng/lệch/vượt/revision dùng hai phiên tuần tự trên cùng revision; xung đột
hai session song song và fault injection sau từng bước không chạy trong lượt này.

## Unit — 25/25 PASS

Lệnh: `dotnet test tests/CafeManager.Tests -c Debug --filter 'FullyQualifiedName~CafeManager.Tests.Finance|FullyQualifiedName~CafeManager.Tests.Inventory'`.
Bổ sung `PurchaseRulesTests`: chặn số lượng lẻ `1.5`, đơn giá lẻ `10000.5`, chấp nhận số nguyên.
Toàn bộ suite không chạy trọn được vì các fixture `Test-G02/G03/G04…` cần harness DB riêng; không
suy PASS/FAIL sản phẩm từ nhóm bị chặn này.

## In phiếu và UI component — PASS (chưa phải GUI thật)

Probe tạm (ngoài repo, tham chiếu bản build WinForms, gọi qua reflection; log tại
`runs/acceptance_20261123_220006_d9d1cf50/print-probe/probe.log`):

- `T20b-K11.SQL-PDF`: sinh PDF hợp lệ `%PDF` cho bản Chờ nhận và đã chốt; tiêu đề, mã phiếu,
  trạng thái, tiền (180.000/160.000/156.000 ₫) và ghi chú “không phải hóa đơn GTGT” có trong báo cáo.
  Bản Chờ nhận để trống thực nhận, bản đã chốt in số thực nhận (theo code).
- `T20b-K12.UI-component`: 13/13 PASS — cột editor `MÃ NVL…THÀNH TIỀN`, nút **Lưu phiếu chờ nhận**;
  cột nhận `SỐ ĐẶT/THỰC NHẬN/LÝ DO LỆCH`; STORE không sửa trả ngay, không chốt; ADMIN chốt khi Đã kiểm;
  Đã chốt chỉ đọc; trạng thái unknown chặn lưu/chốt và hiện Kiểm tra kết quả; blocked ẩn nút này.

## NOT RUN / chuyển người dùng

- Thao tác GUI thật end-to-end ADMIN/STORE (mở dialog từ menu, nhập liệu, phím/tab/focus, ErrorProvider
  tại ô, preview WebView2, lệnh in máy in) — NOT RUN do lượt này không có công cụ điều khiển desktop;
  người dùng tự kiểm.
- Hình thức giao diện, DPI 100%/125%/150% — SKIPPED theo phân công; agent không đo DPI trong lượt này.
- Hai session song song, fault injection sau commit, timeout mạng thật — NOT RUN.

Không phát hiện bug mới trong phạm vi đã chạy. T20b giữ trạng thái **Đang làm** cho tới khi GUI thật
và phần in được người dùng nghiệm thu.

## Đợt 24/11/2026 — số lượng thực (migration 045)

Đợt test được người dùng duyệt ngày 24/11/2026, phạm vi SQL T20b sau khi cho phép số lượng
tối đa 4 chữ số thập phân. Không chạy GUI/hình thức/DPI.

- DB test riêng `CoffeeManager_Test_G05_20261124_171230_15f773d7_demo`, schema 001–045,
  `InventoryUnchanged=true`, đã dọn DB (`Cleaned=true`).
- Runner: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/database/Test-T20bReceiving.ps1`;
  bằng chứng `runs/acceptance_20261124_171230_15f773d7/` (manifest, migrations.jsonl, sql-results.json).
- **35/35 PASS**, `SqlFailed=0`, `SchemaVersion=45`.

Thay đổi kỳ vọng so với đợt 23/11 (do nghiệp vụ số thực):

| ID | Kỳ vọng mới | Bằng chứng | Kết quả |
|---|---|---|---|
| T20b-K02 | Số lượng lẻ `7.5` được lưu (`SoLuongNhan=7.5`); vẫn chặn thiếu dòng/vượt đặt/lệch thiếu lý do/quyền. | `T20b-K02.SQL-fraction`, `SQL-count` | PASS |
| T20b-K09 | `1.5` được chấp nhận; đơn giá lẻ `10000.5` và trùng nguyên liệu vẫn bị chặn. | `T20b-K09.SQL-frac-qty`, `SQL-frac-price`, `SQL-duplicate` | PASS |

Lượt chạy đầu `acceptance_20261124_171145_7e2a9fd7` FAIL do **test fixture** giữ revision cũ sau
khi ca fraction chuyển từ “bị chặn” sang “lưu thành công”, nên lệnh kiểm cuối báo
“Phiếu đã đổi…” và script dừng trước khi ghi kết quả. Đã sửa fixture: đọc `Revision` mới vào
`@revCount` sau ca fraction, dùng cho các ca lệch/cash/count; giữ `@revDraft` cũ riêng cho K05
(stale revision). Không đổi kỳ vọng nghiệp vụ, không sửa source/migration trong lượt test.
Lượt chạy lại 35/35 PASS; DB test của lượt FAIL đã được dọn.

Vẫn **NOT RUN** như đợt trước: GUI thật end-to-end, hình thức/DPI, hai session song song,
fault injection/timeout. T20b vẫn **Đang làm** cho tới khi người dùng nghiệm thu GUI.
