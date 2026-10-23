## Cập nhật20/10/2026

Đã được người dùng duyệt và đã triển khai. Chờ duyệt đợt test tại [test-request.md](test-request.md); xem [báo cáo triển khai](report.md). Các nhãn chờ duyệt ở nội dung gốc dưới đây là lịch sử trước triển khai.

# Thiết kế lại bố cục các màn quản lý Orbit

**Ngày:** 20/10/2026. **Trạng thái:** Người dùng chốt giữ cả cuộn/phân trang và yêu cầu thiết kế đúng bố cục ảnh; requirements được xác nhận ngày20/10/2026. Chưa sửa source/chưa chạy test. Đợt UI riêng tiếp nối G03 đã được người dùng chấp nhận nghiệm thu, không tự mở G04 hoặc sửa checklist T01–T32.

## 1. Mục tiêu và nguồn chuẩn

Thiết kế lại shell/sidebar và các màn quản lý theo 10 ảnh người dùng gửi ngày20/10/2026, kết hợp docs/ui/README.md, HuongDan_UI_Orbit_Cafe.md, reference.html, design-tokens.json, references/README.md, WINFORMS_IMPLEMENTATION.md và assets/README.md. Ảnh người dùng quy định bố cục màn cụ thể; guide quy định palette/font/icon/interaction. Ảnh là tham chiếu hình thức, không là dữ liệu runtime hoặc bằng chứng kiểm thử.

Giữ C# WinForms/.NET10, MainForm + UserControl, DataGridView/ComboBox native; không nhúng web. Không đổi tên sản phẩm CoffeeManager sang ORBIT chỉ vì logo trong mockup. Nếu có điểm xung đột, ưu tiên yêu cầu UI mới của người dùng trong phạm vi này và giữ nghiệp vụ/contract hiện có.

## 2. Phạm vi màn hình

| Ảnh | Màn cần chỉnh | Mục tiêu |
|---|---|---|
| 1 | Menu & công thức | Header/hành động chính; search/category/selling; toolbar phụ; grid giãn hết chiều ngang; footer phân trang |
| 2 | Nguyên liệu & tồn | Cùng khung danh sách; ưu tiên tên giãn rộng, lượng căn phải, đơn vị rõ |
| 3 | Bàn — sơ đồ | Card dùng toàn vùng nội dung, tên/mã/trạng thái/đơn thật; chuyển Danh sách/Sơ đồ để tránh hai vùng cạnh tranh chiều cao |
| 4 | Khu vực | Tab nhẹ dưới header, bảng mã/tên rộng, CRUD đúng quyền |
| 5 | Khách hàng | Search rộng, mở sổ/hóa đơn/sửa/xóa ở toolbar phụ; điểm/hạng/ưu đãi rõ |
| 6 | Khuyến mãi | Header riêng, tab nhóm ưu đãi nhẹ, ngày và phần trăm căn đúng |
| 7,9 | Lịch sử điểm | Hai ảnh cùng một màn; chọn khách/tải lại; empty state giữa vùng dữ liệu; không giả dữ liệu |
| 8 | Chính sách hạng | Bảng full width, ngưỡng/tỷ lệ căn phải; nút thay chính sách ở header |
| 10 | Danh mục nền | Các loại danh mục hiện có dùng tab nhẹ; header/search/CRUD/grid/footer nhất quán |
| Tất cả | MainForm/sidebar/header/status | Icon cho mọi mục, icon và chữ căn trái; active rõ; tên/vai trò đúng session, giữ Đăng xuất và mục điều hướng hiện có |

Trang phiên/Hệ thống chỉ chịu thay đổi shell dùng chung; không thiết kế lại nội dung. Dialog editor/công thức/chọn ngày hiện có giữ nguyên trong đợt này, trừ nối nút đang có vào vị trí mới. Không mở rộng sang POS hoặc màn chưa được gửi.

## 3. Bố cục thống nhất

- Sidebar gọn theo ảnh: icon cùng cột, chữ căn trái, khoảng cách/padding thống nhất; active dùng caramel, nền espresso. Không để chữ nằm giữa nút hoặc chồng icon. Giữ menu theo role và luồng điều hướng/đăng xuất.
- Header nội dung có tiêu đề/mô tả và một hành động chính góc phải. Breadcrumb phản ánh màn/tab hiện tại. Không thêm các nhãn kỹ thuật như SQL/workspace tiếng Anh chỉ vì mockup có.
- Dưới header là tab có phân cấp nhẹ khi có nhiều phân hệ. Giữ ý nghĩa tab thật, không biến nút CRUD/filter thành tab; không dùng viền hộp dày/đặt nhiều hàng hành động lẫn lộn.
- Khung dữ liệu: hàng tìm kiếm + filter hiện có + tải lại; hàng số dòng đang tải và hành động cho dòng chọn; vùng grid/card co giãn; footer phân trang. Hành động chính và phụ không chen cùng hàng tìm kiếm.
- Grid dùng hết bề rộng khả dụng. Cột tên/lý do nhận phần co giãn; mã/ngày/đơn vị/trạng thái có độ rộng hợp lý, tiền/lượng/điểm căn phải. Không kéo mọi cột bằng nhau, không bỏ các dữ liệu nghiệp vụ hiện có (ví dụ mốc mở sổ) chỉ vì ảnh không có cột đó. Header/selection/badge theo palette Orbit, trạng thái luôn có chữ.
- Dữ liệu ít vẫn giữ vùng dữ liệu cân đối như ảnh; không kéo giãn chiều cao dòng để lấp khoảng trống. Nội dung trống hiển thị trạng thái rỗng phù hợp; lỗi tải giữ thông báo riêng, không biến thành tổng0 giả.
- Nút cùng chức năng dùng chung kích thước/font/padding/icon giữa các màn, không phụ thuộc AutoSize từng caption hoặc việc màn đang maximize. Chốt token kích thước trong plan (nút chính, phụ, icon) theo guide và nhãn dài nhất; cùng DPI thì cùng kích thước, DPI khác scale theo hệ số. Không bắt tất cả chức năng khác nhau phải cùng độ rộng.
- Nút action theo selection/quyền/busy giữ hành vi hiện có; Tab/focus/tooltip rõ; không tạo đường gọi nghiệp vụ thứ hai.

