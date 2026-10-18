# Kế hoạch chỉnh giao diện hiện tại — Orbit UI

Ngày lập: 17/10/2026. Requirements và plan đã được người dùng duyệt ngày 17/10/2026.
Trạng thái: đã triển khai; đợt test được duyệt và đã chạy phần khả thi ngày17/10/2026.
Chưa nghiệm thu: có B01–B03 và các ca BLOCKED/NOT RUN; xem test-results.md và bug-report.md.
Phạm vi chi tiết: [requirements.md](requirements.md).

## 1. Chốt mốc và khoanh vùng thay đổi

- Trước thao tác Git, kiểm tra root bằng `git rev-parse --show-toplevel`; chỉ đọc
  trạng thái/diff để ghi mốc và phân biệt thay đổi có sẵn, không stage/commit.
- Ghi inventory và mốc source trước triển khai trong `technical-checks.md`.
- Chụp baseline WinForms từng màn/biến thể có thể truy cập trên DB test riêng;
  giữ nguyên trạng thái role, trường và nút để đối chiếu sau thay đổi.
- Đối chiếu T01, T06–T08 và UX01/02/09; giữ ADMIN vào Danh mục nền, CASH/STORE
  vào trang phiên hiện tại. Không thay bằng dashboard/POS theo UX đích chưa triển khai.
- Ghi whitelist: `Theme/`, UI helper trình bày, các Form/Modules hiện có,
  resource nội bộ cần dùng và tài liệu của đợt này. Project chỉ chỉnh mục resource
  nếu cần; không đổi target framework, package, startup composition hoặc kết nối.

Đầu ra kiểm tra: danh mục màn đầy đủ; mọi khác biệt hành vi phát hiện khi rà soát
được ghi riêng, không đưa thành fix trong đợt visual.

## 2. Chuẩn hóa theme và control cần thiết

File chính: `src/CafeManager.WinForms/Theme/AppTheme.cs`,
`UI/ModuleState.cs`; helper mới tối thiểu tại `UI/` hoặc `Theme/` khi cần.

- Ánh xạ token 3.0.0 sang màu/font/spacing/radius/motion tập trung, có chú thích nguồn.
  Giữ điểm gọi AppTheme hiện có để tránh sửa rộng.
- Tách vai trò canvas/surface/text/muted/primary/secondary/selection/state;
  không Apply đệ quy ghi đè toàn bộ control thành cùng màu.
- Skin Button, Label, TextBox, ComboBox, NumericUpDown, DateTimePicker,
  CheckBox, TabControl, DataGridView và ErrorProvider theo control thực có.
  Các popup native có giới hạn skin phải ghi sai khác, không thay engine/control nhập.
- Giữ enum và chuỗi trạng thái hiện tại; chỉ chỉnh presentation của presenter.
- Ưu tiên native control; chỉ thêm nút vẽ riêng nếu cần cho bóng/hover/press.
  Giữ Click/DialogResult/mnemonic/focus/default button semantics.
- Nếu cần chuyển động, dùng timer UI chung + thời gian thực; chỉ chạy lúc có hiệu ứng,
  dispose theo vòng đời, không tạo timer mỗi control/hạt. Không thêm hệ bụi sao.
- Chỉ đóng gói icon PNG đúng size/DPI thật sự sử dụng và license; không đưa product
  assets, logo đổi thương hiệu hay tooling browser vào ứng dụng.

Đầu ra kiểm tra kỹ thuật: build/Designer hợp lệ; rà mọi màu hard-code ghi đè theme,
không có handler hiệu ứng gọi BLL hoặc thay validation/Enabled/Visible.

## 3. Làm mẫu trên shell + danh mục hiện có

File: `Forms/MainForm.*`, `Modules/Catalogs/CatalogsControl.*`,
`Forms/CatalogEditorDialog.*` trong project WinForms.

