# T20 — Biên bản đợt test G05

27/10/2026. Đợt test đã được duyệt; source cố định304 file,0 thay đổi (source-freeze-verification.json). SQL Server thật localhost, schema001–021; runtime G05Runtime/CafeManagerApp, actor ADMIN/STORE/CASH. DB fixture CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo; dữ liệu ghi test chỉ trong DB riêng. Build Debug/Release sạch và22/22 xUnit PASS; không thay nghiệm thu native bằng build/unit.

Bằng chứng dưới nằm tại ../runs/acceptance-full-20261027_110730/ nếu không ghi đường dẫn task. Mỗi JSON có dữ liệu, expected/actual, mã lỗi, hash/transaction hoặc session/barrier; lệnh là pwsh -NoProfile -File harness tương ứng. Environment/schema lưu environment.json; trước/sau source hash lưu source-frozen.csv và source-freeze-verification.json. Các ca chỉ đạt nhánh SQL nhưng thiếu nhánh native được giữ BLOCKED/NOT RUN.

Hình thức SKIPPED theo phân công. DeviceDpi native chưa đo xác nhận nên không ghi PASS125%; DPI100/150% NOT RUN, không đổi Windows Scale. DB fixture/credential đã dọn sau lưu bằng chứng.

| ID | Expected gốc | Actual / dữ liệu / thao tác | Kết quả | Bằng chứng |
|---|---|---|---|---|
| T20-K01 | [M/I] Soạn nháp/đóng dialog không đổi kho; chốt n dòng cộng đúng tồn và tổng dòng làm tròn. | SQL nhập nhiều dòng/làm tròn từng dòng PASS; không mở được editor từ workspace để kiểm nháp/đóng | BLOCKED | sql-extended-v2-results.json, ../t20-nhap-kho/bug-report.md |
| T20-K02 | [I] NCC/nhân viên sai, lượng 0/âm, giá 0/âm, NVL trùng cùng phiếu bị từ chối. | SQL NCC/qty/price/precision/trùng/NVL FK bị chặn; chưa gọi riêng actor/nhân viên không tồn tại nên chưa PASS toàn ID | NOT RUN | sql-extended-v2-results.json |
| T20-K03 | [I] Một dòng lỗi/thiếu quyền rollback header, mọi chi tiết, kho, chi và nợ. | Thiếu quyền và fault sau ghi kho/chi rollback header/CT/kho/chi/nợ/receipt, transaction0:0 | PASS | sql-extended-v2-results.json |
| T20-K04 | [I] Trả ngay 0 tạo nợ toàn bộ, không chi 0; trả đủ tạo một chi, không nợ 0. | Trả0 có nợ10000 không chi0; trả đủ10000 một chi không nợ0 | PASS | sql-extended-v2-results.json |
| T20-K05 | [I] Trả ngay một phần: chi trả ngay đúng phần, nợ đúng tổng trừ phần đã trả. | Tổng10000/trả4000/nợ6000, nguồn chi và nợ đúng PN | PASS | sql-extended-v2-results.json |
| T20-K06 | [I] Trả ngay âm/vượt tổng hoặc dòng chi sai loại bị chặn. | Âm/vượt tổng/precision bị chặn; nguồn chi sai loại bị bảo vệ bởi invariant hệ thống | PASS | sql-extended-v2-results.json |
| T20-K07 | [I] Nhấn/gọi lại cùng yêu cầu không nhập kho/chi lần hai; đối chiếu theo mã nguồn khi timeout. | Replay cùng request không đổi dữ liệu; timeout trước commit receipt0/snapshot giữ nguyên, sau commit receipt1/replay không đổi | PASS | sql-extended-v2-results.json, timeout-v4-results.json |
| T20-K08 | [I] Phiếu chốt không sửa/xóa header/NCC/ngày/chi tiết; trigger cũ không cho bypass sửa lịch sử. | UPDATE/DELETE header và chi tiết đã chốt bị chặn, snapshot giữ nguyên | PASS | sql-extended-v2-results.json |
| T20-K09 | [I] Baseline có seed được backfill cờ chốt đúng, không chạy trigger cộng tồn lần nữa. | 10 PN legacy đều DaChot1; so sánh đủ hash với backup schema016 xác nhận kho và nguồn không đổi qua021 | PASS | sql-final-results.json, development-backup-verification.json |
| T20-K10 | [I] Hai phiên nhập cùng NVL cập nhật tồn đúng, không lost update. | Hai SQL session cùng NVL88, hai thứ tự, cả hai commit và tồn tăng đúng3 | PASS | concurrency-v2-results.json |
| T20-K11 | [U/M] STORE nhập được, CASH không; trả ngay/ghi tài chính chỉ ADMIN hoặc quy trình được cấp quyền rõ. | SQL STORE trả0 được, STORE trả tiền/CASH bị chặn; native ADMIN thiếu Lập phiếu nhập nên không thao tác theo luồng | FAIL | gui-inventory.txt, ../t20-nhap-kho/bug-report.md |
| T20-K12 | [M] Tính tổng preview khớp SQL và sau reload nguồn chi/nợ hiển thị đúng. | SQL rounding/source khớp; preview/reload nguồn trong editor native bị chặn bởi G05-B01 | BLOCKED | sql-extended-v2-results.json, ../t20-nhap-kho/bug-report.md |

