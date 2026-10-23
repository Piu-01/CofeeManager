# Đồng bộ cửa sổ con với Orbit

Ngày 22/10/2026 — phạm vi được người dùng duyệt bằng “ok”. Tiếp nối các màn quản lý đã đổi giao diện; không mở lại nghiệm thu G03 hoặc thay checklist T01–T32.

## Mục tiêu

Các cửa sổ mở từ Thêm, Sửa, Mở sổ, Công thức, Preview, Điều chỉnh, Hóa đơn và xác nhận trong các màn hiện có phải cùng hình thức Orbit với màn chính. Ưu tiên Theme/control đã có; chỉ bổ sung helper/control dùng chung khi thiếu thực tế. Không thiết kế riêng từng dialog theo cảm tính.

## Kiểm kê đã xác nhận từ source

- `OperationEditorDialog` tạo phần lớn editor/preview/xóa; hiện dùng Panel Dock.Top, hàng nhập cao78 cố định, nhãn ghi chú rộng520, footer FlowLayout và AppTheme.Apply cũ.
- `CatalogEditorDialog` dùng layout hai cột, màu/font/spacing cũ; bảy loại danh mục dùng chung một editor, ẩn/hiện trường theo loại. Có ErrorProvider, reload/đối chiếu, nháp và khóa khi ghi.
- `CustomerPickerDialog` dùng CustomersControl ở chế độ picker, vừa sửa bỏ thao tác quản lý. Giữ giới hạn đó khi đồng bộ khung cửa sổ.
- Bảng Công thức, Chính sách hạng và Hóa đơn khách còn được tạo bằng DataGridView/RecordListControl; không thay bằng grid chỉ đọc làm mất nhập/xóa dòng nháp.
- Xác nhận ghi/bỏ nháp/xóa đang dùng MessageBox; cùng phạm vi hình thức dialog, phải giữ nguyên điều kiện mở, lựa chọn và kết quả xử lý.
- Đã có `OrbitManagementTheme`, `OrbitButton`, `OrbitField`, `OrbitFieldLayout`, `OrbitFilterBox`, `OrbitDateField`, `OrbitTimeInput`, `OrbitDataGridView`, `OrbitEmptyState`, `OrbitPagination` và các khung danh sách. Kiểm tra khả năng tái sử dụng trước bổ sung.

## Phạm vi cửa sổ

| Màn | Các cửa sổ cần đồng bộ |
|---|---|
| Danh mục nền | Thêm/Sửa cho Sản phẩm, Nhà cung cấp, Phương thức, Đối tác, Thu/chi, Chức vụ, Ca làm; xác nhận xóa/bỏ nháp/đối chiếu |
| Nguyên liệu/Tồn | Thêm, Sửa, Mở sổ, Xóa/xác nhận |
| Menu/Công thức | Thêm, Sửa, bảng công thức và nút xóa dòng, Xóa/xác nhận |
| Bàn/Khu vực | Thêm/Sửa/Xóa bàn và khu vực; giữ chọn khu vực và trạng thái thật |
| Khách hàng | Thêm, Sửa, Mở sổ điểm, Xóa/xác nhận, bảng Hóa đơn khách |
| Ưu đãi/Điểm | Thêm/Sửa/Xóa khuyến mãi, Preview ưu đãi, Thay chính sách hạng, Điều chỉnh điểm, hộp Chọn khách |

Chỉ các cửa sổ nghiệp vụ đã tồn tại trong các màn quản lý hiện tại. Không mở rộng sang màn đăng nhập/bootstrap hoặc module T14–T32 chưa triển khai.

## Yêu cầu hình thức và tương tác

