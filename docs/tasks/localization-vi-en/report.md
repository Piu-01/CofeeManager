# Báo cáo đợt test VI/EN — 10/11/2026

**Cập nhật sau duyệt debug:** LOC-B01–B03 đã sửa; 153/153 unit, 21/21 component,
Debug/Release và audit 1.521 key PASS. Xem `regression-results.md`. Phần báo cáo test
ban đầu dưới đây giữ lịch sử FAIL/BLOCKED; các nhánh native còn thiếu vẫn chưa được
chốt PASS. Task Đang làm, không cần duyệt sửa lại ba bug đã thuộc batch này.

**Trạng thái: Đang làm; chưa nghiệm thu hoàn tất.** Người dùng đã duyệt đợt test;
phát hiện ba bug chữ/ngôn ngữ cần duyệt debug tại `bug-report.md`. Không sửa app,
migration hoặc test gốc trong đợt này. Giữ lịch sử kiểm tra kỹ thuật cũ và 325 ID
T01–T32, không nâng trạng thái G08.

## Môi trường và tính cố định

- Windows, .NET SDK 10.0.401; SQL Server localhost **17.0.1000.7**; DB test schema
  **028**, tạo mới từ baseline/seed/migrations hiện tại. DPI thực tế đo được **120
  (125%)**, WorkingArea 1920×1020; không đổi Windows Scale.
- HEAD tham chiếu `8a38f86` + working tree. Trước test khớp manifest triển khai;
  source/migrations/tests/tools trước/sau lượt mới **0 thay đổi**. Hai manifest tại
  `runs/acceptance-20261110/source-before.csv` và `source-after.csv`.
- Chỉ tạo và ghi vào ba DB `CoffeeManager_Test_LOC_20261110_094901_ff92cc76_vi`,
  `_en`, `_clean`; không dùng DB development/DB đang sử dụng làm fixture. Mỗi DB có
  marker cài đặt và marker của lượt test. Credential fixture được hash qua BLL;
  không lưu password/hash trong evidence.
- Probe bổ sung nằm trong hồ sơ runs, gọi assembly/control/procedure thật, không
  sửa source app hoặc chèn bypass đăng nhập vào app. Chỗ gọi handler/private field
  để kiểm component được phân biệt với thao tác native của người dùng.

## Kết quả đã chạy

| Lớp kiểm | Kết quả thực tế | Bằng chứng |
|---|---|---|
| Build app Debug cho lượt | PASS, 0 warning / 0 error | `build.log` |
| Unit thuần | **152/152 PASS**, không chạy integration trong xUnit này | `unit-acceptance.trx` |
| Audit tĩnh | **1.519 used / 1.519 EN, 0 vi phạm**; không bao phủ mọi nhãn runtime | `audit.log` |
| SQL/BLL E2E ở VI và EN | **14/14 subcase PASS** (7 mỗi ngôn ngữ) | `vi-e2e.json`, `en-e2e.json` |
| Probe UI/component chính | 92 entry: **85 PASS, 7 bị cờ FAIL**; 3 là bug app, 4 là oracle viewport chưa xét cuộn | `probe-results.json` |
| Probe bổ sung | **25/25 PASS**, trong đó 4 ca xác minh cuộn thay cho suy đoán từ viewport | `supplement/probe-results.json` |
| Native accessibility | Đọc đúng login EN, set username fixture và đọc lại được | `native-accessibility.json` |
| Click/Tab native | **BLOCKED**: click báo `coordinate input geometry is unavailable`; Tab/Shift+Tab không chuyển focus sau refresh | Cùng evidence native |

Build probe đầu đã dừng để tránh chờ NuGet audit mạng; build lại thành công. Build
probe bổ sung có NU1900 (không truy cập được metadata NuGet), **0 error**; ghi log,
không coi là lỗi chức năng. Không sửa package/source để bỏ lỗi app.

### SQL, dữ liệu và quyền

- Nhập 10×1.000, trả ngay 2.000, trả nợ 3.000+5.000: tồn +10, dư nợ 0, lịch sử và
  receipts đúng; replay không tạo nguồn chi thứ hai.
- Đơn tại quán hai dòng khác ghi chú, hai thanh toán 20.000+30.000: tổng 50.000,
  tồn NVL01 −0,036 và NVL03 −0,020, replay không thu/trừ kho lần hai.
