# B01 — Bỏ qua đăng nhập TEST bị SQL229 khi đọc marker

23/10/2026 — **B01 đã sửa và PASS regression** sau khi người dùng duyệt cấp quyền đọc marker trong DB test. Điều tra ngày22/10 bên dưới được giữ làm lịch sử. Người dùng báo bấm nút TEST không vào app; nhận định lần trước về project Tests không giải quyết lỗi nút.

## Kết quả sau duyệt

Đã GRANT SELECT đúng bảng dbo.G03TestMarker cho user G03Gui trong DB test đã xác minh;
không đổi schema/procedure/dữ liệu nghiệp vụ/account/password hoặc DB development.
Biên bản: test-results.md; bằng chứng runs/marker-provision.json và runs/b01-regression.json.
Helper/BLL và handler LoginForm→ApplicationSessionContext→mộtMainForm thật đãPASS ởDPI120
(125%) trong process fixture riêng. Không thao tác cửa sổ đăng nhập người dùng bằng công cụ UI.
Toàn bộQ01–Q10 chưa được nghiệm thu chỉ từ regressionB01 này.

## Hiện tượng và bằng chứng

- App thật đang mở ở `src/CafeManager.WinForms/bin/Debug/net10.0-windows`, cửa sổ Login. Đọc UI bằng computer-use (không bấm đăng nhập) thấy nút TEST và status **“Tài khoản kết nối không có quyền thao tác này.”**
- Probe chỉ đọc tại `runs/PermissionDiagnostic` đã kiểm marker đúng trên SQL Server thật, DB `CoffeeManager_Test_G03_20261019_215457_29e552a9_demo`; không sửa dữ liệu hoặc tạo phiên.
- `runs/permission-diagnostic.json`: runtime `HAS_PERMS_BY_NAME(...,'SELECT') = 0`; `SELECT RunId FROM dbo.G03TestMarker` lỗi **229: SELECT permission was denied on object G03TestMarker**.
- Cùng connection runtime đọc setup thành công: Completed, cóAdminAccountId; ReadIdentity thành công: ADMIN, Active=true, Employed=true. Chưa tới bước tạo SessionContext vì marker bị từ chối.
- Lần build probe đầu chạm DLL của app đang mở nên bị file lock; đã chuyển output riêng sang runs/permission-bin, build sạch và chạy chẩn đoán thành công. Không dừng app người dùng hoặc coi file lock này là bug nút.

## Nguyên nhân

TemporaryTestTarget.ReadAdminIdentityAsync yêu cầu đọc marker bằng runtime connection trước khi đọc identity. Fixture DB test hiện có chưa cấp SELECT trên bảng marker cho principal runtime. Các procedure auth có EXECUTE hợp lệ nên đăng nhập thường không cần quyền này; bước kiểm marker mới của tiện ích phát triển chưa được cấp đủ quyền fixture.

Đây là thiếu cấu hình quyền cho fixture test, không phải nhập sai mật khẩu, marker sai hoặc ADMIN bị khóa. Chưa có bằng chứng lỗi tạo MainForm, gửi lặp hoặc đăng xuất; các luồng đó cần kiểm riêng sau sửa.

## Hướng sửa đề xuất — chờ duyệt

1. Viết script provision tiện ích test: chỉ chấp nhận localhost + DB test explicit + manifest Cleaned=false; xác minh marker chính xác bằng kết nối quản trị local trước khi thay quyền. Không tự fallback owner khi người dùng bấm nút.
2. Xác định đúng database user của connection runtime đang được launcher/profile sử dụng; **GRANT SELECT ON OBJECT::dbo.G03TestMarker cho đúng user này trong DB test đó**. Không cấp db_owner/db_datareader, không mở quyền bảng nghiệp vụ, không đổi SQL login/server role hoặc password/account ứng dụng. Tham số giá trị và quote identifier an toàn, từ chối user dbo/guest/public hoặc target không khớp.
3. Ghi provisioning trong hồ sơ tiện ích/README để launcher mới dùng DB test khác cũng biết cần cấp quyền marker; không đặt GRANT trong handler đăng nhập hoặc AuthService. Giữ marker guard, không bỏ kiểm tra để nút vào được.
4. Sau sửa, regression đúng B01: marker đúng đọc được qua runtime; marker sai vẫn bị chặn; đọc ADMIN và tạo phiên TEST bằng luồng hiện có, xác minh một MainForm và quyền BLL thực. Giữ đăng nhập thường/Release không bypass. Chỉ kiểm chức năng ở125%, không kiểm hình thức. Không tự gán PASS toàn Q01–Q10 từ ca sửa này.

## Ảnh hưởng và rủi ro

Chỉ thêm quyền đọc một bảng nhận diện DB test, không thay logic backend hoặc source đăng nhập thường. Quyền được cấp trong DB test riêng, không đụng DB development/sử dụng thật. Script phải chặn nhầm target/principal trước khi chạy GRANT. Không chạy SQL thay quyền hoặc sửa source khi chưa duyệt đợt bug này.

Điều kiện gỡ cuối G08 vẫn giữ: gỡ helper/nút/profile/launcher/provision tiện ích khỏi source bàn giao, kiểm lại đăng nhập thường trước PASS G08; giữ hồ sơ lịch sử.
