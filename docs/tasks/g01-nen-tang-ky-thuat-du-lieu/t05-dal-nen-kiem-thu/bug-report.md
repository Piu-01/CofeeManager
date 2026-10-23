# T05 — Bug đợt test G01_20261008_220313

## G01-B10 — Save Designer làm mất cấu hình grid và đổi layout

**Mới nhất sau duyệt:** Đã sửa cột/style tường minh, filter, root runtime và
ErrorProvider/container. Regression bản sau Save **PASS**: Debug/Release
0 warning/0 error, Designer round-trip giữ source, DPI/bàn phím/minimize/scroll
đạt. Biên bản [b10-test-results.md](b10-test-results.md). Mô tả chờ duyệt dưới
đây giữ lịch sử lúc phát hiện.

**Trạng thái:** Phát hiện trong regression Designer B09 đã duyệt; chờ duyệt
đợt bug bổ sung B10, chưa sửa sau phát hiện. Sở hữu T05 UI mẫu; ảnh hưởng T04 Designer.

**Bằng chứng:** Người dùng cho phép nạp lại file từ đĩa. Designer nạp lại,
Save/close/reopen được nhưng viết lại StartupForm.Designer.cs và StartupForm.resx
(manifest R3 có 2 file đổi). Lượt mới `G01_B09_Designer_20261009_131956` giữ
source cố định, build Debug 1 warning CS0649, 0 error. Ảnh runtime 100% hiển thị
header MaBan/TenBan/MaKV/TrangThai/MaHD, màu chọn xanh mặc định; ảnh 150%
1280×720 chỉ còn header, nhãn bộ lọc wrap và chiếm thêm chiều cao.
Trước Save, ảnh R3 có header tiếng Việt và màu guide, có một dòng grid ở cùng
1280×720/150%. Các ảnh là dữ liệu demo, không phải kết quả SQL.

**Nguyên nhân có bằng chứng:** File được serializer viết lại có AutoScaleDimensions
120 thay 96, ClientSize 1250×750 thay 1000×600; các size/padding thay theo Designer.
ColumnHeadersDefaultCellStyle hiện trỏ tới style rỗng; tên header hiện tự sinh
từ DTO; các font/style trước đó không được giữ đầy đủ. `components` khai báo
nhưng không được gán, sinh CS0649. Chưa kết luận lỗi framework; cần kiểm cấu trúc
InitializeComponent trước đây có được serializer hỗ trợ đầy đủ hay không.

**Hướng sửa đề nghị:** Đưa cấu hình grid/column/style về cấu trúc mà Designer
serialize được, đặt tên/khởi tạo thuộc tính tường minh; giữ header tiếng Việt,
font và màu guide. Dùng layout/size theo DPI cơ sở nhất quán, bảo đảm filter
không tăng chiều cao đến mức mất dòng grid ở 150%. Khởi tạo/dispose components
theo mẫu WinForms để hết CS0649. Giữ thay đổi B09 về nhãn rỗng/cuộn; không đổi
SQL/DAL/BLL hay thêm MainForm/T07. Không chỉ chép lại source rồi bỏ qua Save.

**Regression đề nghị:** Build Debug/Release; Designer save/close/reopen, đối
chiếu source và các thuộc tính sau round-trip; lấy manifest mới sau serialization
rồi UI ở DPI Windows thật 100/125/150%, 1280×720 và 1366×768, grid/rỗng/loading/
lỗi, Tab/Shift+Tab, cuộn và minimize/restore. Trả Scale về 125%.
**Rủi ro:** Serialization có thể thay source hợp lệ dù Form vẫn mở được; phải
kiểm bản sau Save, không dùng PASS của binary trước Save để chốt bản sau Save.

Biên bản: [b09-test-results.md](b09-test-results.md). Bằng chứng source sau Save
đã giữ trong run mới; chưa tự sửa B10 trong lượt test.

## G01-B09 — MinimumSize không cho thu Form ở DPI150%

**Chốt regression sau B10:** Bounds/layout/nhãn cuộn và keyboard ở ba DPI đã
PASS trên source sau Save; xem b10-test-results.md. B09 đã giải quyết.

