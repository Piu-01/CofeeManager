# Orbit — tám màn còn lại

Ngày 21/10/2026. Đã triển khai tám màn và xác nhận bản R11 trên app thật ở Scale 125% sau khi người dùng đăng nhập. Các lỗi sidebar, tab và overlay header đã qua kiểm tra lại; bàn giao để xem giao diện. Không đổi trạng thái nghiệm thu nghiệp vụ G03/T01–T32.

Menu, Bàn/Sơ đồ, Khu vực, Khách hàng, Khuyến mãi, Chính sách hạng, Lịch sử điểm và Danh mục nền đã dùng control WinForms Orbit chung. Header, tìm/lọc, tab, action và phân trang nằm ngoài vùng bảng cuộn. Giữ nút/handler thật, Sửa/Xóa liền nhau, batch50 hiện có; hạng không có icon cạnh tên. Inventory/sidebar được regression. Không WebView, ảnh chụp làm nền hoặc stardust.

Business, DataAccess, Models không đổi so baseline; không sửa SQL. Hash chứng minh phạm vi source, không thay thế kiểm thử mọi giao dịch ghi.

## File đã sửa

22file WinForms (18sửa,4mới), danh sách/hash đầy đủ tại [source-changes-r11.csv](runs/source-changes-r11.csv); chi tiết ở [audit.md](audit.md).

| Nhóm | File trong src/CafeManager.WinForms |
|---|---|
| Shell/dialog | Forms/MainForm.cs; Forms/CustomerPickerDialog.cs |
| Điều hướng | Navigation/NavigationController.cs |
| Màn | Modules/Menu/ProductsControl.cs; Modules/Tables/TablesControl.cs; Modules/Customers/CustomersControl.cs; Modules/Loyalty/LoyaltyControl.cs; Modules/Catalogs/CatalogsControl.cs và CatalogsControl.Designer.cs |
| Theme | Theme/OrbitManagementTheme.cs |
| Control đã có | UI/OrbitButton.cs; OrbitDataGridView.cs; OrbitEmptyState.cs; OrbitFilterBox.cs; OrbitListWorkspace.cs; OrbitManagementTabs.cs; OrbitPagination.cs; OrbitSidebar.cs |
| Control mới | UI/OrbitRecordListControl.cs; OrbitSectionTabs.cs; OrbitTableCard.cs; OrbitViewSwitch.cs |

Đã bỏ field/layout Catalog cũ do lần thay này làm dư. Giữ RecordListControl/OrbitManagementLayout vì dialog hóa đơn khách và MainForm còn dùng. Không xóa lịch sử tài liệu, không commit/push.

## Kiểm chứng

Build Debug/Release R11 sạch. Rules42/42 PASS ởR3; source rules/backend giữ nguyên. Fullprobe R11 tại120DPI:19snapshot,30resize,7tab danh mục,7biên phân trang,lọc/chọncard/mode/empty đềuPASS; khôngexception. Probe shell MainForm125%:sidebar250physical=200logical,tab khôngscrollbar/tràn; PASS. Probe kiểm control/binding, không thay thế app đã đăng nhập.

App thật R9 đã kiểm chuyển màn cache và tab125/150%; người dùng xác nhận chữ150% rõ. R10 xác nhận header Points125% không còn bị overlay che. R11 căn lại shell sau autoscale và tab sau khi HWND con được tạo. App thật khởi tạo ở 125%: sidebar đúng 200 logical, tab Danh mục/Khuyến mãi không có scrollbar thừa; Points rỗng giữ đủ header. Chọn KH001 qua dialog thật: 3433 điểm, BKIM, 4 giao dịch; footer hiển thị 1–4 / 4, hai nút phân trang khóa đúng. PASS các ca hiển thị/đọc này; không ghi DB.

DBtest duy nhất CoffeeManager_Test_G03_20261019_215457_29e552a9_demo có marker. Không ghi giao dịch trong đợt này. [test-results.md](test-results.md) và [bug-report.md](bug-report.md) giữ từng lượt source, expected/actual, logFAIL và kết quả sửa.

## Ảnh app thật

Đủ9màn R8 ở100%, client1366×768. R9–R11 bổ sung sửa DPI/khởi tạo, giữ bố cục chuẩn100%:

- [Menu](runs/native-r8-02-menu-100-0.png)
- [Bàn/Sơ đồ](runs/native-r8-03-tables-100-0.png), [Khu vực](runs/native-r8-04-areas-100-0.png)
- [Khách hàng](runs/native-r8-05-customers-100-0.png)
- [Khuyến mãi](runs/native-r8-06-promotions-100-0.png)
- [Chính sách hạng](runs/native-r8-07-ranks-100-0.png)
- [Lịch sử điểm](runs/native-r8-08-points-100-0.png)
- [Danh mục phương thức](runs/native-r8-09-methods-100-0.png)
- [Nguyên liệu regression](runs/native-r8-01-inventory-100-0.png)

