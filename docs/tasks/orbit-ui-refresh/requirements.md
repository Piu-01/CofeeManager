# Chỉnh toàn bộ giao diện hiện tại theo Orbit UI

Ngày lập: 17/10/2026. Requirements và plan được người dùng duyệt qua tin nhắn
“duyệt” ngày 17/10/2026. Đợt test sau đó cũng được duyệt và đã chạy phần khả thi.
Trạng thái: Đang làm, có B01–B03 và ca BLOCKED/NOT RUN; chưa nghiệm thu.

## Mục tiêu và giới hạn

Áp dụng style guide chung `docs/ui/` cho toàn bộ Form/UserControl hiện có.
Yêu cầu người dùng: chỉ chỉnh giao diện; không thay logic, DB hoặc nghiệp vụ.
Đây là đợt chỉnh thị giác độc lập trên các đầu ra G01/G02 đã có, không mở G03/G04,
không thay checklist T01–T32, không viết lại kết quả nghiệm thu cũ.

Được chỉnh: màu, font, spacing, icon, nền, border, bố cục co giãn, skin control,
focus hiển thị, hover/press và chuyển động thuần trình bày. Giữ tên CoffeeManager;
Orbit là concept thiết kế, không phải quyết định đổi thương hiệu.

Không sửa Business, DataAccess, Models, database, migration, seed, cấu hình kết nối,
contract, tham số request, binding dữ liệu, validation, phân quyền, session,
điều hướng mặc định, xử lý nháp, timeout/retry hoặc quy tắc khóa gửi lặp.
Không thêm màn nghiệp vụ, dữ liệu demo, dashboard số liệu giả, WebView hay thư viện UI mới.

## Danh mục giao diện phải phủ

| Khu vực | Source hiện tại | Phạm vi thị giác |
|---|---|---|
| Khởi động/tra bàn nền G01 | `Forms/StartupForm.cs`, `.Designer.cs` | Header, bộ lọc, nút tải/hủy, grid, trạng thái rỗng/lỗi |
| Đăng nhập | `Forms/LoginForm.cs`, `.Designer.cs` | Khối nhận diện, input mật khẩu, nút, vùng lỗi |
| Thiết lập ban đầu | `Forms/BootstrapForm.cs`, `.Designer.cs` | Các nhóm thiết lập, danh sách/input và footer |
| Shell | `Forms/MainForm.cs`, `.Designer.cs` | Sidebar, mục đang chọn, header danh tính, vùng nội dung/status |
| Trang phiên | `Modules/Home/SessionHomeControl.cs` | Bố cục thông báo theo vai trò; giữ nội dung chưa triển khai |
| Hệ thống | `Modules/System/SystemInfoControl.cs` | Nhóm thông tin/schema/trạng thái hiện có |
| Danh mục nền | `Modules/Catalogs/CatalogsControl.cs`, `.Designer.cs` | 7 tab, tìm kiếm, grid, phân trang, toolbar và status |
| Editor danh mục | `Forms/CatalogEditorDialog.cs`, `.Designer.cs` | Toàn bộ biến thể editor của 7 danh mục, nhãn/input/lỗi/footer |
| Thành phần chung | `Theme/AppTheme.cs`, `UI/ModuleState.cs` | Token, skin và màu trạng thái; giữ nguyên enum/nội dung/luồng |

Các đường dẫn source trong bảng tính từ `src/CafeManager.WinForms/`.
7 danh mục: danh mục sản phẩm, nhà cung cấp, phương thức thanh toán, đối tác giao hàng,
danh mục thu chi, chức vụ, ca làm việc.
MessageBox hệ thống giữ nguyên lời nhắc, lựa chọn và DialogResult; không thay bằng
dialog tự viết trong đợt này vì có thể đổi hành vi xác nhận/đóng form.

## Nguồn chuẩn và điều chỉnh áp dụng

- Đọc `docs/ui/README.md` và đủ 6 tài liệu theo thứ tự của README.
- Hành vi thị giác theo `HuongDan_UI_Orbit_Cafe.md`; hình thức theo `reference.html`
  và ảnh/video `references/`; giá trị theo `design-tokens.json` phiên bản 3.0.0.
- Nền espresso tối, mặt panel tím than, chữ kem, nhấn caramel/lilac; không trộn bảng màu cũ.
- Thông số CSS px phải đổi sang pt/logical px đúng; không giảm chữ để ép bố cục.
- POS/checkout chưa tồn tại trong source hiện tại. Quy tắc guide “POS trước rồi mở rộng”
  được điều chỉnh theo phạm vi người dùng: đối chiếu trước trên shell + danh mục nền/editor
  đã có, không dựng POS mới. Đây là điểm cần chốt khi duyệt phạm vi.
- Chưa triển khai ProductCard, OrderPanel hoặc overlay bụi sao. Không nút hiện tại nào
  được bật bụi sao; xác nhận thanh toán chỉ áp dụng khi task nghiệp vụ tương ứng có UI.
- Bố cục các màn ngoài POS là ánh xạ từ hệ thống token/component, không tuyên bố có
  bản mẫu riêng đã được duyệt cho từng màn. Giữ đầy đủ trường/thao tác đang có.
- Giữ control nhập liệu native và hỗ trợ Designer. Hiệu ứng không sửa Bounds gây rung
  layout, không nhận click thay control và không gọi service/event nghiệp vụ.

## Tiêu chí hoàn tất

1. Tất cả khu vực trong bảng dùng Orbit thống nhất; không còn màu cũ ghi đè lúc
   EnabledChanged, chọn menu hoặc chuyển trạng thái.
2. Chữ đọc rõ: tương phản chữ thường tối thiểu 4,5:1, chữ lớn 3:1; đủ normal,
   hover, pressed, focus, disabled, loading, empty, error, conflict, unknown, saved.
3. 1280×720 và 1366×768, DPI 100/125/150%: không che thao tác, lỗi trường hoặc
   footer; nội dung dài cuộn được, không mất cột/trường cần dùng.
4. Chuột/bàn phím giữ nguyên hành động; hover/press không phát sinh lệnh, một lần
   kích hoạt vẫn một hành động; giữ nguyên Enabled/Visible/AcceptButton/CancelButton.
5. CRUD 7 danh mục, khóa mã hệ thống, role, nháp, đối chiếu sau timeout và phản hồi
   query muộn giữ hành vi đã nghiệm thu. Kiểm chứng trên DB test SQL Server riêng.
6. Designer mở/lưu lại được; không truy cập DB lúc design-time; chuyển màn/đóng form
   giải phóng timer, event và tài nguyên vẽ do UI mới sở hữu.
7. Có ảnh/video WinForms thật và biên bản kiểm thử mới; không dùng HTML/build làm
   bằng chứng PASS UI. Thiếu công cụ ghi BLOCKED/NOT RUN.
8. Diff chỉ chứa trình bày và tài liệu/tài nguyên cần thiết; không đổi SQL/BLL/DAL/Models.

## Quy trình duyệt

Người dùng đã duyệt phạm vi và `plan.md` sau khi được trình cả hai tài liệu.
Đã triển khai trong phạm vi duyệt. Sau triển khai chờ duyệt đợt test riêng;
bug điều tra và gom đợt xin duyệt trước sửa. Không commit/push nếu chưa được giao.


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
