# Hộp nghiệp vụ nổi bên trong CoffeeManager

## Điều chỉnh mới nhất — hộp thoại Windows có owner

Sau khi dùng thử bản host, người dùng xác nhận cách hiểu đúng là: “Hộp thoại
Windows như trước, có thanh tiêu đề/X, đi cùng cửa sổ chính và không có taskbar
riêng”. Phần này THAY THẾ yêu cầu container/card/TopLevel=false bên dưới; giữ các
đoạn cũ làm lịch sử của phạm vi đã triển khai, không dùng để ghi đè ý mới.

Hộp nghiệp vụ dùng Form modal có owner chính xác (MainForm hoặc hộp cha đang
mở), titlebar/X native, căn giữa cha và ShowInTaskbar=false. Windows khóa owner
trong khi hộp mở; hộp cha/con đi cùng vòng đời app. Không nhúng Form vào panel,
không titlebar giả, không MDI. Hộp có thể nằm vượt vùng client như hộp Windows
bình thường, vẫn clamp WorkingArea; không ép nó nằm trong khung app. Giữ theme/
control/nội dung đang có, chọn hết input chung và guard nghiệp vụ. Hộp máy in/
lưuPDF native nhận owner là hộp đang hoạt động.

Đợt test host đã chạy giữ nguyên source và bằng chứng, chưa nghiệm thu kiểu hiển
thị. Plan điều chỉnh tại native-dialog-adjustment-plan.md, chờ duyệt trước sửa.

Trạng thái: requirements và plan (gồm chọn hết nội dung input) đã được người dùng
duyệt26/10/2026; đã triển khai, chờ duyệt đợt test. Áp dụng quy trình task lớn
độc lập tại `.agents/skills/task-approval-workflow/SKILL.md` vì thay cơ chế hiển thị
chung của nhiều module, không thay nhóm G01–G08 hoặc checklist325ID.

## 1. Mục tiêu và quyết định người dùng

Người dùng chọn: “Hộp nổi bên trong app, nền phía sau bị khóa; không thêm cửa sổ/
taskbar riêng”. Mỗi phiên vẫn có một cửa sổ CoffeeManager; các hộp nghiệp vụ từ
sidebar hoặc từ màn con được hiển thị trong vùng client của cửa sổ chính.

Hộp nổi ở trong khung app, co theo vùng còn lại khi resize, có tiêu đề và hành
động đóng/quay lại theo nghiệp vụ. Khi hộp mở, nội dung nền và sidebar không nhận
chuột/bàn phím hoặc mở thêm thao tác. Không cho kéo hộp ra desktop, minimize hoặc
tạo taskbar entry riêng. Chỉ tắt ShowInTaskbar của Form top-level chưa đáp ứng
toàn bộ yêu cầu này.

## 2. Hiện trạng và phạm vi

MainForm đang chứa UserControl của các tab và POS. Nhiều editor/detail vẫn gọi
ShowDialog(owner); SalesUi.Fit chưa đặt ShowInTaskbar=false đồng nhất. Một số
dialog dùng OrbitDialogLayout/OperationEditorDialog đã tắt taskbar nhưng vẫn là
Form top-level. Rà cả caller lẫn vòng đời đóng/return để tránh hộp cũ chồng nhau.

Chuyển các hộp nghiệp vụ hiện có sang cơ chế hộp nổi dùng chung:

- Thêm/sửa/xóa/xem của danh mục, nguyên liệu, menu/công thức, bàn/khu vực,
  khách hàng, khuyến mãi/hạng/điểm; các editor dựng bằng OperationEditorDialog.
- POS: nhập/sửa món, F3 khách/ưu đãi/giao hàng, thêm/chọn khách, F4 đơn đang mở,
  F8 thanh toán, xem chi tiết hóa đơn và xem chi tiết/chuyển bước giao hàng.
- Preview hóa đơn và các hộp xác nhận do app sở hữu, gồm xác nhận bỏ nháp,
  hủy/chốt và câu hỏi in sau thanh toán. Cửa sổ POS vẫn là nội dung MainForm;
  hành động Quay lại POS phải đóng hộp và điều hướng đúng một lần.

Tận dụng theme, layout, control Orbit và nội dung nghiệp vụ đã có; chỉ bổ sung
cơ chế host/modal và thành phần hiển thị chung thật sự cần thiết. Không tạo một
bản overlay riêng cho từng màn. Không dùng MDI nhiều cửa sổ con tự do.