- Shell: nền espresso, rail/sidebar phù hợp tên menu tiếng Việt, mục chọn caramel,
  header danh tính và status đọc rõ. Không bỏ nhãn menu chỉ để giống rail 76px của POS.
- Danh mục: tiêu đề, toolbar, tìm kiếm, 7 tab, bảng và phân trang; giữ nguyên mapping
  tab, cột, DataPropertyName, format, binding và số dòng mỗi trang.
- Editor: nhóm trường hợp lý, nhãn rõ, input native, lỗi gần trường, footer Lưu/Hủy/
  Tải lại theo những nút đã có. Không đổi điều kiện hiển thị/khóa từng trường.
- Hover/press/focus có phản hồi, disabled không sáng như nút hoạt động; không xoay icon.
  Giữ chiều cao footer và nút thao tác khi resize/DPI, không sửa handler nghiệp vụ.
- Ghi ảnh/clip mẫu và sai khác với palette/component trong reference. Không tuyên bố
  mẫu này là POS đã đối chiếu. Kiểm tra thị giác kỹ thuật trước nhân rộng.

Điều kiện qua bước: theme/control đã nhất quán trên màn nhiều dữ liệu và editor,
không cần thay contract hay luồng. Nếu không đạt trong phạm vi UI, báo blocker.

## 4. Áp dụng cho các màn còn lại, tuần tự

| Thứ tự | File | Thay đổi |
|---|---|---|
| 1 | `Forms/LoginForm.*` | Nhận diện CoffeeManager, input, nút chính, vùng lỗi và trạng thái đang gửi |
| 2 | `Forms/BootstrapForm.*` | Panel/nhóm bước thiết lập, danh sách và footer; giữ khóa setup, xác nhận và thoát process |
| 3 | `Forms/StartupForm.*` | Thay màu cũ cả Designer và UpdateButtonAppearance; bố cục grid/filter/empty state, giữ hủy tải/version request |
| 4 | `Modules/Home/SessionHomeControl.cs` | Panel thông tin vai trò, khoảng cách và typography; không thêm nút hay số liệu mới |
| 5 | `Modules/System/SystemInfoControl.cs` | Panel metadata/schema và hierarchy chữ; giữ nội dung và SetSchema |

Tên file ở bảng tính từ `src/CafeManager.WinForms/`.
Với `.cs`, chỉ sửa đoạn khởi tạo/layout/paint/appearance; không refactor phương thức
async hay luồng xử lý dù cùng file. Giữ MessageBox native và mọi kết quả xác nhận.
Reveal chỉ áp dụng vùng dài có sẵn nếu không làm mất focus/scroll; không thêm trang
component hoặc cuộn giả để chạy hiệu ứng.

## 5. Rà phạm vi trước bàn giao triển khai

- Đối chiếu diff với mốc đầu: SQL, DAL, BLL, Models, navigation controller,
  session/composition và request không thay đổi.
- Rà riêng event subscription, DialogResult, AcceptButton/CancelButton, TabIndex,
  binding, Enabled/Visible và nhánh lỗi; thay đổi chỉ được thuộc trình bày.
- Kiểm tra build và Designer kỹ thuật; ghi lệnh/môi trường/kết quả thật trong
  `technical-checks.md`. Không gán PASS nghiệm thu từ các kiểm tra này.
- Bàn giao danh sách file/màn đã chỉnh, sai khác native control và blocker còn lại;
  xin duyệt một đợt test, không tự chạy nghiệm thu từ việc duyệt plan.

## 6. Đợt kiểm thử đề xuất — chỉ chạy sau khi được duyệt

Mỗi lượt giữ source cố định. Ghi từng subcase vào `test-results.md`: môi trường,
commit/manifest source, schema DB test, dữ liệu, thao tác, expected/actual, evidence
và PASS/FAIL/BLOCKED/NOT RUN. Không sửa code hoặc expected giữa lượt.

