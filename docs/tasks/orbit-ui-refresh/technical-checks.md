# Kiểm tra kỹ thuật trong triển khai Orbit UI

Ngày: 17/10/2026. Không thay biên bản nghiệm thu.

## Mốc và phạm vi

- Root Git xác nhận: `D:/Workspace/Nam_3/HK1/CoffeeManager`.
- HEAD khi bắt đầu: `fb9f9efa9e8d539f3c3e9b89e7d0a3be5ed2763d`.
- Source `src/` sạch trước khi triển khai. Đã có thay đổi AGENTS.md, xóa bộ guide cũ,
  thêm docs/ui và hồ sơ lập kế hoạch; giữ nguyên các thay đổi có sẵn này.
- Không stage, commit, push; không chạy SQL hoặc thay dữ liệu DB.
- Các màn kiểm kê: Startup, Login, Bootstrap, Main, SessionHome, SystemInfo,
  Catalogs (7 tab) và CatalogEditor (7 biến thể). POS/checkout chưa có.

## Kiểm tra đã thực hiện

| Kiểm tra | Kết quả thực tế |
|---|---|
| Đối chiếu nguồn thiết kế | Đọc README/guide/token/implementation/assets/references; xem ảnh chuẩn pos-1366x768.png. Chưa so ảnh WinForms |
| Rà diff các file `.cs` có nghiệp vụ | Thay ở constructor/khởi tạo trình bày và UpdateButtonAppearance; Main.Navigate chỉ đổi hai đoạn màu menu. Handler async/service/request, Snapshot, validation, quyền và điều hướng giữ nguyên |
| Rà phạm vi Git | File source thay đổi chỉ trong CafeManager.WinForms; Business/DataAccess/Models/database/tests không có diff |
| `git diff --check` | Exit 0, không có lỗi whitespace |
| Build Debug đầu tiên mặc định | Exit 1, báo 0 warning/0 error, chưa có chẩn đoán cụ thể |
| Build nối tiếp `-m:1 -v normal` | Thành công, 0 warning/0 error; log có compiler server named pipe UnauthorizedAccessException rồi compiler fallback vẫn hoàn tất |
| Build `--no-restore -m:1 -p:UseSharedCompilation=false -v minimal` | Thành công, 0 warning/0 error; dùng compiler riêng để tránh named pipe; log tại build-debug.log |

Lệnh build áp dụng cho `src/CafeManager.WinForms/CafeManager.WinForms.csproj`;
SDK đã dùng `10.0.401`, reference pack/runtime compilation `10.0.12` theo output build.
Không thay SDK/target/package trong source. Build các project tham chiếu chỉ sinh bin/obj,
không sửa code nghiệp vụ.

## Cấu trúc UI đã áp dụng

- AppTheme ánh xạ palette/font/motion từ token 3.0.0; màu trạng thái là các vai trò
  lỗi/cảnh báo/thành công mở rộng có chữ đi kèm, cần kiểm tương phản ở lượt nghiệm thu.
- OrbitButton kế thừa Button: chỉ vẽ gradient/border/shadow/focus, hover/press,
  sheen và ripple; không gọi PerformClick hoặc service. Một timer dùng chung chỉ
  giữ nút đang animate và hủy khi rảnh; dọn nút khi disable/ẩn/dispose.
- OrbitHeader là TableLayoutPanel vẽ nền/họa tiết quỹ đạo; không tạo bề mặt nhận input mới.
- Icons milk 32px của gói dùng cho 4 mục menu, vẽ 20 logical px (20/25/30px tại
  DPI100/125/150%). Bitmap được dispose theo nút. License copy vào output/publish.
- Giữ native TextBox/ComboBox/DateTimePicker/DataGridView/CheckBox và MessageBox.
  Popup native, calendar/disabled input hoặc viền TabControl có thể chịu theme Windows;
  chưa khẳng định tương đồng thị giác trước khi thao tác thực tế.
- Bootstrap chuyển vùng trường sang panel cuộn riêng, nút hoàn tất/status ở footer.
  Editor giữ footer bên ngoài vùng trường cuộn; thao tác/điều kiện khóa không thay.
- Không tạo ProductCard/OrderPanel/bụi sao hoặc trang component demo. Không tạo reveal
  opacity trên native grid/tab/form; tránh snapshot che editor/focus. Vùng cuộn hiện tại
  giữ native scroll, cần ghi sai khác này khi đối chiếu motion.

## Chưa kiểm chứng

Ảnh/video baseline và sau sửa trên app thật, visual đối chiếu từng màn, DPI,
Designer round-trip, keyboard/accessibility, tương phản thực tế, GDI/handles và
regression SQL/role/CRUD đều NOT RUN, chờ duyệt đợt test UI01–UI12.
Không dùng build hoặc ảnh HTML để gán PASS. Chưa chạy app nối DB, unit test,
integration test hay 325 ca backlog. Trạng thái task vẫn Đang làm.

## Cập nhật sau duyệt test 17/10/2026
Đã chạy đợt kiểm thử phần khả thi; kết quả hiện tại tại test-results.md/report.md.
Các NOT RUN trong phần kiểm tra kỹ thuật ở trên là trạng thái trước đợt test,
không thay thế kết quả mới. Source giữ nguyên24/24hash; B01–B03 chưa sửa.

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
