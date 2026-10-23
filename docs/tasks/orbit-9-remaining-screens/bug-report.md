# Kiểm tra và tự sửa hình thức

R2 kết thúc, source-r2.csv cố định. Release 0 warning/error; UiProbe đọc SQL test có marker: 18 snapshot, 7 tab danh mục, lọc DM002 và 7 biên phân trang 0/1/49/50/51/100/101. Không exception. Ảnh probe là bitmap control trong process ở DPI120, chưa là nghiệm thu GUI app thật.

Điểm lệch đã quan sát: tab chọn có vạch trái vốn dành sidebar; footer bị Label che đường kẻ; Tải lại ở chính sách dùng biến thể tab thay vì secondary. Points trống thiếu empty prompt trong bitmap. Source: khi query hoàn tất lúc page cha còn ẩn, empty.Visible getter trả false nên BringToFront bị bỏ qua, grid còn nằm trên overlay. R3 đặt z-order dựa rowCount, tránh phụ thuộc effective visibility.

Tự sửa trong phạm vi hình thức đã được yêu cầu: giới hạn vạch trái cho Navigation, chừa 1px đường kẻ footer, secondary refresh, empty.BringToFront theo số dòng. Không đổi backend/handler. Kết thúc R2 trước sửa; regression R3 độc lập. Kế thừa quyền tự sửa điểm lệch của yêu cầu Orbit, không mở đợt bug G03 hay thay trạng thái T01–T32.

R3 kết thúc: Debug/Release 0 warning/error, rules42/42 PASS; probe96/120 không exception, pagination7/7; app thật client1366×768 ở100%: Menu lọc DM002→2, Bàn map/list, Khu vực, Khách, KM, Hạng, Điểm (empty icon hiện đúng; chọn KH001→4 giao dịch), Catalog phương thức, Inventory regression đã chụp. Ảnh DrawToBitmap không có overlay là giới hạn thứ tự vẽ bitmap, native empty đã xác nhận. Không kết luận native từ bitmap.

R4 hình thức còn cần sửa theo ảnh/source: tên card bàn dùng Body14px nhưng HTML20px; RowActions cố định33px cắt nút secondary40px ở policy và picker; AppTheme.Apply(CustomerPickerDialog) ghi đè SearchBox/TextBox bằng style cũ; tabs padding ngang20 thay vì34. Chuyển mode bàn đang là hai tab độc lập, cần nhóm segment đúng HTML. Tự sửa hình thức đúng scope trước lượt regression mới; không đổi handler/query/schema.

R4 lượt probe96 đã kết thúc, không exception/boundary fail. Xem bitmap phát hiện tab gạch chân bị ellipsis: preferred padding10px không đủ với vùng text chừa7px mỗi bên. R5 trả padding loại tab này20px như R3; giữ pill tab34px. Đây là sửa một giá trị đo kích thước trong control chung. Chưa nhận ảnh native R4, không gọi R4 đạt hình thức.

## Ngoài thay đổi hình thức: editor sản phẩm chọn sai danh mục
R5 native: tìm SP005 (grid DM002), bấm Sửa, editor hiển thị Ca phe thay vì Tra sua. Evidence native-r5-menu-editor-cancel-before-*.png/.txt. Đã Hủy, không Lưu. Source Edit đặt ComboBox.SelectedValue trước AddField/BindingContext; đây là giả thuyết binding cần điều tra riêng, chưa kết luận nguyên nhân. Body Edit giữ từ trước đợt UI; chưa có snapshot source nội dung trước đợt để chứng minh lịch sử từng dòng. Không tự sửa handler ngoài phạm vi đã chốt. Trạng thái OPEN, không báo nghiệp vụ editor PASS.

