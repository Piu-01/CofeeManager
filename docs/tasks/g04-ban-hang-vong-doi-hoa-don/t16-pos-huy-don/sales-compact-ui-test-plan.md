# Đợt test Bán hàng gọn

Trạng thái: USER ĐÃ DUYỆT đợt test riêng30/10/2026; đã chạy, có FAIL tiền dài.
Kết quả chi tiết tại sales-compact-ui-test-results.md. Ma trận SC01–SC08 tại
[plan đã duyệt](sales-compact-ui-plan.md), giữ checklist/expected gốc T14–T17.

1. Kiểm source/binary và tài nguyên trước chạy. Workspace có module khác đang
   thay đổi: cố định bản test riêng, không sửa nguồn/migration trong lượt test;
   không kế thừa kết quả từ binary cũ hoặc lấy binary G04cũ để testUI mới.
2. SC01: kiểm chức năng/bounds125% các desktop/nhánh compact, cuộnmenu/grid,
   tên/ghi chú/tiền lớn, giữ input/selection; user kiểm hình thức. Không đổi Scale,
   chụp ảnh/testvisual bởi agent;100/150% NOT RUN nếu chưa chạy.
3. SC02–SC04: tìm/trang/danh mục/loading/lỗi, single Add/disabled, tạo/mở đơn
   3kênh, F4/chọn bàn, đơn mới và hỏi draft, rời/quay lại. Chọn bàn không ghi
   header rỗng, layout/resize không gửi query/ghi ngoài ý định.
4. SC05–SC06: MaDong/qty/note/giá snapshot/Amount, F3khách/KM/ship và tổngPayable
   từSQL khớp breakdownF8, previewinvalid/unknown không0giả; keyboard/selectall/
   nativeowner/Enter không checkout, busy/session/quyền và reconcile không lặp.
5. SC07: tiền mặt/hỗn hợp/0₫/thiếu kho/stale, hỏi inCó/Không/hủy, in lỗi saucommit
   và POSreset. Tạo fixture riêng, không chốt đơn cũ đang giữ. Snapshot tiền/kho/
   điểm/thanh toán/receipt/tiêuhao trước-sau và hai session ở ca cần concurrency.
6. SC08: regression đúng phần sharedsurface/badge/grid/button và dialogs; ADMIN/
   CASH/STORE. Smoke/read không thay kiểm chứng write/native hoặc checkout thật
   trong SQLtest; phần thiếu ghi riêng, không gán PASS toànca.
7. SQLServer thật/DBtest riêng có ownership/marker: đọc lại manifest và schema
   hiện hữu30/10 trước dùng, không mặc định schema016 hoặc DB cũ còn là bản mới.
   Không tạo DB ngoài số lượng cho phép hoặc fixture vào development; không
   dùng thông tin profile/credential trong log. Provision/dọn allowlist đúng
   fixture process/principal/order của đợt mới, không dọn của G05/G06 hay người dùng.
8. Lưu test-results và report nhóm kể cả FAIL/BLOCKED/NOT RUN. Bug mới ghi ởT16/
   bug-report.md, gom duyệt trước sửa/regression. Không commit/sourcefix trong test.