**Cập nhật sau duyệt:** Đã sửa giới hạn kích thước/nhãn theo phạm vi B09;
bounds 6 tổ hợp DPI/kích thước và keyboard trước Save đạt. Regression Designer
phát hiện B10 mới nên chưa chốt trọn regression. Xem b09-test-results.md;
các mô tả “chờ duyệt, chưa sửa” dưới đây là lịch sử lúc phát hiện B09.

**Trạng thái:** Mới phát hiện09/10 trong kiểm chứng bổ sung đã duyệt; chờ duyệt
sửa, chưa sửa source. TaskT05/B07 hình thức; T04 Designer regression chịu ảnh hưởng.

**Actual:** Windows Scale150%, GetDpiForWindow144, StartupForm compiled thật
qua host4 DTOdemo. Resize1280×720 trả1280×812; resize1366×768 trả1366×812.
100%GetDpi96 thu đúng cả2 kích thước; máy đã được trả125% sau test.
Bằng chứng dpi-keyboard.json/PNG ở runs/G01_Supplement_20261009_120403 và
[supplement-test-results.md](supplement-test-results.md).

**Nguyên nhân source:** B07 đặt MinimumSize800×560 trong Designer, AutoScaleMode.Dpi.
MinimumSize được WinForms scale theo DPI, tạo giới hạn cao hơn720/768 ở150%.
Resize không throw nhưng bị clamp812px. Không coi ảnh1280×812 làPASS1280×720.

**Đề nghị sửa:** Hạ MinimumSize theo viewport nhỏ đã quy định (bắt đầu từ640×400
như form trước B07), kiểm bố cục AutoSize/Dock/Flow wrap/scroll để nút/status/
header và ít nhất phần grid truy cập được ở1280×720 và1366×768 tại100/125/150%.
Chỉ thay giới hạn/layout cần thiết theo số đo; không giảm font/đổiframework hoặc
tắt DPI để né lỗi. Không đưa MainForm/sidebar T07 vào phạm vi.

**Regression:** DPIWindows thật100/125/150 ở2 kích thước; native bounds/ảnh,
grid/empty/error/loading/buttons, resize/minimize/reopenDesigner; Tab/ShiftTab
manual (harness trước mất foreground chưa chứng minh bugkeyboard). Giữ source
cố định; trả display scale125% sau test. SQL/permission/cancellation không đổi.
**Ảnh hưởng/rủi ro:** StartupForm.Designer.cs, DesignerT04 và UI sampleT05;
minsize nhỏ có thể wrap toolbar nên cần kiểm truy cập control/grid, không chỉ
assert kích thước. Không ảnh hưởng dữ liệu/transaction.

## G01-B08 — Hủy query SQL xong nhưng UI vẫn Đang tải…

**Mới nhất:** Đã sửa theo duyệt và runtime regression **PASS** trên app16932
dưới Windows account riêng. Text đã hủy đúng trước load đầu/có rows/sau unlock/
hủy lặp; reload10 bàn, close khi blocked requests0. Build0 warning/0 error,
source fixed count0. Biên bản [b08-test-results.md](b08-test-results.md).
Các mô tả chờ sửa/regression dưới đây giữ lịch sử trước lượt PASS.

**Cập nhật sau duyệt09/10:** Đã sửa StartupForm.cs: guard disposed/stale giữ trước,
nhánh cancellation hiện tại gọi ShowCanceledLoad; cùng helper dùng ở catch
OperationCanceledException. Không đổi SQL/DAL/transaction/rows/Designer.
Build Debug/Release0 warning/0 error. Run G01_B08_20261009_111336 đã chụp source
manifest; runtime regression chờ nhập credential tại máy để mở app identity riêng.
Đã chuẩn bị harness assert cancellation text cả trước load đầu, có rows cũ,
sau unlock và hủy lặp; không xóa bằng chứng FAIL trước. B08 chưa chốt PASS.

**Trạng thái:** Phát hiện09/10/2026 trong manual được duyệt; chờ duyệt bổ sung,
chưa sửa. **Task sở hữu:** T05, T05-K07 FAIL.