R5 kết thúc: build Debug/Release sạch; probe96 (19screen,38resize) lọc/menuempty/tablearea/list/map và7biênpage đều PASS. Native search Enter SP005=1, cuộn bảng trong vùng body PASS, editor mở và Hủy PASS về đóng dialog nhưng chọn danh mục FAIL riêng ở trên. Native card B0003: status đọc đúng HD20250410, viền chọn vẫn ở B0001. Source SelectionChanged gọi HighlightCards đọc CurrentRow trước khi CurrentCell đổi hoàn tất. R6 chuyển đồng bộ highlight sang CurrentCellChanged và gọi HighlightCards sau khi đặt CurrentCell trong click card. Giữ query/use case. Cần probe xác nhận CurrentRow và card Selected cùng B0003; nếu CurrentRow còn sai phải điều tra tiếp, không chỉ đổi màu.

## R6 kết thúc — lỗi font khi đổi DPI trực tiếp (FAIL)
Người dùng xác nhận chữ app thật cũng mờ/đứt nét ở150%, không chỉ ảnh công cụ. Scale đã trả100% trước điều tra, sau đó tái hiện riêng bằng DpiProbe không DB. Log runs/dpi-live-before.jsonl:96→144, Label.Font19.5→29.25, Grid.Font9→13.5; DefaultCellStyle.Font vẫn9, Header vẫn7.5, Graphics.DpiY vẫn96 và đo chữ vẫn71×15. RowHeight46→69. Vì font style/vẽ trực tiếp lấy static Theme.Font theo point, chúng không theo font đã scale của Control. Cỡ chữ và hình học không cùng tỷ lệ. Chưa kết luận mọi hiện tượng mất nét chỉ do nguyên nhân này.

Hướng tự sửa trong yêu cầu Orbit đã duyệt: font dùng cho style bảng và vẽ trực tiếp (badge/card/empty/avatar) lấy kích thước pixel từ token logical96×DeviceDpi; cache font theo DPI trong Theme, cập nhật style ở ApplyMetrics. NativeLabel/control.Font tiếp tục do WinForms scale. Không thay handler/SQL. Regression mới tách khỏiR6: so sánh đo chữ96/120/144 và đổi live100→125→150→100 trên cửa sổ thật, rồi app thật. Không công nhận hình thức DPI chỉ bằng DrawToBitmap. R5 resize thực tế30case, không phải38 ghi nhầm ở trên.
R7 diagnostic: header đã đổi pixel12.5 ở120DPI nhưng body còn9pt. Điều tra tiếp source WinForms DefaultCellStyle getter: khi Alignment=NotSet nó trả bản sao đã bổ sung ambient defaults; sửa thuộc tính trên getter không lưu lại style gốc. Constructor hiện thiếu Alignment nên ApplyMetrics.Font/Padding vào bản sao. R8 đặt MiddleLeft rõ trong style gốc (đúng căn lề đang hiển thị), không vá OnFontChanged hay trì hoãn timer. Nguồn đối chiếu: https://raw.githubusercontent.com/dotnet/winforms/main/src/System.Windows.Forms/System/Windows/Forms/Controls/DataGridView/DataGridView.cs (DefaultCellStyle getter). Giữ log R7 FAIL; kết thúc lượt trước sửa.

## R8 native kết thúc — live DPI cache/tab FAIL
100% đã chụp đủ9màn native-r8-01…09. 100→125 trên app: points có scrollbar che tab (native-r8-08-points-125); quay Menu cache có row/title nhỏ hơn (native-r8-02-menu-125-fail). Không chốt150/native.
DpiProbe khôngDB mô phỏng NavigationController.Controls.Clear/Add: form120/root120 nhưng cached96, sau Add cached vẫn96. Log dpi-cache-before.jsonl. Control bị tháo khỏi parent không nhận DPI change. Sửa UI navigation: cache vẫn gắn content, ẩn/hiện và BringToFront, không tạo lại dữ liệu hoặc đổi guard quyền/nháp. Tabs: log containerheight47,buttonpreferred48 ở125%; FlowLayout AutoScroll bật vì chênh1pixel và scrollbar che nhãn. Sửa giới hạn heightbutton theoClientSize, dự phòng horizontal scrollbar khi thật thiếu ngang. Đây là sửa hình thức đã được yêu cầu tự sửa, không đổi contract. Regression mới R9 gồm liveDPI/cachedswitch/tabs/narrow, giữ logR8FAIL.

