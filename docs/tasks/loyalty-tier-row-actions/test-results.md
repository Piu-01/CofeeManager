# Kết quả kiểm thử — điều chỉnh màn Hạng thành viên

- Ngày chạy: 21/11/2026, DPI 125% (agent chỉ kiểm chức năng; AR-12 hình thức do người dùng tự kiểm).
- Build: `Debug/net10.0-windows` xuất tạm tại `C:\Users\Lenovo\.qoder\tmp\cm-build-tier-row\Debug\net10.0-windows` từ working tree ngày 21/11/2026; source giữ nguyên trong suốt lượt test (không sửa file nào).
- DB: SQL Server thật `localhost`, database test riêng `CoffeeManager_Test_RG_20261121_b_ui` (schema 37), không dùng DB sử dụng thật.
- Tài khoản: `admin/Admin@123` (ADMIN), `thungan/Cash@123` (CASH).
- Thao tác UI qua UIA/Win32 (script tạm trong `C:\Users\Lenovo\.qoder\tmp\cm-rg-tests`), không dùng công cụ chỉnh sửa source.
- Bằng chứng: `runs/20261121-ui/`.

## Tổng hợp

| Mã | Kết quả | Ghi chú |
|---|---|---|
| AR-01 | PASS | Không còn Tải lại/Thay chính sách; có Thêm; 4 hạng tăng dần |
| AR-02 | PASS (có bất thường BR-01 ở lần thử 1) | Lần 2 lưu SILV thành công; xem bug-report.md |
| AR-03 | PASS | Trùng mã bị chặn ở preview; Lưu khi chưa preview hợp lệ bị chặn |
| AR-04 | PASS | Sửa DONG bỏ ngưỡng 0 bị chặn |
| AR-05 | PASS | Sửa SILV 5000/4% → 6000/6%; khách xếp lại hạng; danh sách sắp lại |
| AR-06 | PASS | Đổi mã SILV → PLAT trong cùng lần sửa; không còn SILV, không mồ côi FK |
| AR-07 | PASS | Xóa VANG (7 khách) → khách chuyển PLAT; danh sách còn 4 hạng |
| AR-08 | PASS | Xóa hạng ngưỡng 0 bị chặn ở UI, DB giữ nguyên |
| AR-09 | PASS (khác câu chữ thông báo) | Xung đột revision bị chặn, không ghi đè, không crash |
| AR-10 | PASS | CASH không thấy mục Ưu đãi / Điểm; ADMIN thao tác bình thường |
| AR-11 | PASS (phần Tải lại); phần ưu đãi hóa đơn không kiểm dương tính | Xem ghi chú |
| AR-12 | SKIPPED | Hình thức do người dùng tự kiểm theo phân công |

## Chi tiết từng ca

### AR-01 — Toolbar màn Hạng thành viên

- Thao tác: đăng nhập ADMIN → Ưu đãi / Điểm → tab Hạng thành viên.
- Expected: không có “Tải lại”, không có “Thay chính sách”; có “Thêm”; danh sách tăng dần theo ngưỡng.
- Actual: đúng; danh sách DONG(0) / BAC(2000) / VANG(10000) / KIM(30000); không có nút tải lại trong vùng lọc/dòng.
- Evidence: `ar01-ranks.txt`.

### AR-02 — Thêm hạng mới

- Thao tác: Thêm → SILV / 5000 / 4 → “Xem lại từ SQL” → Lưu → Đồng ý.
- Expected: lưu thành công, hạng mới nằm giữa BAC và VANG, DB có dòng mới, khách được xếp lại hạng.
- Actual (lần 2): preview “14 khách sẽ đổi hạng… SILV: 5000/4%”; lưu thành công; danh sách 5 hạng DONG/BAC/SILV/VANG/KIM; DB có `SILV 5000 4`; 14 khách chuyển BAC → SILV; `G03OperationReceipt` có `PolicyReplace` ResultCount 14; revision chính sách tăng.
- Lần thử 1: hộp “Unhandled exception” với `NullReferenceException` tại `OperationEditorDialog.WriteAsync` dòng 63; DB không đổi; xem BR-01 và `ar02-first-attempt-exception.txt`.
- Evidence: `ar02-list-after-add.txt`, `db-final-state.txt`.

### AR-03 — Trùng mã / trùng ngưỡng

- Thao tác: Thêm với mã BAC / 6000 / 5% → preview; sau đó bấm Lưu khi chưa có preview hợp lệ.
- Expected: bị chặn, thông báo rõ, DB không đổi.
- Actual: preview hiện “Hạng cần mã/ngưỡng duy nhất, có ngưỡng 0 và tỷ lệ 0..100.”; Lưu hiện “Cần xem lại SQL cho nội dung hiện tại.”; không mở xác nhận, không ghi DB, không có exception.
- Evidence: `ar03-dup-preview.txt`, `ar03-save-blocked.txt`.

### AR-04 — Bắt buộc còn hạng ngưỡng 0

