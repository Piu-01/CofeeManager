# Bổ sung bằng chứng native từ đợt G08

02/11/2026. Thực hiện trong đợt test G08 được người dùng duyệt, trên package ứng viên schema027 và SQL login runtime riêng/DB test của lượt. Không sửa source/migration giữa test, không viết lại kết quả G07 ngày31/10 hoặc tự đổi tổng trạng thái nhóm.

Phần T28-K06 đã thao tác thật trên WinForms:

- Báo cáo SalesDay, DB clean không giao dịch: query thành công mới trả tổng0 và0dòng; metadata đúng kỳ.
- Người dùng cho phép fixture quyền tạm: DENY EXECUTE sp_ReportSales chỉ userG08NativeRuntime/DBclean test. UI hiện “Lỗi tải; chọn Xem báo cáo để thử lại” và “Chưa cập nhật: Tài khoản kết nối không có quyền thao tác này”, không dựng tổng0 giả.
- Sau khoảng30.23giây, finallyREVOKE khôi phục quyền kế thừa từ role; CanExecute1. Thử lại thành công, trả0dòng, không giữ lỗi cũ. Không đổi quyền development.

[Native transcript](../../g08-van-hanh-nghiem-thu-ban-giao/runs/acceptance_full_20261102_160135_579468c5/native-transcript.json), [fixture quyền được duyệt và đã thu hồi](../../g08-van-hanh-nghiem-thu-ban-giao/runs/acceptance_full_20261102_160135_579468c5/report-fault-fixture.json), [source/package không đổi](../../g08-van-hanh-nghiem-thu-ban-giao/runs/acceptance_full_20261102_160135_579468c5/freeze-audit.json).

Đây là phần bổ sung cho shared report state với SalesDay. Không chứng nhận mọi loại report/filter/drilldown/latequery/phiên hoặc toàn bộ9ID native còn thiếu ở G07. Kết quả G08 và phần còn BLOCKED tại [report G08](../../g08-van-hanh-nghiem-thu-ban-giao/report.md). Hình thức SKIPPED theo phân công người dùng;100/150%DPI NOT RUN, không đổi Windows Scale.
