# T22 — Biên bản đợt test G05

27/10/2026. Đợt test đã được duyệt; source cố định304 file,0 thay đổi (source-freeze-verification.json). SQL Server thật localhost, schema001–021; runtime G05Runtime/CafeManagerApp, actor ADMIN/STORE/CASH. DB fixture CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo; dữ liệu ghi test chỉ trong DB riêng. Build Debug/Release sạch và22/22 xUnit PASS; không thay nghiệm thu native bằng build/unit.

Bằng chứng dưới nằm tại ../runs/acceptance-full-20261027_110730/ nếu không ghi đường dẫn task. Mỗi JSON có dữ liệu, expected/actual, mã lỗi, hash/transaction hoặc session/barrier; lệnh là pwsh -NoProfile -File harness tương ứng. Environment/schema lưu environment.json; trước/sau source hash lưu source-frozen.csv và source-freeze-verification.json. Các ca chỉ đạt nhánh SQL nhưng thiếu nhánh native được giữ BLOCKED/NOT RUN.

Hình thức SKIPPED theo phân công. DeviceDpi native chưa đo xác nhận nên không ghi PASS125%; DPI100/150% NOT RUN, không đổi Windows Scale. DB fixture/credential đã dọn sau lưu bằng chứng.

| ID | Expected gốc | Actual / dữ liệu / thao tác | Kết quả | Bằng chứng |
|---|---|---|---|---|
| T22-K01 | [I/M] Tạo snapshot đúng tồn tại thời điểm lấy, chưa chốt chưa đổi tồn. | SQL snapshot đúng tồn, countNULL, chưa đổi kho PASS; native lấy snapshot bị chặn bởi UI/exception | BLOCKED | sql-supplement-v2-results.json, ../t22-kiem-ke/bug-report.md |
| T22-K02 | [I] Số đếm 0/bằng tồn/khác tồn hợp lệ; âm/quá precision/NVL trùng bị chặn. | NULL phân biệt0, đếm0 và bằng tồn chốt đúng; âm/precision/trùng bị chặn | PASS | sql-extended-v2-results.json, sql-supplement-v2-results.json |
| T22-K03 | [I/M] Khác tồn yêu cầu lý do, thiếu lý do bị từ chối. | SQL thiếu lý do khi lệch bị chặn; nhánh validation native chưa chạy do lỗi danh sách | BLOCKED | sql-extended-v2-results.json, ../t22-kiem-ke/bug-report.md |
| T22-K04 | [I] Hôm nay/chưa chốt/snapshot khớp: điều chỉnh đúng một lần. | Phiếu hôm nay khớp snapshot chốt đúng một lần, replay giữ dữ liệu | PASS | sql-extended-v2-results.json, sql-supplement-v2-results.json |
| T22-K05 | [I] Chốt lại, reset DaDieuChinh, đổi ngày/nhân viên, sửa/xóa chi tiết sau chốt bị chặn. | Chốt lại/reset/header/ngày/nhân viên/CT sau chốt bị chặn, transaction0:0 | PASS | sql-extended-v2-results.json |
| T22-K06 | [I] Phiếu ngày cũ/ngày tương lai/rỗng không chốt; không tự sửa ngày để né điều kiện. | Header ngày cũ/tương lai/rỗng do owner chuẩn bị cho ca âm đều bị từ chối chốt; không sửa ngày/expected | PASS | sql-extended-v2-results.json |
| T22-K07 | [I] Phát sinh kho sau snapshot: từ chối, không ghi đè tồn mới bằng số cũ. | Mua rồi xuất về cùng lượng nhưng Revision khác: ABA vẫn bị từ chối, không đè tồn | PASS | sql-extended-v2-results.json |
| T22-K08 | [I] Nhiều NVL một dòng snapshot lệch: toàn bộ rollback. | Một trong nhiều NVL stale khiến cả phiếu rollback | PASS | sql-extended-v2-results.json |
| T22-K09 | [I] Hai session chốt cùng phiếu/checkout song song được tuần tự hóa hoặc từ chối hợp lệ; không mất biến động kho. | Hai session cùng KK có một commit/một52902; KK và checkout hai thứ tự giữ đúng biến động, không lost update | PASS | concurrency-v2-results.json, concurrency-kk-results.json |
| T22-K10 | [M] Không giữ khóa DB trong thời gian người dùng đếm; UI nêu cần tạm dừng phát sinh để chốt. | Chưa kiểm đủ native thời gian đếm và lời nhắc tạm dừng vì không mở được luồng; không suy PASS từ source | BLOCKED | ../t20-nhap-kho/bug-report.md, ../t22-kiem-ke/bug-report.md |
| T22-K11 | [U/M] STORE/ADMIN được; CASH không; reload trạng thái và chênh lệch đúng. | SQL STORE/ADMIN được và CASH bị chặn; native danh sách gặp LinesJson NULL của header rỗng thì Unhandled exception | FAIL | gui-stocktake-stack.txt, ../t22-kiem-ke/bug-report.md |