- Xuất hủy, stocktake cũ bị từ chối khi có phát sinh; snapshot mới chốt đúng một
  lần. Lương ca qua đêm 8 giờ ×35.000 =280.000; chốt/chi replay một nguồn.
- Giao hàng đã trả chuyển đúng bước; hash tài chính/kho/điểm giữ nguyên. CASH/STORE
  đăng nhập thật, bị chặn tài chính; khóa fixture thu hồi phiên. `@@TRANCOUNT` và
  `XACT_STATE()` cuối luồng là 0:0.
- Hai session cùng checkout một request: **2/2 PASS**, mỗi ngôn ngữ một ca; mỗi
  đơn chỉ một payment row, NVL01 −0,018, cộng đúng 25 điểm, ghi chú Việt giữ nguyên.
- Khách tên `Trong` được tạo bằng BLL và đọc FormattedValue cột Name trên WinForms:
  **2/2 PASS**, không dịch nhầm tên thành nhãn trạng thái.

### WinForms, chọn ngôn ngữ và layout kỹ thuật

- Đã mở 17 target của shell và các tab có sẵn: **50 snapshot caption/header
  màn/tab**, 25 mỗi ngôn ngữ. Bao gồm Home, POS, hóa đơn, giao hàng, danh mục,
  menu/công thức, bàn, khách/điểm, nguyên liệu/kho, thu chi/nợ, nhân sự, tài sản,
  báo cáo, hệ thống. Đây là kiểm component có binding thật, chưa là toàn bộ tổ hợp
  loading/rỗng/lỗi/xác nhận/unknown và toàn bộ editor.
- Đọc 9 ReportKind ×2 ngôn ngữ qua BLL/SQL. Chạy clean Bootstrap load/validation ở
  VI và EN; đọc nội dung InvoicePrintDocument, mở preview/picker và kiểm enum Chi
  của catalog editor khi label đã dịch.
- ADMIN/CASH/STORE mở được mục ngôn ngữ: 6/6 lượt. Save + Để sau: 6/6 lượt xác
  minh file lưu ngôn ngữ mới nhưng phiên giữ ngôn ngữ cũ, modal có owner MainForm.
- Confirmation native của app có Owner/Modal/ShowInTaskbar đúng và trả No một lần:
  PASS bằng message loop/timer của component probe, không gọi là click native PASS.
- Các ca cuộn kỹ thuật tại 1280×720 và 1366×768, 125% PASS theo phạm vi đưa nút vào
  WorkingArea. Không nghiệm thu hình thức/spacing hoặc mọi bounds của dialog.

## Lỗi và phần còn thiếu

| Bug | Hiện tượng | Trạng thái |
|---|---|---|
| LOC-B01 | POS còn `SL` ở EN; cùng literal có trong chi tiết hóa đơn | FAIL, đã điều tra, chờ duyệt sửa |
| LOC-B02 | Preview EN còn nút `In…` | FAIL, đã điều tra, chờ duyệt sửa |
| LOC-B03 | Login EN→VI vẫn giữ thông báo cấu hình bằng EN | FAIL, đã tái hiện component, chờ duyệt sửa |

Chi tiết nguyên nhân, file/dòng, phạm vi sửa và regression: `bug-report.md`.

- **BLOCKED/NOT RUN:** thao tác click/Tab native đầy đủ; logout ngay → login lại
  qua native; busy/nháp/unknown và fault/timeout qua từng dialog; in thật/đa trang;
  toàn bộ ma trận 18 màn ×hai ngôn ngữ ×mọi trạng thái/editor. Không suy PASS từ
  các snapshot caption hoặc SQL E2E.
- **SKIPPED theo phân công:** nghiệm thu hình thức do người dùng tự kiểm, không
  chụp ảnh đối chiếu/hover/spacing. DPI 100%/150% **NOT RUN**.
- Source vẫn cố định, phần lựa chọn ngôn ngữ local đã được khôi phục, app test đã
  đóng. DB test được dọn sau xác minh hai marker; xem `cleanup.json`.

Toàn bộ đường dẫn evidence trong bảng tính từ `runs/acceptance-20261110/`.
Đợt test kết thúc với FAIL/BLOCKED công khai; bước kế tiếp là duyệt batch debug ba
bug, không cần duyệt lại requirements hoặc plan localization.
