# Plan — Đăng nhập nhanh theo role khi development

09/11/2026. Requirements và plan đã được người dùng duyệt bằng “duyệt”. Đã triển khai/build Debug/Release; đợt test chức năng chờ duyệt riêng, xem report.md.

## Cơ sở và phạm vi

- T06 và mục 12.1/13, điều kiện chốt G08 trong `docs/KeHoach_QuanLiQuanCafe.md`; luồng đăng nhập/phiên mục 6 của UX; AGENTS và bộ UI Orbit tại `docs/ui/`.
- Source hiện tại: LoginForm đã có pending/cancel/generation dùng chung; ApplicationSessionContext tạo LoginForm mới khi logout; AuthService xác thực và trả identity thật. DevelopmentConnection đã dùng DPAPI CurrentUser.
- Giữ AuthService/PermissionService và contract SQL. Không thêm migration, DAL, dependency hoặc tạo/reset tài khoản. Không khôi phục bypass cũ.

## 1. Giới hạn chế độ DEV và nối dependency

File: `src/CafeManager.WinForms/Program.cs`, `ApplicationSessionContext.cs`, `Properties/launchSettings.json`; helper mới `Configuration/DevelopmentQuickLogin.cs`.

- Chỉ tạo dịch vụ đăng nhập nhanh trong Debug và khi đồng thời có `CAFEMANAGER_USE_DEVELOPMENT=1`, `CAFEMANAGER_DEV_QUICK_LOGIN=1`, auth/kết nối hợp lệ. Thêm cờ mới vào profile CoffeeManager.Development để F5 Debug có nút ngay.
- Giới hạn target đúng development local hiện có: server localhost, database CoffeeManager_Development; không tự đổi target hoặc dùng cờ môi trường để bỏ qua đối chiếu target. Khi sai target, tiện ích không hoạt động và không đọc credential lưu của DB khác.
- Truyền dependency tùy chọn từ Program qua ApplicationSessionContext tới LoginForm, bao gồm lần tạo mới sau logout. Constructor mặc định/Designer và đường login thường vẫn sử dụng được.
- Release loại đường khởi tạo tiện ích bằng compile guard; không cho bật lại qua biến môi trường. Đây là giới hạn lúc phát triển, không thay checklist gỡ source cuối T32.

Kiểm chứng: build cả cấu hình; trong đợt test kiểm Debug đúng/sai cờ, sai DB/server, cấu hình lỗi, Release có cờ và publish Release.

## 2. Lưu credential local theo DB/Windows user

File mới: `src/CafeManager.WinForms/Configuration/DevelopmentQuickLogin.cs` (giữ helper nhỏ; tách file chỉ nếu implementation cần).

- Lưu envelope version/server/database và map ADMIN/CASH/STORE → username/password, mã hóa toàn bộ bằng DPAPI CurrentUser với entropy riêng phiên bản này.
- File riêng dưới `%LOCALAPPDATA%/CoffeeManager/development/quick-login/`, tên theo SHA256 của server/database đã chuẩn hóa; không hash toàn connection string chứa SQL password. Kiểm lại identity target trong envelope khi đọc.
- Chỉ đọc credential của role được chọn khi cần; không giữ credential map dài hạn, không đặt password vào SessionContext. Xóa buffer byte plaintext trong finally khi có thể; không tuyên bố xóa được immutable string của .NET.
- Ghi qua file tạm cùng thư mục rồi replace/move an toàn, dọn đúng file tạm khi lỗi; lỗi ghi không phá bản đã lưu. Giới hạn kích thước/schema, chỉ chấp nhận ba role, không ghi log payload/exception chứa credential.
- Có thao tác lưu/thay thế và xóa credential một role từ cấu hình. File thiếu/hỏng/khác user hoặc target báo tiếng Việt, giữ login thường dùng được; không tự reset cấu hình hay dữ liệu DB.

Kiểm chứng: roundtrip, thay thế/xóa, DB scope, dữ liệu hỏng/version sai, lỗi I/O và file cũ còn nguyên khi ghi thất bại; không xuất secret vào evidence.

## 3. Cấu hình một lần bằng dialog có owner

File mới: `src/CafeManager.WinForms/Forms/DevelopmentQuickLoginDialog.cs`; tận dụng OrbitDialogForm/OrbitDialogHost, AppTheme và OrbitField hiện có.

- LoginForm có nút “Cấu hình đăng nhập nhanh”. Dialog chọn ADMIN/CASH/STORE, nhập tên đăng nhập và mật khẩu; không hiện mật khẩu cũ đã lưu. Có “Lưu”, “Xóa cấu hình role” và “Đóng”.
- Lưu gọi AuthService.LoginAsync để xác thực, kiểm role khớp rồi dispose session tạm, sau đó ghi DPAPI; thao tác này không mở MainForm. Lỗi xác thực/role/I/O giữ dialog, không báo lưu thành công.
- Native titlebar/X, ShowInTaskbar=false, owner LoginForm; dùng cơ chế modal chung và clamp WorkingArea/DPI. Input áp dụng select-all chung một lần. Không thêm hiệu ứng bụi sao.
- Chi tiết tích hợp sau đọc source: OrbitDialogHost hiện yêu cầu MainForm, trong khi Login chưa có MainForm. Bổ sung entry point nội bộ ShowLoginDialog có owner LoginForm trong chính host này; giữ nguyên stack nghiệp vụ, dùng ShowDialog(owner) và OrbitField chung cho hộp thuộc vòng đời Login.
- Một request đang chạy; khóa chọn role/input/Lưu/Xóa, dùng cancellation/generation khi đóng; callback muộn không lưu cấu hình sau khi đóng. Password được clear khi đóng. Enter không tự lưu credential.

