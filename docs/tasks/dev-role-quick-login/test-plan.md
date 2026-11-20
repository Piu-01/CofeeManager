# Đợt test đăng nhập nhanh — đã duyệt và thực hiện

09/11/2026. Người dùng duyệt đợt test bằng “duyệt'” và sau đó duyệt sửa DQ-B01 kèm regression. Đợt gốc18PASS/1FAIL được giữ lịch sử; regression mới57/57 subcase và142unit PASS, xem bug-regression-results.md. Native còn BLOCKED/chưa đủ. Không còn chờ duyệt sửa DQ-B01, không suy PASS từ build.

Giữ source cố định, ghi SDK/schema/hash DLL và file source vào manifest, evidence không chứa credential. Dữ liệu account dùng SQL Server thật, DB test riêng theo tooling hiện có. Không đổi mật khẩu/role/trạng thái trên development. Provision account test qua cơ chế sẵn có, lưu mã identity thay password vào evidence, dọn đúng DB/process/file theo manifest.

| ID | Thao tác | Expected |
|---|---|---|
| DQ01 | Debug/F5 hai cờ; lần lượt bỏ cờ; target khác; cấu hình lỗi; Release và publish có cờ | Chỉ Debug đúng hai cờ/target hợp lệ có tiện ích; mọi nhánh khác login thường |
| DQ02a | Lưu/thay/xóa mỗi role; restart đọc lại; đọc ciphertext | Credential roundtrip đúng, từng role độc lập, không plaintext/password trong log/session |
| DQ02b | File hỏng/version sai/khác target; lỗi quyền ghi/file bị khóa, đối chiếu hash file trước/sau; Windows user khác khi có identity riêng | Báo lỗi rõ, không thay bản cũ khi ghi lỗi, không dùng nhầm target/user; thiếu identity thật ghi BLOCKED |
| DQ03 | Account từng role hợp lệ; sai password; khóa/nghỉ; đổi role sau lưu | Xác thực thật, đúng identity/màn/quyền; lỗi không vào MainForm, mismatch dispose session |
| DQ04 | Native click role thiếu cấu hình; cấu hình/lưu/đóng; click/Tab/click lần2, Esc/X/Enter; resize125% | Owner bị khóa đúng, input chọn hết lần vào, Enter không tự lưu, lưu không tự login, nút tiếp cận được |
| DQ05 | Click lặp/nhanh-thường; trì hoãn SQL rồi đóng Login/dialog; logout rồi đổi role | Một request, không lưu/phát login muộn sau hủy, phiên cũ không dùng lại, mở nút đúng sau lỗi |
| DQ06 | Login thường đúng/sai; logout/relogin, thu hồi/quyền; inspect local files và package | Luồng thường còn đúng, không secret trong source/evidence/package; release không bật qua cờ |

Helper production chỉ nhận target development cố định. Harness có thể tạo instance local của helper bằng reflection để dùng scope DB test được chỉ định trong manifest, không thêm cờ hoặc public factory nới guard trong sản phẩm. DQ01 đối chứng guard vẫn kiểm factory thật. Native end-to-end tích hợp test qua harness dùng LoginForm thật/AuthService thật trên DB test. Native F5 nếu chạy development chỉ xác thực bằng credential người dùng tự nhập; không thay account của development để tạo ca lỗi.

Lưu `test-results.md`: từng subcase, data identity, lệnh/thao tác, expected/actual, PASS/FAIL/BLOCKED/NOT RUN và đường dẫn evidence. Build Debug/Release đã được phép trong plan triển khai; xUnit/integration/native/publish kiểm chức năng chờ duyệt đợt này. Hình thức SKIPPED, DPI100/150%NOT RUN; chỉ125% chức năng/kỹ thuật, không đổi Scale. Lỗi mới ghi bug và xin duyệt sửa sau lượt, không sửa source khi test. Tham chiếu T06-K03/K04/K06–K10, UX-K01 và gate T32, không đổi lịch sử325ID.
