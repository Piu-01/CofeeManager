# Kết quả test T33 — Đơn "Chờ thanh toán"

Lượt test ngày 17/11/2026 (đợt test T33–T35 đã được duyệt). Không sửa source trong
lượt test; các chỉnh sửa chỉ nằm ở script test (ghi rõ bên dưới).

## Môi trường và bằng chứng

- SQL Server `localhost`, Windows `LAPTOP-79UNRIRC\Lenovo`, schema **034**
  (migrations 001–034), commit nền `8a38f86` + thay đổi chưa commit của T33–T35.
- DB test riêng do runner tạo/xóa: `CoffeeManager_Test_G04_20261117_003501_600081b1_demo`
  (và `_clean`), marker `G04TestMarker`.
- Manifest: `runs/acceptance_20261117_003501_600081b1/manifest.json`
  (`SourceUnchanged=true`).
- SQL subset: `database/tests/g04/acceptance-pending-payment.sql` → **PASS**.
- Unit: `dotnet test --filter FullyQualifiedName~CafeManager.Tests.Sales` →
  **23/23 PASS** (`runs/acceptance_20261117_003501_600081b1/unit/g04-unit-manual.trx`;
  runner dừng trước bước unit do typed smoke lỗi T34-01 nên chạy tay cùng tham số).
- **Sau sửa T34-01 (migration 035, schema 035):** regression
  `runs/acceptance_20261117_020633_de5483fe` — `acceptance-pending-payment.sql`
  **PASS**, typed smoke `Success=true`, unit 23/23 chạy trong runner,
  `SourceUnchanged=true`.
- **UI 18/11/2026:** app WinForms thật trên DB clone
  `CoffeeManager_Test_G04_20261118_174233_3319aa73_ui` (schema 35, DPI 120 = 125%),
  bằng chứng `runs/ui_20261118_174233_3319aa73/`; DB dev không bị ghi.

## Kết quả theo mã

| Mã | Kết quả | Ghi chú/bằng chứng |
|---|---|---|
| PP01 | PASS | Đơn tại bàn ≥1 dòng → `Cho thanh toan`, receipt `SalesRequestPay`, revision tăng, chưa ghi `NgayThanhToan`. |
| PP02 | PASS | Đơn 0 dòng bị 52108, trạng thái/revision giữ nguyên, không receipt, transaction sạch. |
| PP03 | PASS | Thêm/sửa/xóa dòng và sửa header sau `Cho` bị 52102; không ghi một phần. |
| PP04 | PASS | Preview/checkout từ `Cho` thành công; tiền/kho/điểm/snapshot giống từ `Chua`. |
| PP05 | PASS | Hủy từ `Cho` thành công trước khi thu; `TrangThai=Huy`. |
| PP06 | PASS | ADMIN mở lại đúng receipt `SalesReopen`; CASH 51605; revision cũ 52103; đơn không Chờ 52109; sửa lại được sau mở. |
| PP07 | PASS | View `Cho thanh toan`; tạo đơn thứ hai cùng bàn 52104; không nhân dòng. |
| PP08 | PASS | `sp_SalesOpenOrders`/`sp_InvoiceList` lọc `Cho` đúng; `sp_SalesReadCore`/`sp_InvoiceDetail` chạy được ở mức SQL. |
| PP09 | PASS (giới hạn) | `sp_ReportDashboard.OpenOrders` gồm `Chua`+`Cho`, doanh thu chỉ tăng theo đơn đã thu. Nguồn `sp_DashboardAnalyticsSources`: **NOT RUN** — proc từ chối khi `@@TRANCOUNT<>0`, `INSERT…EXEC` mở transaction ngầm (xem bug-report T34-01 mục ghi chú phụ). |
| PP10 | PASS | Replay cùng RequestId không đổi trạng thái lần hai; receipt đúng actor. |
| PP11 | PASS | Tạo đơn thường/nháp, checkout từ `Chua`, hủy, danh sách/lọc hóa đơn, giao hàng đều PASS; chi tiết/in lại hóa đơn hết lỗi sau migration 035 (typed smoke `Success=true`). |
| PP12 | PASS (UI, 125%) | Chạy app thật trên DB clone `CoffeeManager_Test_G04_20261118_174233_3319aa73_ui` (DPI 120), bằng chứng `runs/ui_20261118_174233_3319aa73/`. Mở đơn `HD26093444` (B0007) → "Yêu cầu thanh toán" + xác nhận: DB `Cho thanh toan`, receipt `SalesRequestPay`, revision tăng, chưa có thanh toán; 18 nút "Thêm …" bị khóa, dòng trạng thái "đã khóa sửa món". F8 từ `Cho` chốt 30.000: `Da thanh toan`, `ThanhTien=30000`, kho NVL21 95→94 (đúng 1.0000 theo `TIEUHAO_HOADON`), receipt `SalesCheckout`. Hình thức **SKIPPED** theo phân công; DPI 100%/150% **NOT RUN**. |

## Chỉnh sửa script test trong lượt (không phải source sản phẩm)

- `acceptance-pending-payment.sql` PP09: bỏ `INSERT @sources EXEC sp_DashboardAnalyticsSources`
  (không thể chạy do guard `@@TRANCOUNT`), thay bằng kiểm `sp_ReportDashboard` và ghi
  giới hạn; baseline doanh thu đổi sang cửa sổ hôm nay đúng định nghĩa dashboard.

Kết luận: T33 **đủ điều kiện nghiệm thu** — PP12 đã PASS trên UI thật (125%) ngày
18/11/2026; PP09 PASS kèm giới hạn nguồn tổng quan do guard `@@TRANCOUNT` (ghi rõ,
không phải lỗi T33). PP11 PASS sau sửa T34-01. Hình thức và DPI 100%/150% do người
dùng tự kiểm theo phân công.
