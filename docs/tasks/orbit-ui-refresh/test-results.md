# Kết quả đợt test Orbit UI — 17/10/2026

Trạng thái: đã chạy phần có thể kiểm chứng; **chưa đạt nghiệm thu**, có B01–B03
và các phần BLOCKED/NOT RUN. Người dùng duyệt test bằng tin nhắn “duyệt”. Không
sửa source ứng dụng, migration, expected hoặc test nghiệp vụ trong lượt này.

## Môi trường và bằng chứng

- Source HEAD nền: `fb9f9efa9e8d539f3c3e9b89e7d0a3be5ed2763d`, cộng diff UI đã duyệt.
- Manifest UI 24 file: `source-manifest.csv`; kiểm cuối lượt **24/24 unchanged** tại
  `runs/20261017-acceptance/source-freeze-check.csv`.
- SDK/.NET/OS: `runs/20261017-acceptance/dotnet-info.txt`.
- SQL Server thật localhost 17.0.1000.7, schema 001/002/003; checksum đầy đủ trong
  `runs/20261017-acceptance/environment.json`.
- DB UI riêng có marker: `CoffeeManager_Test_G02_20261017_173711_c86ff044`.
  Test G02 tạo thêm 3 DB riêng, ghi trong results.json của run dưới đây.
- DPI thật của fixture: 120 (125%). Form ngoài 1280×720/client1262×673 và
  1366×768 được mở bằng launcher. Không giả DPI100/150 bằng cách scale bitmap.
- `UiLauncher/` chỉ là bố trí fixture tại docs/runs: mở Form production và session
  test qua BLL, không thay production, không dùng PerformClick/SendKeys/UIA tự viết.
  Thao tác UI dùng Computer Use. Fixture login qua BLL **không** thay ca nhập/login
  trên LoginForm hoặc chứng minh vòng ApplicationSessionContext đầy đủ.
- Ảnh là WinForms Windows thật từ sky. Accessibility có lúc trễ một nhịp/không cache
  được modal; ưu tiên đọc lại và ảnh thực, không coi tree cũ là actual trạng thái mới.

## Các lệnh tự động đã chạy

1. `dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj --no-restore -m:1 -p:UseSharedCompilation=false -v minimal`
   → PASS, 0 warning/0 error; `runs/20261017-acceptance/build.txt`.
2. Lệnh trên thêm `-c Release` → PASS, 0 warning/0 error;
   `runs/20261017-acceptance/build-release.txt`.
3. `tools/database/Test-G02.ps1 -Task All -AcceptanceApproved` → **484/484 PASS**,
   0 skipped, source before/after unchanged. Biên bản từng test/TRX tại
   `../g02-truy-cap-ung-dung-cau-hinh-nen/runs/20261017_173535_5365c60e/`.
   `results.json` có DB, thời điểm, actual, test name và manifest đầy đủ.
4. Unit filter AuthRulesTests/CatalogRulesTests → **36/36 PASS**;
   `runs/20261017-acceptance/unit.trx`. Các ca này có trong bộ G02 ở trên, đây là
   lượt chạy riêng, **không cộng thành 520 test khác nhau**.
5. `git diff --check` → PASS. Không có diff Business/DataAccess/Models/database/tests/tools.

Named pipe/SQL từ sandbox không truy cập được lúc đầu; chạy quyền phù hợp sau đó
thành công, không đổi code để xử lý môi trường. Testhost trong sandbox đã được dừng
trước lượt unit thành công. `unit.txt` là output thử đầu chưa hoàn tất; dùng TRX để
đọc kết quả chính thức. Launcher có warning NU1900 vì endpoint NuGet audit không
truy cập được; không đổi package. Đây không phải lỗi build production.

## Ma trận kết quả theo ID

Không dùng một PASS backend để gán PASS toàn bộ mã UI. Các subcase chưa chạy
được liệt kê riêng bên dưới; ID có FAIL giữ FAIL dù phần khác đã PASS.

