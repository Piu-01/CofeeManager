# G04 — Kết quả đợt test ngày23/10/2026

**Trạng thái: Đang làm, chưa PASS nghiệm thu.** Người dùng đã duyệt đợt test bằng
'duyệt'. Đã chạy trên source/schema015 cố định; không sửa sản phẩm, migration hoặc
test đã chạy. Chưa commit/push. Các checklist T01–T32/325ID giữ nguyên.

| Phạm vi | Kết quả thực tế |
|---|---|
| Unit G04 |18/18PASS,0skip |
| Unit regression quyền/phiên/khách/ưu đãi/kho |34/34PASS,0skip |
| SQL subset chuẩn bị sẵn |3/3batchPASS trên DBtest SQL Server thật |
| SQL mở rộng |32subcasePASS,2FAIL bộ kiểm thử |
| SQL còn lại/điều tra |12subcasePASS,4FAIL gồm fixture và assertion phụ thuộc |
| Checklist gốc56ID |18PASS,4FAIL,34BLOCKED; không đồng nhất subcasePASS với toàn ca |
| Native WinForms/print probe |125%:1row/3columns,CTA trong client;100dòng/210entry→13trang,exit0; không gửi job in |
| GUI thao tác nghiệp vụ |BLOCKED fixture compile, chưa có chứng cứ keyboard/handler/checkout thực |

Đã kiểm hai kết nối tranh bàn, sửa cùng revision, checkout cùng đơn và tranh tồn
cuối; lỗi cưỡng bức tại header/line/receipt/tiêu hao/payment/điểm rollback đầy đủ,
transaction0. Đã kiểm snapshot giá/ưu đãi,0₫/ship,phí đối tác,lịch sử điểm,replay,
DML DENY runtime user và một số filter/detail/giao hàng. Dữ liệu và lệnh/expected/
actual từng subcase nằm trong JSON/script, không suy PASS nhánh chưa chạy.

Có **năm lỗi bộ kiểm thử**, chưa chứng minh lỗi sản phẩm cần vá: expected điểm bỏ
qua giảm hạng10%; mã lỗi ngày dùng51108 thay52205; fixture GUI thiếu namespace và
using sai; fixture ngày cố sửa đơn paid nên paging/filter phụ thuộc không hợp lệ;
classifier timeout làm mất mã exception. FAIL cũ giữ nguyên. Điều tra độc lập chứng
minh gross1.000−giảm100=net900→0điểm đúng. [Đề nghị duyệt cả đợt sửa](test-bug-batch.md)
liên kết báo cáo T14/T15/T16/T17 và phạm vi regression/chạy bù.

[Biên bản T14](t14-tao-sua-don/test-results.md), [T15](t15-checkout/test-results.md),
[T16](t16-pos-huy-don/test-results.md), [T17](t17-tra-cuu-in/test-results.md),
[T18](t18-giao-hang/test-results.md) giữ đủ56ID với expected gốc và phần thiếu.
Các nhánh GUI/UX,phiên/revoke,timeout trước commit,biên actor/recipe,policy/recipe
đồng thời hai thứ tự,preflight legacy-invalid/rerun/checksum chưa được chứng nhận.
Hình thức UI/bản in **SKIPPED theo phân công người dùng**; DPI100/150% **NOT RUN**.

[Manifest đợt test](runs/acceptance-wave_20261023_195243_f9876e48/manifest.json),
[kết quả mở rộng](runs/acceptance-wave_20261023_195243_f9876e48/extended-results.json),
[kết quả còn lại/điều tra](runs/acceptance-wave_20261023_195243_f9876e48/remaining-results.json),
[subset/unit](runs/acceptance_20261023_195253_7d540db5/manifest.json),
[TRX regression](runs/acceptance-wave_20261023_195243_f9876e48/regression/regression.trx).
Đợt này tạo4DBtest,đã dọn4,còn0; không fixture vào development. Backup development
trướcG04 tiếp tục giữ theo chính sách bàn giao, không tự xóa. Không đổi SQL service,
WindowsScale hoặc tạo Windowsuser/login. SQL user WITHOUT LOGIN chỉ trong DBtest.

Đang chờ duyệt **một đợt sửa bộ kiểm thử và chạy bù**; lý do/skill bắt buộc được trích
trong test-bug-batch.md. Không đề nghị sửa sản phẩm khi chưa có lỗi được chứng minh.
Lịch sử bàn giao triển khai trước khi duyệt test lưu tại
[report-before-test.md](runs/acceptance-wave_20261023_195243_f9876e48/report-before-test.md),
gồm build Debug/Release0warning/error,SQL kỹ thuật,source/schema và nâng development.
