# G08-B01 — Lỗi kết nối/chứng chỉ bị báo thành lỗi dữ liệu chưa xác định

02/11/2026 · **Đã được người dùng duyệt sửa kèm regression; đã sửa, regression PASS và đóng G08-B01 ngày02/11/2026.** Phát hiện trong đợt test G08 được người dùng duyệt, source/package cố định. Sở hữu ca T30-K07/UX-K44; tầng mapper dùng chung từ T05/T06, ảnh hưởng đường cấu hình/đăng nhập trên bản publish.

## Hiện tượng và bước tái hiện

Package: artifacts/g08/20261102_155435_c6b76b42, Windows x64/framework-dependent, archive SHA256 D1F7A782FB0C083DE02D30D0597DE4EC1AF68F138D17C36BF3143EBF88D8C54A. SQL Server localhost17.0.1000.7; runtime đã tải System.Windows.Forms10.0.1326.50504. Cấu hình chỉ ở tiến trình con, runtime SQL login riêng và DB test của lượt.

1. Offline: trỏ server tcp:127.0.0.1,1, Connect Timeout4, nhập account fixture rồi bấm Đăng nhập.
2. Chứng chỉ: trỏ tcp:localhost, Encrypt=true/TrustServerCertificate=false, nhập fixture rồi bấm Đăng nhập; không bypass cảnh báo hoặc thay chứng chỉ.
3. Cả hai trường hợp Login hiển thị **“Có lỗi dữ liệu chưa xác định; tra mã hỗ trợ trước khi tiếp tục.”** Không có hướng dẫn kiểm kết nối hoặc nguyên nhân chứng chỉ. Input username còn, app không vào MainForm.

Expected: lỗi kết nối/chứng chỉ được phân loại và thông báo dễ hiểu, không lộ secret, không tự chọn DB khác hoặc bỏ xác thực TLS. Nhánh thiếu cấu hình/syntax sai và sai mật khẩu đã có thông báo riêng đúng.

## Bằng chứng và nguyên nhân đã xác nhận

- [Native transcript](../runs/acceptance_full_20261102_160135_579468c5/native-transcript.json): T30-K07.offline/certificate, thông báo thật từ WinForms.
- [Điều tra DLL đúng package](../runs/acceptance_full_20261102_160135_579468c5/connection-error-investigation.json): SqlClient trả **258/Class20** cho endpoint offline; **−2146893019/Class20** cho chứng chỉ không tin cậy. Gọi AuthRepository.ReadStateAsync từ cùng DLL trả DataAccessException với Number tương ứng, Kind=Unknown và cùng thông báo generic.
- Trên snapshot trước sửa: src/CafeManager.DataAccess/SqlErrorMapper.cs:144–157, danh sách số lỗi Connection ở155–156 chưa gồm258 và−2146893019, nên rơi nhánh Unknown ở157. AuthRepository.cs:62 chuyển SqlException qua mapper; LoginForm.RunLoginAsync hiển thị thông báo DataError. Đây là đường gây hiện tượng đã đối chứng, không chỉ giả thuyết từ source.

**Không có bằng chứng bypass TLS, fallback DB, ghi tiền/kho sai hoặc mất dữ liệu.** Lỗi ở phân loại/thông báo và khả năng vận hành; không đánh đồng với blocker cache/geometry của công cụ nhập modal. Việc GenericUnknown không chỉ rõ nguyên nhân làm T30-K07 FAIL theo plan/UX.

## Đợt sửa đề xuất để duyệt một lần

1. Sửa tối thiểu SqlErrorMapper cho các mã kết nối/chứng chỉ đã có bằng chứng; thông báo Việt có hành động kiểm kết nối/chứng chỉ phù hợp. Không đổi mọi Unknown thành Connection, không hướng dẫn tắt TLS/TrustServerCertificate=true để né lỗi.
2. Giữ nguyên quyền/flow xác thực, không log password/connection string/raw credential và không đổi policy đối chiếu giao dịch ghi. Khi phân loại lỗi connection, xem `writing` để không vô tình bỏ bảo vệ unknown/reconcile hiện có.
3. Thêm unit regression có giá trị cho258/chứng chỉ cùng các mã cũ/querytimeout/unknown, rồi kiểm offline/chứng chỉ/missing/syntax/sai mật khẩu/đăng nhập đúng trên WinForms và package mới. Tạo package mới/manifest/hash, không patch ứng viên đang được test.
4. Regression các call site BLL/UI đọc và ghi chịu mapper theo ảnh hưởng; giữ source cố định trong từng lượt. Duyệt đợt sửa bao gồm regression này; không bao gồm tự sửa các bug sản phẩm mới chưa được ghi nhận.

## Phạm vi và rủi ro

File sản phẩm dự kiến: SqlErrorMapper.cs; tests/CafeManager.Tests/ParameterAndErrorTests.cs hoặc test cùng vùng. Có thể cần thông điệp riêng theo mã lỗi, không cần migration/đổi schema/role/password/UI layout. Mapper dùng chung nên rủi ro là phân loại quá rộng hoặc thay semantics `RequiresReconciliation` cho ghi; regression phải giữ các bất biến đó. Không refactor DAL hoặc làm recovery/login bypass.

Không có tối ưu ngoài nguyên nhân trong đợt đề xuất này. Các ca native/E2E chưa chạy đủ vẫn giữ BLOCKED; sửa G08-B01 không tự biến toàn bộ29ID hoặc325ID thành PASS.


## Kết quả sau sửa

Xem [biên bản regression G08-B01](bug-regression-results.md):142unit,4kiểmDAL/BLLtrướcsend và7ca nativePASS;1DB/1SQLlogin/DPAPIđãdọn. Lịch sử15PASS/1FAIL/13BLOCKED trước sửa giữ trong report-acceptance-20261102.md và test-results-acceptance-20261102.md; hiệnG08 16PASS/0FAIL/13BLOCKED, chưaHoàntất.