1. Tái dùng palette/font/DPI/hover/pressed của theme Orbit hiện tại, đối chiếu `docs/ui/` và bộ Orbit_9_HTML đã bàn giao. Ảnh màn chính là chuẩn ngôn ngữ hình thức; không tuyên bố có mẫu dialog riêng nếu gói không có.
2. Khung modal có tiêu đề rõ, vùng nội dung và footer thống nhất. Input có nhãn, nền/viền/focus đồng bộ; nút cùng vai trò có cùng chiều cao/padding, đủ rộng theo chữ. Lưu là chính, Hủy/Đóng là phụ, Đối chiếu đúng trạng thái cũ. Không bỏ nút nghiệp vụ.
3. Form ngắn co theo nội dung trong giới hạn màn hình; form dài cuộn phần nhập; form có bảng dùng phần diện tích còn lại, bảng tự cuộn. Header, lỗi và footer không bị cuộn mất. Không kéo giãn từng dòng để lấp form hoặc để khoảng trắng lớn do chiều cao cố định không cần thiết.
4. ComboBox/TextBox/DataGridView vẫn là control WinForms thật; giữ typing tiếng Việt, clipboard, data binding, giá trị typed và read-only theo quyền. Grid nhập không dùng cấu hình readonly của grid danh sách.
5. Giữ lịch/ngày/giờ đang có, popup không bị clipping; tab order/focus/Escape/đóng/X và trạng thái nháp/loading được bảo toàn. Không gán Enter thành hành động ghi kho/điểm/tài chính.
6. Lỗi/validation/unknown/reconciliation giữ rõ và giữ input. Không đưa lại nhãn đếm phía trên bảng hoặc những ghi chú tĩnh cuối biểu mẫu đã được yêu cầu bỏ.
7. Hover bóng và phản hồi nhấn từ control chung; không stardust ở các dialog này, không WebView, không ảnh chụp làm nền.
8. Kiểm tra kích thước1280×720,1366×768 và DPI100/125/150%; chữ tiếng Việt dài, số lớn, lỗi nhiều dòng không bị cắt/chồng.

## Bảo toàn và dọn code

- Không thay SQL, DAL, BLL, Models, quyền, validation, payload, revision, CaptureWrite/Reconcile, preview bắt buộc, khóa gửi lặp, transaction hoặc thứ tự nghiệp vụ.
- Giữ khách vãng lai ở Preview ưu đãi; picker Lịch sử điểm chỉ chọn/đóng. Không đưa lại action quản lý trong picker.
- Chỉ dọn style/layout/helper cũ đã được thay và chứng minh không còn tham chiếu, kể cả Designer/resources. Không xóa AppTheme/RecordListControl toàn cục khi login hoặc nơi khác vẫn dùng; không xóa file lịch sử/test/ảnh, không commit/push.
- Lỗi độc lập SP005 danh mục editor và rounding1px footer125% đang ghi riêng: không âm thầm sửa nghiệp vụ hoặc gộp nghiệm thu vào lượt đổi hình thức. Nếu bố cục dialog mới có vấn đề thì ghi đúng phạm vi và duyệt đợt bug theo quy trình.

## Tiêu chí bàn giao

- Toàn bộ cửa sổ trong bảng phạm vi sử dụng bộ quy tắc/control chung, không còn pha style cũ trong nội dung/footer/bảng con.
- Build Debug/Release; sau duyệt đợt test, thao tác app thật, chụp ảnh đại diện cho editor ngắn/dài, bảng nhập, picker, preview và xác nhận. Ghi ma trận resize/DPI, bàn phím, lỗi/nháp/loading; không đánh dấu PASS từ build hoặc fixture cấu trúc.
- Kiểm giao dịch chỉ trên DB test riêng có marker; không ghi DB sử dụng thật. Không tự đăng nhập; gom yêu cầu đăng nhập thủ công khi cần.
- Gửi danh sách file sửa/xóa, lý do dọn, ảnh thật và PASS/FAIL/BLOCKED/NOT RUN; không thay trạng thái nghiệm thu T01–T32.

## Quy trình

Yêu cầu này là đợt UI độc lập vì hồ sơ tám màn trước đã ghi dialog editor giữ nguyên. Theo skill cục bộ: duyệt requirements này trước; sau đó lập plan toàn đợt để duyệt; triển khai tuần tự; duyệt test một lần. Chưa sửa source hoặc chạy test mới ở bước requirements.

Không có câu hỏi nghiệp vụ cần bổ sung; mặc định dùng ngôn ngữ Orbit đang được chấp nhận, không thêm tính năng/cột/nội dung.