| Mã | Ca kiểm tra | Expected |
|---|---|---|
| UI01 | Mỗi màn/biến thể trong inventory, đối chiếu ảnh với reference/token | Orbit thống nhất, tên CoffeeManager và nội dung nghiệp vụ giữ nguyên |
| UI02 | Normal/hover/press/focus/disabled và mọi trạng thái presenter | Đọc rõ, đúng tương phản, không đen trên nền tối, không bụi sao |
| UI03 | Từng màn × 1280×720/1366×768 × DPI100/125/150% | Trường, lỗi và footer truy cập được; không scale hai lần/cắt chữ |
| UI04 | Tab/Shift+Tab, mnemonic, Enter/Esc, giữ/thả chuột ngoài nút, click nhanh | Hành vi như trước, focus rõ, không kẹt visual hoặc gửi hai lần |
| UI05 | Đăng nhập thành công/sai/lỗi DB; ADMIN/CASH/STORE; đăng xuất | Điều hướng/quyền/session giữ nguyên; không lộ chức năng chưa triển khai |
| UI06 | Bootstrap theo các trạng thái hiện có trên fixture test được phép | Không đổi xác nhận, khóa hoặc cơ chế setup/runtime |
| UI07 | Startup: tải/rỗng/lỗi/hủy và query trả muộn | Giữ dữ liệu/thông báo/cancellation semantics như source cũ |
| UI08 | 7 danh mục: xem/tìm/phân trang/thêm/sửa/hủy/xóa và biên validation | DTO/input/CRUD, khóa hệ thống và phân quyền không đổi |
| UI09 | Editor: nháp/rời form/xung đột/kết quả chưa biết/đối chiếu | Giữ input, lựa chọn và khóa gửi lặp hiện tại |
| UI10 | Designer round-trip từng Form/UserControl hỗ trợ Designer | Không kết nối DB lúc design-time, không mất binding/event/resource |
| UI11 | 100 lần đổi module, mở/đóng editor, hover/scroll liên tục | Timer dừng khi idle, tài nguyên về mức ổn định; ghi số đo GDI/handles thực |
| UI12 | Rà diff phạm vi, build và resource offline | Không đổi nghiệp vụ/DB; asset local có license, không dependency Internet |

UI08/UI09 và bootstrap chỉ chạy trên SQL Server thật với DB test riêng và fixture
hiện có; không chạy ghi trên DB sử dụng thật. Đối chiếu persisted values theo fixture
trước/sau để phát hiện tác động ngoài ý muốn. Tái dùng ca G01/G02 liên quan để
regression theo ảnh hưởng, tham chiếu ID gốc; không ghi đè lịch sử PASS cũ hoặc bắt
chạy toàn bộ 325 ca chưa thuộc phạm vi.

FAIL được điều tra, ghi `bug-report.md` và tổng hợp đợt xin duyệt trước debug;
duyệt debug bao gồm regression phần liên quan. Báo cáo cuối `report.md` nêu đủ
PASS/FAIL/BLOCKED/NOT RUN, không đánh dấu hoàn tất khi còn ca bắt buộc chưa PASS.

## Rủi ro và xử lý trong phạm vi

- Dark theme có thể bị native popup/system dialog ghi đè: giữ tính ổn định nhập liệu,
  ghi sai khác nhìn thấy; không tự thay toàn bộ control/dialog.
- Màu đang set lại khi đổi module/EnabledChanged có thể làm mất theme: xử lý đúng
  các đoạn appearance, không thay điều kiện thay đổi trạng thái.
- Hiệu ứng custom có thể nháy/tăng GDI: giới hạn vùng invalidate, dispose tài nguyên;
  không trì hoãn lệnh nghiệp vụ để đợi animation.
- Thiếu SDK/Designer/GUI/DB test ghi blocker cụ thể; không dùng HTML thay test app.

Không có migration/contract/fixture nghiệp vụ mới trong kế hoạch này.
Không tự commit/push hay triển khai task G03–G08.


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
