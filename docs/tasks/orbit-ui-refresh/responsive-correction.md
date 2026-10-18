# Đợt sửa bố cục thích ứng — B04–B09

Ngày 17/10/2026. Người dùng yêu cầu hỗ trợ maximize, restore và chia đôi màn hình.
Đây là yêu cầu bắt buộc của bản sửa UI, không được coi maximize là thao tác ngoài
phạm vi kiểm chứng. Sau yêu cầu tiếp theo của người dùng “tối ưu hóa lại bố cục
nhưng vẫn giữ đúng luồng UX và luôn theo sát kế hoạch dự án”, đã triển khai đợt này.
Kết quả thực tế tại [responsive-results.md](responsive-results.md).

## Bằng chứng và nguyên nhân

| ID | Hiện tượng | Bằng chứng source / kết luận |
|---|---|---|
| B04 | Login và editor kéo rộng theo cửa sổ | LoginForm.Designer: layout Fill, cột100%, input Dock.Top, không giới hạn bề rộng nội dung. CatalogEditorDialog.Designer tương tự. Maximize làm input nhận toàn bộ bề rộng |
| B05 | Nút cách xa trường, trạng thái thành khối lớn | Editor dành hàng fields Percent100; actions nằm dưới cùng. Login dành hàng status Percent100 và status Dock.Fill. Khoảng dư được phân cho vùng nội dung/trạng thái thay vì vùng căn bố cục |
| B06 | Textbox/tab/giờ thiếu đồng bộ | TextBox và DateTimePicker native; tab hiện có mới tô chrome. B01 đã chứng minh giờ trắng ở DPI125. Chưa có container field như guide khuyến nghị |
| B07 | Shell thiếu chế độ hẹp, khó chia đôi | MainForm sidebar Absolute232, nút194, MinimumSize900×520; danh mục toolbar chỉ wrap, chưa có chuyển bố cục toàn shell. Trên vùng nửa màn hình nhỏ hơn minimum không thể bảo đảm fit |
| B08 | Nhận diện phẳng, banner lấn workspace | OrbitHeader chỉ LinearGradientBrush cho hình cầu và DrawEllipse cho quỹ đạo; không có highlight, bóng cầu hay che khuất vòng trước/sau |
| B09 | Empty state thành khối bảng lớn | Catalogs dành hàng tabs Percent100, chỉ status footer thể hiện không dữ liệu; chưa có vùng empty state có thông điệp trong workspace |

Phản ánh người dùng và các ảnh regression trước là chứng cứ thị giác. Source giải
thích cơ chế kéo giãn; chưa có kết quả test split-screen hoặc DPI100/150 mới.

## Phương án sửa cả đợt

1. **Login:** card rộng tối đa khoảng440 logical px, căn giữa vùng khả dụng; input
   và nút nằm trong card, status AutoSize. Khi hẹp, card co theo padding16; khi thấp,
   cuộn toàn nội dung. Maximize vẫn giữ tỷ lệ card, không kéo dài ô nhập.
2. **Editor:** card rộng tối đa khoảng640 logical px, các trường AutoSize theo
   loại danh mục đang hiện; nhóm hành động ngay sau trường và lỗi. Không dùng
   khoảng trống Percent giữa fields/actions. Chiều cao thiếu thì cuộn vùng card;
   giữ focus và đưa trường lỗi vào vùng nhìn. Không tắt maximize để né vấn đề.
3. **Shell theo ClientSize quy đổi96DPI:** từ1100px dùng sidebar đầy đủ; 760–1099px
   dùng navigation gọn; dưới760px chuyển navigation thành hàng/menu gọn phía trên.
   Các mốc là giá trị thiết kế ban đầu, điều chỉnh theo PreferredSize khi triển khai.
   Tên màn/tooltip/accessibility còn đủ; giữ quyền và handler hiện tại.
4. **Danh mục:** nhóm tìm kiếm và nhóm thao tác tách rõ; cửa sổ hẹp xuống dòng có
   chủ đích. Tab hỗ trợ nhiều hàng, selected/focus/hover cùng token và hình thức
   Orbit. Bảng còn vùng đọc được; cột tối thiểu + cuộn ngang khi thiếu chỗ, không
   giảm font hoặc ẩn dữ liệu nghiệp vụ để ép vừa. Footer trạng thái gọn.
