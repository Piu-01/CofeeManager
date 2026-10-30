# T23 — Biên bản đợt test G05

27/10/2026. Đợt test đã được duyệt; source cố định304 file,0 thay đổi (source-freeze-verification.json). SQL Server thật localhost, schema001–021; runtime G05Runtime/CafeManagerApp, actor ADMIN/STORE/CASH. DB fixture CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo; dữ liệu ghi test chỉ trong DB riêng. Build Debug/Release sạch và22/22 xUnit PASS; không thay nghiệm thu native bằng build/unit.

Bằng chứng dưới nằm tại ../runs/acceptance-full-20261027_110730/ nếu không ghi đường dẫn task. Mỗi JSON có dữ liệu, expected/actual, mã lỗi, hash/transaction hoặc session/barrier; lệnh là pwsh -NoProfile -File harness tương ứng. Environment/schema lưu environment.json; trước/sau source hash lưu source-frozen.csv và source-freeze-verification.json. Các ca chỉ đạt nhánh SQL nhưng thiếu nhánh native được giữ BLOCKED/NOT RUN.

Hình thức SKIPPED theo phân công. DeviceDpi native chưa đo xác nhận nên không ghi PASS125%; DPI100/150% NOT RUN, không đổi Windows Scale. DB fixture/credential đã dọn sau lưu bằng chứng.

| ID | Expected gốc | Actual / dữ liệu / thao tác | Kết quả | Bằng chứng |
|---|---|---|---|---|
| T23-K01 | [I] NCC của nợ đúng NCC phiếu nhập và không đổi sau chốt; một PN một nợ. | Nợ đúng NCC qua PN, nguồn bất biến và unique một PN một nợ bị chặn | PASS | sql-supplement-v2-results.json, sql-extended-v2-results.json |
| T23-K02 | [I] Nợ vượt tổng nhập hoặc nợ 0 bị chặn; hạn NULL hợp lệ. | Nguồn độc lập không có nợ: INSERT0 bị CHECK chặn và vượt tổng bị trigger chặn; hạnNULL hợp lệ | PASS | sql-supplement-v4-results.json, sql-supplement-v2-results.json |
| T23-K03 | [I] Trả một phần/nhiều lần/cuối cùng: đã trả/còn nợ khớp, trạng thái đúng. | Trả một phần/nhiều lần/hết khớp số dư và trạng thái | PASS | sql-extended-v2-results.json |
| T23-K04 | [I] Trả 0/âm/vượt còn nợ/phương thức sai bị chặn và không phiếu chi rác. | 0/âm/vượt/precision/method sai và PT010 bị chặn; fault liên kết không để chi rác | PASS | sql-extended-v2-results.json |
| T23-K05 | [I] Ngày trả trước ngày nguồn nhập bị chặn; ngày hợp lệ lưu đúng. | Trước ngày nguồn bị chặn; ngày hợp lệ lưu đúng | PASS | sql-extended-v2-results.json |
| T23-K06 | [I] TC011 thiếu/sai loại phải lỗi rõ hoặc khởi tạo trước, không chọn nhầm loại Thu. | Tạm thiếu mapping SupplierDebt trong DB test: lỗi52602 rõ, rollback không đổi tiền; finally khôi phục đúng mapping fixture | PASS | sql-supplement-v2-results.json |
| T23-K07 | [I] Hai session trả lượng nợ cuối: không trả vượt; mã phiếu chi không trùng. | Hai session tranh nợ cuối qua hai thứ tự: một trả10000, một53003, một lịch sử và còn0 | PASS | concurrency-v2-results.json |
| T23-K08 | [I] UPDATE/DELETE nợ có trả/lịch sử/phiếu chi liên quan bị chặn; đổi nguồn không làm chuyển NCC. | Sửa/xóa lịch sử/chi/nợ/nguồn chốt bị chặn, không chuyển NCC | PASS | sql-extended-v2-results.json |
| T23-K09 | [I] View quá hạn theo thời điểm hiện tại; báo cáo ngày X bỏ lần trả sau X. | Hiện tại quá hạn đúng; as-of bỏ lần trả sau X, trước nguồn không có dòng và ngàymax không tràn | PASS | sql-extended-v2-results.json, sql-final-results.json |
| T23-K10 | [I] Phiếu trả ngay khi nhập không đồng thời tính là trả nợ gốc đã giảm. | Trả ngay giảm nợ gốc và không được tính thêm vào lịch sử trả nợ | PASS | sql-extended-v2-results.json |
| T23-K11 | [U/M] ADMIN trả được, STORE chỉ xem, CASH không; timeout đọc lại mã chi không retry mù. | SQL quyền ADMIN/STORE/CASH và timeout PASS; native mở Công nợ crash khi có nợ chưa trả, không tiếp tục luồng trả được | FAIL | gui-debt-stack.txt, ../t23-cong-no/bug-report.md |
| T23-K12 | [M] Reload xem được mọi lần trả, phương thức và link nguồn phiếu chi. | Reload không tải được toàn danh sách khi HistoryJson NULL; chưa thể xem mọi lịch sử/link nguồn trong UI | FAIL | gui-debt-stack.txt, ../t23-cong-no/bug-report.md |