| ID | Kết quả tổng hợp lượt này | Actual / phần còn thiếu |
|---|---|---|
| UI01 | FAIL | Mọi màn chính và 7 tab đã quan sát; B01 native white. Chưa đủ 7 biến thể editor và video motion |
| UI02 | NOT RUN (chưa đủ) | 7 cặp màu chính PASS; loading/error/disabled và status đã quan sát. Chưa đối chiếu đầy đủ mọi trạng thái hover/press/focus/conflict/unknown theo thời gian |
| UI03 | FAIL | B02 hàng dữ liệu ở1280×720/DPI125; B03 cột wrap. DPI100/150 BLOCKED trong lượt này; chưa đổi scaling OS |
| UI04 | NOT RUN (chưa đủ) | Enter không lưu, nhập tiếng Việt, No giữ nháp đã PASS; Tab/ShiftTab/mnemonic/giữ-thả ngoài/click nhanh đủ ma trận chưa chạy |
| UI05 | BLOCKED một phần | Auth/role backend PASS; 3 role vào MainForm đúng và menu theo role PASS. LoginForm chưa kết nối có ảnh; không tự động nhập mật khẩu/điều khiển dialog xác thực theo skill computer-use. Vòng login→logout→login app gốc cần manual |
| UI06 | BLOCKED một phần | Bootstrap Pending hiển thị/scroll/footer PASS; backend bootstrap/rollback/concurrency trong G02 PASS. Các bước nhập mật khẩu/xác nhận setup UI cần manual, không suy PASS từ BLL |
| UI07 | NOT RUN (chưa đủ) | Chưa tải/có1bàn/rỗng/lỗiSQL229/hủy query bị khóa PASS; payload query cũ trả sau query mới và đóng app lúc request pending chưa chạy riêng |
| UI08 | NOT RUN (chưa đủ) | Backend CRUD/quyền/biên đủ7danh mục PASS. UI đọc7tab, thêm sản phẩm/validation/reload kết quả PASS. Edit/delete/tìm/phân trang và6biến thể editor còn thiếu bằng chứng UI |
| UI09 | NOT RUN (chưa đủ) | Dirty cancel→No giữ nguyên nháp, Enter không ghi, Lưu ghi đúng PASS. Conflict/unknown/đối chiếu trên dialog thật chưa chạy |
| UI10 | BLOCKED | Đã tạo designer-copy và mở instance VS riêng; helper báo modal element cache unavailable sau refresh/retry, chưa mở/save/reopen Designer. Không dùng build thay round-trip |
| UI11 | NOT RUN | Chuyển7tab, Home/System và mở/đóng editor hoạt động; chưa chạy chuỗi100 lần kèm đoGDI/timer/handles. Phiên GUI có thao tác ngoài agent, helper yêu cầu reobserve; không báo số đo hoặc100lần giả |
| UI12 | PASS | Debug/Release, diff/scope, freeze24file,4icon local+license được dùng trên UI; không đổi package/framework/DB hoặc phụ thuộc HTTP cho UI |

## Subcase đã có actual và bằng chứng

Đường dẫn ảnh/JSON dưới đây tính từ `runs/20261017-acceptance/`.