## R9 native — kết quả và lỗi overlay header trống
Menu/cache và tab125/150 đã xác nhận trên app thật: native-r9-02-menu-125/150, native-r9-06-promotions-125/150. Người dùng trả lời trực tiếp “Chữ trên app đã rõ” ở150%. Không dựa độnét ảnhSky để kết luận ngược.
Points mởlầnđầusauDPI còn headerbịche (native-r9-08-points-150-header). Nguyên nhân: query khi chưa chọnkhách trả list rỗng đồngbộ trong VisibleChanged; PositionEmpty lấyHeaderHeight40 trước HWND tạo. HandleCreated.ApplyMetrics sauđó tăngheader60, overlay không nhận ColumnHeadersHeightChanged nên giữTop40, che20pxdướiheader. Probe dpi-lazy-visible chứngminhgridẩn96/header40, khihiện144/header60/font15 đãđúng. Không sửaFont thêm; R10 chỉ subscribe ColumnHeadersHeightChanged để PositionEmpty cậpnhậttop. LượtR9 kếtthúc trước sửa. Cầnregression offsetoverlay==header khi grid tạo muộn/DPI, nativepointsempty và chọnkhách.

## R10 native125 kết thúc
Points125 (native-r10-08-points-125): header không còn bị overlay che, PASS. Khi login tạoMainForm ở125 sauProgram khởiđộng100: sidebar banđầu250logical thay200, resizeWindow→sidebar200. TabCatalog/KM khởi tạo cóscrollbar dù đủngang; chuyểnsharedtab sangPointsthìhết, chứngminhrelayout sauHWND tạo khắcphục. R10nativekhởitạo125FAIL, không gộp với liveDPI R9 đãPASS.
R11hướngsửa nhỏ: MainForm.Shown gọiArrangeNavigation sauautoscale; OrbitSectionTabs.OnCreateControl gọiPerformLayout saukhiHWNDconđãtạo đểpreferredsize/DPIcuốiđượcápdụng. KhôngthayFlowPanelhaynghiệpvụ. Testmainformstartup125+tabwidth/resize vàđiềuhướngnative.


## R11 native regression cuối
App đã đăng nhập ở125%: shell/sidebar, tab Danh mục/KM và Points header rỗng đều PASS. Chọn KH001 đọc3433/BKIM và4giao dịch, header/footer đúng. Bằng chứng native-r11-09-methods-125, native-r11-06-promotions-125, native-r11-08-points-empty/selected-125. Các lỗi hiển thị R9–R11 đã qua regression trong phạm vi này; SP005 editor category vẫn OPEN, không sửa ngoài scope.

## 22/10/2026 — Phân trang Nguyên liệu/Tồn không khớp mô tả 50 dòng
Người dùng báo không thấy cuộn và hỏi 50 dòng/trang. Kiểm tra source xác nhận MaterialsControl.cs:17 đặt PageSize=7; LoadRows đọc hết các batch50 vào snapshot, RenderRows Skip/Take7. Vì vậy 50 là batch đọc DB, không phải trang hiển thị của màn này. Nếu 7 dòng vừa vùng bảng thì không có scrollbar; đây không chứng minh cuộn bị tắt. Trả lời trước đây rằng mọi màn giữ50dòng/trang là sai đối với Materials.
CatalogsControl.cs:80 lấy rows.Count thực từ kết quả query làm nhãn10danh mục sản phẩm, không phải số minh họa/hardcode; footer lặp lại cùng số. Điều kiện người dùng yêu cầu bỏ nếu chỉ để cho có không đúng với dữ liệu này; chưa xóa nhãn.
Đề xuất: đưa trang hiển thị Inventory về50 để thống nhất yêu cầu phân trang đã chốt, giữ filter/selection/handlers và rowheight, chỉ cuộn vùng bảng khi vượt chiều cao. Không sửa SQL/backend. Kiểm tra49/50/51dòng, lọc, trangcuối và resize; cần xác nhận phạm vi sửa trước triển khai, chưa sửa code hoặc chạy regression mới.

