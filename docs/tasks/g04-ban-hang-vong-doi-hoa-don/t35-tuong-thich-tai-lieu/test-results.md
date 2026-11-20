# Kết quả test T35 — Tương thích, tài liệu và regression

Lượt regression ngày 17/11/2026 trong đợt test T33–T35. Không sửa source.

## Môi trường và bằng chứng

- Chạy chung acceptance batch: `runs/acceptance_20261117_003501_600081b1/manifest.json`.
- SQL subset PASS: `acceptance-orders.sql`, `acceptance-checkout.sql`,
  `acceptance-delivery.sql`, `acceptance-pending-payment.sql`,
  `acceptance-table-sessions.sql`.
- Unit Sales: 23/23 PASS (`unit/g04-unit-manual.trx`).
- **Sau sửa T34-01 (migration 035):** regression `runs/acceptance_20261117_020633_de5483fe`
  — 5/5 SQL subset PASS trên schema 035, typed smoke `Success=true`, unit 23/23,
  `SourceUnchanged=true`; technical `runs/technical_20261117_020349_e6dedcdf`
  native smoke `Success=true`.
- UI 18/11/2026: `runs/ui_20261118_174233_3319aa73/` trên DB clone
  `CoffeeManager_Test_G04_20261118_174233_3319aa73_ui` (schema 35, DPI 120 = 125%).

## Kết quả theo mã

| Mã | Kết quả | Ghi chú |
|---|---|---|
| CP01 | PASS | Rà soát mã + grep: tách `CanEdit`/`IsPayable`; UI không còn hiểu `Cho` là sửa được. |
| CP02 | PASS (mã) / SKIPPED (hình thức) | Nhãn/màu có đủ 4 trạng thái trong mã (POS, panel, lịch sử, card bàn, badge grid); hình thức do người dùng kiểm. |
| CP03 | PASS (mã) | Lọc hóa đơn 4 trạng thái; `Quay lại POS` chỉ cho `Chua`/`Cho`. |
| CP04 | PASS | Dashboard/báo cáo đếm `Chua`+`Cho`; doanh thu theo ngày thanh toán (SQL subset). Nguồn `orders` **NOT RUN** do guard `@@TRANCOUNT` (ghi ở T33). |
| CP05 | PASS | Tạo đơn thường/nháp, checkout từ `Chua`/`Cho`, hủy, danh sách/tra cứu, giao hàng, chi tiết hóa đơn đều PASS; lỗi T34-01 đã sửa bằng migration 035 và regression xác nhận. |
| CP06 | PASS (SQL + UI) | Giao hàng T18 subset PASS. UI 18/11/2026: ADMIN mở đơn `Cho thanh toan` `HD26093335` thấy "Mở lại đơn" **enabled**; đăng xuất, đăng nhập `thungan` (nhãn `Pham Thi Dung · CASH`), mở cùng đơn thấy "Mở lại đơn" **disabled**, F8 vẫn enabled. BLL vẫn là nơi chặn quyền (UI chỉ ẩn/khóa). STORE chưa đăng nhập thử trong lượt này — **NOT RUN**. |
| CP07 | PASS | UX/contract/write-ownership/development-guide cập nhật; 56 ID T14–T18 và 325 ID giữ nguyên. |
| CP08 | PASS (chức năng, 125%) | UI 18/11/2026: cửa sổ thật co về **1280×720** rồi **1366×768** ở DPI 120. Các nút/ô chính (Chọn bàn, Tìm món, Yêu cầu thanh toán, F8, Hủy đơn mở, Trả bàn) nằm trong biên cửa sổ, không chồng lấn; danh sách món cuộn được (mục ngoài khung là nội dung cuộn, không phải tràn bố cục). Hình thức **SKIPPED** theo phân công; DPI 100/150 **NOT RUN**. |

## Phát hiện cần đính chính hồ sơ

- `t35-tuong-thich-tai-lieu/audit.md` kết luận "không cần migration" là **thiếu**
  trường hợp `sp_InvoiceDetail` vẫn trả 25 cột header — đã đính chính và bổ sung
  migration 035 trong báo cáo T34-01.

Kết luận: T35 **đủ điều kiện nghiệm thu** — CP05 PASS sau sửa T34-01; CP06 PASS thêm
phần UI ADMIN/CASH (STORE chưa chạy thử, ghi NOT RUN); CP08 PASS chức năng tại
1280×720/1366×768 ở 125%. Hình thức và DPI 100%/150% do người dùng tự kiểm.
