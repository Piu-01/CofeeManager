# T21 — Biên bản đợt test G05

27/10/2026. Đợt test đã được duyệt; source cố định304 file,0 thay đổi (source-freeze-verification.json). SQL Server thật localhost, schema001–021; runtime G05Runtime/CafeManagerApp, actor ADMIN/STORE/CASH. DB fixture CoffeeManager_Test_G05_20261027_110753_fb67dcbb_demo; dữ liệu ghi test chỉ trong DB riêng. Build Debug/Release sạch và22/22 xUnit PASS; không thay nghiệm thu native bằng build/unit.

Bằng chứng dưới nằm tại ../runs/acceptance-full-20261027_110730/ nếu không ghi đường dẫn task. Mỗi JSON có dữ liệu, expected/actual, mã lỗi, hash/transaction hoặc session/barrier; lệnh là pwsh -NoProfile -File harness tương ứng. Environment/schema lưu environment.json; trước/sau source hash lưu source-frozen.csv và source-freeze-verification.json. Các ca chỉ đạt nhánh SQL nhưng thiếu nhánh native được giữ BLOCKED/NOT RUN.

Hình thức SKIPPED theo phân công. DeviceDpi native chưa đo xác nhận nên không ghi PASS125%; DPI100/150% NOT RUN, không đổi Windows Scale. DB fixture/credential đã dọn sau lưu bằng chứng.

| ID | Expected gốc | Actual / dữ liệu / thao tác | Kết quả | Bằng chứng |
|---|---|---|---|---|
| T21-K01 | [M/I] Nháp không trừ tồn; lưu Huy do hong/Tra hang giảm đúng nhiều nguyên liệu. | SQL xuất giảm tồn đã kiểm; nháp/editor native không truy cập được do thiếu nút lập phiếu | BLOCKED | gui-issue.txt, ../t20-nhap-kho/bug-report.md |
| T21-K02 | [I] LoaiXuat=Xuat ra quay hoặc loại lạ bị chặn theo mô hình một kho. | Xuat ra quay bị từ chối52801, không đổi kho/chứng từ | PASS | sql-extended-v2-results.json |
| T21-K03 | [I] Lượng 0/âm/quá precision, NVL/FK sai, trùng NVL bị chặn. | Qty0/âm/quáprecision, FK và trùng NVL bị chặn, snapshot giữ nguyên | PASS | sql-extended-v2-results.json |
| T21-K04 | [I] Xuất bằng tồn đưa tồn 0 hợp lệ; hơn tồn rollback toàn chứng từ. | Xuất đúng lượng tồn về0; thiếu tồn bị chặn và rollback | PASS | sql-extended-v2-results.json |
| T21-K05 | [I] Một dòng thiếu kho sau dòng khác hợp lệ không để dòng đầu bị trừ dở. | Dòng đầu đủ và dòng sau thiếu vẫn rollback toàn bộ, transaction0:0 | PASS | sql-extended-v2-results.json |
| T21-K06 | [I] Hai xuất/checkout tranh lượng tồn cuối: không âm, chỉ các giao dịch đủ tồn commit. | Hai xuất và checkout/xuất tranh tồn cuối qua hai thứ tự, barrier/DMV thấy cả hai chờ khóa; chỉ giao dịch đủ tồn commit | PASS | concurrency-v2-results.json |
| T21-K07 | [I] Chốt lại/retry không trừ lần hai; sửa/xóa chứng từ chốt bị chặn. | Replay/chốt lại và sửa/xóa bị chặn; timeout trước/sau commit không trừ hai lần | PASS | sql-extended-v2-results.json, timeout-v4-results.json |
| T21-K08 | [M/I] Xuất hủy cho đồ đã pha nhưng hủy đơn không đồng thời ghi tiêu hao bán cùng lượng. | SQL hủy đơn mở rồi xuất hủy đúng1, không có tiêu hao bán của đơn hủy; chưa chạy đủ luồng nháp/native do G05-B01 | BLOCKED | sql-supplement-v4-results.json |
| T21-K09 | [M] Xuất trả hiển thị rõ chưa tự hoàn tiền/cấn trừ nợ. | Workspace thật hiển thị chữ Hủy do hỏng / Trả hàng; chưa tự hoàn tiền hoặc cấn nợ | PASS | gui-issue.txt |
| T21-K10 | [U/M] STORE/ADMIN thao tác được, CASH bị chặn; reload đúng lịch sử/tồn. | SQL ADMIN/STORE/CASH đúng quyền; native thiếu nút lập và nhóm thao tác nên không hoàn tất nghiệp vụ/reload theo luồng | FAIL | gui-issue.txt, ../t20-nhap-kho/bug-report.md |

Kết quả: 7 PASS / 1 FAIL / 2 BLOCKED / 0 NOT RUN. Đây là biên bản task trong báo cáo nhóm; không chốt hoàn tất riêng task.