Kiểm chứng: cấu hình từng role bằng account thật của DB test riêng, sai role/sai password/khóa/nghỉ việc, đóng khi đang xác thực, lỗi lưu và owner/focus chức năng tại125%.

## 4. Ba nút đăng nhập nhanh và vòng đời phiên

File: `src/CafeManager.WinForms/Forms/LoginForm.cs`, `LoginForm.Designer.cs` nếu cần row/layout; logic để ngoài Designer.

- Thêm vùng “Đăng nhập nhanh — DEV”: ADMIN, CASH, STORE và nút cấu hình, theo theme hiện có; layout wrap/AutoSize và cuộn khi cần để tiếp cận các nút ở cửa sổ nhỏ. Giữ nhập tài khoản/mật khẩu thường.
- Chưa có cấu hình: bấm role mở dialog cấu hình chọn sẵn role; lưu xong trở lại Login, lần bấm tiếp theo đăng nhập. Không tự đăng nhập từ sự kiện lưu.
- Có cấu hình: đọc đúng role, gọi AuthService.LoginAsync trong RunLoginAsync hiện có, kiểm role thực trước LoggedIn. Sai role dispose session và báo cấu hình cần cập nhật; không phát LoggedIn.
- Khóa nút thường, ba nút nhanh và nút cấu hình trong cùng pending; không tạo đường request song song. Dispose session nếu cancelled/generation cũ, bao gồm trường hợp lệch role; lỗi mở lại nút đúng trạng thái.
- Logout tạo LoginForm mới với dependency dev; không giữ password/session cũ, không tự đăng nhập lại.

Kiểm chứng: click lặp và trộn nút thường/nhanh, từng role vào đúng màn/quyền, logout đổi role, đóng đang chờ, lỗi DB và phản hồi muộn. Ghi rõ phần chưa kiểm được native, không suy PASS từ component.

## 5. Build và tài liệu bàn giao dev

File: `docs/tasks/dev-role-quick-login/README.md`, `report.md`, `test-plan.md`; bổ sung checklist vào `docs/tasks/temporary-test-login/README.md` và hồ sơ T32, UX mục đăng nhập để ghi ngoại lệ DEV mới đã duyệt, giữ lịch sử cũ.

- Hướng dẫn F5 Debug → cấu hình một lần → bấm role → logout đổi role; vị trí file DPAPI và cách xóa qua UI. Không ghi mật khẩu mẫu hoặc yêu cầu gửi mật khẩu trong chat.
- Checklist T32: gỡ helper/dialog/UI/dependency/hooks/cờ profile/hướng dẫn vận hành dev mới; xử lý đúng file credential local của tiện ích khi kết thúc sử dụng; build/publish và regression đăng nhập thường/phiên/quyền sau gỡ. Không đánh dấu gate G08 đã PASS.
- Chạy build Debug/Release sau triển khai để kiểm compile, ghi warning/error thực; không coi build là nghiệm thu chức năng. Không commit/push hoặc nâng DB development.
- Lập test-plan cụ thể và chờ duyệt một đợt test riêng theo post-change-test-workflow.

## Đợt kiểm thử dự kiến sau duyệt riêng

| Nhóm ca | Nội dung |
|---|---|
| DQ01 | Debug/F5 bật; thiếu từng cờ, target khác, cấu hình lỗi không bật; Release/publish có cờ vẫn không bật |
| DQ02 | Lưu/thay/xóa ba role; DPAPI roundtrip, version/file hỏng, tách target, lỗi I/O không mất bản cũ |
| DQ03 | ADMIN/CASH/STORE xác thực thật, đúng identity/màn/quyền; khóa/nghỉ/sai password/sai role bị chặn |
| DQ04 | Native bấm từng nút; chọn hết input; owner/modal, X/Esc/Tab, loading/error, giữ input khi lỗi |
| DQ05 | Click lặp/trộn nút, đóng khi chờ, logout đổi role, callback muộn, session tạm được dispose |
| DQ06 | Login thường đúng/sai, logout, recheck phiên/quyền không bị ảnh hưởng; không secret trong source/log/package |

Test dùng SQL Server thật và DB test riêng provision bằng tooling sẵn có; không thay dữ liệu/tài khoản của development. DQ01 dùng target test đối chứng cho trường hợp bị từ chối; các ca tích hợp helper cần fixture/harness riêng truyền target test trong harness, không thêm cờ production để nới guard. Native F5 đúng target chỉ kiểm đọc/xác thực trên development khi tài khoản người dùng tự cấu hình; ca thay đổi account dùng DB test. Source cố định trong lượt test, ghi PASS/FAIL/BLOCKED/NOT RUN và evidence không chứa secret. Hình thức SKIPPED theo phân công; agent chỉ kiểm chức năng/layout kỹ thuật125%, không đổi Scale,100/150%NOT RUN. Bug mới báo điều tra/duyệt trước sửa.

## Điểm dừng

Plan đã duyệt và triển khai. Duyệt plan cho phép code/build trong phạm vi trên, chưa phải duyệt đợt test chức năng; hiện chờ duyệt test-plan.md theo post-change-test-workflow.
