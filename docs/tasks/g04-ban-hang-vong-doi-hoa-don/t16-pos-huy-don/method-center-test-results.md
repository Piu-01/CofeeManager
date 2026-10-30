# G04-UI04 — Căn giữa ô phương thức, 25/10/2026

Sau bàn giao, người dùng xác nhận “test tiếp đi ok rồi”: ghi nhận phần căn giữa
đã được người dùng xác nhận; không mở rộng thành nghiệm thu hình thức toàn G04.

Yêu cầu cụ thể của người dùng: căn giữa ô chọn Tiền mặt và giải thích Số thu có
được chỉnh không. Đã xác định editor38px nằm Top0 trong panel58px từ số đo UI03.
Sửa tối thiểu bằng cột combobox native dùng chung OrbitPaymentMethodColumn,
override PositionEditingControl sau base; Checkout chỉ đổi loại cột Method.
Không thêm hành vi tài chính, không khóa Số thu hoặc đổi schema/role/BLL.

Môi trường .NET10 WinForms, Darkmode, DeviceDpi120=125%, source chưa commit.
Các lượt component dùng đúng DLL Debug đóng băng; thao tác binding/handler trực
tiếp, không phải chuột/phím vật lý. Source cố định trong lượt test.

| Kiểm tra | Actual | Kết quả |
|---|---|---|
| Build Debug/Release |0 warning/0 error | PASS |
| Căn giữa editor | DLL cũ Top0/Height38/Panel58, DLL mới Top10/Height38/Panel58 | PASS, trước sửa FAIL |
| Component cũ | Validation số thu/khách đưa/method, mixed/bank/0₫, busy/unknown, disposal, shared controls |32/32 PASS |
| Layout/binding cũ Darkmode |60ca body/footer/resize/nhiều dòng/scroll và mapping PT002 |60/60 PASS |
| SQL | Không đổi dữ liệu, allocation hay handler tài chính; 7/7 SQL bridge UI03 giữ lịch sử | Không chạy lại |
| Hình thức | Người dùng tự kiểm | SKIPPED |
| DPI100/150%, physical input | Không đổi Scale, chưa thêm thao tác vật lý | NOT RUN |

Artifacts tại [runs/method-center-20261025](../runs/method-center-20261025/), gồm
center-results.json, before/after-observations.json, component-results.json,
layout-results.json, source-frozen.csv và final-manifest.json. Các mốc cũ giữ nguyên.

Theo UX mục Checkout, Số thu là phân bổ có thể nhập để chia phương thức; tổng
phải bằng số phải trả. Ví dụ hóa đơn81000: tiền mặt31000 + chuyển khoản50000.
Khách đưa là input khác chỉ tính tiền thối cho phần tiền mặt, không tính doanh thu.
G04 vẫn Đang làm 33 PASS /0 FAIL /23 BLOCKED, chưa nâng toàn T16-K11.