Kết quả: 8 PASS / 1 FAIL / 2 BLOCKED / 1 NOT RUN. Đây là biên bản task trong báo cáo nhóm; không chốt hoàn tất riêng task.

## Regression đợt bug đã duyệt29/10/2026

B01 PASS: slot workspace được ghép đúng; component hai kích thước và pager >50 dòng PASS, native mở được Lập phiếu nhập và đóng nháp chưa chỉnh. B05 mới FAIL: footer không tiếp cận Chốt nhập kho/Quay lại, chờ duyệt bổ sung; chưa hoàn tất T20-K01/K11/K12.

Môi trường: SQL Server thật localhost, DB riêng CoffeeManager_Test_G05_20261029_134725_96d98d09_demo, schema021, DeviceDpi120. Kết quả/lệnh/actual tại ../runs/debug-batch-20261029/: db-regression.json, component-results.json, hash-regression.json và các native-*.txt; từng harness giữ nguyên trong run. Build Debug/Release sạch;22/22 xUnit. Source cố định theo source-after-debug.csv/source-freeze-check.json. DBtest/GUI/credential đã dọn theo resource-audit.json. Đây là kết quả regression theo bug, giữ nguyên bảng55ID lịch sử27/10 phía trên. Hình thức SKIPPED;100/150% NOT RUN.


## Regression B05 bổ sung29/10/2026

Người dùng đã duyệt B05 kèm regression. Helper footer sửa theo opt-in,28/28 helper case và23/23 checkpoint trên9 dialog thật PASS, DeviceDpi120; Debug/Release0warning/0error,22unit và5SQL replay subcase PASS. Source cố định; chỉ DB test schema021. Native bỏ nháp nhập Có/Không đúng owner, kho/PN/nợ không đổi; ghi thu chi2345đ qua xác nhận và reload đúng một phiếu/receipt. Luồng preview→chốt nhập BLOCKED bởi B06 mới (grid ReadOnly toàn bảng), chưa sửa. T21 có cùng cấu hình qua source, chưa suy FAIL native. Không phát hành tổng55ID mới; giữ bảng lịch sử phía trên. Hình thức SKIPPED,100/150% NOT RUN. Bằng chứng đầy đủ và giới hạn harness: [biên bản B05](../runs/b05-20261029/test-results.md). Tài nguyên test đã dọn2/2DB,GUI/credential tạm đã xóa; development không bị thao tác.

## Regression B06 đã duyệt29/10/2026

Hai editor mở grid nhập Quantity/Price và xuất Quantity với EditOnEnter; cột tham chiếu vẫn khóa.54/54component assertion ADMIN/STORE/khóa unknown/validation/binding/selection/default readonly/CASH quyền PASS;22unit và5SQL subcase PASS. Native ADMIN PN20250504: lượng2×10000/trả4000/nợ16000; PX20250506: lượng1, sau ca0 bị chặn và9999 vượt tồn rollback giữ input. Bỏ nháp sửa3 không ghi. SQL đối chiếu1PN/1PX/1chi/1nợ/2receipt, tồnNVL1229→30, các NVL khác không đổi. Source cố định;Debug/Release sản phẩm0warning/0error;DBtest/GUI/credential đã dọn. [Chi tiết](../runs/b06-20261029/test-results.md). Native Tab chưa chứng minh do công cụ kích hoạt owner; STORE/trả hàng native và các nhánh UX còn thiếu, không suy đủ55ID/tick bảng lịch sử.125% đã đo;hình thức SKIPPED,100/150% NOT RUN. Không có bug mới từ lượt này.


## Bổ sung30/10/2026 — Native follow-up

Native STORE nhập3×7000, trả ngay khóa0, lưu PN20250502/nợ21000/không phiếu chi PASS; nguồn và nợ chỉ đọc đúng quyền. T20-K02.ActorMissing runtime51505/dữ liệu không đổi/transaction0:0 PASS subcase.

Bằng chứng và môi trường: [biên bản](../runs/native-followup-20261029/test-results.md). Source không đổi,1DB test đã dọn, không thay lịch sử ID/nghiệm thu toàn task; keyboard còn thiếu, hình thức SKIPPED, DPI100/150 NOT RUN.


## Regression B07 ngày30/10/2026 — Đã duyệt

Control danh sách thuộc task này đã sửa vòng đời CTS. Tải/ẩn/hiện/refresh, dispose lặp, dispose khi SQL read đang bị khóa và phiên revoke PASS. Tổng năm màn47/47 lifecycle,5/5 CASH-BLL,22/22unit; Debug/Release0warning/0error. Native chuyển màn và logout/relogin/đóng MainForm PASS theo phạm vi. Xem [biên bản B07](../runs/b07-20261030/test-results.md) để phân biệt component/native và giới hạn. DB test dọn1/1, không đổi dữ liệu kho/nguồn/receipt, source cố định. Không suy PASS toàn ID hoặc55ca; native keyboard còn BLOCKED công cụ, hình thức SKIPPED, DPI100/150 NOT RUN. B07 đã xử lý, G05 vẫn Đang làm.
