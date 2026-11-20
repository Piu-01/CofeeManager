# Điều tra luồng chạy và tác động môi trường CoffeeManager — 18/10/2026

## Đã xác minh phiên F5 sau khi người dùng giữ cửa sổ mở

Process3928, tạo18/10/2026 13:53:27, chạy executable tại
`C:\Users\Lenovo\AppData\Local\Temp\opencode\G02B03B06DesignerRound2\src\CafeManager.WinForms\bin\Debug\net10.0-windows\CafeManager.WinForms.exe`.
Module CafeManager.WinForms.dll và các DLL Business/DataAccess/Models đều được
nạp từ bản sao này. DLL UI có hash
`F0AA34ADBF6004665E096B84C45B416F75A531DA72918DA59852F8E034CFB9AD`,
không có metadata OrbitContentHost. Đây là bằng chứng trực tiếp cho AUD18-05:
phiên F5 đang chạy bản sao Designer test trong Temp, không phải source ổ D:.

Build lại source ổ D: không cập nhật DLL của bản sao Temp. Chọn Startup Project
đúng tên vẫn không đổi solution/project path đang mở. Hướng xử lý là dừng phiên
debug và mở đúng `D:\Workspace\Nam_3\HK1\CoffeeManager\CafeManager.slnx`.
Chưa tự đóng cửa sổ, đổi solution hoặc xóa bản sao; chưa thay cấu hình DB.
Phần ghi chưa xác định exe bên dưới là lịch sử trước khi bắt được process này.

## Kết luận hiện tại

Điều tra chỉ đọc source/tài liệu/metadata Windows và SQL Server. Không chạy lại
test, không tạo DB/tài khoản, không sửa cấu hình, không đổi mã ứng dụng, không xóa
dữ liệu hoặc sửa kế hoạch/checklist. Đã tạo hồ sơ và script SELECT kiểm kê bên cạnh.

Đã xác minh khoảng cách giữa nghiệm thu kỹ thuật trên fixture và bàn giao để người
dùng chạy trực tiếp bằng Visual Studio. Việc chọn CafeManager.WinForms làm Startup
Project là đúng; thao tác đó không tự cấu hình DB hoặc khởi tạo tài khoản ứng dụng.

| ID | Kết luận | Chủ sở hữu / trạng thái |
|---|---|---|
| AUD18-01 | Thiếu đường chạy development có cấu hình DB và first-admin rõ ràng; Login dừng trước authentication | T06, xác minh từ source + môi trường |
| AUD18-02 | Seed có3 account nhưng inactive/hash placeholder; thiếu bàn giao account demo3 role dùng được | T06, xác minh; quản trị account đầy đủ vẫn thuộc T24 |
| AUD18-03 |112 DB test còn trên instance,4480 MiB cấp phát; runner cố ý giữ DB mỗi lượt, thiếu vòng đời dọn | T05, xác minh bằng SELECT |
| AUD18-04 | CafeManagerApp là local Windows user vẫn enabled, có SQL Windows login và1 mapping DB test G01 | T03/T05, xác minh; chưa thay đổi |
| AUD18-05 | Ảnh Login nâu–kem không khớp source/DLL chuẩn Orbit hiện có; chưa xác định executable của ảnh | T07, chưa đủ bằng chứng root cause |
| AUD18-06 | Ẩn mật khẩu có trong source; ảnh mới để trống nên chưa chứng minh mask bị lỗi | T06, chưa xác nhận bug runtime |

## 1. Rà soát kế hoạch T01–T32

Đã đối chiếu kiến trúc/phạm vi mục1–11, bảng nhóm G01–G08, mô tả/phụ thuộc/đầu ra
T01–T32 và trạng thái mục12–13. Những điểm trực tiếp ảnh hưởng phản ánh:

- **T02** là SQL baseline/schema/seed/trigger/procedure. **G02** là nhóm gồm
  T06–T08; T06 mới là login/logout/session/permission/bootstrap. Nếu người dùng
  nói G02 đã xong auth thì đúng theo báo cáo hiện tại; không nên lẫn nhóm với task.
- T06 đã lên kế hoạch xác thực, first-admin và quyền. T07 là shell/menu; T08 là
  danh mục. CASH/STORE tại G02 chỉ có màn phiên, chưa có POS/kho T16/T20.
- T24, phụ thuộc T23, mới có tạo/reset/khóa account nhân viên. Không có public
  registration trong kế hoạch. Việc chưa có màn quản trị account hiện nay đúng
  thứ tự backlog, nhưng **không giải quyết nhu cầu chạy thử3 role ở G02**.
- T30–T32 có cài sạch/publish/bàn giao đầy đủ ở cuối. Tuy nhiên mỗi nhóm trước đó
  vẫn cần một lối chạy development ổn định; không thể dùng việc T32 chưa đến để
  coi bàn giao Login không dùng được là đủ.
