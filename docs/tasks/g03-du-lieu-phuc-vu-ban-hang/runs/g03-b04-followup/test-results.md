## Xác nhận của người dùng — 20/10/2026

Người dùng phản hồi sau khi nhận bản sửa: “ok test vậy mình thấy đủ ok”. Ghi nhận chấp nhận giao diện lịch/nút lịch bản mới trong phiên manual hiện tại, kết hợp bằng chứng bounds125% đã PASS. Phần lỗi visual B04 được người dùng chấp nhận đóng. Không suy rộng phản hồi này thành đã chạy bàn phím, Designer hoặc DPI100/150%; các ca đó giữ NOT RUN. Không thay đổi kết quả nghiệm thu toàn nhóm G03 hay các ca nghiệp vụ còn lại. Không sửa source hoặc dữ liệu DB trong lượt cập nhật này.
# G03-B04 bổ sung — regression ngày 20/10/2026

Được người dùng duyệt sửa và regression. Windows/.NET SDK 10.0.401; màn hình hiện tại 120 DPI (125%). Không thay Windows DPI. Source sản phẩm cố định sau sửa: source-frozen.csv; audit.json: ChangedCount=0.

## Nguyên nhân đo được và thay đổi

layout-before.json: native MCM_GETMINREQRECT và SingleMonthSize cùng 281×248, nhưng calendar/host 262×207, popup264×209. Đây là phép đo HWND do harness sở hữu, không phải điều khiển process bên ngoài. SetWindowTheme sau tạo handle làm kích thước native khác kích thước ban đầu; AutoSize host trước đó không đồng bộ lại. Nút Width=76 cùng EndEllipsis khiến chữ bị rút theo ảnh người dùng.

OrbitDateField giờ tạo handle trước đo, lấy SingleMonthSize rồi đồng bộ calendar/host/popup, kiểm lại khi popup mở; đặt popup trong working area, mở phía trên nếu thiếu chỗ phía dưới. Nút vuông theo chiều cao field, icon Lucide calendar milk20/24/32 từ guide (license hiện có), có AccessibleName và tooltip. Không đổi SQL/BLL/giá trị khuyến mãi.

| Ca | Expected/actual | Kết quả |
|---|---|---|
| B04-F01 build Debug/Release | 0 warning, 0 error; build-debug.log/build-release.log | PASS |
| B04-F02 unit G03/MaterialRules | 20/20; unit.log, g03-b04-followup.trx | PASS |
| B04-F03 datefield STA | 11/11 logic ngày; date-results.json/date-probe.log | PASS |
| B04-F04 bounds popup giữa màn, 125% | Native281×248; calendar/host283×250, popup285×252; đủ native và nằm trong working area; layout-after.json/log | PASS |
| B04-F05 sát đáy màn, 125% | Popup mở phía trên; đủ native, nằm trong working area; layout-edge.json/log | PASS |
| B04-F06 visual/icon/keyboard thực tế trong editor | Chưa kiểm đầy đủ bằng Computer Use; harness đã mở nhưng helper báo “Computer Use app approval timed out” khi activate/capture. Không coi phép đo layout là nghiệm thu visual/keyboard. | NOT RUN |
| B04-F07 DPI100/150% và Designer | Chưa có bằng chứng trên các DPI này; không đổi setting máy người dùng | NOT RUN |
| B04-F08 lưu/chọn ngày trên DB test | Đã mở lại app bản sửa bằng launch.ps1, PID9684; chờ người dùng đăng nhập. Không tự thao tác auth, không ghi DB. | NOT RUN |

Hai lần build đầu gặp file lock từ app test PID30908 (MSB3026/MSB3021), không phải lỗi compile. Đã dùng Computer Use bấm Hủy editor rồi Alt+F4 đóng app cũ; build Debug/Release cuối thành công. Harness sau đo đã đóng; chỉ phiên CoffeeManager DB test hiện tại được mở lại. B04 chưa đóng cho đến khi có native visual/keyboard và ca ngày cần thiết.

