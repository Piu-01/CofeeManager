# DQ-B01 — Cấu hình sai phiên bản làm lọt InvalidDataException

09/11/2026. **Người dùng đã duyệt; DQ-B01 đã sửa và regression PASS:57/57 subcase,142unit.** Xem [biên bản](bug-regression-results.md). Lịch sử nguyên nhân/FAIL bên dưới giữ nguyên; không còn chờ duyệt fix này. Native còn thiếu không được nâng PASS từ regression.

## Hiện tượng và bằng chứng

Ca DQ02.CorruptAndVersion tạo file fixture DPAPI hợp lệ với envelope Version=2, gọi helper Get. Expected: InvalidOperationException có thông báo Việt hướng dẫn kiểm cấu hình, Login vẫn dùng được. Actual: InvalidDataException lọt ra ngoài helper. FAIL lặp lại trên cùng DLL sản phẩm trong Probe/ProbeV2/ProbeV3; phần corrupt bytes được xử lý, phần version sai thất bại.

Bằng chứng chính: [probe-results-v3.json](runs/acceptance_20261109_204755_debbbe06/probe-results-v3.json), ca DQ02.CorruptAndVersion; mã tái hiện trong ProbeV3/Program.cs. Debug DLL SHA256:50A3CA288C1A751EE47B0AE26D2C8EFE4E53AC0048A966A836D80DDC44CEEDC9. DeviceDpi120, Windows/.NETSDK10.0.401, source-audit.json xác nhận sản phẩm không đổi.

## Nguyên nhân đã có bằng chứng

- `Configuration/DevelopmentQuickLogin.cs:42,51` chủ động ném InvalidDataException khi file lớn hoặc envelope không hợp lệ.
- `DevelopmentQuickLogin.cs:92`: IsStorageError chỉ nhận IOException, UnauthorizedAccessException, CryptographicException, JsonException.
- Runtime xác nhận `typeof(InvalidDataException).BaseType.FullName = System.SystemException`; đây không phải IOException, vì vậy catch filter ở Get/Update không dịch lỗi này.
- `Forms/LoginForm.cs:71` chỉ bắt InvalidOperationException quanh quick-login. Dialog Lưu/Xóa cũng không bắt InvalidDataException. Hậu quả có thể là lỗi UI chưa xử lý khi dùng file không hợp lệ; đã chứng minh exception thoát helper, chưa cố tình gây crash native nên không bịa log crash.

## Hướng sửa đề xuất

Thêm InvalidDataException vào IsStorageError của helper; tiếp tục dùng StorageError thông báo hiện có, không expose dữ liệu plaintext hoặc inner exception. Không đổi AuthService, quyền, schema, file format/version hay bỏ kiểm envelope. Không xóa cấu hình tự động khi lỗi.

Một file sản phẩm bị ảnh hưởng. Regression: DPAPI corrupt bytes/version sai/server-DB mismatch/schema không hợp lệ/oversize; Get/Save/Delete giữ thông báo dự kiến; ghi thất bại giữ file cũ; Login/dialog không phát event hoặc báo thành công giả khi lỗi cấu hình. Chạy lại storage và ba-role/pending liên quan, build Debug/Release. Source cố định trong lượt regression mới. Các ca native còn bị giới hạn công cụ vẫn ghi BLOCKED nếu chưa thao tác được.

## Ảnh hưởng và rủi ro

Lỗi nằm ở tiện ích DEV, tái hiện bằng fixture local; chưa có bằng chứng sai xác thực hoặc dữ liệu nghiệp vụ. Sửa filter có phạm vi nhỏ nhưng cần kiểm không nuốt lỗi lập trình khác hoặc ghi đè/xóa file cấu hình sau lỗi. Các blocker native và Windows user thứ hai không phải bug sản phẩm mới và không được coi đã giải quyết bằng sửa filter.
