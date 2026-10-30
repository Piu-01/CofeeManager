# G04-UI03 — Sửa bố cục Checkout, 25/10/2026

Người dùng đã duyệt debug và regression UI03. Đã sửa trong bốn file sản phẩm:
CheckoutDialog.Layout.cs, CheckoutDialog.cs, OrbitSummaryPanel.cs và OrbitDataGridView.cs.
Không thay SQL/DAL/BLL, role hay luồng chốt/đối chiếu giao dịch.

Bảng dùng chiều cao theo header và tối đa ba dòng rồi cuộn nội bộ, không dùng
hàng Percent trong container AutoSize. Cash/notice nằm ngay dưới bảng. Header có
mã hóa đơn/kênh; ghi chú ưu đãi nằm dưới dòng Ưu đãi. Dùng lại theme và control
Orbit; extension AddNote ở Summary và owner-draw editor chỉ thuộc profile Payment.
Method trống báo “Chọn phương thức thanh toán.”; mapping code/name giữ nguyên.

## Môi trường và cách kiểm

.NET 10 WinForms thật, Windows Scale 125%, DeviceDpi=120. Không chụp ảnh QA,
không đổi Scale. Source sản phẩm chưa commit; hash đóng băng trước mỗi lượt.
Fixtures điều khiển binding/editor/handler trực tiếp, không thay thế kiểm chuột/phím
trên toàn app hoặc nghiệm thu hình thức của người dùng.

Lượt đầu vẫn FAIL notice bị viewport cắt 23px; kết thúc lượt source cố định và
giữ toàn bộ chứng cứ tại `../runs/display-fix-20261025/`. Sau đó tiếp tục cùng
UI03 đã duyệt: giảm khoảng đệm và chỉ dành chỗ scrollbar khi cần. Chứng cứ bản cuối
tại `../runs/display-fix-20261025-r2/`; không đổi expected cũ để né FAIL.
Harness mới từng lỗi khởi tạo SetUnhandledExceptionMode sau khi Darkmode tạo control;
đã lưu log lỗi harness, sửa thứ tự khởi tạo riêng harness; source sản phẩm không đổi.

| Kiểm tra | Actual | Kết quả |
|---|---|---|
| Build Debug/Release sản phẩm | Cả hai 0 warning/0 error | PASS |
| Regression component cũ | Validation tiền/method/reference, mixed/bank/0₫, busy/unknown, footer, shared profiles/dialog, dispose | 32/32 PASS |
| Probe mới Darkmode | Toàn bộ grid/cash/notice/summary/toolbar/footer trước/sau focus, default1150×750,1280×720,1366×768 | PASS, thuộc 60/60 |
| Editor | OwnerDrawFixed, màu Input, chọn tên lưu đúng PT002; bank vô hiệu cash; method trống báo rõ và khóa Confirm | PASS, thuộc 60/60 |
| Nhiều dòng và cửa sổ hẹp | Sáu dòng chỉ hiển thị ba dòng và cuộn đến dòng6; cash/notice/summary tiếp cận bằng viewport scroll, footer còn trong client tại740×700 và740×500 | PASS, thuộc 60/60 |
| Bounds tái hiện | Grid từ1408px xuống108px; cash/notice không bị ancestor clipping trước/sau focus ở form1150×750 | PASS |
| SQL bridge bản cuối | Fixture SQL Server riêng schema016:81000/100000, mixed/bank/0₫, competing checkout, unknown injected/reconcile, transaction0:0 | 7/7 PASS |
| Chuột/phím vật lý, popup xác nhận và toàn luồng POS | Chưa có thêm thao tác toàn app của người dùng trong lượt này | NOT RUN |
| Hình thức so mẫu | Theo phân công người dùng tự kiểm | SKIPPED |
| DPI100/150% | Không đổi Windows Scale | NOT RUN |

Artifacts: [60 kiểm tra layout/binding](../runs/display-fix-20261025-r2/layout-results.json),
[32 kiểm tra cũ](../runs/display-fix-20261025-r2/component-results.json),
[bounds trước/sau focus](../runs/display-fix-20261025-r2/body-observations.json),
[source/binary audit](../runs/display-fix-20261025-r2/final-manifest.json).

UI03 được sửa về chức năng/layout kỹ thuật trong phạm vi đã kiểm; hình thức chờ
người dùng xác nhận. T16-K11 còn các nhánh tìm kiếm, nhiều bàn/món và loading POS
chưa đủ chứng cứ nên BLOCKED; không nâng toàn ca thành PASS. G04 giữ Đang làm,
33 PASS / 0 FAIL / 23 BLOCKED trên 56 ID; FAIL trước sửa vẫn giữ lịch sử.

DB riêng `CoffeeManager_Test_G04_20261025_172233_7933d112_checkout` đã dọn sau
đối chiếu marker/exact name. [SQL evidence](../runs/display-fix-20261025-r2/sql-results.json):
phân bổ81000, điểm+81, NVL01−0.0180, replay không đổi snapshot; tiền mặt31000+
bank50000, bank-only và0₫ đúng. Unknown được inject vào state cho ca reconcile,
không mô tả là mất response thật. Fixture gọi SalesService trực tiếp sau capture
từ dialog, không khẳng định đã click Confirm vật lý trên app.
