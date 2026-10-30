# T19 — Biên bản đợt test G05

27/10/2026. Đợt test đã được duyệt; source cố định304 file,0 thay đổi (source-freeze-verification.json). SQL Server thật localhost, schema001–021; runtime G05Runtime/CafeManagerApp, actor ADMIN/STORE/CASH. DB fixture CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo; dữ liệu ghi test chỉ trong DB riêng. Build Debug/Release sạch và22/22 xUnit PASS; không thay nghiệm thu native bằng build/unit.

Bằng chứng dưới nằm tại ../runs/acceptance-full-20261027_110730/ nếu không ghi đường dẫn task. Mỗi JSON có dữ liệu, expected/actual, mã lỗi, hash/transaction hoặc session/barrier; lệnh là pwsh -NoProfile -File harness tương ứng. Environment/schema lưu environment.json; trước/sau source hash lưu source-frozen.csv và source-freeze-verification.json. Các ca chỉ đạt nhánh SQL nhưng thiếu nhánh native được giữ BLOCKED/NOT RUN.

Hình thức SKIPPED theo phân công. DeviceDpi native chưa đo xác nhận nên không ghi PASS125%; DPI100/150% NOT RUN, không đổi Windows Scale. DB fixture/credential đã dọn sau lưu bằng chứng.

| ID | Expected gốc | Actual / dữ liệu / thao tác | Kết quả | Bằng chứng |
|---|---|---|---|---|
| T19-K01 | [I/M] Phiếu Thu/Chi thông thường số tiền dương, ngày/nhân viên/danh mục hợp lệ lưu đúng. | SQL Thu3456/Chi1234 đúng; native ADMIN ghi TC20250521 NV991 số1234 và reload | PASS | sql-supplement-v2-results.json, gui-finance-after.txt |
| T19-K02 | [I] Tiền 0/âm/quá precision, tên/ghi chú quá dài và FK nguồn sai bị chặn. | 7 subcase tiền/ghi chú201/FK PASS; nhánh tên quá dài chưa kiểm đủ trong lượt này | NOT RUN | sql-extended-v2-results.json |
| T19-K03 | [I] MaPN/MaBL FK hợp lệ; hai phiếu chi cho cùng nguồn trả ngay/lương bị unique chặn. | PN/BL FK và unique bị chặn, nguồn hợp lệ lưu được; MaBL là fixture chưa thay tích hợp T26 | PASS | sql-extended-v2-results.json |
| T19-K04 | [I] Phiếu nguồn hệ thống phải loại Chi đúng, không thể đổi thành Thu. | Nguồn hệ thống sai loại bị chặn, tiền/nguồn không đổi sau lỗi | PASS | sql-extended-v2-results.json |
| T19-K05 | [U/M] UI thủ công không chọn danh mục trả nợ/trả ngay/lương hệ thống; chỉ ADMIN ghi chi. | SQL ADMIN/CASH/STORE và cấm helper/rawDML PASS; chưa hoàn tất dropdown/role native do hạn chế thao tác hộp con | BLOCKED | sql-extended-v2-results.json |
| T19-K06 | [I] Nguồn trùng và lỗi giữa lưu phiếu/liên kết rollback, không phiếu rác. | Nguồn trùng và fault sau ghi chi rollback, snapshot không đổi và transaction0:0 | PASS | sql-extended-v2-results.json |
| T19-K07 | [I/M] Chính sách sửa chi thường rõ: bản đã xác nhận không xóa/sửa tiền âm để đảo; lịch sử chi nguồn bất biến. | SQL sửa/xóa phiếu và đổi loại danh mục bị chặn; nhánh detail chỉ đọc native chưa hoàn tất | BLOCKED | sql-extended-v2-results.json |
| T19-K08 | [I] Tổng Thu/Chi theo ngày không cộng các dòng lịch sử trả nợ thêm lần nữa. | Tổng Thu3456/Chi47702 tại thời điểm kiểm khớp aggregate trực tiếp PHIEUTHUCHI, không cộng lịch sử trả nợ lần hai | PASS | sql-supplement-v2-results.json |
| T19-K09 | [I/M] Bán hàng không đồng thời tạo phiếu thu rồi nhân đôi nguồn doanh thu. | Checkout fixture SP991 trả1000; số phiếu PHIEUTHUCHI không tăng, thanh toán đúng1000 | PASS | sql-supplement-v4-results.json |
| T19-K10 | [M] Tra cứu/source link và reload hiển thị đủ loại tiền/ghi chú/mã nguồn. | Ghi thường/reload được; bộ lọc và nhóm thao tác thiếu, source links chưa kiểm đủ native | BLOCKED | gui-finance-after.txt, ../t20-nhap-kho/bug-report.md |