5. **Control:** container bo góc/viền/focus thống nhất quanh TextBox native, giữ IME,
   clipboard, password masking, ErrorProvider, binding và TabIndex. Với giờ: editor
   HH:mm có skin, adapter giữ Value kiểu DateTime và ValueChanged, không thay request
   hoặc quy tắc BLL. Đây là thay đổi tương tác cần duyệt trong đợt này; không coi ô
   trắng là ngoại lệ mặc định khi người dùng đã yêu cầu đồng bộ.
6. **Phân cấp:** heading gọn; nội dung/form trong card có label và khoảng cách rõ;
   giảm trang trí ở cửa sổ thấp/hẹp. Hành tinh có gradient cầu, điểm sáng, bóng và
   vòng trước/sau theo reference; không thêm animation/timer hoặc asset online.
7. **Trạng thái rỗng:** thông điệp, icon và hướng dẫn ngắn ngay trong vùng làm việc,
   phân biệt không có dữ liệu với không tìm thấy. Không tạo tổng0 giả khi lỗi;
   loading/error/conflict vẫn giữ contract cũ. Không tự gọi ghi hoặc tải dữ liệu mới.

## Phạm vi và rủi ro

Chỉ Forms, Modules presentation, Theme và UI helpers; các file dự kiến gồm
Login/Bootstrap/CatalogEditor/Main/Startup, Catalogs/Home/System, AppTheme,
OrbitHeader/OrbitTabControl và helper field/layout tối thiểu. Không sửa DB, DAL,
BLL, Models, auth/session/quyền, query, paging, validation hay nội dung nghiệp vụ.

Reparent control có thể ảnh hưởng tab order/ErrorProvider; giờ có thể ảnh hưởng
nhập/phím; resize có thể gây nhảy focus hoặc mất nháp. Phải giữ instance control,
không dựng lại module/form khi đổi breakpoint, không reset input/selection/scroll
ngoài việc đưa nội dung bắt buộc vào vùng nhìn. Không mở rộng thành framework UI.

## Bộ kiểm chứng sau sửa

- Fullscreen/maximize và restore trên màn hiện có; resize liên tục qua breakpoint.
- Cửa sổ1280×720,1366×768; snap trái/phải nửa working area thật. Thêm viewport hẹp
  khoảng640–720px nếu màn hiện có cho phép; ghi rõ Form/ClientSize và DPI thực tế.
- DPI100/125/150%: chạy thật mới PASS; thiếu môi trường phải BLOCKED.
- Login, Bootstrap, Startup, Main/Home/System,7tab và7loại editor: không cắt chữ,
  không kéo input vô hạn, nút gần trường, empty/loading/error phân biệt.
- Nhập tiếng Việt/nháp, resize/maximize/snap không mất dữ liệu; giờ00:00/23:59,
  input sai, Tab/ShiftTab/mnemonic/Enter, password và ErrorProvider; hành vi gửi lệnh
  đúng contract cũ. Không nhập tự động vào hộp xác thực.
- Bảng0/1/nhiều dòng, tên dài/header nhiều dòng, paging, vùng cuộn, focus và Designer.
- Build Debug/Release, regression quy tắc liên quan; hash source cố định trong lượt
  test. Không coi screenshot HTML hay build là nghiệm thu WinForms.

Chỉ triển khai các màn đang có của T06–T08 và màn kiểm tra G01. UX01/UX02/UX09
quy định luồng; kế hoạch mục12–13 quy định phạm vi/trạng thái; docs/ui quy định
hình thức. Không thêm POS hay module T09–T32, không sửa checklist325ca hoặc tự
đánh dấu task hoàn tất. Yêu cầu tiếp theo của người dùng là chỉ thị triển khai
đợt chỉnh bố cục này; không xin duyệt lại từng màn.

Điều chỉnh khi hiện thực: Login giới hạn480 logical px; dưới1000 dùng navigation
hàng trên có wrap, không thêm rail icon trung gian. Danh mục dưới780 dùng ComboBox
chọn nhóm thay tab nhiều hàng để giữ vùng đọc dữ liệu. Cùng7nhóm, cùng SelectedIndex,
query và handler; không thêm bước xác nhận. Editor giữ footer trong card; chỉ fields
cuộn khi thiếu chiều cao. Các mốc dựa trên ClientSize quy đổi96DPI.
