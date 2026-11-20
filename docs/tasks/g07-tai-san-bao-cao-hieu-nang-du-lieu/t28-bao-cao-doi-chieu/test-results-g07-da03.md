# Biên bản kiểm G07-DA03 — thứ tự khóa nguồn theo món

10/11/2026. Phạm vi: migration `029_dashboard_source_lock_order.sql` (chỉ ALTER
`sp_DashboardAnalyticsSources`) và regression theo phạm vi đã duyệt trong
[bug-report.md](bug-report.md). Đây là kiểm chức năng/kỹ thuật; hình thức giao diện do
người dùng tự kiểm.

## Môi trường

- SQL Server localhost (integrated security), engine thật; Windows 10.0.26200 x64.
- Source: commit `8a38f86` + thay đổi làm việc hiện có; không sửa source/migration
  trong lúc đo. Probe build riêng tại `build/g07probe` vì app WinForms đang mở khóa `bin`.
- DB test tạo mới, có `G07TestMarker`; không ghi vào DB sử dụng thật.
  Development lúc chạy parity/race/đo vẫn ở schema 27; sau đó được nâng 027→029 trong
  đợt G07-DEV02 ([biên bản](../runs/developmentG07-dashboard029_20261110/development-upgrade-029-results.md)),
  không nằm trong lượt kiểm này.
- Thư mục bằng chứng: `docs/tasks/g07-tai-san-bao-cao-hieu-nang-du-lieu/runs/dashboard-da03_20261110/`
  và `runs/dashboard-da03-perf_*/`.

## Kết quả parity và hợp đồng

DB `CoffeeManager_Test_G07_20261110_111837_5845e46f_demo` (schema+seed+fixture analytics+migration 1–28),
sau đó áp 029. Bằng chứng: `parity.json`, `sources-before.json`, `sources-after.json`,
`schema-before.json`, `data-before.json`.

| Mã | Nội dung | Expected | Actual | Kết quả |
|---|---|---|---|---|
| DA03-parity-19cases | 19 kịch bản nguồn (doanh thu ngày/tháng, invoices, average, trend, channel×3, product, product rỗng SP999, orders, delivery, material, debt, expense, skip 12/50) trước/sau | Kết quả giống nhau | 19/19 giống, `ReadAt` bị loại khỏi so sánh | PASS |
| DA03-schema-data | Bảng/cột/index và dữ liệu nghiệp vụ không đổi sau ALTER procedure | Không đổi | SchemaUnchanged=true, DataUnchanged=true, diff rỗng | PASS |
| DA03-migration-apply | Áp 029 lần 1/lần 2 và checksum | Applied rồi AlreadyApplied, checksum ổn định | `Applied`, `AlreadyApplied`, `05E2508D0F902F9A431D31971749377A2009819DE7A245C311FBB6720A706361` | PASS |
| DA03-grant | Quyền EXECUTE cho `CafeManagerApp` sau khi ALTER | Giữ 1 grant | 1 grant | PASS |
| DA03-contract-errors | Lỗi hợp đồng: kind/kỳ/product/channel/độ dài mã, actor không hoạt động 51505, transaction ngoài 53501 | Đúng mã lỗi, không vào transaction | 7/7 trước, 2/2 sau | PASS |
| DA03-lock-order | Statement đầu (materialize header) không tham chiếu `dbo.CHITIET_HD`; lọc món ở statement sau | Không có tham chiếu dòng món ở statement header | `DA03-lock-order-source` PASS | PASS |

## Hai phiên: đọc nguồn theo món trong khi tạo đơn/thêm dòng/checkout

DB race `CoffeeManager_Test_G07_20261110_111837_5845e46f_race` (4.909 bill, 028),
start gate chung, chạy 60 giây mỗi phase; writer tạo đơn → thêm 2 dòng → mỗi 10 đơn
checkout PT001; reader gọi `Kind='product'` liên tục. Mọi lỗi ghi vào `dbo.G07RaceLog`
theo từng lượt (1205/timeout/khác). Bằng chứng: `race.json`, `*-writer.log`,
`*-reader.log`, `control-028-deadlock.xml`.

| Phase | Deadlock 1205 | Timeout 1222 | Lỗi khác | Writer loops | Reader loops | Integrity |
|---|---:|---:|---:|---:|---:|---|
| control-028 | 229 (reader) | 0 | 0 | 611 | 238 | không chạy (dữ liệu bị thay thế khi restore) |
| fix-029 | 0 | 0 | 0 | 1.870 | 760 | 6/6 hạng mục bằng 0 |

Integrity fix-029: BillsWithoutLines=0, OrphanLines=0, NegativeStock=0,
PaidWithoutPayment=0, PaymentMismatch=0, DuplicateCheckoutReceipts=0; 6.779 bill,
5.093 đã trả. Reader chạy nhanh hơn 3,2 lần (238→760 vòng) và writer 3,1 lần
(611→1.870) sau khi hết deadlock.

Ghi chú bằng chứng: `fix-029-ringbuffer-stale-control-events.xml` là các sự kiện 1205
cũ còn trong ring buffer `system_health` (timestamp UTC 04:47 thuộc phase control),
không phải deadlock mới của phase fix; số liệu chính thức lấy từ `G07RaceLog`
(phase fix không có bản ghi 1205 nào). Tên file đã đổi để tránh đọc nhầm.

