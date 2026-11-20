# G07-DA02 — Regression 10.000 hóa đơn và kiểm native sau sửa

**Ngày:** 10/11/2026
**Phạm vi đã duyệt:** chạy lại regression 10.000 hóa đơn + timing UI trên dataset đó;
kiểm trạng thái ngày/tháng/rỗng/lỗi/hide-dispose/query muộn/nguồn trên WinForms thật;
ma trận 4/2/1 KPI và 2/1 chart tại 1280×720, 1366×768.
**Nguồn cố định trong lượt:** HEAD `8a38f8691d37089feec149b71a8f5718d37167f3` + working tree;
Snapshot source trước/sau 469 file (trừ `bin/obj`), diff = 0 dòng
([source-before.csv](runs/da02-regression_20261110_143622/source-before.csv),
[source-after.csv](runs/da02-regression_20261110_143622/source-after.csv),
[source-diff.txt](runs/da02-regression_20261110_143622/source-diff.txt),
[head.txt](runs/da02-regression_20261110_143622/head.txt)). Không sửa source/migration
trong lượt test.

## Môi trường

| Thành phần | Giá trị |
|---|---|
| Máy | Windows 10.0.26200, x64, DPI hệ thống 125% (DeviceDpi 120) |
| SDK / runtime | .NET SDK 10.0.401, app `net10.0-windows` |
| SQL Server | localhost 17.0.1000.7 Developer, integrated security, Encrypt + TrustServerCertificate, `Pooling=false` cho harness |
| Schema DB test | 29 (migrations 001–029 áp qua `tools/database/Apply-Migration.ps1`) |
| DB dataset | `CoffeeManager_Test_G07_20261110_144033_ca33988f_demo`, marker `20261110_144033_ca33988f` |
| DB rỗng | `CoffeeManager_Test_G07_20261110_152830_fceb2357_demo`, marker `20261110_152830_fceb2357` |
| Cửa sổ đo | `MainForm.ClientSize` 1366×768 (chuẩn), phụ 1280×720 / 1920×1080 / 600×720 |

Harness nối chuỗi với app: `Harness/Program.cs` dựng `MainForm` thật, gắn
`AdminDashboardControl` vào panel `content` (reflection), đăng nhập ADMIN qua
`sp_SetupBootstrap ActivateDemo` + `SessionContext`, dùng `EXECUTE AS USER='ExtendedRuntime'`
để kiểm đường quyền runtime. Không chụp ảnh, không đổi Windows Scale.

## Dataset

Tạo qua contract bán hàng thật (`SalesService`), không INSERT SQL trực tiếp:

| Chỉ tiêu | Giá trị |
|---|---:|
| Tổng hóa đơn | 10.000 |
| Đã thanh toán | 7.999 |
| Chưa thanh toán | 1.001 |
| Hủy | 1.000 |
| Món bán | 1 (`SP999`, 50 ký tự) × 1 ly/hóa đơn trả tiền → 7.990 ly |
| Thời gian sinh | 26,7 phút |

Bằng chứng: [bills.json](runs/da02-regression_20261110_143622/bills.json),
[prepare.json](runs/da02-regression_20261110_143622/prepare.json). DB rỗng tạo riêng
bằng `empty-db/setup.ps1` để trạng thái rỗng không bị dataset perf ghi đè.

## Timing UI trên 10.000 hóa đơn (mục tiêu <3.000 ms)

Đo từ `Shown` → control `ready=true` trên UI thread thật, gồm đọc SQL và dựng toàn bộ cây card.

| Chế độ | Thời gian đến ready | Kết quả |
|---|---:|---|
| Startup (Hôm nay) | 582,6 ms | PASS |
| Tháng này | 645,2 ms | PASS |
| Hôm nay (đổi chế độ) | 661,5 ms | PASS |

SQL service cùng kỳ (3 lượt mỗi chế độ, `ReadDashboardAnalyticsAsync`):

| Chế độ | Lượt 0 | Lượt 1 | Lượt 2 | Rows |
|---|---:|---:|---:|---:|
| Hôm nay | 711,8 ms | 404,4 ms | 375,9 ms | 63 |
| Tháng này | 353,6 ms | 342,4 ms | 375,7 ms | 51 |

So với trước sửa (startup 4.939 ms / monthly 5.389 ms / today 5.370 ms trên 10.000 bill),
UI nay dưới mục tiêu 3 s và chi phí layout lặp đã bị loại khỏi đường chạy thật.

## Kết quả kiểm

### Perf — 12/12 PASS ([perf-results.json](runs/da02-regression_20261110_143622/perf-results.json))

