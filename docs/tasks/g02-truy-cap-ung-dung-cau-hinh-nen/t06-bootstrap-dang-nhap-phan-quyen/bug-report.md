# T06 — Bug mới từ đợt nghiệm thu bổ sung

**Trạng thái cuối17/10/2026:** B03/B04/B05/B07 đã được duyệt, sửa vàregression
PASS; khôngcònbugmở. WindowsR7setup/runtime/popup/restart cũngPASS,khôngcòn
blockerUAC. Xem`test-results.md`/`../acceptance-matrix.md`; lịch sử dưới giữnguyên.

## G02-B07 — SQL từ chối username có gạch dưới/gạch ngang

**17/10/2026: người dùng đã duyệt B07; đã sửa, regression PASS.**

Sửa đúng bốn pattern bằng cách đặt `-` đầu bracket. Thêm262test kiểm toàn bộ
ASCII ở auth/legacy-read và CHECK, cùng6Unicode ngoài ASCII ở boundaryNVARCHAR.
Runner `20261017_155948_e814a21e`:484/484PASS. UI mới
`G02_B07_UI_REGRESSION_20261017`:15/15PASS, gồm ca mixedcharset từng FAIL.
`G02_B07_SQL_SUPPLEMENT_20261017`:83/83PASS (69invalid bootstrap không ghi
account/NV/state, transaction0:0;12valid bootstrap/login và2legacy preflight/
confirmation trên CI/CS). Dùng DB mới, không đổi checksum DB cũ. Manifest99file
không đổi trong lượt; Windowsidentity thật còn chờ xác nhận UAC, chưa chốtG02.

- Ca T06-K10.a: UI đăng nhập `aB._-19` (có khoảng trắng ASCII ngoài), account
  fixture là `Ab._-19`. C# chuẩn hóa hợp lệ nhưng UI trả lỗi username từ SQL,
  không cấp session. Các ca UI length1/50 và native input limit50 đã PASS.
- Bằng chứng: `../runs/G02_FINAL_BOUNDARY_DIAG_20261017/results.json` và
  `sql-charset-investigation.json`; DB test có marker
  `CoffeeManager_Test_G02_20261016_223719_4d0416e5`.
- SELECT trên SQL Server thật, BIN2: pattern `'%[^A-Za-z0-9._-]%'` cho kết quả
  invalid=1 riêng `_`, `-` và chuỗi `ab._-19`. Không phải lỗi trim hoặc mật khẩu.
  Đặt `-` đầu bracket: `'%[^-A-Za-z0-9._]%'` cho valid=0 với cả ba, còn `/`
  và space vẫn invalid=1. Chỉ chạy SELECT đối chiếu, chưa thay object SQL.
- Nguồn ảnh hưởng: migration002 dòng7 preflight, dòng19 CK_TK_Username,
  dòng54 auth-read và dòng131 bootstrap. B03 đã sửa truncation nhưng chưa kiểm
  đủ ký tự hợp lệ; không được suy PASS charset từ ca alphanumeric/dot.
- Đề xuất sửa cả bốn pattern, giữ nguyên contract ASCII1–50 và cách trim/case;
  kiểm bootstrap/login/legacy preflight/check constraint trên DB test mới,
  CI/CS, từng ký tự hợp lệ và nhiều invalid/control/Unicode; giữ raw FAIL.
  Migration002 chưa phát hành: không rewrite checksum của DB cũ.
- Rủi ro: nới sai bracket có thể nhận ký tự ngoài whitelist hoặc vẫn chặn
  migration legacy hợp lệ. Cần đối chiếu toàn bộ ASCII và Unicode boundary,
  regression auth/atomic bootstrap; không cần đổi schema/cột/API/UI contract.


Trạng thái: **B03/B04/B05 đã sửa, regression PASS ngày16/10/2026**.
Người dùng đã duyệt đợt B03–B06. Giữ nguyên bằng chứng trước sửa bên dưới;
không dùng kết quả này thay các caWindowsidentity/setup-popup cònBLOCKED.

## Kết quả đợt sửa đã duyệt

- B03: migration002 nhậnUsername/LoginKey bằngNVARCHAR(MAX), kiểmASCII/length
  vàcanonicalspace trướcconvertVARCHAR50; lookup dùng biếnVARCHAR50 để giữ
  kiểu so sánh vớiLoginKey. Không nới cột/key hoặc đổi normalization nghiệp vụ.