## Regression đợt bug đã duyệt29/10/2026

B01 component Xuất kho: layout hai kích thước và pager trên >50 phiếu PASS. Chưa chạy đủ native STORE/editor/chốt/reload; footer helper G05 có B05 chờ duyệt. Không thay T21-K01/K08/K10 thành PASS từ component.

Môi trường: SQL Server thật localhost, DB riêng CoffeeManager_Test_G05_20261029_134725_96d98d09_demo, schema021, DeviceDpi120. Kết quả/lệnh/actual tại ../runs/debug-batch-20261029/: db-regression.json, component-results.json, hash-regression.json và các native-*.txt; từng harness giữ nguyên trong run. Build Debug/Release sạch;22/22 xUnit. Source cố định theo source-after-debug.csv/source-freeze-check.json. DBtest/GUI/credential đã dọn theo resource-audit.json. Đây là kết quả regression theo bug, giữ nguyên bảng55ID lịch sử27/10 phía trên. Hình thức SKIPPED;100/150% NOT RUN.


## Regression B05 bổ sung29/10/2026

Người dùng đã duyệt B05 kèm regression. Helper footer sửa theo opt-in,28/28 helper case và23/23 checkpoint trên9 dialog thật PASS, DeviceDpi120; Debug/Release0warning/0error,22unit và5SQL replay subcase PASS. Source cố định; chỉ DB test schema021. Native bỏ nháp nhập Có/Không đúng owner, kho/PN/nợ không đổi; ghi thu chi2345đ qua xác nhận và reload đúng một phiếu/receipt. Luồng preview→chốt nhập BLOCKED bởi B06 mới (grid ReadOnly toàn bảng), chưa sửa. T21 có cùng cấu hình qua source, chưa suy FAIL native. Không phát hành tổng55ID mới; giữ bảng lịch sử phía trên. Hình thức SKIPPED,100/150% NOT RUN. Bằng chứng đầy đủ và giới hạn harness: [biên bản B05](../runs/b05-20261029/test-results.md). Tài nguyên test đã dọn2/2DB,GUI/credential tạm đã xóa; development không bị thao tác.

## Regression B06 đã duyệt29/10/2026

Hai editor mở grid nhập Quantity/Price và xuất Quantity với EditOnEnter; cột tham chiếu vẫn khóa.54/54component assertion ADMIN/STORE/khóa unknown/validation/binding/selection/default readonly/CASH quyền PASS;22unit và5SQL subcase PASS. Native ADMIN PN20250504: lượng2×10000/trả4000/nợ16000; PX20250506: lượng1, sau ca0 bị chặn và9999 vượt tồn rollback giữ input. Bỏ nháp sửa3 không ghi. SQL đối chiếu1PN/1PX/1chi/1nợ/2receipt, tồnNVL1229→30, các NVL khác không đổi. Source cố định;Debug/Release sản phẩm0warning/0error;DBtest/GUI/credential đã dọn. [Chi tiết](../runs/b06-20261029/test-results.md). Native Tab chưa chứng minh do công cụ kích hoạt owner; STORE/trả hàng native và các nhánh UX còn thiếu, không suy đủ55ID/tick bảng lịch sử.125% đã đo;hình thức SKIPPED,100/150% NOT RUN. Không có bug mới từ lượt này.


## Bổ sung30/10/2026 — Native follow-up

Native STORE xuất trả NVL12 qty2, PX20250504 đúng loại/NV992; tồn29+3−2=30, nợ21000 không cấn trừ và không phiếu chi PASS theo phạm vi.

Bằng chứng và môi trường: [biên bản](../runs/native-followup-20261029/test-results.md). Source không đổi,1DB test đã dọn, không thay lịch sử ID/nghiệm thu toàn task; keyboard còn thiếu, hình thức SKIPPED, DPI100/150 NOT RUN.


## Regression B07 ngày30/10/2026 — Đã duyệt

Control danh sách thuộc task này đã sửa vòng đời CTS. Tải/ẩn/hiện/refresh, dispose lặp, dispose khi SQL read đang bị khóa và phiên revoke PASS. Tổng năm màn47/47 lifecycle,5/5 CASH-BLL,22/22unit; Debug/Release0warning/0error. Native chuyển màn và logout/relogin/đóng MainForm PASS theo phạm vi. Xem [biên bản B07](../runs/b07-20261030/test-results.md) để phân biệt component/native và giới hạn. DB test dọn1/1, không đổi dữ liệu kho/nguồn/receipt, source cố định. Không suy PASS toàn ID hoặc55ca; native keyboard còn BLOCKED công cụ, hình thức SKIPPED, DPI100/150 NOT RUN. B07 đã xử lý, G05 vẫn Đang làm.