## 4. Cuộn và phân trang — quyết định đề xuất

Source hiện tại RecordListControl/MaterialsControl/CatalogsControl gửi skip theo bội50; migrations004–008 dùng OFFSET @Skip ROWS FETCH NEXT50 ROWS ONLY. Cuộn hiện tại không tải phần ngoài lô. AutoSizeColumnsMode.DisplayedCells khiến cột co về trái, để trống phần bên phải.

Giữ phân trang server50 dòng để không đổi backend. Chuyển Trước/Sau xuống footer, có nhãn TrangN và khoảng dòng đã tải, phân biệt rõ với scrollbar xem các dòng trong trang. Dùng cuộn trong vùng grid/card khi 50 dòng không vừa; tránh cuộn cả trang kèm cuộn bảng nếu không cần. Header/filter/footer giữ tiếp cận được khi resize; ở cửa sổ nhỏ/DPI cao chỉ thêm cuộn khi thực sự không đủ chỗ.

Khóa Trước ở trang đầu; khóa Sau khi lô hiện tại ít hơn50 dòng, loading/lỗi không tạo trạng thái trang giả. Khi nhận đúng50 dòng, contract chưa biết chắc có trang sau: không bịa tổng trang/tổng bản ghi; vẫn cho kiểm tra trang tiếp theo bằng luồng tải hiện có. Không hiển thị1/2 hoặc tổng14 như mockup nếu backend không cung cấp. Không tải toàn bộ dữ liệu âm thầm để bỏ phân trang, không đổi page size thành7 để giống hình.

Chính sách hạng không phân trang nếu service hiện trả toàn bộ; không đặt pager trang trí không hoạt động. Empty state khi chưa chọn khách ghi rõ cần chọn khách, tránh gọi là truy vấn đã trả0 giao dịch.

## 5. Giới hạn thay đổi

Chỉ WinForms layout/style/resources và presentation state cần thiết cho bố cục: chia nhóm action, hiển thị phân trang, trạng thái chọn dòng, đổi Danh sách/Sơ đồ. Giữ event/use case, validation, quyền, cancellation/loading, revision/reconcile, confirmation và dữ liệu typed.

Không sửa Business/DataAccess/Models/SQL/procedure/migration/schema, không đổi tồn/điểm/giá hay dữ liệu test/development. Không thêm filter không có contract: ảnh nguyên liệu có filter đơn vị nhưng service hiện chỉ tìm kiếm, vì vậy không thêm dropdown giả hoặc lọc cục bộ50dòng rồi gọi là lọc toàn bộ. Không bỏ lookup/filter/chức năng hiện có chỉ để giống ảnh. Nếu phát sinh thay contract sẽ báo riêng trước làm.

Không chép con số/tên/avatar từ mockup vào app. Giữ định dạng Việt Nam tiền/ngày/lượng theo UX; không sao chép dấu phân cách số tiếng Anh trong ảnh. Không thêm stardust vào nút quản lý.

## 6. Đầu ra và nghiệm thu dự kiến

- Layout/helper/token dùng chung vừa đủ, áp dụng nhất quán các màn trên; PNG icon Orbit đúng kích thướcDPI và license.
- Plan sau duyệt nêu file/thứ tự triển khai và regression theo ảnh; không sửa code trước duyệt plan.
- Kiểm WinForms thật ở1280×720,1366×768 vàDPI100/125/150%; grid full width, không clip tên/nút/footer, sidebar căn trái; nút cùng loại đo kích thước nhất quán.
- Kiểm thao tác tìm/lọc hiện có, tải lại, chọn dòng, tab, Danh sách/Sơ đồ, pager bằng fixture hơn50dòng và ít hơn50; bảo đảm không mất dữ liệu hoặc gọi sai action/quyền. Đối chiếu service/backend source không đổi.
- Build/Designer và regression chức năng bị tác động trong đợt test được duyệt; không coi referenceHTML, ảnh mockup hoặc build là PASS nativeGUI. DB test riêng nếu cần thao tác ghi; không dùng development để test.
- Ghi bằng chứng/ảnh và PASS/FAIL/NOT RUN trung thực; không ghi đè lịch sử nghiệm thu G03. Không commit khi chưa được giao.

## 7. Bước duyệt

Đây là yêu cầu lớn độc lập về UI sau G03: chờ duyệt requirements một lần, sau đó viết plan đầy đủ và xin duyệt plan một lần trước triển khai; test riêng theo quy trình hiện có. Không xin duyệt từng màn hoặc từng nút.


