# G04 — Kết quả regression24/10/2026

**Đang làm, chưa PASS nghiệm thu.** Người dùng đã duyệt sửa năm lỗi bộ kiểm thử
và chạy bù bằng 'duyệt' ngày24/10; đã thực hiện trong bản v2, giữ nguyên v1 và source
sản phẩm/schema015. Không commit/push, không sửa migrations hoặc dữ liệu development.

| Phạm vi | Kết quả hiện tại |
|---|---|
| Năm lỗi đã duyệt |TEST01/02/03/04/05 đã sửa; regression SQL/core đạt; GUI toàn luồng còn giới hạn công cụ |
| Unit G04 + regression liên quan |52/52PASS,0skip |
| SQL extended v2 sau audit raw |36PASS,2FAIL; JSON runner ghi38PASS sai do validator Pair, không dùng làm tổng nghiệm thu |
| SQL còn lại/điều tra v2 |16/16PASS |
| SQL chạy bù header/token/decimal/overflow/quyền |7/7PASS |
| Checklist gốc56ID |26PASS,2FAIL,28BLOCKED |
| Native WinForms125% |SetValue100.000→thối19.000;80.000→thiếu tiền;abc→validation. Không chứng nhận click/keyboard/commit |
| Print probe |100dòng/210entry;9trang đầu,controller tái dùng trả18previewpages tích lũy; không gửi job in hoặc kiểm hình thức |

Đã chứng minh header tổng/giảm0 bên trong transaction rồi trigger cập nhật25.000;
preview token đổi do KM/policy/khách/đối tác; tiêu hao haiSP/chungNVL đúng1.2885 và
4.7035, kho9998.7115/9995.2965, đổi recipe không đổi snapshot. Net999/1.000/1.999
được0/1/1điểm; overflow rollback. Ngày tương lai bị52205 không đổi snapshot. Fixture
ngày được dựng trước chốt: Created/Paid2/1,cuối ngày23:59:59,filter1,paging50/1 có51
mã duy nhất. Timeout sau commit ghi đúng-2 và đọc lại1receipt/1header,transaction0.
SQL ADMIN/CASH chuyển giao được,STORE bị51605. Những kết quả này không thay UI đầy đủ.

Hai vấn đề mới chưa sửa, cần [duyệt đợt bổ sung](additional-bug-batch.md):

- **G04-TEST-06 / T18-K05:** fixture nhúng rowversion sai→SQL102/102; validator Pair
  vẫn ghi PASS. Audit độc lập hạ FAIL. Cần revision typed và validator số đúng, có
  negative control, chạy lại concurrency. Không kết luận lỗi chuyển giao sản phẩm.
- **G04-CONC-01 / T15-K11:** raw0/1205 thay expected0/52203. Đồ thị system_health
  xác nhận vòng khóa kho↔HOADON trong trigger thanh toán. Một đơn paid,tồn không âm,
  đơn thua không payment vẫn đạt; chưa thấy sai tiền/kho/điểm. Cần actual plan/lock
  để quyết định patch tối thiểu hoặc ghi quyết định chấp nhận rollback1205. Chưa đổi
  SQL/expected; không dùng chữ PASS sai của runner.

[Biên bản T14](t14-tao-sua-don/test-results.md),[T15](t15-checkout/test-results.md),
[T16](t16-pos-huy-don/test-results.md),[T17](t17-tra-cuu-in/test-results.md),
[T18](t18-giao-hang/test-results.md) giữ đủ56ID/expected gốc. Các checklist T01–T32
và325ID toàn dự án không bị tick/sửa. Các ca kế thừa v1 vẫn có link bằng chứng gốc.

GUI đã được người dùng cấp quyền công cụ. Accessibility/SetValue làm việc, nhưng
click trả 'coordinate input geometry is unavailable' hai lần sau refresh/Raise,
Tab không cho thấy focus chuyển; không chứng nhận Enter/doubleclick/handlerConfirm.
Fixture in-memory không là login hoặc commitDB. POS tạo/mở/reload,nháp,unknown,
refresh bàn/khách/tồn,quyềnUI/phiên/revoke và UI in vẫn BLOCKED. Timeout trước commit,
hai thứ tự policy/recipe đồng thời,preflight legacy-invalid/rerun/checksum chưa chạy
đủ. Hình thức UI/bản in **SKIPPED** theo phân công; DPI100/150% **NOT RUN**,không đổiScale.

Evidence: [manifest v2](runs/regression_20261024_testfix_v2/manifest.json),
[raw concurrency audit](runs/regression_20261024_testfix_v2/concurrency-audit.json),
[đồ thị deadlock](runs/regression_20261024_testfix_v2/deadlocks-file.xml),
[SQL chạy bù](runs/regression_20261024_testfix_v2/supplement-results.json),
[GUI actions](runs/regression_20261024_testfix_v2/gui-actions.json),
[52unit TRX](runs/regression_20261024_testfix_v2/unit/g04-regression.trx).
Đợt v2 tạo3DBtest,đã dọn3,còn0; hai GUI fixture tự đóng,không còn probe. Backup
trướcG04 của development tiếp tục giữ theo chính sách cũ. Không đổi service/login/
WindowsScale hoặc ghi fixture vàoDBsửdụngthật.

Năm lỗi cũ đã được duyệt và sửa, không hỏi duyệt lại. Chờ duyệt **hai vấn đề mới**
ở additional-bug-batch.md trước sửa; không tự mở scope sang module khác. Lịch sử đợt
23/10 lưu [report-before-regression](runs/regression_20261024_testfix_v2/report-before-regression.md).
