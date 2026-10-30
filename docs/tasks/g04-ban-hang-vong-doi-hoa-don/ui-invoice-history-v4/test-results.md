# Kết quả V4 —26/10/2026

**Đóng lượt source cố định trước triển khai UI Giao hàng26/10:** giữ53/53,
94/94 và18/18 trên binary/hash V4 đã lưu. Người dùng chưa xác nhận chuỗi manual
V4/hình thức; phản hồi không thấy cửa sổ chỉ là vấn đề mở app, không là PASS.
App V4 có thể tiếp tục dùng binary cũ, nhưng kết quả không tự chuyển sang binary
Giao hàng mới. Không sửa hoặc chạy tiếp source trong cùng lượt V4 này.

Trạng thái: ĐÃ DUYỆT VÀ CHẠY đợt test chức năng; chờ xác nhận thao tác/hình thức
của người dùng trên V4. Source/binary tại
[manifest triển khai](../runs/invoice-history-v4-implementation-20261026/manifest.json).

| Kiểm kỹ thuật trong plan triển khai | Actual | Kết quả |
|---|---|---|
| Build Debug |0 warning,0 error |PASS |
| Build Release |0 warning,0 error |PASS |
| Smoke bounds/readonly/độ rộng mã,125% |28/28; DeviceDpi120; client1280×720 và1366×768 |PASS trong phạm vi kỹ thuật |
| Nghiệm thu hình thức |Người dùng tự kiểm theo AGENTS.md |SKIPPED theo phân công |
| DPI100/150% |Không thay Scale, chưa chạy |NOT RUN |

Smoke không gọi query/Auth/SQL hoặc thao tác nghiệp vụ; không thay IH01–IH10 hay
T17 gốc. Log/source hashes và layout-smoke.json nằm cùng manifest.

| Ca nghiệm thu | Actual | Trạng thái |
|---|---|---|
| IH01 |Smoke bounds28/28 tại125%; hình thức người dùng chưa xác nhận |PASS kỹ thuật, hình thức SKIPPED theo phân công |
| IH02 |Paid/today mặc định, Created/Paid, range đảo, ngày31/02 báo lỗi/khóa action; đầu ngày và ngày cuối của fixture khớp SQL |PASS các subcase đã chạy; mốc23:59:59 riêng NOT RUN |
| IH03 |Mã/tên/SDT, Enter/kính lúp, filter tự áp dụng, draft không submit qua filter/paging/refresh, clear/status/channel |PASS component; physical input chờ người dùng |
| IH04 |0/1/49/50/51/100/101 thực trên SQL; first/lastNext, trang cuối đủ50; stale-empty fallback qua query seam |PASS |
| IH05 |Hai task hoàn tất ngược thứ tự, lỗi đọc/cancel, không stale grid/action hoặc0giả |PASS controlled query seam; không giả lỗi SQL thật |
| IH06 |Readonly/code freeze/ngày format/currency0₫ trong fixture; độ rộng mã qua smoke |PASS các subcase kỹ thuật; tiền lớn/hình thức chưa nghiệm thu |
| IH07 |Đơn421paid, fixtureopen,473canceled: native modal/owner/no taskbar/khóa HWND cha; QuayPOS đúng; notfound rõ |PASS component; thao tác tay chờ user |
| IH08 |Focus date/search chọn hết, calendar popup mở/đóng, Enter submit |PASS component; click/caret/Tab thực và DPI100/150% NOT RUN |
| IH09 |ADMIN/CASH đọc thật, STORE bị BLL chặn; menu role qua regression |PASS |
| IH10 |Customers/Menu/Table/Area/Promotion/Policy/Point tải query thật, profile cũ, toolbar không stacked; pager API cũ; native/dialog regression |PASS trong phạm vi đọc/loading/control, không kiểm CRUD ghi |

Đợt acceptance tại
[thư mục bằng chứng](../runs/invoice-history-v4-acceptance-20261026/results.json):
53/53 functional checks,94/94 regression checks,18/18 xUnit Sales. Không cộng các
check này thành số caT17/G04 hoặc325ID. Binary V4 đã mở qua đăng nhập thường,
PID23728 trên DBtest; câu hỏi manual đã gửi, chưa có trả lời tại thời điểm ghi.

Tạo và tự dọn101 header đơn mở IH26000001–IH26000101, khách IV426, actor
IHA01/IHC01/IHS01; regression dùng NNA01/NNC01/NNS01 cũng dọn xong. Không checkout,
không in, không thay các đơn476–478. Snapshot tài chính/kho/điểm trước/sau cleanup
khớp; regression cũng xác nhận bất biến. Source142file không đổi và DLL khớp
implementation, xem source-verification.json. Không tạo DB mới; giữ DBtest đang
chung theo manifest cũ và appPID23728 để user kiểm, review27/10/2026.

Các lần harness lỗi fixture/managed Enabled/missing-code/synchronization đã lưu
raw FAIL riêng, giải thích tại
[harness-notes](../runs/invoice-history-v4-acceptance-20261026/harness-notes.md).
Không sửa source sản phẩm trong lượt test. Không kế thừa kết quả native cũ thành
PASS V4; không tuyên bố toàn bộ IH/T17/G04 hoàn tất khi subcase còn thiếu.
