# T24 — Kết quả nghiệm thu G06

Đợt được duyệt30/10/2026, tổng hợp31/10/2026. SDK10.0.401; SQL Server localhost, baseline+seed/migrations001–024; DB test riêng có marker. Source chưa commit, SHA256 trước/sau không đổi (src/database/tests/tools). Không nâng development. Hình thức SKIPPED theo phân công; DPI100/150% NOT RUN; app component thực DeviceDpi120.

Bằng chứng/lệnh/input/expected/actual chi tiết: [runner acceptance](../runs/acceptance_20261030_155806_b10d6581/manifest.json), [extended](../runs/extended-20261030/results.json), [bổ sung](../runs/extended-20261030/supplement/results.json), [SQL followup](../runs/extended-20261030/transaction-followup-v2.json), [native trace](../runs/extended-20261030/native-trace.json), [audit](../runs/extended-20261030/final-audit.json). Các script/project đặt cạnh kết quả để tái hiện. Expected gốc dưới đây giữ nguyên.

| ID | Expected gốc | Actual | Kết quả |
|---|---|---|---|
| T24-K01 | [I/M] Tạo/sửa full-time/part-time đúng mức lương và chức vụ; tên/SDT/FK hợp lệ. | Regression B01/B02 PASS: create/update tiền18chữ số và format grid đúng. Native editor còn chưa nghiệm thu đủ. | BLOCKED |
| T24-K02 | [I] Hợp đồng sai, lương âm, tên rỗng/quá dài, SDT thực trùng bị chặn; nhiều NULL hợp lệ. | Các nhánh đầu vào sai đã PASS; regression valid50tên/15SDT/maxmoney PASS, SQL vẫn chặn tiền lẻ và không ghi/receipt. | PASS |
| T24-K03 | [I] Một nhân viên một tài khoản theo unique; username trùng bị chặn. | Unique employee/account, username/canonical login được chặn đúng; hai session tạo account cùng nhân viên:2waiters,1success/1uniqueerror. | PASS |
| T24-K04 | [M/U] Reset password ghi hash mới, mật khẩu cũ không login; hash/pwd không lộ trên grid/log. | BLL reset/login cũ-mới và DTO không có secret PASS; biên mật khẩu8/128,7/129 và confirmation đã kiểm. Native reset dialog/grid/log thao tác chưa nghiệm thu đủ. | BLOCKED |
| T24-K05 | [I] Nhân viên nghỉ khóa account, không xóa lịch sử hóa đơn/chấm công. | Nghỉ việc khóa account, session cũ bị từ chối; hóa đơn/công lịch sử còn nguyên. | PASS |
| T24-K06 | [I/M] Không khóa/xóa admin hoạt động cuối cùng; bootstrap không bị mở lại để reset tùy ý. | Race haiADMIN tự hạ quyền2waiters/1success; admin cuối lock/retire/delete/demote bị chặn; bootstrap bị chặn qua SQL followup. Native thao tác còn BLOCKED. | BLOCKED |
| T24-K07 | [I] Đổi chức vụ không tự đổi vai trò; đổi vai trò chỉ thao tác được cấp quyền. | Đổi chức vụ không đổi role; CASH/STORE đang hoạt động không đổi hồ sơ/account qua BLL/SQL. | PASS |
| T24-K08 | [I] Đổi mức lương không sửa snapshot BANGLUONG kỳ cũ. | Mức lương snapshot legacy và snapshot mới không đổi sau cập nhật hồ sơ; đối chiếu T26-K06. | PASS |
| T24-K09 | [I] Xóa nhân viên đã tham chiếu bị chặn; employee editor không tạo cascade delete. | NV có account/hóa đơn bị chặn xóa; NV mới không tham chiếu xóa được, không cascade. | PASS |
| T24-K10 | [U/M] CASH/STORE gọi service quản lý nhân sự/account bị từ chối; reload hồ sơ đúng. | CASH/STORE service/SQL từ chối đúng; filter typed/phiên bản/receipt đúng. Reload/bàn phím native chưa chạy được. | BLOCKED |

Lịch sử trước nghiệm thu được giữ tại runs/extended-20261030; không đổi expected để né lỗi. PASS là phạm vi ca gốc đã kiểm ở tầng được nêu; không suy native/hình thức PASS từ service/component. Các nhánh native chưa chạy giữ BLOCKED. Xem [báo cáo nhóm](../report.md) để biết giới hạn và đợt bug đề nghị.


## Cập nhật sau đợt debug đã duyệt31/10/2026

G06-B01/B02 đã sửa, [regression chung](../regression-b01-b02.md):78/78subcase SQL/BLL/component PASS và14/14unit PASS. Lượt đầu75PASS/3FAIL của harness được lưu riêng, không ghi đè. Mỗi lượt nguồn cố định; chỉ3fileG06 được sửa trong đợt. Các ca native còn thiếu vẫn BLOCKED, không suy PASS từ formatter/component. Trạng thái mới G06:26PASS/0FAIL/7BLOCKED; lịch sử trước debug nằm trong thư mục run.
