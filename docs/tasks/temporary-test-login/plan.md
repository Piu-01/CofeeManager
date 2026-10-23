# Plan nút Bỏ qua đăng nhập — TEST

22/10/2026 — requirements và plan đã được người dùng duyệt. Đã triển khai bước1→4, build Debug/Release sạch; đợt test tại test-plan.md chờ duyệt riêng. Không tạo tài khoản/đổi password/migration hoặc mở lại nghiệm thu G02/G03. AGENTS.md đã cập nhật phân công hình thức và DPI theo yêu cầu.

## 1. Helper test và kiểm DB đích

**File mới dự kiến:** `src/CafeManager.DataAccess/Repositories/TemporaryTestTarget.cs`, `src/CafeManager.WinForms/Configuration/TemporaryTestLogin.cs`. Toàn bộ hai helper đặt trong `#if DEBUG`.

- Đọc cấu hình explicit gồm bật chế độ test, tên DB test, run marker; mặc định không có delegate đăng nhập test. Xác minh SQL Server localhost và tên DB thực tế khớp đúng DB cấu hình, có prefix DB test của dự án; marker phải khớp record trong DB, không chỉ tin tên DB/biến môi trường.
- DAL helper chỉ đọc marker `dbo.G03TestMarker` và xác minh DB thực tế trên connection; dispose connection, query async, parameter hóa giá trị, không đặt SQL trên Form. Không sửa quyền SQL để vượt lỗi truy cập, không tự fallback sang Windows owner hoặc DB khác. Nếu runtime không đọc marker được thì báo cấu hình test chưa sẵn sàng và ghi blocker để xử lý đúng phạm vi.
- Tái sử dụng `AuthRepository.ReadStateAsync`: yêu cầu Completed và AdminAccountId có giá trị. Dùng `ReadIdentityAsync(AdminAccountId)` để lấy identity ADMIN thật, đang hoạt động/còn làm việc/security stamp hiện tại. Không dùng ReadCredentialAsync, không đọc hash/password và không gọi AuthService.LoginAsync cho nút TEST.
- Helper WinForms tạo SessionContext từ identity hợp lệ, kiểm cancellation trước trả về; không chế role/stamp hoặc giảm kiểm PermissionService. Kiểm lại marker/identity mỗi lần bấm để tránh dựa vào cache phiên trước.

**Verify trong đợt test được duyệt:** thiếu flag, server/DB/marker sai, setup chưa hoàn tất, thiếu/khóa/nghỉ việc/sai role ADMIN không tạo phiên. Nhánh đúng đọc identity mà không cần password. Không bật logic ở Release.

## 2. Nối luồng phiên đang có

**File sửa:** `src/CafeManager.WinForms/Program.cs`, `src/CafeManager.WinForms/ApplicationSessionContext.cs`.

- Program chỉ cấu hình delegate TEST khi `#if DEBUG` và đủ tùy chọn test; dùng cùng connection mà các module thực sự sử dụng. Không thay profile F5 hiện có, không đổi target DB âm thầm hoặc chế độ setup.
- Bổ sung theo yêu cầu trực tiếp của người dùng sau triển khai: thêm profile `CoffeeManager.Test` trong launchSettings.json, giữ nguyên Development. Profile chỉ rõ manifest test; helper Debug đọc manifest và profile kết nối local trước khi tạo repository. Thiếu/sai manifest hoặc chạy Release thì giữ Login lỗi cấu hình, không fallback DB. Profile này cũng phải gỡ ở cuốiG08.
- ApplicationSessionContext đưa delegate tùy chọn vào LoginForm bằng một điểm nối Debug, giữ constructor/call site đang có hoạt động khi không bật test. Sau test bypass vẫn gọi event LoggedIn hiện tại để mở một MainForm, giữ handler LoggedOut/dispose/session revocation.
- Đăng xuất quay lại Login, nút TEST vẫn chỉ có nếu chế độ test còn đúng. Không giữ password hoặc cho phiên test rò sang lần chạy/DB khác.

**Verify:** đúng một MainForm/session, role ADMIN hiển thị đúng identity DB; logout thu hồi/dispose phiên và quay Login; khởi chạy thường/Release giữ luồng cũ.

## 3. Nút tạm trên LoginForm

**File sửa:** `src/CafeManager.WinForms/Forms/LoginForm.cs`. **File mới dự kiến:** `Forms/LoginForm.TemporaryTestLogin.cs`, toàn bộ nội dung trong `#if DEBUG`.

- Thêm nút “Bỏ qua đăng nhập — TEST” qua theme/OrbitButton đang có, vào một hàng riêng ở layout hiện hữu, không thay các ô nhập/nút đăng nhập thường. Không tạo nút nếu không có cấu hình test hợp lệ; không thêm widget/cột hoặc mở rộng role test ngoài ADMIN.
- Đặt hook/helper trong partial tạm để dễ gỡ; giữ Designer không có logic nghiệp vụ. Dùng Dock/AutoSize/tab order, layout logical96DPI/PerMonitorV2 sẵn có, hỗ trợ100/125/150%, ưu tiên125%.
- Luồng nhập thường vẫn gọi AuthService.LoginAsync. Hai nút dùng chung pending/generation/cancellation và chặn gửi lặp; có thể trích phần nhận session/xử lý busy nhỏ dùng chung nếu cần, không viết lại AuthService.
- Bấm TEST với username/password trống vẫn vào được. Khi một nút đang chạy, khóa cả hai; đóng form hủy yêu cầu, kết quả về trễ không mở MainForm và phải dispose phiên. Lỗi giữ Login, không lộ connection string.