## Đo I/O và timing trên 10.000 hóa đơn (028 → 029)

DB `CoffeeManager_Test_G07Bench_20261110_120959_21ead801`, schema-only (không nạp
`Insert_Data` vì seed trùng mã NVL01–NVL50 của fixture). Probe `benchmark` dựng dữ liệu
qua contract: 10.036 hóa đơn, 30.720 dòng món, 8.036 đã thanh toán (ngày trả theo ngày
thực 10/11/2026), rồi áp migration 27–28, đo, áp 029, đo lại trên cùng DB/dữ liệu.
Mỗi kind chạy 5 lần; số đọc lấy từ `SET STATISTICS IO`, thời gian SQL từ
`STATISTICS TIME`, thời gian tường từ mốc `PRINT REP|kind|rep|microsecond`.
Bằng chứng: `runs/dashboard-da03-perf_20261110_120959_21ead801/` — `measure.json`,
`028-<kind>.log`, `029-<kind>.log`, `probe/results.json`, `migrations.jsonl`.

| Kind | Logical reads/lần 028 → 029 | SQL elapsed ms 028 → 029 | Wall ms 028 → 029 |
|---|---|---|---|
| revenue | 451 → 515 | 223,2 → 133,6 | 124,2 → 78,6 |
| invoices | 451 → 516 | 206,0 → 119,6 | 112,9 → 67,5 |
| average | 451 → 516 | 186,2 → 112,8 | 98,9 → 64,2 |
| trend | 451 → 516 | 191,6 → 119,0 | 102,3 → 67,2 |
| channel | 327 → 349 | 125,0 → 69,4 | 70,0 → 44,0 |
| product | 78.575 → 659 | 533,2 → 120,4 | 273,0 → 71,0 |
| orders | 289 → 298 | 101,6 → 44,8 | 58,0 → 30,1 |
| delivery | 329 → 352 | 139,6 → 68,4 | 76,9 → 41,9 |
| material | 10 → 10 | 17,2 → 13,8 | 16,9 → 14,5 |
| debt | 284 → 284 | 18,8 → 20,2 | 17,5 → 19,9 |
| expense | 110 → 110 | 14,8 → 20,0 | 14,9 → 19,1 |

- Nguồn theo món giảm 78.575 → 659 logical reads/lần (99,1%), SQL elapsed 533,2 → 120,4 ms,
  wall 273 → 71 ms. Đây là kind chịu thay đổi nhiều nhất và cũng là kind gây deadlock.
- Các kind theo hóa đơn tăng nhẹ 48–65 reads do `#headers` materialize, nhưng SQL elapsed
  và wall đều thấp hơn: reader giờ đọc header trước rồi mới chốt tập, optimizer chọn được
  plan tốt hơn. `channel` 125 → 69,4 ms, `orders` 101,6 → 44,8 ms.
- `material`/`debt`/`expense` không dùng `#headers`: reads y nguyên; dao động 0–5 ms
  (debt/expense chậm thêm khoảng 2–5 ms) nằm trong nhiễu đo, không phải thoái hóa logic.
- Parity kết quả 028 so 029 trên cùng dữ liệu: 11/11 kind giống hệt theo từng dòng
  (`ResultParity`, `SourceUnchanged=true`). Không file `src/`, `database/`, `tools/` nào bị
  sửa trong khoảng đo.
- Rủi ro đã nêu trong bug (tempdb/overhead với kỳ lớn) được đo ở đúng ngưỡng 10.000 bill:
  chi phí thêm nằm ở 8 kind theo hóa đơn, không vượt 65 reads/lần, và tổng thời gian vẫn giảm.
- Ghi chú: `measure.json` còn trường `Error` là lỗi của hàm parity cũ (`Compare-Object`
  với mảng rỗng), đã thay bằng so sánh chuỗi nối trong `finalize-measure.ps1`; số đo và
  `ResultParity` bên dưới là kết quả sau khi sửa harness.

## Lỗi của kịch bản đo (không phải lỗi source/migration)

- `INSERT @t EXEC` mở transaction ngầm (`@@TRANCOUNT=1`) nên procedure chặn bằng 53501;
  reader chuyển sang gọi `EXEC` trực tiếp.
- `NEWID()` không dùng trực tiếp trong danh sách tham số `EXEC` (lỗi cú pháp 102);
  writer gán biến trước.
- Không nạp seed vào DB benchmark: seed có NVL01–NVL50 trùng mã fixture của probe.
- Hàm đo trùng tên alias `measure` của PowerShell và `Compare-Object` lỗi khi gặp mảng
  rỗng; đổi tên hàm và so sánh bằng chuỗi nối, rồi chạy lại bước đo trên cùng DB
  (`measure-only.ps1`, `finalize-measure.ps1`).

Các lỗi này được sửa trong script của thư mục run; migration 029 giữ nguyên.

## Chưa chạy

- Nghiệm thu hình thức dashboard và kiểm thủ công trên WinForms 10.000 bill: người dùng tự làm.
- DPI 100%/150%: chưa chạy (agent chỉ kiểm kỹ thuật ở 125% khi có ca liên quan).
- Nâng development: không thuộc lượt này; đã thực hiện riêng ở G07-DEV02, Development hiện schema 29.