Kết quả: 10 PASS / 2 FAIL / 0 BLOCKED / 0 NOT RUN. Đây là biên bản task trong báo cáo nhóm; không chốt hoàn tất riêng task.

## Regression đợt bug đã duyệt29/10/2026

B03 PASS: chưa trả/as-of lịch sử0, partial9000/1 lịch sử và full0/2 lịch sử, STORE/CASH trả bị51605; native lịch sử rỗng và link PN lồng mở/đóng đúng. B01 layout/pager component PASS. B05 footer còn chặn nhiều hành động; chưa nghiệm thu đầy đủ T23-K11/K12.

Môi trường: SQL Server thật localhost, DB riêng CoffeeManager_Test_G05_20261029_134725_96d98d09_demo, schema021, DeviceDpi120. Kết quả/lệnh/actual tại ../runs/debug-batch-20261029/: db-regression.json, component-results.json, hash-regression.json và các native-*.txt; từng harness giữ nguyên trong run. Build Debug/Release sạch;22/22 xUnit. Source cố định theo source-after-debug.csv/source-freeze-check.json. DBtest/GUI/credential đã dọn theo resource-audit.json. Đây là kết quả regression theo bug, giữ nguyên bảng55ID lịch sử27/10 phía trên. Hình thức SKIPPED;100/150% NOT RUN.


## Regression B05 bổ sung29/10/2026

Người dùng đã duyệt B05 kèm regression. Helper footer sửa theo opt-in,28/28 helper case và23/23 checkpoint trên9 dialog thật PASS, DeviceDpi120; Debug/Release0warning/0error,22unit và5SQL replay subcase PASS. Source cố định; chỉ DB test schema021. Native bỏ nháp nhập Có/Không đúng owner, kho/PN/nợ không đổi; ghi thu chi2345đ qua xác nhận và reload đúng một phiếu/receipt. Luồng preview→chốt nhập BLOCKED bởi B06 mới (grid ReadOnly toàn bảng), chưa sửa. T21 có cùng cấu hình qua source, chưa suy FAIL native. Không phát hành tổng55ID mới; giữ bảng lịch sử phía trên. Hình thức SKIPPED,100/150% NOT RUN. Bằng chứng đầy đủ và giới hạn harness: [biên bản B05](../runs/b05-20261029/test-results.md). Tài nguyên test đã dọn2/2DB,GUI/credential tạm đã xóa; development không bị thao tác.


## Bổ sung30/10/2026 — Native follow-up

STORE nợ chưa trả chỉ đọc PASS; ADMIN trả10000+11000, còn0, lịch sử hai dòng/link TC20250505–TC20250506 đúng nguồn PASS. Đăng xuất phát sinh B07 ObjectDisposedException CTS: FAIL mới; CASH native tiếp theo BLOCKED, chờ duyệt B07.

Bằng chứng và môi trường: [biên bản](../runs/native-followup-20261029/test-results.md). Source không đổi,1DB test đã dọn, không thay lịch sử ID/nghiệm thu toàn task; keyboard còn thiếu, hình thức SKIPPED, DPI100/150 NOT RUN.


## Regression B07 ngày30/10/2026 — Đã duyệt

Control danh sách thuộc task này đã sửa vòng đời CTS. Tải/ẩn/hiện/refresh, dispose lặp, dispose khi SQL read đang bị khóa và phiên revoke PASS. Tổng năm màn47/47 lifecycle,5/5 CASH-BLL,22/22unit; Debug/Release0warning/0error. Native chuyển màn và logout/relogin/đóng MainForm PASS theo phạm vi. Xem [biên bản B07](../runs/b07-20261030/test-results.md) để phân biệt component/native và giới hạn. DB test dọn1/1, không đổi dữ liệu kho/nguồn/receipt, source cố định. Không suy PASS toàn ID hoặc55ca; native keyboard còn BLOCKED công cụ, hình thức SKIPPED, DPI100/150 NOT RUN. B07 đã xử lý, G05 vẫn Đang làm.