- G02 được ghi PASS27/27 trên commit1ab09af, với fixture, quyền và process được
  chuẩn bị riêng. Kết quả đó không tự cấu hình Visual Studio của người dùng và
  không đồng nghĩa DB phát triển/tài khoản demo đã được bàn giao. Đợt Orbit sau
  đó còn các ca chưa nghiệm thu; không áp dụng PASS cũ cho mọi source UI mới.
- Kế hoạch yêu cầu DB test riêng nhưng không chốt ngân sách lưu giữ/số lượng,
  DB phát triển cố định, người sở hữu cleanup và kiểm kê tác động Windows sau mỗi
  đợt. Đây là thiếu sót quy trình vận hành, không phải lý do bỏ kiểm thử giao dịch.

Đề xuất giữ32task/325ca và bất biến nghiệp vụ. Bổ sung gate bàn giao chạy được
từng nhóm, tài khoản demo phát triển và lifecycle test trong hồ sơ hiện hành;
không chuyển toàn bộ module nhân sự T24 lên trước hoặc mở tính năng ngoài scope.
Chưa sửa nội dung/checkbox kế hoạch trong lượt điều tra.

## 2. Authentication, authorization, validation và tài khoản

- Authentication: xác minh tên/mật khẩu qua hash, account hoạt động, nhân viên
  đang làm, trạng thái first-admin Completed, sau đó tạo session.
- Authorization: ADMIN/CASH/STORE được làm gì; PermissionService kiểm lại
  account/role/security stamp ở BLL, không chỉ dựa vào menu ẩn.
- Validation: kiểm định dạng/độ dài/dữ liệu trước xử lý. Username1–50 ASCII theo
  PasswordService; mật khẩu bootstrap8–128 và xác nhận khớp. Không tự đăng ký,
  cấp account hoặc tạo cấu hình SQL khi validate input.
- ADMIN đầu tiên được người cài thiết lập bằng bootstrap1 lần. Sau đó ADMIN quản
  lý account nhân viên ở T24. Không phải mọi role đều tạo được role thấp hơn;
  CASH và STORE không có quyền quản trị account. App nội bộ không public signup.
- App user trong TAIKHOAN **khác** Windows user và SQL principal. Không cần một
  Windows account cho mỗi nhân viên/role ứng dụng.

Seed `database/Insert_Data.sql:454–457` có admin/ADMIN,thungan/CASH,kho/STORE;
cả3HoatDong0 và hashplaceholder. Auth contract cố ý chỉ bootstrap ADMIN, không
tự bật CASH/STORE. Test fixture tạo mật khẩu riêng để kiểm cơ chế không thay cho
account demo có hướng dẫn sử dụng. Không có password mặc định để đưa cho người dùng.

## 3. Vì sao F5 dừng ở Login

`Program.cs:17` chỉ đọc CAFEMANAGER_APP_CONNECTION từ môi trường process.
Thiếu giá trị dẫn tới thông báo đúng như ảnh và repository/auth=null.
`LoginForm.cs:25` đặt Enabled=false cho nút; handler còn guard authnull ở dòng30.
Vì vậy chưa chạy validation/authentication khi nút vô hiệu. Đây không phải bằng
chứng validation thành công hoặc đăng nhập sai mật khẩu.

Trong môi trường điều tra, biến không có ở Process/User/Machine; không có
Properties/launchSettings.json trong project. Không đọc được môi trường bên
trong devenv bằng phép kiểm tra biến của process audit; **thông báo trong ảnh**
mới là bằng chứng nhánh thiếu cấu hình của process app đã chạy.

Solution tham chiếu đúng src/CafeManager.WinForms. Instance SQL có DB cũ
QuanLyCafe_ERP tạo24/05/2026; SELECT metadata4 tên bảng chỉ trả NHANVIEN, không
TAIKHOAN/AppSetupState/SchemaMigrations. Không thấy QuanLyCafe_ERP_V2 trên
instance này. Chưa có cơ sở tự chọn DB cũ làm đích hoặc chạy migration vào đó.
Không đọc dữ liệu nghiệp vụ của DB cũ, không đưa nó vào danh sách dọn.

## 4. Password masking và ảnh giao diện

`LoginForm.Designer.cs:43` đặt UseSystemPasswordChar=true, checkbox mặc định
không tick. Handler LoginForm.cs:50 đặt mask=!showPassword.Checked.
Ảnh trước có tick Hiện mật khẩu nên plaintext là hành vi toggle; ảnh mới các
trường trống, không thể dùng để kết luận mask hỏng. Không sao chép thông tin tài
khoản từ ảnh, không tự nhập vào dialog xác thực.

