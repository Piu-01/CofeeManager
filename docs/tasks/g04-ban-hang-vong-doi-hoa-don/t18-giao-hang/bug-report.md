# Bug bộ kiểm thử T18 — phát hiện trong regression24/10/2026

## Trạng thái hiện tại24/10/2026 — G04-TEST-06 đã sửa theo duyệt

V3 đọc revision binary8 trực tiếp từ bảng fixture trên SQL, không nội suy binary
thành SQL string. Validator so số phần tử/từng int sau sort, negative102/102 bị
từ chối,positive đảo thứ tự được chấp nhận và thiếu phần tử bị từ chối. Chụp
transaction state bằng DECLARE trước SELECT, không dùng biểu thức inline gây nhiễu.
4lượt giao hàng hai session cùng revision đều0/52103, một transition/receipt, đơn
vẫn mở, không payment. Cả18cặp concurrency được lưu raw expected/actual/state và
đối chiếu đúng; không thay expected để nhận mã lỗi bất kỳ. Kết quả ở
`../runs/regression_20261024_concurrency_v3/After/manifest.json`.
Giữ nguyên v1/v2 FAIL/PASS sai và audit, không sửa lịch sử. T18-K05 hiệnPASS; GUI
chuyển giao/refresh/quyền menu chưa đủ bằng chứng nên task/G04 chưaHoàn tất.

Mục chờ duyệt bên dưới là lịch sử trước khi người dùng duyệt đợt bổ sung.

## G04-TEST-06 — PASS sai khi hai session thực tế đều lỗi cú pháp

Trạng thái: **chờ duyệt bổ sung**, chưa sửa. Không là lỗi chuyển trạng thái sản phẩm.

Evidence `../runs/regression_20261024_testfix_v2/extended-results.json`: ca
T18-K05 expected0/52103, Actual102/102 nhưng RunnerStatusPASS. SQL1/2 chứa ký tự
NUL/tab từ rowversion trong biến @r thay vì literal0x. `concurrency-audit.json`
đối chiếu trực tiếp mảng expected/actual, hạ ca này thành FAIL; không sửa JSON gốc.

Hai lỗi của fixture: CONVERT(varchar,Revision,1) chưa ép Revision về varbinary
trước đổi thành chuỗi nên nội suy ra byte thô; biểu thức PowerShell so hai mảng
bằng -join và -eq không đặt ngoặc đúng, khiến kết quả dùng trong if là chuỗi truthy.
Các ca đồng thời T14/K10/K15 cũ được đối chiếu lại bằng số thực; không kế thừa chữ
PASS của Pair. Audit còn phát hiện T15-K11 nhận1205 thay52203, có hồ sơ ở T15.

Đề xuất sau duyệt: fixture dùng revision typed đọc từ bảng fixture trên SQL trong
hai session, tránh đưa binary vào SQL string; nếu cần log thì ép varbinary rồi
ToHexString. So mảng đã sort với số phần tử và từng phần tử bằng kiểu int, thêm
negative control102/102 chắc chắn phải FAIL. Giữ exception chain/SqlException.
Không thay expected để nhận mã lỗi bất kỳ. Chạy lại năm cặp concurrency, cả hai
thứ tự, receipt/revision và snapshot trước/sau; lưu phiên bản test mới, giữ v1/v2.

Rủi ro: validator dùng chung có thể từng che lỗi mới, nên audit raw toàn bộ cặp
trước khi kết luận. Các cặp raw đáp ứng expected mới được kế thừa. Không thay
procedure giao hàng từ một lỗi cú pháp do fixture tạo ra.