**Tái hiện có bằng chứng:** App PID30036 chạy Windows account CafeManagerApp,
SQL session cùng identity; tải10 bàn. Session test thứ hai giữ BAN TABLOCKX trong
transaction; app tải bị blocked, bấm Hủy tải. SQL request dừng, nút tải enabled,
nút hủy disabled, dữ liệu cũ giữ nguyên, nhưng status vẫn “Đang tải…”. Sau rollback
lock vẫn cùng text; bấm tải lại thành công. Đóng khi query blocked process thoát.
Bằng chứng runs/G01_Manual_20261008_225305_cebd98ad/runtime-slow-ui.json và
runtime-canceled.png; biên bản [manual-test-results.md](manual-test-results.md).

**Source liên quan:** StartupForm.cs sau await service.LoadAsync có guard
`cancellation.IsCancellationRequested` -> return trước nhánh thông báo; finally
mở lại nút. Catch OperationCanceledException mới cập nhật “Đã hủy tải”.
DAL catch SqlException dịch thành DataAccessException, service chuyển thành
result.Error, nên cancellation không luôn đi qua catch OperationCanceledException
của Form. Đây là đường source cho phép kết thúc im lặng, phù hợp actual text/
buttons. Chưa thu exception number của lần cancel nên không bịa mã SQL cụ thể.

**Đề nghị:** Tách guard stale/disposed khỏi nhánh cancellation của request hiện
tại; nhánh hủy hiện tại cập nhật status/content đã hủy và giữ rows cũ. Gom helper
hiển thị cancellation nhỏ dùng chung cho nhánh result và catch, không đổi SQL/
transaction hay bỏ request-version guard. Không cập nhật request cũ/control disposed.

**Regression:** Có/không dữ liệu cũ, real SQL lock/hủy/unlock/reload, lặp hủy,
close khi loading và request stale; assert text đã hủy thay chỉ buttons.
Build và Designer nếu helper tác động control initialization; giữ source cố định
mỗi lượt. Dữ liệu/permission/login không đổi. Rủi ro: chặn update stale đúng
thứ tự; không sửa ngoài handler status/cancellation liên quan.

## G01-B07 — StartupForm chưa đáp ứng đầy đủ hình thức style guide

**Trạng thái:** Đã duyệt “ok duyệt”, đã sửa; build và ảnh WinForms125% PASS,
Designer mới/DPI100/150/slow-query còn chờ. Chi tiết: [b07-test-results.md](b07-test-results.md).
Các nhận xét dưới đây giữ bằng chứng trước sửa.

- Bằng chứng: ảnh runtime người dùng gửi, StartupForm.Designer.cs:55–98,
  UI guide index.html mục 03/06/07. Screenshot là bằng chứng hội thoại.
- Đã áp dụng màu nền/panel/nút chính và Segoe UI; chưa đủ nghiệm thu hình thức.
- Grid chỉ đặt BackgroundColor; không cấu hình BorderStyle/GridColor/header/
  selection/row styling, nên runtime còn khung và grid lines mặc định đen,
  header không có nền chọn #EAD8C3/viền #DDD0C2 theo guide.
- Toolbar dùng TextBox mặc định so với nút AutoSize+Padding, chiều cao không
  thống nhất; ảnh cho thấy input thấp hơn nút rõ rệt.
- Disabled nút chính trên nền nâu có chữ tối khó đọc trong ảnh; source chỉ đặt
  ForeColor.White cho trạng thái bình thường, thiếu kiểm chứng disabled native.
- Thiếu config hiển thị tên biến môi trường và hướng dẫn kỹ thuật dài ngay
  vùng trạng thái; không có empty/config-state panel có phân cấp trực quan.
- Khung sidebar/MainForm thuộc T07 (kế hoạch:482–490), chưa triển khai trong G01;
  không ghi thiếu sidebar của form mẫu là bug nghiệp vụ T05.

**Đề nghị sửa:** Trong StartupForm, bố cục header/filter/content rõ, khoảng cách
theo guide, grid header/viền/chọn dòng đúng palette, input/nút cân chiều cao,
disabled dễ đọc và config/empty/loading/error có trình bày rõ. Giữ service/
query/cancellation/contract; không tạo module hoặc đưa T07 vào G01.
**Regression:** Designer lưu/mở lại; build; ảnh WinForms thật ở 1280×720,
1366×768 và DPI 100/125/150%; Tab/focus, normal/disabled/loading/error/empty,
tải/hủy/đóng. Source cố định trong mỗi lượt kiểm chứng; cập nhật manual evidence.
**Ảnh hưởng:** Form mẫu T05 và Designer T04; không SQL/tiền/kho/quyền.