Bổ sung: [Menu R9 tại100%](runs/native-r9-02-menu-100-0.png), [Menu125%](runs/native-r9-02-menu-125-0.png), [Menu150%](runs/native-r9-02-menu-150-0.png), [Khuyến mãi125%](runs/native-r9-06-promotions-125-0.png), [Points R10 tại125% sau sửa overlay](runs/native-r10-08-points-125-0.png). Ảnh DPI cao qua công cụ có thể mất nét; xác nhận trực tiếp được ghi riêng. Không tuyên bố pixel-perfect.

Ảnh R11 cuối ở 125%:

- [Danh mục / Phương thức](runs/native-r11-09-methods-125-0.png)
- [Khuyến mãi](runs/native-r11-06-promotions-125-0.png)
- [Lịch sử điểm chưa chọn khách](runs/native-r11-08-points-empty-125-0.png)
- [Lịch sử điểm KH001](runs/native-r11-08-points-selected-125-0.png)
- [Hộp chọn khách](runs/native-r11-customer-picker-125-0.png)

## Chưa kiểm chứng / còn mở

Native R11 sau đăng nhập ở 125% đã xác nhận. Hiện giữ Scale 125% theo yêu cầu. Ghi CRUD đầy đủ sau đổiUI, VisualStudioDesigner và resize MainForm từngmàn ởmọicỡ chưa chạy đầy đủ. EditorSP005 hiển thị sai danh mục so grid đã Hủy, không Lưu; bug ngoài thay đổi hình thức vẫn OPEN, chưa tự sửa handler. Không báo toàn bộ nghiệp vụ PASS từ việc nút còn tồn tại.


## Cập nhật 22/10/2026 — Inventory50
Theo yêu cầu mới, Nguyên liệu/Tồn hiển thị tối đa50dòng/trang thay7. Bảng cuộn bên trong khi đủ dòng; chiều cao dòng/nút/query/nghiệp vụ không đổi. Danh mục nền giữ dòng đếm vì là dữ liệu thật. Releasebuild0warning/error; probe10PASS gồm0/14/49/50/51,cuộnđếndòng50,Trước/Sau,lọc. Hai assertion resize giữFAIL do giao1pxbody/footer125% cósẵn; chưa tựsửa ngoài phạmvi. Chưa chạy lại app đầyđủ sauđăngnhập; xem test-results và bug-report để biết giới hạn.


## Bỏ nhãn đếm ở mọi trang — 22/10/2026
Theo yêu cầu mới, OrbitListWorkspace không hiển thị Count khi layout; OrbitManagementLayout cũ cũng ẩn Count. Áp dụng các trang và danh sách/dialog dùng hai layout chung. Giữ RowActions và footer phân trang; không thêm chữ thay thế. Releasebuild0warning/0error; chưa chụp/kiểm lại app đang đăng nhập. Cần chạy bản build mới để thấy thay đổi.


## Bỏ hướng dẫn tĩnh cuối dialog — 22/10/2026
Đã xóa4AddNote tĩnh cuối biểu mẫu: sản phẩm, công thức, nguyên liệu và khuyến mãi; không thêm nội dung thay thế. Giữ thông tin bản ghi ở đầu dialog, xác nhận xóa/ghi, preview kết quả và lỗi/validation. Chỉ sửa3file module UI: ProductsControl.cs, MaterialsControl.cs, LoyaltyControl.cs. Releasebuild0warning/0error. Chưa kiểm lại ảnh app sau đăng nhập; không ghi DB.


## Thu gọn hộp chọn khách — 22/10/2026
Đã bỏ toàn bộ thao tác quản lý khỏi picker; chỉ tìm/đọc/chọn/đóng và phân trang. Preview ưu đãi còn lựa chọn Khách vãng lai theo luồng hiện có; Lịch sử điểm không có. Trang Khách hàng chính giữ nguyên. Releasebuild sạch;6/6ca cấu trúc controlPASS, chưa thao tác lại app đầy đủ sau đăng nhập. Chi tiết bug-report/test-results và runs/customer-picker-actions-results.json.


## Xác nhận hộp chọn khách trên app thật — 22/10/2026
Đã đăngnhậpadmin và kiểmtra: picker chỉ Chọnkhách/Đóng, khôngactionquảnlý; chọnKH001đọc3433/BKIM và4giao dịch; Đónggiữkhách; trangKháchhàngchínhvẫnđủaction.4caPASS. Tìmkiếm chưa xácnhận vì côngcụ không nhập được vào modal; Escape/PreviewnativeNOTRUN. Ảnh runs/native-picker-clean-20261022-0.jpg; chi tiết test-results.md. KhôngghiDB/khôngsửasource trong lượt test.