- Thao tác: double-click DONG → đổi ngưỡng 0 → 500 → preview.
- Expected: bị chặn đúng quy tắc.
- Actual: preview hiện cùng thông báo validate (cần ngưỡng 0); DB giữ DONG 0/0%.
- Evidence: `ar04-threshold0.txt`.

### AR-05 — Sửa ngưỡng/giảm % bằng double-click

- Thao tác: double-click SILV → 6000 / 6% → preview → Lưu → Đồng ý.
- Expected: lưu thành công, khách xếp lại hạng, danh sách sắp xếp lại.
- Actual: preview “14 khách sẽ đổi hạng… PLAT: 6000/6%”; lưu thành công; DB `PLAT 6000 6`; phân bố khách 9 PLAT / 5 BAC / 7 VANG / 1 KIM; receipt `PolicyReplace` ResultCount 14; dialog điền sẵn mã/ngưỡng/% đúng hạng được double-click.
- Evidence: `ar05-edit-preview.txt`, `ar05-after-save.txt`, `db-final-state.txt`.

### AR-06 — Đổi mã hạng

- Thao tác: trong dialog sửa SILV, đổi mã SILV → PLAT (ngưỡng/% đổi cùng lúc).
- Expected: khách hạng cũ chuyển mã mới; hạng cũ không còn; không mồ côi FK.
- Actual: DB chỉ còn DONG/BAC/PLAT/KIM; mọi khách thuộc BAC/KIM/PLAT, không còn tham chiếu SILV; receipt ghi nhận trong cùng transaction.
- Evidence: `ar05-after-save.txt`, `db-final-state.txt`.

### AR-07 — Xóa hạng có khách bằng icon thùng rác

- Thao tác: bấm icon thùng rác dòng VANG → hộp xóa hiện “VANG · ngưỡng 10000 · giảm 5%. 7 khách sẽ được xếp lại hạng theo ngưỡng còn lại.” → Lưu → Đồng ý.
- Expected: xác nhận thì ghi một transaction và reload; khách xếp lại hạng còn lại.
- Actual: xóa thành công; DB còn 4 hạng; 16 khách PLAT (9+7), 5 BAC, 1 KIM; receipt `PolicyReplace` ResultCount 7; danh sách 4 dòng.
- Evidence: `ar07-delete-dialog.txt`, `ar07-list-after-delete.txt`, `db-final-state.txt`.

### AR-08 — Xóa hạng ngưỡng 0 / hạng cuối

- Thao tác: bấm icon thùng rác dòng DONG.
- Expected: chặn ở UI, DB giữ nguyên.
- Actual: hiện “Phải giữ một hạng có ngưỡng 0 điểm.” ở thanh trạng thái danh sách; không mở hộp xác nhận; DB giữ DONG.
- Ghi chú: nhánh “hạng cuối cùng” (`current.Count<=1`) không tách riêng được qua UI vì DB hợp lệ luôn phải có hạng ngưỡng 0; nhánh ngưỡng 0 đã chặn trước. Không suy diễn PASS riêng cho nhánh đếm.
- Evidence: `ar08-blocked.txt`.

### AR-09 — Xung đột revision

- Thao tác: mở sửa BAC → đổi giảm 3% → 4% → preview → ngoài app bump revision chính sách (`UPDATE LoyaltyPolicyState SET Id=1 WHERE Id=1` trên DB test) → Lưu → Đồng ý.
- Expected: báo chính sách đã thay đổi, không ghi đè.
- Actual: dialog giữ nguyên input, status hiện “Dữ liệu đã thay đổi hoặc bị xóa; tải lại trước khi lưu.”; DB giữ BAC 3%; không có exception, không đóng dialog.
- Khác biệt: câu chữ thực tế là thông báo chung của catalog lỗi thay vì nguyên văn “Chính sách đã thay đổi.” nêu ở requirements mục 4.7. Hành vi bảo vệ đúng; câu chữ do `UserOperationException` của contract hiện có.
- Evidence: `ar09-revision-conflict.txt`. Lưu ý: revision DB test đã bị bump trực tiếp cho ca này nên không khớp số receipt.
- Dữ liệu: BAC 3% sau ca.

### AR-10 — Phân quyền CASH

- Thao tác: đăng xuất ADMIN → đăng nhập thungan/Cash@123 → kiểm menu.
- Expected: CASH không thấy màn Hạng thành viên.
- Actual: menu CASH chỉ có Bán hàng/Lịch sử hóa đơn/Giao hàng/Khách hàng/Tồn phục vụ bán/Thông tin hệ thống; không có mục Ưu đãi / Điểm để vào tab Hạng thành viên. ADMIN vẫn thao tác bình thường ở các ca trên.
- Evidence: `ar10-cash-nav.txt`.

### AR-11 — Regression danh sách khác và ưu đãi hóa đơn

