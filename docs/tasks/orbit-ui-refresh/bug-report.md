# Đợt bug Orbit UI — lịch sử điều tra và sửa

## Phản hồi bố cục mới ngày 17/10/2026

Đã điều tra B04–B09 và lập [phương án sửa bố cục thích ứng](responsive-correction.md):
maximize, restore, chia đôi màn hình, card giới hạn bề rộng, nhóm trường/hành động,
control đồng bộ, heading và empty state. Bao gồm giải pháp editor giờ của B01.
Chờ duyệt cả đợt bổ sung; chưa sửa source ở bước điều tra này. Các mục ban đầu
bên dưới giữ nguyên làm lịch sử, kết quả debug được cập nhật ở cuối tài liệu.

Ngày 17/10/2026. Đợt test đã được người dùng duyệt; source ứng dụng giữ nguyên.
Chưa sửa bất kỳ bug nào dưới đây. Phạm vi đề xuất vẫn chỉ UI, không DB/BLL/DAL/Models.

Môi trường: Windows, SQL Server localhost 17.0.1000.7, schema 001–003;
DB UI riêng có marker `CoffeeManager_Test_G02_20261017_173711_c86ff044`.
App chạy DPI thật 120 (125%). Form 1280×720 có client 1262×673.
Mốc/hash xem `source-manifest.csv` và `runs/20261017-acceptance/source-freeze-check.csv`.

## B01 — Vùng native chưa có skin tối nhất quán [P2]

**Hiện tượng:** TabControl của danh mục còn dải trắng bên phải các tab và viền
trắng quanh vùng bảng. Editor Ca làm có hai ô DateTimePicker nền trắng. Đây là
sai khác ngay trong vùng làm việc; không tính titlebar hoặc MessageBox hệ thống
đã được giữ native theo phạm vi duyệt.

**Tái hiện:** mở MainForm ADMIN → Danh mục nền; chuyển qua các tab. Mở Thêm ở
tab Ca làm. Không cần nhập hay ghi dữ liệu.

**Bằng chứng:** `main-admin-1280x720.png`, `catalog-payment.png`,
`catalog-shift.png`, `editor-shift-native-white.png` trong `runs/20261017-acceptance/`.

**Nguyên nhân có bằng chứng từ source:** `AppTheme.cs:60` đặt OwnerDrawFixed
và `DrawTab` chỉ vẽ từng tab; không có xử lý nền/viền native của toàn TabControl.
Nhánh DateTimePicker tại dòng 50 chỉ đặt thuộc tính Calendar*, không skin ô nhập giờ
đang hiển thị. Không kết luận việc gán BackColor đơn thuần sẽ sửa được HWND native.

**Đề xuất:** xử lý skin nền/viền tab bằng phần trình bày nhỏ, giữ TabPages,
SelectedIndex và handler hiện tại. Với ô giờ, thử skin/wrapper trình bày giữ nguyên
DateTimePicker, Format/Value/keyboard/validation; nếu giới hạn native buộc đổi control
hoặc hành vi thì báo lựa chọn trước, không tự thay. Không đổi MessageBox hệ thống.

**Ảnh hưởng/rủi ro:** cần kiểm tra DPI, focus, vùng click/tab và giờ sau sửa skin;
không thay giá trị giờ hoặc mapping ca làm. Regression UI01/UI02/UI03/UI04,
editor Shift và Designer.

## B02 — Bảng không đủ chỗ hiển thị hàng ở 1280×720, DPI125% [P1]

**Hiện tượng:** có bản ghi DM001 trong DB và accessibility tree, nhãn báo 1 dòng,
nhưng vùng bảng chỉ còn một dải hàng mỏng, không đọc được mã/tên.

**Tái hiện:** mở fixture ADMIN với Form.Size=1280×720 ở DPI120, có danh mục
`DM001 / Orbit UI thử nháp tiếng Việt`; để cửa sổ normal, chưa maximize.
Đã đóng/mở lại và tái hiện lần nữa. 1366×768 đọc được hàng đầu; không suy ra mọi
viewport lớn hơn đều đã đạt.