DLL chuẩn Debug18/10/2026 13:13:46 và Release13:14:04 đều có metadata lớp
OrbitContentHost. Source hiện dùng card Orbit; ảnh lại nâu–kem cũ. Lúc kiểm kê
process chỉ thấy devenvPID7712, không thấy CafeManager.WinForms/UiLauncher.
**Chưa xác minh đường dẫn exe của ảnh hoặc source solution thực sự được F5**.
Kết luận trước rằng chắc chắn chỉ do build cũ là chưa đủ bằng chứng; build lại
đã được làm nhưng chưa phải xác minh runtime của chính phiên Visual Studio này.
Đã đề nghị người dùng giữ cửa sổ F5 mở để đối chiếu process, không yêu cầu password.

## 5. DB test còn lại

SELECT sys.databases/sys.master_files và marker trực tiếp:

- 112 DB mang prefix CoffeeManager_Test_:93 G02 và19T02/T03/T05.
- 4480 MiB cấp phát data+log, tương đương khoảng4,38 GiB. Đây là file allocation,
  không phải lượng dữ liệu nghiệp vụ hoặc RAM sử dụng.
- 93 G02 có G02TestMarker;39 markerRunId khớp nguyên tên DB. Các lượt supplemental
  có marker generic: ví dụ DB20261009_214237_4969002a mang RunId SUPPLEMENTAL.
  Marker không khớp không tự chứng minh DB lạ, nhưng không đủ cho auto-delete
  theo phép kiểm exact-run. Chưa đánh dấu bất kỳ DB nào là đã được phép xóa.
- Không còn CMG02 temporary Windows-user mapping trong112 DB theo bộ lọc audit.
- 1 mapping CafeManagerApp_Runtime ở
  CoffeeManager_Test_T05_20261008_225305_cebd98ad.

`tools/database/Test-G02.ps1` tạo3 DB mỗi lượt All và ghi rõ giữ DB để đối chiếu.
Các lượt bổ sung tiếp tục tạo DB. Finally dọn connection/process environment,
không DROP DATABASE. Vì vậy nhiều lượt PASS vẫn để lại DB là hệ quả trực tiếp
của runner, thiếu bước lưu giữ/dọn được bàn giao. Cleanup fixture/transaction
khác cleanup toàn bộ DB và tài khoản máy.

Danh sách đầy đủ: database-inventory.csv; tổng hợp: database-summary.json.
Script Read-TestDatabaseInventory.ps1 chỉ SELECTmetadata/marker, không chạy test
hay DDL. Không gọi runner để tái hiện, tránh sinh thêm DB trong cuộc điều tra này.

## 6. Windows CafeManagerApp và các tác động đã kiểm kê

Đã đọc bằng cmdlet Windows và SQL metadata:

- Localuser CafeManagerApp enabled, nhóm Users; không nằm Administrators.
- SQL Windows login cùng tên tồn tại, create_date08/10/2026 23:56:55,sysadmin0,
  không explicit server-role membership trong kết quả audit.
- PasswordRequired=False là **cờ chính sách**, không chứng minh mật khẩu rỗng.
  Không thử đăng nhập, đọc/reset mật khẩu.
- G01 manual-checklist/report ghi tạo account này để kiểm runtime khôngsysadmin
  và chạy RunAs, trước G02. Ngày đặt mật khẩu08/10và SQL login phù hợp mốc đó.
  Không lấy được event4720phù hợp để xác nhận actor tạo account; không kết luận
  ai đã gõ lệnh chỉ từ hồ sơ dự án.
- G02R7 dùng2 account khác CMG02S_d4434b36/CMG02R_d4434b36. Các account và login
  CMG02 không còn trong kết quả audit. Báo cáo dọn account tạm G02 không bao gồm
  account CafeManagerApp đã giữ từ G01; không đồng nghĩa cả máy không còn account dự án.
- CafeManagerApp trên màn đăng nhập Windows phù hợp việc nó là localuser enabled;
  đây không phải chức năng đăng ký account nhân viên của ứng dụng.
- Không thấy service/scheduledtask có tên khớp CafeManager/CoffeeManager/G02.
  Đây là kiểm tra theo tên, không là rà soát mọi thành phần hệ thống.
- Có .NETCore/WindowsDesktop10.0.13 cùng10.0.12, và8.0.31. Hồ sơ G02 ghi đã cài
  runtime10.0.13 do PKCS; đây là tác động ngoài thư mục source đã được ghi nhận.
- Có config/local/Run-ManualApp.ps1 và Start-ManualApp-WithPrompt.ps1 từ G01;
  không đọc hoặc xuất bí mật. Biến kết nối app không được thấy lưu User/Machine.