| Mã | Nội dung | Kết quả |
|---|---|---|
| P01 | Startup Hôm nay < 3.000 ms | PASS · 582,6 ms |
| P02 | Doanh thu/hóa đơn Hôm nay khớp SQL | PASS · 7.990.000 ₫ / 7.990 (SQL 7.990) |
| P03 | Tháng này < 3.000 ms | PASS · 645,2 ms |
| P04 | Doanh thu/hóa đơn Tháng này khớp SQL | PASS · 7.990.000 ₫ / 7.990 |
| P05 | Hôm nay (đổi chế độ) < 3.000 ms | PASS · 661,5 ms |
| P06 | Doanh thu/hóa đơn sau đổi chế độ khớp SQL | PASS · 7.990.000 ₫ / 7.990 |
| P07 | Top 5 món khớp SQL, đúng 1 thanh `Bar`/dòng | PASS · “SP999 · 7.990 ly”, 1 bar |
| P08 | Query muộn (Tháng→Hôm nay dồn dập) kết thúc ở chế độ cuối | PASS · “Hôm nay · 10/11/2026 00:00:00 → …” |
| P09 | Ẩn khi đang tải | PASS · state “Đang tải tổng quan…”, 4 KPI “—” |
| P10 | Dispose khi đang tải | PASS · Disposed=true, 0 unhandled |
| P11 | Lỗi DB không hiện số 0 giả | PASS · “Chưa cập nhật · Không kết nối được dữ liệu; kiểm tra kết nối và thử lại.”, KPI “—”, Tải lại bật |
| P12 | Nguồn theo hóa đơn/món phân trang khớp tổng | PASS · TotalCount 7.990, trang 1/2 = 50 dòng, nguồn món 7.990 dòng |

Ca lỗi P11 chạy với cấu hình DB không tồn tại (`CoffeeManager_Test_G07_missing_db`, timeout 5 s),
không ghi vào DB thật hay development. `Unhandled=[]`, `Error=null`, `Status=PASS`.

### Rỗng — 3/3 PASS ([empty-results.json](runs/da02-regression_20261110_143622/empty-results.json))

| Mã | Nội dung | Kết quả |
|---|---|---|
| E01 | Ngày rỗng vẫn về ready | PASS · “Đã cập nhật số liệu.” |
| E02 | Ngày rỗng: doanh thu 0 ₫, 0 hóa đơn, Top 5 rỗng | PASS · `["0 ₫","0","—","0 ₫"]` + “Chưa có món bán trong kỳ.” |
| E03 | Tháng rỗng cùng kết quả | PASS · `["0 ₫","0","—","0 ₫"]` |

### Ma trận bố cục (125% DPI, cả dataset lẫn rỗng)

| ClientSize | KPI cột | Chart cột | KPI rộng | Chart rộng | Page/Scroll |
|---|---:|---:|---:|---:|---|
| 1366×768 | 2 | 2 | 511/512 | 511/512 | 1047/1095 |
| 1280×720 | 2 | 2 | 468/469 | 468/469 | 961/1009 |
| 1920×1080 | 4 | 2 | 388–389 | 788/789 | 1601/1649 |
| 600×720 | 1 | 1 | 441 | 441 | 453/501 |

Mọi card cao > 0, không tràn ngang (page ≤ viewport scroll), không cắt chữ khảo sát
qua kích thước control; 600 px dùng `MinimumSize=320×320` trên form test để không bị
clamp về ngưỡng 2 cột. Ma trận hai lượt (dataset/rỗng) trùng nhau.

### Build

| Mã | Nội dung | Kết quả |
|---|---|---|
| B01 | `dotnet build CafeManager.slnx -c Release` | PASS · 0 warning / 0 error |
| B02 | `dotnet build CafeManager.slnx -c Debug` | BLOCKED · `bin\Debug` bị CafeManager.WinForms (PID 33760) và Visual Studio (36512) khóa; không kill process người dùng |

## NOT RUN

- DPI 100%/150%: không đổi Windows Scale; ghi NOT RUN, không suy PASS.
- Hình thức thanh Top 5 sau đổi `Bar` (màu/độ dày/khoảng cách) và cảm nhận độ mượt
  trên app đang mở: người dùng tự kiểm theo phân công trong AGENTS.md.
- Kiểm native màn nguồn chi tiết dạng dialog owned trên app đang mở (P12 kiểm
  dữ liệu/phân trang qua service + UI, không mở dialog trực quan).
- Màn Tổng quan trên development sau nâng 028→029 vẫn chờ người dùng mở kiểm
  (thuộc G07-DEV02, không phải lượt này).

## Dọn DB test

Hai DB `CoffeeManager_Test_G07_20261110_144033_ca33988f_demo` và
`CoffeeManager_Test_G07_20261110_152830_fceb2357_demo` được tạo riêng cho lượt này
và đã drop sau khi kiểm marker khớp `RunId` (`Retained=false`, xem
[cleanup.json](runs/da02-regression_20261110_143622/cleanup.json)). Không đụng
development hay DB sử dụng thật.

Còn 9 DB `CoffeeManager_Test_G07*` sót từ các lượt điều tra/bench trước
(`20261110_1024`–`111837`, `G07Bench_20261110_1205`–`120959`), không thuộc phạm vi
lượt này nên chưa xoá — chờ người dùng quyết định dọn.