- Thao tác: ADMIN → tab Khuyến mãi, tab Điểm khách hàng; mở “Preview ưu đãi” của chương trình đang chọn.
- Expected: các màn khác vẫn có “Tải lại”; ưu đãi hóa đơn không đổi.
- Actual: Khuyến mãi và Điểm khách hàng đều còn nút “Tải lại danh sách” và hoạt động; màn Hạng thành viên không có. Hộp Preview ưu đãi mở được và gọi SQL, nhưng với chương trình KM24BF đang chọn trả “Chưa có preview hợp lệ · Thông tin ưu đãi/chính sách/điểm không hợp lệ hoặc chương trình không hiệu lực.” — không xác nhận được số giảm dương tính trong lượt này.
- Đánh giá: phần regression danh sách PASS; phần “ưu đãi hóa đơn không đổi” chỉ dựa trên phạm vi thay đổi (chỉ UI list/toolbar, không sửa fn/sp ưu đãi) — không ghi PASS hành vi ưu đãi.
- Evidence: `ar11-promo-refresh.txt`, `ar11-points-refresh.txt`.

### AR-12 — Hình thức

- SKIPPED theo phân công: người dùng tự kiểm màu icon/hover/spacing. Không ghi PASS hình thức.

## Ghi chú dữ liệu test

- DB test kết thúc lượt: hạng `DONG 0/0% | BAC 2000/3% | PLAT 6000/6% | KIM 30000/8%`; khách BAC 5 / PLAT 16 / KIM 1; 3 receipt `PolicyReplace`.
- Dữ liệu này là kết quả các ca AR-02, AR-05/06, AR-07; không phải trạng thái seed ban đầu.

## Bug ghi nhận trong lượt

- **BR-01** — `NullReferenceException` không bắt được tại `OperationEditorDialog.WriteAsync` dòng 63 ở lần thử AR-02 đầu tiên; không tái hiện lần 2, DB không đổi ở lần lỗi. Chi tiết, bằng chứng và đề xuất sửa: `bug-report.md` (chờ duyệt đợt bug).

## Đợt regression BR-01 (21/11/2026, gộp với đợt CP)

- Build: `C:\Users\Lenovo\.qoder\tmp\cm-build-cph` (sau fix BR-01), DB test `CoffeeManager_Test_RG_20261121_b_ui`,
  DPI 125%, source giữ nguyên trong suốt lượt.
- Bằng chứng: `runs/20261121-ui/cph-br01-*.txt`, `cph-r7-*.txt`.

| Mã | Kết quả | Ghi chú |
|---|---|---|
| BR01-R1 | PASS | Thêm `TST 50000/1%` → preview “0 khách sẽ đổi hạng” → Lưu → Đồng ý → lưu thành công, danh sách 5 hạng, receipt `PolicyReplace` |
| BR01-R2 | PASS | Bấm Lưu 2 lần liền nhanh: chỉ một hộp xác nhận, một lượt ghi; sau đó sửa TST 2% thành công, một receipt |
| BR01-R3 | PASS | Hủy hộp xác nhận giữ nguyên dialog và input; bấm Lưu lại lưu được `TST 3%`, không NRE, không báo “Chưa cấu hình thao tác.” |
| BR01-R4 | PASS | Xóa TST: note “0 khách”, xác nhận “Xóa hạng TST?” → xóa, còn 4 hạng, một receipt |
| BR01-R5 | PASS | Thêm mã trùng `KIM 40000/1%` → preview bị chặn với “Hạng cần mã/ngưỡng duy nhất, có ngưỡng0 và tỷ lệ0..100.”, giữ input; DB không đổi (4 hạng, không receipt mới) |
| BR01-R6 | N/A | Dialog “Điều chỉnh điểm khách” đã bị gỡ theo task `customer-point-history` (CP đã duyệt); không còn dialog để chạy |
| BR01-R7 | PASS | Thêm khuyến mãi `KM20261121` (5%) lưu một lần, receipt `PromotionCreate`, đúng 1 dòng DB; thêm nguyên liệu `NVL99` lưu bình thường, receipt `MaterialCreate` |
| BR01-R8 | PASS | Lưu khi chưa có preview hợp lệ hiện `status` “Cần xem lại SQL cho nội dung hiện tại.”, không mở xác nhận, không ghi; Hủy khi có nháp hỏi “Bỏ nháp và đóng?”, đồng ý thì đóng cả editor lẫn hộp xác nhận, DB giữ nguyên |

- Hạn chế: BR01-R2 dùng 2 lần Invoke cách 100ms; lượt thứ hai rơi sau khi dialog đã đóng
  (`ElementNotAvailableException` trong script) nên chỉ chứng minh một lượt ghi, không tái hiện
  đúng kịch bản “double-click khi hộp xác nhận đang mở” như AR-02. Kịch bản đó đã được kiểm ở
  BR01-R5 (Lưu lặp trong khi preview lỗi, không ghi).
- Dữ liệu giữ lại trong DB test: hạng về `DONG 0/0% | BAC 2000/3% | PLAT 6000/6% | KIM 30000/8%`;
  có thêm `KM20261121` và `NVL99` do BR01-R7 tạo.