### Sửa phân trang đã được duyệt — 22/10/2026
Người dùng trả lời “đúng rồi làm đi”. Chỉ đổi MaterialsControl.PageSize từ7 thành50; không đổi query batch50, snapshot/filter, rowheight hoặc nghiệp vụ. Dòng đếm Danh mục giữ vì là số thật, đúng điều kiện đã giải thích trước khi sửa.
Regression control WinForms fixture:10caPASS,2caResizeFAIL do Body.Bottom vượt Pagination.Top1physicalpx tại125% (400/399;448/447). Grid vẫn50row và rowheight58px đúng cả2cỡ. Đây là làm tròn P(bodyTop)+P(height) trong OrbitListWorkspace đã tồn tại trước thay PageSize; không liên quan phân trang, không tự sửa thêm. Giữ expected/logFAIL; không thay assertion để né lỗi. Có thể xử lý riêng nếu được duyệt.


## 22/10/2026 — Bỏ nhãn đếm Danh mục nền
Người dùng xác nhận yêu cầu bỏ dòng chữ phía trên bảng. Trước đó agent hiểu sai điều kiện và giữ nhãn. Đã bỏ gán management.Count.Text trong CatalogsControl.RefreshRows cho toàn bộ tab Danh mục nền; label mặc định rỗng, không thêm nội dung thay thế. Giữ nguyên Sửa/Xóa, footer phân trang và dữ liệu. Không thay shared layout hoặc các màn khác. Chưa kiểm lại app sau đăng nhập.

## 22/10/2026 — Hộp chọn khách chỉ làm nhiệm vụ chọn
Nguyên nhân đã xác nhận trước sửa: CustomerPickerDialog nhúng CustomersControl(picker:true), nhưng constructor đăng ký các action quản lý trước nhánh picker; vì vậy modal có handler thật Thêm/Sửa/Xóa/Mở sổ/Hóa đơn. Backend vẫn DemandAsync theo quyền; đây là phạm vi UI quá rộng, không phải vượt quyền.
Người dùng duyệt sửa sau giải thích. CustomersControl hiện nhánh picker đăng ký Chọn khách/Đóng rồi return trước action quản lý. CustomerPickerDialog gắn CancelButton để Escape đóng. Lịch sử điểm không có Khách vãng lai; Preview ưu đãi truyền allowGuest=true giữ lựa chọn vãng lai cần cho luồng cũ. Trang Khách hàng không đổi handler/quyền/query; chọn khách vẫn GetAsync đối chiếu trước trả kết quả. Không thay backend/SQL.
Release build0warning/0error. CustomerPickerProbe:6/6PASS kiểm cấu trúc nút với fixture ADMIN/CASH và allowGuestfalse/true, CancelButton và trang quản lý vẫn giữ action. Không đăng nhập, không kết nối DB. Đây không phải test quyền backend hoặc thao tác modal trên app đầy đủ; chưa kiểm GUI sau đăng nhập. Bằng chứng runs/customer-picker-actions-results.json; probe không show form để tránh query/ghiDB.
File sản phẩm: Forms/CustomerPickerDialog.cs, Modules/Customers/CustomersControl.cs, Modules/Loyalty/LoyaltyControl.cs.


Native picker22/10: bốncaPASS (bỏactionquảnlý, chọnKH001/tải4giao dịch, trangchínhgiữaction, Đónggiữkhách). Tìmkiếm BLOCKED do côngcụ không nhập vào modal, Escape/PreviewnativeNOTRUN; chưa cóbằngchứng lỗiapp mới. Chi tiết test-results.md. Không sửa thêm trong lượt test.