- B04: Login8row có7AutoSize+1Percent, columnPercent; không cònserialize20px.
- B05: Bootstrap assignment đơn và9row tường minh, giữ mode/fields/handler.
- Runner cuối `../runs/20261016_211207_a26a8e4f/`:222/222PASS, exit0,
  sourceunchanged=true;26test mới kiểm auth/legacy-read SQLboundary.
- `../runs/G02_B03_BOOTSTRAP_20261016/`:17/17PASS, gồm13invalid input
  (51/1001/Unicode/fullwidth/null/space/control) không tạoNV/account/CV hoặc
  Completed,transaction0:0;4validlength/collationCI-CS loginđúngcase/trim.
- `../runs/G02_B03_AUTH_SUPPLEMENT_20261016/`:28/28PASS, clean/demo/existing/
  legacy/rehash/role/revoke vàCI-CS trênDBtest mới dùng002sửa.
- Designer cuối `../runs/G02_B03_B06_DESIGNER_FINAL_CONFIRMED_20261016/`:
 5surface mở/save/close/reopen; ảnh đã xem, không dựa vào captionDesign.
- Binary sauSave đượcbuild/chạy vàđốichiếuSHA256. NativeLogin/Bootstrap,
  editor/keyboard/viewport tại96/120/144DPI đềuPASS ở3lượtFINAL_LAYOUT,
  mỗi12/12case; CRUD7kind sauSave23/23PASS. Chi tiếtbiên bảnT07/T08.
- Manifest98file không đổi tronglượt cuối tại
  `../runs/G02_B03_B06_HANDOFF_20261016/integrity.json`.
  Migration002sửa chỉáp dụngDBtest mới, khôngrewritechecksumDBcũ.

## G02-B03 — SQL bootstrap cắt username51 và hoàn tất thiết lập

- Ca: T06-K10.a.SQL.usernameLength51. Expected: từ chối input51, không tạo
  account/NV/Completed. Actual: tạo admin với username50, stateCompleted.
- Evidence: `../runs/G02_FINISH_DATA_20261011/results.json`; DB riêng có marker
  `CoffeeManager_Test_G02_20261011_173517_c4978bd4`. Probe bind VarChar(max)
  để gửi nguyên input51, password/hash sinh trong RAM, không ghi vào evidence.
- Nguyên nhân xác nhận: `002_auth_and_bootstrap.sql:109` khai báo
  `@Username varchar(50)`; SQL cắt giá trị trước validation DATALENGTH tại123.
  C# chặn51 nhưng contract yêu cầu cả SQL, không được coi C# là bằng chứng SQL.
- Đề xuất: input procedure đủ rộng, chuẩn hóa/kiểm ASCII và length trước ghi;
  giữ cột/key50 và uniqueness/collation; kiểm các boundary auth cùng nguyên nhân.
  Migration002 chưa phát hành: kiểm trên DBtest mới, không đổi checksum DB cũ.
- Regression: username1/50/51, ngoài charset/space/case ở UI/BLL/SQL;
  clean/demo/legacy, atomic Completed, concurrent/rollback/restart và login.
- Rủi ro: không để tên login bị đổi ngầm, không làm lệch key giữa read/setup;
  không nới cột hay điều kiện xác nhận legacy để né lỗi.

## G02-B04 — Login Designer Save làm mất bố cục runtime

- Ca: T07-K04.c.Login, sở hữu source T06. Mở đúng solution của bản sao rồi
  Save→close→reopen; serializer thêm8RowStyleAbsolute20 vào TableLayoutPanel.
  Build bản sauSave thành công nhưng nút login chỉ12px, font23px, chữ bị cắt.
- Evidence: `../runs/G02_DESIGNER_SOLUTION_20261011/LoginForm-designer.png`,
  `../runs/G02_DESIGNER_AFTER_SAVE_20261011/bounds.json` và ảnh runtime sauSave.
  Bản sao tại Temp/G02DesignerFinal; source làm việc không bị ghi đè bởi IDE.
- Nguyên nhân: LoginForm.Designer.cs khai báo8row nhưng không có RowStyles
  tường minh. Runtime tự tính kích thước; Designer dựng mặc định20px rồi lưu
  các giá trị đó. Native sauSave xác nhận RowHeights20, ButtonHeight12.
- Đề xuất: RowStyles AutoSize/Percent phù hợp từng hàng và ColumnStyles rõ;
  giữ handler/TabIndex/theme/masking, không hardcode chiều cao để che lỗi.
- Regression: Designer save/close/reopen thật, diff/hash/binary sauSave và UI
  Login runtime trước/sauSave tạiDPI100/125/150; keyboard/auth giữ nguyên.
- Rủi ro: serializer thay AutoScaleDimensions theoDPI; cần kiểm bounds thật,
  không copy mù file được IDE serialize tại125% vào source96DPI.