**Verify:** không tự đăng nhập lúc mở app; test bypass không đọc input/password; nhập thường sai mật khẩu vẫn bị từ chối; lỗi/hủy/nhấn lặp không tạo hai phiên. Không yêu cầu screenshot/visual review của agent.

## 4. Launcher và cách gỡ ở cuối G08 / T32

Theo yêu cầu bổ sung đã chốt, bước gỡ thực hiện ở cuối G08 trước chốt PASS. Phải gỡ
cả source/hook/launcher, kiểm Debug/Release/publish và regression đăng nhập thường;
không chỉ ẩn nút hoặc dựa vào conditional compilation. Chưa gỡ/kiểm lại thì G08 chưa PASS.
Gate này đã ghi tại cuối T32 trong kế hoạch dự án; giữ nguyên325ID kiểm thử cũ.

**File mới dự kiến:** `tools/ui/Start-TestLogin.ps1`, `docs/tasks/temporary-test-login/README.md`; chỉnh tài liệu hồ sơ task khi triển khai.

- Launcher chọn manifest DB test riêng explicit; mặc định có thể dùng manifest test hiện có của G03 nếu chưa dọn và marker đúng. Chọn Debug exe, truyền cấu hình kết nối/test flags cho process con, không xuất secret hoặc set biến môi trường toàn máy.
- Tái sử dụng profile kết nối DPAPI hiện có rồi chỉ đổi catalog theo manifest đã xác minh; không đọc accounts.dpapi/password ứng dụng. Không sửa database hoặc account. Nếu manifest đã Cleaned, target sai hoặc thiếu cấu hình thì dừng và báo rõ.
- Không buộc dùng góiRelease mới có tính năng Debug. Hướng dẫn lệnh mở bản test, nút phải bấm và cách chạy bình thường khi không bật launcher.
- Hướng dẫn gỡ trước bàn giao: xóa hai helper/partial/launcher, gỡ hook Program/ApplicationSessionContext/LoginForm, build Debug/Release và regression đăng nhập thường. Ghi checklist gỡ trong README của tiện ích; chưa đánh dấu đã gỡ khi dự án còn đang làm.

**Verify:** launcher không đổi profile F5 và DB development thường, flags chỉ ở process con; Release không chứa helper/nút kể cả có biến môi trường.

## 5. Build, test và bàn giao

Sau duyệt plan, triển khai tuần tự bước1→4, build Debug/Release và lập test-plan cụ thể. Theo quy trình hiện tại, xin duyệt **một đợt test chức năng** trước chạy; giữ source cố định và ghi lỗi thay vì tự vá trong lượt test. Đợt này không kiểm thị giác; chỉ kiểm ởDPI125%. Nếu Windows chưa125%, báo rõ hoặc chờ người dùng đổi, không ép Program.cs về125% và không chạy ma trận100/150%.

Các nhóm ca tối thiểu:

| ID | Ca chức năng | Expected |
|---|---|---|
| Q01 | Debug không bật test; Release có/không flags | Không có nút/helper bypass khả dụng, đăng nhập thường còn nguyên |
| Q02 | Flag đúng nhưng sai server/DB/marker/manifest Cleaned | Không tạo phiên test hoặc fallback DB; lỗi không lộ secret |
| Q03 | Đúng DB test, để trống username/password, bấmTEST | Một phiên ADMIN thật, một MainForm, module BLL kiểm quyền được |
| Q04 | Setup/identity không hợp lệ hoặc không phải ADMIN | Từ chối tạo phiên; không tự cấp quyền/reset password |
| Q05 | Đăng nhập thường sai mật khẩu và thiếu input | Vẫn validation/xác thực cũ, không tự chuyển sang TEST |
| Q06 | Bấm lặp, bấm đồng thời hai nút, đóng khi pending | Không gửi chồng, không có phiên/response tới form đã đóng |
| Q07 | Logout→Login→TEST; thu hồi phiên sau khi vào | Dispose/revocation đúng, kiểm DB/identity lại mỗi lần |
| Q08 | Build/inspection Release và kiểm source secrets | Không còn type/helper test trong Release; không password/hash hard-code/log |
| Q09 | Chạy chức năng ở125% | Control/nút thao tác được; ghi đúng DPI; hình thức người dùng tự kiểm |

Không tự đổi tài khoản fixture có sẵn của người dùng để tạo ca lỗi; sử dụng fixture test riêng hoặc harness có phạm vi rõ, đối chiếu SQL thật cho nhánh tích hợp. Thiếu quyền/GUI/DPI phải ghi BLOCKED/NOT RUN đúng chỗ. Không commit/push hoặc sửa T01–T32.

## Gate hiện tại

Plan đã duyệt và triển khai; chờ duyệt một đợt test chức năng theo test-plan.md. Không xin duyệt riêng từng file/nút. Phần hình thức được miễn theo AGENTS.md và yêu cầu đã chốt; chưa báoPASS hoặc thay trạng thái nghiệm thu cũ.