| Subcase | Thao tác / dữ liệu | Expected | Actual | Kết quả | Bằng chứng |
|---|---|---|---|---|---|
| UI01-Login-NoConfig | Mở exe production, không có CAFEMANAGER_APP_CONNECTION | Orbit; báo cấu hình, khóa đăng nhập | Có thông báo, nút disabled; không có DB call/login giả | PASS | login-native.png |
| UI01-Bootstrap-Pending | Mở BootstrapForm thật trên DB test sạch schema3 trước tạo admin | Đủ trường qua scroll, footer truy cập được | Header/input/scroll/footer đúng; chưa submit | PASS | bootstrap-native.png, bootstrap-820x660-geometry.json |
| UI01-Catalog-7Tabs | ADMIN chuyển lần lượt 7 tab trên app thật | Tab/cột đúng, nội dung readable và Orbit | Cột theo tab đúng; native trắng B01, header wrap B03 | FAIL | main-admin-1280x720.png, catalog-supplier/payment/delivery/expense/position/shift.png |
| UI01-Editor-Product | Thêm sản phẩm, chưa ghi | Form/error/footer native theo theme | Các trường và nút có đủ | PASS | editor-product-125.png |
| UI01-Editor-Shift | Thêm ca, không sửa input | Ô giờ theo palette, giữ HH:mm | 06:00/14:00 đúng nhưng nền ô trắng B01 | FAIL | editor-shift-native-white.png |
| UI01-Home-System | ADMIN mở Home rồi System | Đúng nội dung có sẵn, schema thật | Trang phiên đúng; schema3 từ DB test | PASS | home-admin.png, system-admin.png |
| UI02-Contrast-Tokens | Tính luminance tuyến tính và ratio WCAG trên7cặp màu | Chữ thường≥4.5:1 | Ratio thấp nhất5.405, các cặp còn lại8.001–17.657 | PASS | contrast.json |
| UI03-1280-125 | Form1280×720/client1262×673/DPI120; DB cóDM001 | Đọc được ít nhất hàng đang có | Chỉ dải hàng mỏng, chữ không hiện; tái hiện sau restart | FAIL | main-1280-loaded-reproduction.png, main-ADMIN-1280x720-geometry.json |
| UI03-1366-125 | Cùng bản ghi, Form1366×768/DPI120 | Đọc được mã/tên và nút | Đọc được DM001/tên, toolbar vẫn2hàng | PASS | main-1366x768-125.png |
| UI03-ColumnHeaders-125 | Mở Đối tác và Chức vụ | Toàn bộ tiêu đề có đơn vị đọc được | `(%)` và dòng “ca” bị cắt | FAIL | catalog-delivery.png, catalog-position.png |
| UI04-Enter | Nhập “Orbit UI thử nháp tiếng Việt”, nhấn Return trong editor | Không tự lưu/đóng | Editor giữ input; SQL COUNT(DANHMUC_SP)=0 ngay sauEnter | PASS | editor-enter-no-save.png; kết quả SQL trong transcript |
| UI08-Validation | Nhấn Lưu khi Tên rỗng | Không ghi, ErrorProvider/message gần trường | Hiện lỗi tên, form còn mở | PASS | editor-empty-validation.png |
| UI08-Create | Sau Enter và No, nhấn Lưu rõ ràng | Một mã cấp từ service, DB giữ nguyên text | DM001/tên tiếng Việt chính xác; reload hiện1dòng | PASS | product-after-save.json; main-1280-loaded-reproduction.png |
| UI09-Dirty-No | Nhấn Hủy khi có nháp → No | Dialog còn mở, giữ input, không ghi | Có prompt; No giữ text; sau đó Lưu mới tạo bản ghi | PASS | editor-dirty-cancel-prompt.png, product-after-save.json |
| UI05-Role-ADMIN | Session ADMIN tạo qua BLL, mở MainForm | Danh mục nền là màn đầu | Có tên/role, 7tab, menu quản trị | PASS | main-admin-accessibility.json, main-admin-1280x720.png |
| UI05-Role-CASH | Session CASH thật trong testDB | Trang phiên, không danh mục quản trị | Đúng nội dung chưa triển khai, không menu Danh mục nền | PASS | home-cash-125.png |
| UI05-Role-STORE | Session STORE thật trong testDB | Trang phiên kho, không danh mục quản trị | Đúng nội dung chưa triển khai, không menu Danh mục nền | PASS | home-store-125.png |
| UI07-Load | Startup query toàn bộ, fixture BA901/KV901 | Hiện đúng1bàn | BA901/Bàn kiểm thử Orbit/KV901/Trong, hóa đơnNULL | PASS | startup-loaded.png |
| UI07-Empty | MãKV999 không tồn tại, tải thành công | Rỗng đúng ngữ cảnh | “Không có bàn trong khu vực…”; giữKV999 | PASS | startup-empty.png |
| UI07-Error | Tạm DENY SELECT view cho G02_Runtime trong DBtest | Lỗi khác rỗng, giữ input | Lỗi229; “Chưa tải được dữ liệu”; giữKV999 | PASS | startup-sql-error.png, startup-fault.txt |
| UI07-Cancel | SessionSQL giữ TABLOCKX45giây, UI tải rồi Hủy | Loading khóa gửi, hủy được, khôi phục nút | Tải disabled/Hủy enabled; sau hủy hiệnĐãhủytải và nút phục hồi | PASS | startup-slow-loading.png, startup-cancelled.png, slow-query-cleanup.txt |