## G01-B01 — Integration bị chặn bởi helper SQL dùng chung

Task sở hữu T03; chi tiết và hướng sửa ở
[`../t03-contract-migration-cap-ma/bug-report.md`](../t03-contract-migration-cap-ma/bug-report.md).
T05 run `20261008_220349_2a7e3e26` exit 1, `Unexpected seed USE.`, không có TRX
integration. 7 unit test độc lập vẫn PASS. Không ghi PASS toàn K02/K04/K09/K10
chỉ từ unit khi phần SQL/manual cùng ca chưa chạy.

## G01-B02 — Cảnh báo nullable ở nhánh hiển thị dữ liệu

**Trạng thái:** Đã duyệt bằng “ok” ngày 08/10/2026; đã sửa, build regression PASS; UI dữ liệu còn BLOCKED.  
**Mức ảnh hưởng:** Thấp; build/app vẫn hoạt động trong đường khởi động đã thử.

- Debug, Release và build copy sạch đều có 1 warning CS8602, 0 errors:
  `src/CafeManager.WinForms/Forms/StartupForm.cs:53`.
- Bằng chứng: `../runs/G01_20261008_220313/build-debug.log`,
  `build-release.log`, `clean-build.log`.
- Nguyên nhân: `TableStatusResult.Rows` nullable để phân biệt lỗi. Nhánh sau
  `result.Error is null` không được type system chứng minh Rows khác null.
  Dòng 52 chỉ suppress bằng `Rows!`, dòng 53 dereference Rows không có guard.
- Chưa có bằng chứng runtime NullReferenceException; không báo là crash đã tái hiện.
- Hướng sửa: bind một biến rows sau kiểm tra hợp lệ, dùng cùng biến khi bind/
  đếm; trường hợp Error null và Rows null phải báo lỗi an toàn, không fake rỗng.
  Không suppress cảnh báo toàn project hoặc đổi nullable settings.
- Phạm vi: handler mẫu async T05; T04 build chịu ảnh hưởng, không thay contract
  SQL/transaction. Regression Debug/Release/copy sạch và tải thành công/rỗng/
  lỗi/hủy/đóng Form; cần GUI/principal app riêng cho các ca manual.

### Regression B02

Guard `result.Rows is not { } rows` giữ dữ liệu cũ và báo phản hồi không hợp lệ;
bind/count cùng biến rows. Debug/Release/copy sạch đều 0 warning/0 error;
bằng chứng `../runs/G01_Regression_20261008_221528/`. Chưa PASS toàn regression
UI success/empty/error/cancel/close vì chưa có principal app riêng.

## G01-B04 — Runner dừng khi xUnit ghi stderr

**Cập nhật sau duyệt:** Đã sửa runner dùng Start-Process, stdout/stderr riêng,
chờ process kết thúc/lấy ExitCode rồi parse TRX. Run 20261008_222503_29aa4634
thu đủ 20 results, exit 0, SetupError null. Nhánh nonzero/stderr chưa cưỡng bức
test lại trong lượt cuối; bằng chứng runner failure cũ giữ nguyên.

**Trạng thái:** Chờ duyệt bổ sung; chưa sửa.

Run `20261008_221556_329fb1d8`: setup/migration thành công nhưng dòng xUnit FAIL
trên stderr bị PowerShell `$ErrorActionPreference='Stop'` biến thành exception.
JSON ghi SetupError bằng dòng FAIL, DotnetExitCode null, không có TRX đầy đủ.
Đây là lỗi thu kết quả, không phải lỗi setup SQL mới. Chạy dotnet bằng
Start-Process trên DB mới, stdout/stderr riêng, thu được TRX 20 tests đầy đủ.
Đề nghị runner thu stdout/stderr/exit code đến hết, phân biệt setup/test failure;
regression chạy failure thật, giữ TRX/console và mã thoát khác 0. Không đổi expected.

## G01-B05 — Snapshot empty SQL trả DBNull

**Cập nhật sau duyệt:** Đã chuẩn hóa null/DBNull thành [] ở snapshot helper,
giữ mọi bảng/PK/INCLUDE_NULL_VALUES. Hai rollback subcase và checkout session
state PASS trong run 20261008_222503_29aa4634; không đổi expected/SQL procedure.