Kết quả: 6 PASS / 0 FAIL / 3 BLOCKED / 1 NOT RUN. Đây là biên bản task trong báo cáo nhóm; không chốt hoàn tất riêng task.

## Regression đợt bug đã duyệt29/10/2026

B04 PASS: JSON25068 ký tự, hash ổn định khi dữ liệu giữ nguyên và đổi khi thêm dòng cuối; chunk cũ2033 ký tự vẫn không đổi. B01 Finance component layout/pager/filter tập rỗng PASS. Chưa chạy đủ T19-K02/K05/K07/K10.

Môi trường: SQL Server thật localhost, DB riêng CoffeeManager_Test_G05_20261029_134725_96d98d09_demo, schema021, DeviceDpi120. Kết quả/lệnh/actual tại ../runs/debug-batch-20261029/: db-regression.json, component-results.json, hash-regression.json và các native-*.txt; từng harness giữ nguyên trong run. Build Debug/Release sạch;22/22 xUnit. Source cố định theo source-after-debug.csv/source-freeze-check.json. DBtest/GUI/credential đã dọn theo resource-audit.json. Đây là kết quả regression theo bug, giữ nguyên bảng55ID lịch sử27/10 phía trên. Hình thức SKIPPED;100/150% NOT RUN.


## Regression B05 bổ sung29/10/2026

Người dùng đã duyệt B05 kèm regression. Helper footer sửa theo opt-in,28/28 helper case và23/23 checkpoint trên9 dialog thật PASS, DeviceDpi120; Debug/Release0warning/0error,22unit và5SQL replay subcase PASS. Source cố định; chỉ DB test schema021. Native bỏ nháp nhập Có/Không đúng owner, kho/PN/nợ không đổi; ghi thu chi2345đ qua xác nhận và reload đúng một phiếu/receipt. Luồng preview→chốt nhập BLOCKED bởi B06 mới (grid ReadOnly toàn bảng), chưa sửa. T21 có cùng cấu hình qua source, chưa suy FAIL native. Không phát hành tổng55ID mới; giữ bảng lịch sử phía trên. Hình thức SKIPPED,100/150% NOT RUN. Bằng chứng đầy đủ và giới hạn harness: [biên bản B05](../runs/b05-20261029/test-results.md). Tài nguyên test đã dọn2/2DB,GUI/credential tạm đã xóa; development không bị thao tác.


## Bổ sung30/10/2026 — Native follow-up

Bổ sung T19-K02.Name51: runtime public procedure từ chối tên51 ký tự với51501, không đổi dữ liệu, transaction0:0. PASS subcase, không suy cảID.

Bằng chứng và môi trường: [biên bản](../runs/native-followup-20261029/test-results.md). Source không đổi,1DB test đã dọn, không thay lịch sử ID/nghiệm thu toàn task; keyboard còn thiếu, hình thức SKIPPED, DPI100/150 NOT RUN.


## Regression B07 ngày30/10/2026 — Đã duyệt

Control danh sách thuộc task này đã sửa vòng đời CTS. Tải/ẩn/hiện/refresh, dispose lặp, dispose khi SQL read đang bị khóa và phiên revoke PASS. Tổng năm màn47/47 lifecycle,5/5 CASH-BLL,22/22unit; Debug/Release0warning/0error. Native chuyển màn và logout/relogin/đóng MainForm PASS theo phạm vi. Xem [biên bản B07](../runs/b07-20261030/test-results.md) để phân biệt component/native và giới hạn. DB test dọn1/1, không đổi dữ liệu kho/nguồn/receipt, source cố định. Không suy PASS toàn ID hoặc55ca; native keyboard còn BLOCKED công cụ, hình thức SKIPPED, DPI100/150 NOT RUN. B07 đã xử lý, G05 vẫn Đang làm.