## G02-B05 — Bootstrap Designer không đọc được chained assignment

- Ca: T07-K04.c.Bootstrap, sở hữu source T06. Designer báo lỗi tại
  BootstrapForm.Designer.cs:38, không mở được surface để save round-trip.
- Evidence: `../runs/G02_DESIGNER_SOLUTION_20261011/BootstrapForm-designer.png`.
  Đã mở đúng solution, không phải lỗi mở nhầm file ngoài project của các probe cũ.
- Nguyên nhân: assignment nhiều control lồng nhau tại38 và các dòng tương tự
  trong InitializeComponent không được WinForms Designer xử lý.
- Đề xuất: chuyển các chained assignment trong initializer thành assignment
  đơn cho từng control, không thay nghiệp vụ; rà các cú pháp cùng initializer
  thực sự làm parser dừng. Không refactor module ngoài phạm vi.
- Regression: mở/save/close/reopen Bootstrap và chạy setup clean/demo/legacy,
  masked inputs, mode/candidate, loading và completion/timeout. Chụp native.
- Rủi ro: giữ nguyên thứ tự khởi tạo, field/mode và event; không đưa truy vấn
  SQL vào parameterless constructor để giúp Designer.

## Phân biệt lỗi harness

Caption `[Design]` không chứng minh surface mở thành công. Probe typed ghi
OBSERVED nhưng screenshot thể hiện error page: kết quả cuối phải FAIL, không
nâng PASS theo nhãn probe. Các lượt trước mở file ngoài project là lỗi điều
khiển IDE, không phải nguyên nhân B04/B05. Bug được xác nhận sau khi mở đúng
solution và chạy bản clone sauSave. Ca setup clean còn timeout trong harness
MessageBox, DB đãCompleted; chưa đủ bằng chứng để thêm bug runtime setup.

## Điều tra bổ sung 18/10/2026 — chưa sửa

Người dùng báo không chạy được luồng đăng nhập từ Visual Studio và tác động môi
trường ngoài ý muốn. Hồ sơ điều tra chỉ đọc, mapping từng finding về task và hướng
khắc phục: [báo cáo toàn đợt](../../../bugs/project-environment-audit-20261018/report.md).

PASS cũ giữ làm lịch sử của bản/fixture đã nghiệm thu; không thay bằng chứng chạy
F5 hiện tại. Đợt này không chạy test hoặc sửa source/migration/configuration.
Các thao tác khắc phục/xóa DB hoặc tài khoản Windows chưa được thực hiện.

## Bổ sung 18/10/2026 — nhận tài khoản demo ngay trên Login

Người dùng duyệt sau phản ánh không muốn dùng terminal để tìm tài khoản.
Nguyên nhân: tool DevelopmentSetup chỉ hiển thị mật khẩu qua console; Login không
có đường nhận credential demo đã provision. Ba account đã tồn tại, không phải thiếu seed.

Hướng sửa đã triển khai: Login development có ba lựa chọn vai trò, đọc đúng ba
mật khẩu ứng dụng từ DPAPI CurrentUser và điền vào form; người dùng bấm Đăng nhập
qua AuthService như cũ. Không tự đăng nhập, không reset password, không ghi SQL,
không tạo public signup hay thay module account T24. Cờ chỉ được truyền khi Program
thực sự chọn DevelopmentConnection; env connection explicit không bật lựa chọn demo.
Thiếu/hỏng DPAPI báo lỗi tại Login, không xuất exception/secret. Mật khẩu mặc định che.

File: Configuration/DevelopmentAccounts.cs, Program.cs, ApplicationSessionContext.cs,
Forms/LoginForm.cs, README.md. Kiểm tra compile Debug/Release ghi tại
`development-login-ui-results.md` ở thư mục G02. GUI chưa chạy không coi PASS.

## Thay thế phương án chọn account — 18/10/2026

Người dùng bác bỏ nút chọn vai trò, duyệt bàn giao Login thông thường và file mock
có tên/mật khẩu rõ ràng. Phương án GUI chọn account ở mục trước được gỡ bỏ hoàn toàn.
Đã tạo database/demo/accounts.json và README người dùng; đồng bộ hash đúng ba account
trong DB development có marker bằng lệnh operator --apply-mock-accounts. Không đổi baseline,
migration, BLL/DAL, role hoặc dữ liệu nghiệp vụ. Ngoại lệ mật khẩu demo công khai được
người dùng yêu cầu rõ; bí mật SQL/runtime vẫn DPAPI, không xuất ra file mock.