Cặp màu là phép đo tĩnh trên token, không chứng minh toàn bộ native control hay
animation đã đạt. Ảnh ngẫu nhiên sau một click không chứng minh trọn đường hover/
press/release, frame rate hoặc ngân sách timer/GDI. Không có video nghiệm thu trong lượt này.

## Các phần cần bổ sung để đủ toàn bộ plan

- DPI thật100% và150%, đúng2kích thước cho từng màn/biến thể; cần phiên Windows
  được người dùng chuẩn bị scaling thích hợp, không tự đổi OS settings.
- Designer mở/save/reopen trên designer-copy: helper trả
  `element 269 is not available in cached app state for devenv.exe` dù có tree modal;
  trước đó có lỗi geometry/foreground và đã reobserve/activate/retry. Đã đóng hộp
  thoại và riêng instance VS test; không chuyển project CreatorFlow đang mở.
- Manual auth/bootstrap UI, rồi vòng app gốc đầy đủ; đây là giới hạn từ skill
  computer-use (“Do not automate user authentication dialogs”), không phải thiếu SQL.
- Các subcase UI còn NOT RUN ở bảng: đầy đủ keyboard/motion,6editor còn lại,
  edit/delete/tìm/phân trang qua UI, race/unknown trên dialog, querymuộn và100lần/GDI.
  Không dùng484test backend để lấp các ca này. Chưa nghiệm thu UI01–UI12 trọn bộ.

## Kết thúc lượt

- Source ứng dụng vẫn24/24hash khớp; G02 runner cũng xác nhận source/tests/database/tools
  before/after không đổi. Không sửa bug, migration hoặc test nghiệp vụ.
- SQL DENY phục vụ fault injection đã REVOKE, TemporaryDenyCount=0; khóaSQL đã
  rollback. Không sửa DB sử dụng thật. DBtest được giữ lại làm bằng chứng theo runner,
  chưa drop. Account test dùng password ngẫu nhiên trong process, không lưu plaintext.
- [Đợt bug B01–B03](bug-report.md) đang chờ duyệt sửa/regression. Giữ task Đang làm;
  giữ nguyên lịch sử PASS G01/G02, không đánh dấu lại325ca hay commit/push.


## Cập nhật debug B01–B03 được duyệt — 17/10/2026

Đã sửa chrome tab, bố cục bảng và header; regression cửa sổ1280×720/DPI125
đọc được hàng và đủ tiêu đề cột. Phân trang Sau/Trước hoạt động. Debug/Release
0warning/0error; unit36/36PASS; hash source trước/sau không đổi.

B01 còn ô giờ native trắng; giữ DateTimePicker gốc để không thay tương tác.
Chờ quyết định giữ ngoại lệ này hoặc duyệt editor HH:mm riêng. Không đánh dấu
nghiệm thu toàn bộ; DPI100/150, Designer và các ca chưa chạy vẫn giữ trạng thái.
Chi tiết: [regression-results.md](runs/20261017-b01-b03/regression-results.md).

## Cập nhật tối ưu bố cục theo yêu cầu tiếp theo — 17/10/2026

Đã triển khai bố cục thích ứng, card giới hạn bề rộng, nhãn xếp dọc, navigation gọn,
selector danh mục ở cửa sổ hẹp, empty state và editor giờ có skin. Giữ UX01/UX02/
UX09, phạm vi T06–T08 và màn kiểm tra G01; không sửa logic/BLL/DAL/Models/DB.

Kiểm chứng thực tế DPI125:720×720, vùng960×1020bằng nửa WorkingArea, Login/editor
maximize, editor600×720, empty state và phím tăng/giảm giờ. Build Debug/Release
0warning/error; unit36/36PASS; source cố định trong từng lượt. DPI100/150, native
Snap, Designer và toàn bộ regression UX chưa chạy đủ; task vẫn Đang làm.

Báo cáo hiện hành: [responsive-results.md](responsive-results.md).
Bộ phương án/mapping UX: [responsive-correction.md](responsive-correction.md).
Các đề xuất giữ ô giờ trắng ở báo cáo trước là lịch sử, đã được thay bằng editor
HH:mm trong đợt này. Chưa commit/push; không đánh dấu toàn bộ B01–B09 hoàn tất.