Kết quả: 7 PASS / 1 FAIL / 3 BLOCKED / 0 NOT RUN. Đây là biên bản task trong báo cáo nhóm; không chốt hoàn tất riêng task.

## Regression đợt bug đã duyệt29/10/2026

B02 PASS: Get/List legacy rỗng an toàn, không chốt rỗng52902, ActualNULL khác0. Native KKTEST0001 mở chỉ tham khảo, Lưu/Chốt disabled và không crash. B01 layout/pager component PASS. Footer B05 chặn một phần luồng editor; chưa chạy lại đầy đủ T22-K01/K03/K10/K11.

Môi trường: SQL Server thật localhost, DB riêng CoffeeManager_Test_G05_20261029_134725_96d98d09_demo, schema021, DeviceDpi120. Kết quả/lệnh/actual tại ../runs/debug-batch-20261029/: db-regression.json, component-results.json, hash-regression.json và các native-*.txt; từng harness giữ nguyên trong run. Build Debug/Release sạch;22/22 xUnit. Source cố định theo source-after-debug.csv/source-freeze-check.json. DBtest/GUI/credential đã dọn theo resource-audit.json. Đây là kết quả regression theo bug, giữ nguyên bảng55ID lịch sử27/10 phía trên. Hình thức SKIPPED;100/150% NOT RUN.


## Regression B05 bổ sung29/10/2026

Người dùng đã duyệt B05 kèm regression. Helper footer sửa theo opt-in,28/28 helper case và23/23 checkpoint trên9 dialog thật PASS, DeviceDpi120; Debug/Release0warning/0error,22unit và5SQL replay subcase PASS. Source cố định; chỉ DB test schema021. Native bỏ nháp nhập Có/Không đúng owner, kho/PN/nợ không đổi; ghi thu chi2345đ qua xác nhận và reload đúng một phiếu/receipt. Luồng preview→chốt nhập BLOCKED bởi B06 mới (grid ReadOnly toàn bảng), chưa sửa. T21 có cùng cấu hình qua source, chưa suy FAIL native. Không phát hành tổng55ID mới; giữ bảng lịch sử phía trên. Hình thức SKIPPED,100/150% NOT RUN. Bằng chứng đầy đủ và giới hạn harness: [biên bản B05](../runs/b05-20261029/test-results.md). Tài nguyên test đã dọn2/2DB,GUI/credential tạm đã xóa; development không bị thao tác.

## Regression B07 ngày30/10/2026 — Đã duyệt

Control danh sách thuộc task này đã sửa vòng đời CTS. Tải/ẩn/hiện/refresh, dispose lặp, dispose khi SQL read đang bị khóa và phiên revoke PASS. Tổng năm màn47/47 lifecycle,5/5 CASH-BLL,22/22unit; Debug/Release0warning/0error. Native chuyển màn và logout/relogin/đóng MainForm PASS theo phạm vi. Xem [biên bản B07](../runs/b07-20261030/test-results.md) để phân biệt component/native và giới hạn. DB test dọn1/1, không đổi dữ liệu kho/nguồn/receipt, source cố định. Không suy PASS toàn ID hoặc55ca; native keyboard còn BLOCKED công cụ, hình thức SKIPPED, DPI100/150 NOT RUN. B07 đã xử lý, G05 vẫn Đang làm.