Có bằng chứng nguồn gốc liên quan thiết lập dự án; không có cơ sở từ cuộc điều
tra này để gọi account đó là mã độc. Đồng thời không khẳng định đã kiểm tra an
toàn toàn máy. Không thay firewall/antivirus/logon/registry hoặc gỡ runtime.

## 7. Đợt khắc phục đề xuất — chưa thực hiện

1. Chốt1 DB development/demo cụ thể, tách DB cũ và DBtest. Không tự migration vào
   QuanLyCafe_ERP. Chốt account SQL/runtime/setup và local config để F5 chạy đúng,
   không hard-code connection/password và không fallback sa/sysadmin.
2. Bàn giao luồng first-admin +3 account demo đúng hash, người dùng đặt password
   tại máy. Giữ phân quyền BLL; không thêm public signup hoặc reset bootstrap.
3. Đối chiếu exact project/exe khi F5 và kiểm bằng chính luồng chạy đó; xác minh
   mask/validation/login/logout với DB đã chốt, không chỉ screenshot fixture.
4. Lập allowlist dọn từ112 DB, ghi DB giữ lại/DBxóa và kiểm marker/lịch sử/dependency;
   giữ log bằng chứng, hỏi về dữ liệu đã nhập thêm trước khi DROP. Không dùng lệnh
   xóa wildcard hoặc chạy lại seed lên DB có dữ liệu.
5. Sau khi chọn phương án runtime, xử lý mapping→SQLlogin→Windowsaccount
   CafeManagerApp theo kế hoạch được duyệt; kiểm tra phụ thuộc và profile trước
   khi vô hiệu/xóa. Không ẩn account bằng registry để che tác động.
6. SQL instance hiện MixedMode (IsIntegratedSecurityOnly=0), nên **có phương án**
   SQLlogin riêng giới hạnDB mà không thêm Windowsuser. Đó là đề xuất đổi cách
   cài/setup đã chốt, cần duyệt và cập nhật contract, chưa áp dụng trong lượt này.
7. Chốt retentiontest (chỉ giữ các lượt cần điều tra), inventory và cleanup report;
   thay gate bàn giao bằng thao tác chạy được từ Visual Studio +3 role thay vì chỉ
   số lượng PASS. Không chạy lại toàn bộ hàng trăm test khi chỉ cần xác minh cấu hình.

Rủi ro: DROP DB mất dữ liệu; xóa Windows/SQLprincipal có thể làm hỏng RunAs, config,
ACL/profile hoặc kết nối còn dùng. Đổi setupconnection là đổi contract cài đặt,
không làm dưới danh nghĩa sửa UI. Phải duyệt cả đợt với đích/danh sách cụ thể.

Không có thao tác khắc phục nào được thực hiện trong lượt điều tra này. Kết quả
read-only không thay nghiệm thu Txx-Kxx. Lịch sử test/bug cũ được giữ nguyên.

## Dọn môi trường được duyệt — 18/10/2026

Người dùng chọn rõ: xóa cả DB test và Windows account CafeManagerApp.
Đã đối chiếu đúng 112 tên trong database-inventory.csv với SQL metadata trước DROP;
không dùng wildcard làm lệnh xóa, không tạo fixture hoặc chạy lại seed.

- Đã DROP 112 DB test; sau kiểm tra còn **0** DB CoffeeManager_Test_*.
- Đã DROP SQL Windows login LAPTOP-79UNRIRC\CafeManagerApp (mapping nằm trong DB đã xóa).
- Đã xóa local Windows account CafeManagerApp, SID kết thúc 1007. Lần đầu Access denied;
  sau đó thao tác riêng qua UAC Administrator thành công, đã kiểm tra account không còn.
- Không thấy service/task/session hoạt động phụ thuộc account trước xóa; không có DB
  do login này sở hữu, không thấy static dependency/user mapping trong các DB giữ lại online.
- QLBANHANG đang RESTORING: không kiểm tra được dependency bên trong; giữ nguyên trạng thái.
  Kiểm tra static metadata không bao phủ dynamic SQL hay cấu hình bên ngoài máy.
- CoffeeManager_Development và QuanLyCafe_ERP vẫn ONLINE. Tool --verify xác nhận
  DPAPI config, login/permission/session của admin ADMIN, thungan CASH, kho STORE đều PASS.
- Không xóa thư mục profile, sửa registry, gỡ runtime hay đổi source logic ứng dụng.
- Bằng chứng: cleanup-actions.txt, cleanup-windows-result.txt, hai script dọn cạnh báo cáo.
  Script dọn là one-time allowlist theo inventory lịch sử, không dùng như runner định kỳ.

Nội dung điều tra và đề xuất phía trên là lịch sử trước khắc phục; mục này cập nhật
trạng thái dọn hiện tại. Không thay đổi các kết quả test/bug lịch sử T01–T32.