**Bằng chứng:** `main-1280-loaded-reproduction.png` là lần tái hiện ổn định;
`main-admin-1280x720.png` trước thêm; `product-after-save.json` đối chiếu bản ghi;
`main-ADMIN-1280x720-geometry.json` xác nhận kích thước và DeviceDpi.
Ảnh `main-1366x768-125.png` đối chiếu kích thước lớn hơn.
Một số ảnh giữa lượt có cửa sổ đã maximize do thao tác ngoài agent; không dùng
chúng làm chứng cứ đúng kích thước 1280×720.

**Nguyên nhân có bằng chứng:** layout danh mục có heading AutoSize, toolbar
AutoSize+WrapContents, status AutoSize và chỉ hàng giữa Percent nhận phần còn lại
(`CatalogsControl.Designer.cs:46–54`). Toolbar xuống hai dòng ở chiều rộng này;
heading/padding/header chiếm phần lớn chiều cao, không có ràng buộc bố cục bảo vệ
vùng hàng dữ liệu. Style grid cũng tăng padding/chiều cao header. Chưa đo để quy
kết riêng một kích thước hay khẳng định double-scaling là nguyên nhân duy nhất.

**Đề xuất:** điều chỉnh bố cục theo bề rộng sử dụng: giảm footprint heading ở cửa sổ
thấp, đặt phân trang vào footer, bố trí toolbar/tìm kiếm co giãn và bảo vệ vùng bảng
đủ đọc hàng; giữ cột cần thiết và đường cuộn. Không giảm font để ép vừa, không thay
query, paging 50 dòng, binding hoặc action. Nếu cần thêm container chỉ phục vụ layout.

**Ảnh hưởng/rủi ro:** thay cha control có thể ảnh hưởng tab/focus; phải giữ thứ tự
và event, kiểm 2 kích thước × 3 DPI, bảng 0/1/nhiều dòng và các cột tùy tab.
Regression UI01/UI03/UI04/UI08/UI11 và Designer.

## B03 — Tiêu đề cột nhiều dòng bị cắt [P2]

**Hiện tượng:** ở tab Đối tác, phần `(%)` của “Chiết khấu (%)” bị cắt; ở tab
Chức vụ, dòng cuối của “Hệ số tăng ca” bị cắt. Tái hiện ở DPI125%, kể cả khi
cửa sổ lớn, độc lập với B02.

**Bằng chứng:** `catalog-delivery.png`, `catalog-position.png`.

**Nguồn liên quan:** `AppTheme.cs:117` đặt padding cell và dòng 124 đặt chiều cao
header theo một mức tối thiểu cố định. Cột Ratio/Factor có bề rộng cố định trong
Designer; không có bước đo header theo font/padding/WrapMode thực tế. Độ cao
thực tế không đủ cho phần text đã wrap, quan sát trực tiếp trên ảnh.

**Đề xuất:** tách padding/đo chiều cao header khỏi data row; dùng AutoSize/đo
PreferredSize phù hợp font/DPI và bề rộng cột, hoặc tăng bề rộng khi còn chỗ.
Giữ nguyên tên cột, DataPropertyName, định dạng số và dữ liệu; không đổi nhãn để né lỗi.

**Ảnh hưởng/rủi ro:** header cao hơn làm bảng còn ít chỗ hơn, phải xử lý cùng B02.
Kiểm tất cả cột 7 tab, tiếng Việt, DPI100/125/150%, bảng G01 dùng chung AppTheme.
Regression UI01/UI03/UI07/UI08.

## Phạm vi đợt sửa đề nghị

Sửa tuần tự B01 → B02 → B03, chỉ tại WinForms; build/Designer và regression theo
ảnh hưởng của cả đợt sau sửa. Duyệt đợt debug bao gồm regression các bug này;
các ca BLOCKED/NOT RUN không tự chuyển PASS. Bug mới ngoài đợt hoặc đổi control
làm đổi hành vi cần trình bổ sung. Chưa có quyền commit/push.

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