**Trạng thái:** Chờ duyệt bổ sung; chưa sửa.

Hai subcase rollback FAIL trước gọi procedure ở TestDatabaseFixture.cs:84.
`SELECT (SELECT ... FOR JSON PATH)` trên bảng rỗng trả SQL NULL -> DBNull;
`?? "[]"` chỉ xử lý C# null, sau đó cast DBNull sang string gây InvalidCastException.
Log trực tiếp SQL xác nhận EmptySnapshot NULL. Đề nghị chuẩn hóa null/DBNull
thành [] tại helper snapshot, giữ thứ tự PK/INCLUDE_NULL_VALUES; không bỏ bảng.
Regression hai ca late-failure: snapshot trước/sau, tiền/kho/điểm/liên kết,
@@TRANCOUNT/XACT_STATE cùng session. Hiện chưa có bằng chứng rollback DAL sai.

## G01-B06 — Một số lệnh DAL trả Unknown chưa xác định mã gốc

**Cập nhật sau duyệt — nguyên nhân đã xác định:** Probe SqlClient 6.1.3 lặp
mở/query target/EXECUTE AS USER/dispose 6 lượt: pooling bật lỗi SQL596 class21
ở query target các lượt 1/3/5; các lượt 0/2/4 thành công. Pooling tắt 6/6 thành công.
Session EXECUTE AS USER trả về pool không reset sử dụng lại được; lỗi nằm ở
fixture initializer trước procedure, không phải mapping/quote/ownership chain.
Đã tắt pooling **chỉ AppFactory của fixture impersonation**, admin/runtime
factory giữ nguyên. Run 20261008_222503_29aa4634 20/20 PASS, các lỗi Unknown
ngoài expected hết; known 51109/unknown/timeout đều đúng. Probe logs ở
`../runs/G01_Regression_20261008_222427/b06-pooling-on.txt` và `b06-pooling-off.txt`.
Không giữ raw SQL exception/password trong log sản phẩm.

**Trạng thái:** Chờ duyệt điều tra/debug bổ sung; nguyên nhân chưa kết luận.

TRX direct: O'Brien roundtrip và checkout success ném DataAccessException Unknown;
known-error sau unknown/timeout nhận Unknown thay BusinessRule 51109.
Hai text Unicode/injection cùng theory vẫn PASS. SqlProcedureExecutor dịch lỗi
nhưng không giữ SqlException gốc, TRX hiện không chứa mã SQL gây Unknown.
SQL trực tiếp impersonate T05_App trên DB direct: SaveValues/checkout không báo
lỗi; sp_HuyHoaDon trả đúng 51109. Vì vậy chưa có bằng chứng là lỗi quote, grant
hoặc bảng mapping; pooling/session impersonation là giả thuyết cần kiểm chứng.
Đề nghị bổ sung diagnostics chỉ mã lỗi/metadata không secrets, điều tra session
initializer/pooling và mã thực tế, rồi sửa đúng nguyên nhân trong DAL/fixture.
Giữ expected và không thêm blanket mapping Unknown -> BusinessRule.
Regression toàn T05 SQL trên DB mới, chú ý thứ tự sau lỗi/timeout và quyền app.

Bằng chứng B04–B06: `../runs/G01_Regression_20261008_221528/` gồm direct/direct.trx,
t05-direct-output.txt, t05-direct-error.txt, t05-sql-diagnostics.txt và tên DB
t05-direct-database.txt. Run direct đạt **15 PASS/5 FAIL/20 tests** (7 unit PASS).

## Điều tra bổ sung 18/10/2026 — chưa sửa

Người dùng báo không chạy được luồng đăng nhập từ Visual Studio và tác động môi
trường ngoài ý muốn. Hồ sơ điều tra chỉ đọc, mapping từng finding về task và hướng
khắc phục: [báo cáo toàn đợt](../../../bugs/project-environment-audit-20261018/report.md).

PASS cũ giữ làm lịch sử của bản/fixture đã nghiệm thu; không thay bằng chứng chạy
F5 hiện tại. Đợt này không chạy test hoặc sửa source/migration/configuration.
Các thao tác khắc phục/xóa DB hoặc tài khoản Windows chưa được thực hiện.
