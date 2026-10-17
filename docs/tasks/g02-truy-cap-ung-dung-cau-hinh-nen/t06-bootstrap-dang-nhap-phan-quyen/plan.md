# Plan T06 — Bootstrap, đăng nhập và phân quyền

Trạng thái: Hoàn tất; bộplan đãduyệt, nghiệm thu T06 10/10IDgốc PASS17/10/2026. ĐãbàngiaoGit,xem`../git-handoff.md`. Phụ thuộc T05 đã bàn giao.
Nguồn: requirements G02; kế hoạch T06/mục10–13; UX01/mục6; UI guide.
Đầu ra: admin hợp lệ, đăng nhập thật, session và authorization BLL; editor
nhân viên/tài khoản đầy đủ vẫn T24.

## 1. Bước và file

| Bước | File dự kiến | Đầu ra / kiểm tra kỹ thuật |
|---|---|---|
| 1 | database/migrations/002_auth_and_bootstrap.sql | State/username/stamp/procedure/role; compile trên SQL Server DB test, rollback migration khi preflight lỗi |
| 2 | Models/Dtos/AccountIdentity.cs; Models/Requests/LoginRequest.cs, BootstrapRequest.cs | Typed request/identity; session không password/hash/SqlClient |
| 3 | DataAccess/Repositories/AuthRepository.cs, SetupRepository.cs | Async typed query/procedure, dispose, không transaction C# bọc SQL; hash chỉ tồn tại trong dữ liệu auth tạm |
| 3a | DataAccess/SqlErrorMapper.cs; Business thông báo auth/setup | Thêm mã51401–51407, giữ mapping G01; không raw SQL error/hash ra UI |
| 4 | Business/Security/PasswordService.cs, SessionContext.cs, PermissionService.cs; Services/AuthService.cs, BootstrapService.cs | Hash/verify/CAS; phiên bị thu hồi; kiểm quyền trước thao tác |
| 5 | WinForms/Forms/LoginForm.*, BootstrapForm.*; Program.cs | Setup riêng, Login che password, thiếu cấu hình/lỗi không vào phiên; constructor Designer không SQL |
| 6 | tests/CafeManager.Tests/Auth/*; database/tests/t06/*; tools/database/Test-G02.ps1 | Chuẩn bị unit/integration fixtures, guard DB và capture kết quả; chưa chạy nghiệm thu |
| 7 | docs/database/auth-contract.md, migrations.md, README.md | Contract, setup/runtime/password tại máy, lệnh riêng; technical-checks tại task |

Tên thư mục C# ở bảng tính từ src/CafeManager tương ứng từng tầng. Package
Microsoft.Extensions.Identity.Core pin 10.0.0 trong Business, dùng PasswordHasher
V3, IterationCount=100000. Không tự viết hash. Test fixture cấp password tạm trong
bộ nhớ lúc chạy, không file/log/CLI argument password, không tài khoản mặc định.

## 2. Bootstrap và quyền setup

### 2.1. Trạng thái, dữ liệu và transaction đề xuất

Migration002 thêm dbo.AppSetupState singleton Id=1, State varchar(20)
(Pending/LegacyReview/Completed), CompletedAt datetime2(0) NULL,
AdminAccountId int NULL FK TAIKHOAN. CHECK Completed phải có timestamp/account;
không cấp DML trực tiếp cho runtime hoặc setup. Thông tin audit không password.
Vai trò app ADMIN/CASH/STORE được khởi tạo nếu thiếu, không đổi role đúng đã có.
Role có mã/tên xung đột unique thì migration dừng thay vì ghi đè.

State ban đầu Pending chỉ khi không có ADMIN với hash khác placeholder seed
đã biết. Có bất kỳ ADMIN hash khác placeholder (kể cả inactive/nghỉ) →
LegacyReview: SQL không tự kết luận hash hợp lệ, không tự mở đường reset.
State đã có thì migration không đổi; không tạo Completed bằng nhìn HoatDong.

Procedure bootstrap sở hữu transaction SET XACT_ABORT, lấy applock exclusive
CoffeeManager.AdminBootstrap (LockOwner Transaction), lock singleton và account/
nhân viên liên quan, kiểm lại State và điều kiện trước ghi. Completed luôn
từ chối. Trong Pending mà xuất hiện ADMIN hash không-placeholder do race thì
từ chối tạo/reset, yêu cầu xác nhận chuyển phiên bản. Lock timeout trả xung đột.
Ghi admin + State Completed trong cùng transaction; lỗi rollback tất cả.
Sequence NV có thể có gap sau rollback, không hứa trả lại số. Hai session chỉ
một commit, session sau thấy Completed. Không mở lại State khi khóa admin.

| Nhánh | Input tại máy | Thay đổi trong transaction |
|---|---|---|
| DB sạch / tạo admin mới | TenNV bắt buộc≤50; SDT optional≤15; username; password/xác nhận | Nếu thiếu, tạo CVADM (Quản trị ban đầu, phụ cấp0, hệ số1.5); tạo NV bằng sp_CapMa, Full-time, lương0, DangLamViec1; tạo ADMIN account HoatDong1 với hash C#; Completed |
| Demo / kích hoạt một account | Chọn MaNV/account hiện có; username chỉ đọc nếu account đã có; password/xác nhận | Chỉ account ADMIN inactive với đúng placeholder và nhân viên đang làm được kích hoạt, compare hash/stamp; không sửa NV/lương/chức vụ, không bật thungan/kho; Completed |
| Nhân viên có sẵn chưa có account | Chọn nhân viên đang làm chưa có account; username/password | Tạo đúng một ADMIN account, giữ nhân sự cũ; Completed |
| LegacyReview / xác nhận admin hiện có | Username/password hiện tại của ADMIN HoatDong1/DangLamViec1 | BLL verify Identity hash hiện tại; SQL compare MaTK/hash/stamp/role/trạng thái còn đúng rồi chỉ ghi Completed, không đổi hash/nhân viên |

CVADM là mã đề xuất mới, không phải role ADMIN. Nếu đã tồn tại, chỉ dùng khi
tên chuẩn/PhuCap0/HeSo1.5 khớp; khác thì báo xung đột, không đổi cấu hình cũ.
Input chỉ thu tối thiểu để đáp ứng FK; không editor T24. DB demo không thêm CVADM
nếu dùng nhân viên có sẵn. LegacyReview nhưng không verify được admin hoạt động
thì dừng, báo cần quy trình khôi phục quản trị riêng; không implement recovery
hoặc biến bootstrap thành recovery. Setup timeout sau ghi: đọc State/account ID,
không tự retry tạo/reset; Completed dẫn đóng setup.

### 2.2. Setup/runtime đề xuất

Exe có chế độ --setup, chạy bởi Windows identity được người cài provision vào
role DB CafeManagerSetup riêng. --server/--database chỉ nhận đích, không password;
connection Integrated Security, Encrypt, trust certificate chỉ localhost theo G01.
--setup không sử dụng CAFEMANAGER_APP_CONNECTION hoặc tự lấy sysadmin fallback.
Runtime vẫn CAFEMANAGER_APP_CONNECTION dưới account CafeManagerApp riêng.
Đọc quyền setup thực tế trước hiện form; không có quyền thì chỉ hướng dẫn.

Migration grant CafeManagerSetup EXECUTE các procedure đọc ứng viên/trạng thái/
bootstrap/xác nhận legacy; không DML bảng, không DDL/SchemaMigrations. Runtime
chỉ đọc trạng thái tổng quát (không hash/ứng viên); không EXECUTE bootstrap.
Sau success đóng process setup, không mở MainForm bằng connection setup; hướng
dẫn chạy app bình thường bằng identity runtime. Quyền setup provision/revoke bằng
script migration operator `database/security/provision-setup-user.sql` (template,
không identity/password cứng). Checklist ghi việc revoke sau cài; không giả SQL
có thể tự revoke quyền operator trong transaction app. Recovery không thuộc G02.

## 3. Username, auth, session và rehash

Contract setup cụ thể dự kiến:

| Procedure | Input / output | Quyền |
|---|---|---|
| sp_SetupListCandidates | State; danh sách MaNV/TenNV/MaTK/username/role/status và cờ eligible; không hash | Setup |
| sp_SetupReadLegacyAccount | LoginKey → credential tạm giống AuthReadAccount để BLL verify; không bind hash lên UI | Setup |
| sp_SetupBootstrap | Mode NewEmployee/ExistingEmployee/ActivateDemo; trường tối thiểu bảng2.1, NewHash từ BLL, expected stamp/hash cho demo → AdminAccountId/CompletedAt | Setup |
| sp_SetupConfirmLegacy | MaTK/ExpectedHash/ExpectedStamp đã được BLL verify → chỉ ghi State Completed và audit | Setup |
| sp_AppReadState | State/schema version, không credential hoặc ứng viên | Runtime và Setup |

51401 completed; 51402 yêu cầu legacy confirmation; 51403 input/ứng viên không
hợp lệ; 51404 username invalid; 51405 stale identity; 51406 quyền không đủ;
51407 xung đột migration/setup. CAS rehash trả outcome typed thay throw khi
không match. SQL547/2601/2627/229 giữ mapping G01; BLL dịch thông báo theo context.

### 3.1. Username và dữ liệu cũ

Username 1–50 ký tự ASCII [A-Za-z0-9._-], trim chỉ dấu cách ASCII ở hai đầu;
tab/newline/Unicode/space giữa bị từ chối. So sánh không phân biệt hoa/thường.
C# key Trim(' ').ToLowerInvariant(); SQL key LOWER(LTRIM(RTRIM(TenDangNhap)))
với collation Latin1_General_100_CI_AS cho lowercase rồi BIN2 cho unique key.
Password không trim, không đổi case; độ dài8–128, xác nhận bằng ordinal.

Migration preflight username cũ sau trim: ký tự/length, nhóm key trùng; có lỗi
rollback và báo mã account cần người cài xử lý, không log hash. Trim username
cũ hợp lệ, giữ case hiển thị. Thêm persisted LoginKey varchar(50) với unique
index BIN2, CHECK dữ liệu username normalized/ASCII theo DATALENGTH (trailing
spaces SQL không được dùng so sánh '=' đơn thuần). Giữ unique login baseline;
test trên collation case-sensitive/case-insensitive để key vẫn thống nhất. Không
ALTER toàn DB collation. Procedure auth truy vấn LoginKey, không SQL nối chuỗi.

### 3.2. Procedure/repository và quyền

- sp_AuthReadAccount(@LoginKey): trả MaTK/MaNV/TenNV/MaVT/HoatDong/DangLamViec/
  MatKhauHash/SecurityStamp. Hash chỉ về DAL/BLL xác thực tạm, không UI/session.
- sp_AppReadState(): trả State và schema version từ SchemaMigrations, không
  hash/ứng viên; runtime chỉ login khi Completed. LegacyReview phải qua setup
  xác nhận trước, không bypass gate vì thấy admin active. SystemInfo T07 dùng
  metadata này, không SELECT trực tiếp SchemaMigrations từ Form.
- TAIKHOAN thêm SecurityStamp rowversion; sp_AuthReadIdentity(@MaTK) chỉ trả
  identity/trạng thái/stamp, không hash. Lỗi DB không được coi là valid session.
- sp_AuthRehash(@MaTK,@ExpectedHash,@ExpectedStamp,@NewHash): UPDATE có CAS
  hash so sánh BIN2/stamp và account active/nhân viên đang làm; trả updated/new
  stamp hoặc Conflict. Không thay đổi mật khẩu mới khi CAS thua.
- SessionContext chứa identity, stamp, session ID/generation, cancellation;
  không password/hash. PermissionService recheck DB trước **mỗi** service
  read/write được bảo vệ và mỗi điều hướng, không cache quyền giữa lời gọi.
  Account khóa/nhân viên nghỉ/role hoặc stamp đổi → revoke và yêu cầu login lại.
  Không có quyền hoặc DB lỗi thì không gọi thao tác, không giả dữ liệu rỗng.
  Sau await read/recheck, service cũng kiểm generation trước trả kết quả; UI
  kiểm lại trước binding. Write đã gửi/commit có thể hoàn tất sau logout, chỉ
  chặn thao tác mới và UI success muộn, không giả rollback.
- Role BLL theo ma trận mục10; T08 bổ sung permission cụ thể ở plan T08. ADMIN
  không bỏ qua ràng buộc dữ liệu hệ thống. Không implement service giao dịch tương
  lai để test quyền; unit test permission và service hiện có là đủ.
- Runtime EXECUTE auth/identity/rehash; không SELECT toàn TAIKHOAN, không DML
  trực tiếp account/state. Hash không log ngay cả lỗi SQL. MaTK truyền từ phiên
  BLL, không tin MaVT request. Lớp BLL không chống người nắm credential SQL giả
  caller trực tiếp; giữ giới hạn bảo mật mục10, không tự thêm API server.

### 3.3. Login, CAS và logout

PasswordHasher verify Failed/Success/SuccessRehashNeeded. Hash placeholder hoặc
malformed trả thất bại an toàn, không exception ra UI. Account khóa/nghỉ từ chối.
Rehash needed: tạo hash mới, CAS; CAS thua → đọc account lại và verify password
vừa nhập với hash hiện tại, không retry ghi đè. Sai password mới → từ chối login;
đúng thì login với stamp mới. Success cũng đọc identity lần cuối để đối chiếu
stamp/role/trạng thái trước cấp session. Đổi dữ liệu tiếp sau đó bị recheck ở call.

Login một request tại một thời điểm; generation guard sau await trước mở phiên.
Logout revoke SessionContext/generation và cancel work; tham chiếu cũ giữ object
đã revoke, service call kế tiếp không dùng được. Kết quả async trả muộn bị bỏ,
không mở MainForm/login success của generation cũ. T07 đóng/dispose UI phiên.
Không tuyên bố cancellation rollback SQL đã commit.

## 4. Fixture, runner và kiểm tra kỹ thuật

Test-G02.ps1 tạo DB CoffeeManager_Test_G02_<run> với marker, schema baseline,
tuỳ fixture seed/no-seed, migration001→002 (→003 sau T08). Dùng guard tương tự
G01 trước mọi fixture/cleanup; tên DB không đủ nếu thiếu marker. Test impersonation
Pooling=false kế thừa B06; manual riêng dùng Windows runtime/setup identity thật.
Không thay engine. CLI log/TRX/stdout/stderr/exitCode và checksum tách setupError.
Runner nhận Task T06/T08, tag filter G02, không ghi đè biên bản G01.

Fixtures: clean, demo placeholders, legacy valid hash (tạo tại runtime), username
collisions/invalid, inactive employee/account, legacy old hash. Fault rollback
inject qua test-only trigger/constraint, không thêm failpoint production. Hai
connection cho bootstrap race/rehash race; inspect @@TRANCOUNT và toàn bảng sau lỗi.
Test-G02 unit phần Auth/Permission trước SQL; integration thật và UI trong đợt
nghiệm thu được duyệt. Kiểm tra kỹ thuật lúc triển khai: build, apply002 DB test,
smoke một setup/login để T07 nhận dependency; ghi riêng, không PASS nghiệm thu.

## 5. Ma trận nghiệm thu sau duyệt đợt test

Mỗi dòng dưới là subcase bắt buộc, có actual/status/evidence riêng khi chạy.
U/I/M giữ ý nghĩa kế hoạch; các dòng gốc T06-K01–K10 được giữ trong requirements.

| ID | Fixture / thao tác | Expected |
|---|---|---|
| T06-K01.a | Clean, nhập tối thiểu/SDT NULL/password tại máy | Một NV/account ADMIN, CVADM/role hợp lệ, Completed cùng commit, plaintext không lưu |
| T06-K01.b | Demo chọn NV001/admin | Chỉ account được chọn bật và hash hợp lệ; account demo khác giữ nguyên |
| T06-K01.c | Legacy admin valid, chưa state | LegacyReview; verify password hiện tại rồi Completed, không đổi hash/lương |
| T06-K01.d | Legacy invalid/inactive/employee nghỉ | Không tự reset/kích hoạt, hướng dẫn recovery riêng |
| T06-K02.a | Hai session setup đồng thời | Đúng một commit, không trùng NV/account, session sau bị chặn |
| T06-K02.b | Inject lỗi sau ghi account trước Completed | Toàn transaction rollback, không dấu Completed/NV dư; retry hợp lệ thành công |
| T06-K02.c | Bootstrap lần2 rồi restart exe | Bị chặn, hash không đổi |
| T06-K02.d | Khóa/nghỉ admin sau Completed | Bootstrap vẫn chặn |
| T06-K02.e | Runtime gọi procedure setup / timeout sau commit | Quyền229; timeout phải đối chiếu State, không reset/tạo lặp |
| T06-K03.a | Password đúng/sai, login lạ | Chỉ đúng thành công, không leak hash/exception |
| T06-K03.b | Placeholder và malformed hash | Thất bại an toàn |
| T06-K03.c | Password có space ngoài | Không trim; giá trị khác không đăng nhập được |
| T06-K04.a | Account0 / employee0 trước login | Từ chối cả hai riêng biệt |
| T06-K04.b | Khóa/employee nghỉ giữa phiên | Call tiếp theo revoke, không chạy nghiệp vụ |
| T06-K05.a | Unit verify SuccessRehashNeeded | Hash mới verify được, không plaintext |
| T06-K05.b | Integration old hash→login→đọc DB | Hash được nâng thực sự, stamp mới, identity đúng |
| T06-K05.c | Hai session đọc oldhash rồi đổi password trước CAS | CAS không ghi đè newhash; password cũ không cấp session |
| T06-K06.a | Unit toàn ma trận quyền nhóm thao tác3role | Allow/deny đúng, không cần UI |
| T06-K06.b | Đổi role giữa phiên, direct service read/write | Phiên cũ bị revoke; login lại có quyền mới, không dựa menu |
| T06-K07.a | Login/setup UI và log/session snapshot | Password che; không secret/hash/password trong file/log/session |
| T06-K08.a | Logout, gọi service qua object phiên cũ | Bị chặn trước nghiệp vụ |
| T06-K08.b | Logout khi login/read async chậm rồi unlock | Kết quả muộn không mở MainForm/phiên cũ |
| T06-K09.a | Kết nối hỏng lúc login/recheck | Lỗi DB riêng, không sai password giả/success; giữ input |
| T06-K10.a | Username1/50/51, ngoài charset, trim/case | Đúng giới hạn, UI/C#/SQL cùng quy tắc |
| T06-K10.b | Trùng case/key và migration username cũ va chạm | SQL chặn; migration rollback không tự rename |
| T06-K10.c | DB test collations CI/CS và login case khác | LoginKey bảo đảm cùng uniqueness/case behavior |

Actual/status chưa có: toàn bộ NOT RUN cho đến đợt nghiệm thu. T07 bổ sung UI
session lifecycle; T08 kiểm đầy đủ quyền danh mục. Chỉ hoàn tất khi đủ đầu ra,
10 ID gốc và tất cả subcase PASS; không nghiệm thu từ kiểm tra kỹ thuật.