Các hộp của hệ điều hành như chọn máy in, lưu PDF/chọn file vẫn theo native OS;
chúng cần owner đúng để quay về hộp app hiện hành. Đăng nhập, bootstrap/startup
và cửa sổ chính thuộc vòng đời ứng dụng, không phải editor mở từ sidebar. Dropdown,
lịch chọn ngày và overlay hiệu ứng giữ vai trò control, không biến thành màn mới.

## 3. Hành vi bắt buộc

- Một cơ chế host dùng chung quản lý nội dung, kích thước, focus, trả kết quả và
  dispose. Chỉ hộp trên cùng tương tác. Mở hộp con trong hộp cha rồi quay lại phải
  giữ nguyên input/selection/bộ lọc của cha và khôi phục focus hợp lý.
- Nút đóng, Quay lại, Esc và đóng cửa sổ chính tuân thủ nháp/busy/unknown/session.
  Nháp hỏi bỏ khi cần; không tự hủy giao dịch SQL đang chạy khi chỉ đóng UI.
- Khi đang chốt, chặn đóng/gửi lặp/đổi tab; khi kết quả chưa biết giữ đường kiểm
  tra kết quả. Enter không tự chốt tiền/kho/nợ; F2/F3/F4/F8 không xuyên xuống POS.
- Thành công trả kết quả một lần, đóng đúng hộp, refresh đúng màn. Thanh toán vẫn
  hỏi in sau commit và reset POS kể cả chọn Không/hủy hoặc lỗi in.
- Không tạo owner lạc, hộp trùng, focus rơi xuống nền hoặc taskbar entry cho hộp
  app. Thu nhỏ/khôi phục app mang theo nội dung hiện hành. Dispose/cancel callback
  muộn không cập nhật control đã đóng; logout không giữ nội dung của phiên cũ.
- Giữ validation đúng trường, dữ liệu typed, phân quyền cả UI/BLL và trạng thái
  tài chính đã khóa. Không thay SQL/contract, role ADMIN/CASH/STORE hay nghiệp vụ.

## 4. Phạm vi kiểm chứng sau triển khai

Bổ sung theo yêu cầu người dùng26/10: các input có thể sửa trên mọi màn/hộp phải
chọn hết nội dung ở lần vào ô bằng click/Tab; click tiếp trong cùng lần focus
vẫn đặt caret/chọn đoạn bình thường. Bao gồm tìm kiếm, tiền, số lượng, ghi chú,
text editor trong grid và ComboBox cho phép gõ. Ô readonly/disabled và danh sách
chỉ chọn không áp dụng. Dùng helper chung từ SelectAllOnEntry sẵn có, không thay
giá trị, không tự lưu/submit khi focus. Áp dụng cả input động/editor tái sử dụng,
không gắn handler trùng; kiểm nhập tiếng Việt/IME, paste và Enter vẫn đúng contract.

Build Debug/Release; kiểm WinForms thật tại125% (DeviceDpi120) về bounds, focus,
keyboard, modal lồng nhau, close/cancel/nháp/loading/unknown, refresh/điều hướng,
không sinh cửa sổ top-level/taskbar cho hộp app, và dispose tài nguyên. Không tự
đổi Windows Scale;100/150% chưa chạy ghi NOT RUN. Hình thức do người dùng kiểm,
agent ghi SKIPPED theo phân công, không chụp/so ảnh để nghiệm thu hình thức.

Hồi quy theo màn bị đổi, đặc biệt F3validation, F4mở một lần/đóng hết, F8chốt và
hỏi in/reset, detail/giao hàng và ba role. Giao dịch kiểm trên SQL Server thật,
DB test riêng với snapshot tiền/kho/điểm/receipt; không ghi test vào development.
Không tự gán PASS cũ cho host mới hoặc xóa lịch sử G04. Đợt test có duyệt riêng;
duyệt requirements không đồng nghĩa duyệt plan, triển khai hoặc chạy test.

## 5. Tiêu chí hoàn tất

Toàn bộ hộp app trong phạm vi mở trong MainForm, khóa nền và giữ luồng nghiệp vụ;
không còn đường mở cửa sổ nghiệp vụ top-level. Dialog hệ điều hành có owner đúng.
Các ca bắt buộc theo plan đạt, hình thức được người dùng xác nhận riêng, không
còn lỗi chức năng chưa xử lý. Cập nhật UX/style implementation và hồ sơ kiểm
chứng đúng thay đổi; không tự chốt G04/G05 chỉ từ refactor này.
