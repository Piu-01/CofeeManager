# Báo cáo Orbit UI — sau đợt test 17/10/2026

**Đang làm — chưa đạt nghiệm thu.** Đợt test được duyệt và đã chạy phần có thể kiểm
chứng; có 3 bug hiển thị, các ca BLOCKED/NOT RUN chưa được gán PASS.

## Kết quả chính

- Build Debug và Release: PASS, 0 warning/0 error.
- Regression G02 trên SQL Server thật, 3 DB riêng: **484/484 PASS**. Unit Auth/Catalog
  chạy riêng36/36PASS, đã nằm trong bộ484, không cộng thành520ca khác nhau.
- UI thật: đã quan sát Login không cấu hình, Bootstrap Pending, Main/Home/System,
  7tab, editor sản phẩm/ca, cả3role. Tạo danh mục từ UI và đối chiếu DM001/tên
  tiếng Việt trong DB; Enter không lưu, No giữ nháp, validation tên rỗng đúng.
- Startup G01: có dữ liệu/rỗng/lỗiSQL229/hủy query chờ khóa đều có bằng chứng.
- 7 cặp màu token chính đạt tương phản4.5:1; không thay nghiệm thu toàn bộ native UI.
- **24/24 hash source UI không đổi** trong lượt. G02 runner cũng xác nhận source,
  tests/database/tools trước/sau không đổi. Không sửa SQL/BLL/DAL/Models.

Chi tiết từngID/subcase, expected/actual, lệnh, môi trường, ảnh và blocker tại
[test-results.md](test-results.md). Ảnh nằm trong
[runs/20261017-acceptance/](runs/20261017-acceptance/).

## Đợt bug đã được duyệt

| Bug | Mức | Hiện tượng | Hướng sửa chỉ UI |
|---|---|---|---|
| B01 | P2 | Dải/viền TabControl và ô giờ còn nền trắng | Hoàn thiện skin native, giữ control/value/event |
| B02 | P1 | 1280×720/DPI125: hàng danh mục bị ép mỏng, không đọc được | Chỉnh heading/toolbar/footer để bảo vệ vùng dữ liệu |
| B03 | P2 | Tiêu đề cột nhiều dòng bị cắt | Đo chiều cao/padding/độ rộng header theo font vàDPI |

Nguyên nhân, bằng chứng, phạm vi/rủi ro và regression:
[bug-report.md](bug-report.md). **Đã sửa và regression phần khả thi của B01–B03.** B02/B03 đạt ở DPI125%; B01 còn ô giờ native trắng. Duyệt đợt debug bao gồm
regression các bug đã duyệt; phát sinh ngoài đợt cần trình thêm.

## Phần còn thiếu

DPI thật100/150%, Designer round-trip (helper modal cache không truy cập được),
manual auth/bootstrap UI, đầy đủ keyboard/motion,6editor còn lại, edit/delete/
filter/paging qua UI, conflict/unknown, payload querymuộn riêng và100lần đổi module
kèm số đoGDI/timer. Bảng test nêu rõ PASS/FAIL/BLOCKED/NOT RUN từng phần;
không gọi484test backend là hoàn tấtUI01–UI12.

Giữ nguyên source, lịch sử G01/G02 và325ca gốc. Fault DENY trong UItest đã REVOKE,
khóa đã rollback; DBtest giữ làm bằng chứng, không chạm DB sử dụng thật. Chưa commit/push.


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
