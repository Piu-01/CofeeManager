# T07 — Liên kết bug và giới hạn evidence

**Trạng thái cuối17/10/2026:** cácbugliênkếtB01/B04/B05/B06 đãsửa/regressionPASS;
mọisubcaseT07đãđốichiếuPASS,khôngcònblockerhiệnhành. Lịch sửFAIL vàgiới hạn
củalượtcũ dướiđây vẫngiữ; xem`../acceptance-matrix.md`.

Ca loading T07-K05.a FAIL do CatalogsControl thuộc T08. Bug sở hữu:
[G02-B01](../t08-danh-muc-nen/bug-report.md). Người dùng đã duyệt đợt debug
B01/B02; B01 đã sửa và5/5ca native regressionPASS ngày11/10/2026.
Chứng cứFAIL trước sửa vẫn giữ. Các ca chưa đủ chứng cứ ở dưới không tự nângPASS.

Các raw FAIL khác ở native harness (content chưa ready, sys.partitions count,
exact cancel timeout/modal) được giữ nguyên trong runs. Không tự kết luận là
bug ứng dụng khi chưa phân biệt thao tác/đo; ca liên quan giữ BLOCKED và ghi
chứng cứ bổ sung/điều kiện còn thiếu trong test-results.md.

## Đợt bug bổ sung 11–16/10/2026

Ca T07-K04.c đã thao tác được Designer đúngsolution, không còn coi là chỉ
blockerautomation. Lỗi được ghi tại task sở hữu source:

- [T06/B04](../t06-bootstrap-dang-nhap-phan-quyen/bug-report.md): LoginSave
  thêmAbsolute20row, bảnclone sauSave chạy thật bịclipping.
- [T06/B05](../t06-bootstrap-dang-nhap-phan-quyen/bug-report.md): Bootstrap
  parseerror tạiinitializer dòng38.
- [T08/B06](../t08-danh-muc-nen/bug-report.md): editor/catalogsparseerror dòng47.

ĐợtB03–B06 đã được duyệt vàsửa; regressioncuốiPASS ngày16/10/2026.
Modal/DPI/keyboard/GC và
các nhánh đãPASS trong lượt bổ sung được cập nhật ở test-results.md; giữraw
FAIL trước đó, không coi count/captionDesign là nghiệm thuDesigner thànhcông.

B06cólượt1FAILgrid150%5px; đãkếtthúclượt vàdebug phầnColumnStyle tựthêm,
giữAutoSize nhưsemantics cũ. Lượt cuối binary sauSave36/36UI+DPI và
23/23CRUDPASS; bằngchứngFAILtrước vẫn lưu tại taskT08. Xemtest-results.md.

## Điều tra bổ sung 18/10/2026 — chưa sửa

Người dùng báo không chạy được luồng đăng nhập từ Visual Studio và tác động môi
trường ngoài ý muốn. Hồ sơ điều tra chỉ đọc, mapping từng finding về task và hướng
khắc phục: [báo cáo toàn đợt](../../../bugs/project-environment-audit-20261018/report.md).

PASS cũ giữ làm lịch sử của bản/fixture đã nghiệm thu; không thay bằng chứng chạy
F5 hiện tại. Đợt này không chạy test hoặc sửa source/migration/configuration.
Các thao tác khắc phục/xóa DB hoặc tài khoản Windows chưa được thực hiện.

## T07-NAV-20261024 — Menu CASH còn các lối vào quản trị không đúng UX

Trạng thái: người dùng đã duyệt hướng sửa ngày24/10; đã sửa và18/18component
regressionPASS. Xem [biên bản](menu-fix-test-results.md). Không suy PASS các thao tác
CRUD/checkout hoặc toàn bộ G04 từ đợt sửa menu.
Người dùng đăng nhập thungan trên bản sau gỡ bypass và báo màn hình giống ADMIN.

Bằng chứng: SQL chỉ đọc trên DB G04 test schema016 trả thungan,MaVT=CASH,
HoatDong=true. Accessibility WinForms thật trả Nhân viên · CASH và màn Bán hàng,
không có Danh mục nền; nhưng sidebar vẫn có Menu / Công thức, Bàn / Khu vực,
Ưu đãi / Điểm thành lối vào riêng. Xem
[CASH main](../../temporary-test-login/runs/auth-regression-20261024/cash-main-readonly.txt).
Không kiểm hình thức/ảnh và không suy có quyền ghi quản trị từ việc thấy menu.

Nguyên nhân: MainForm.cs dùng một danh sách modules chung. Điều kiện tạo nút chỉ
chặn một tập Sales/Tables/Customers/Loyalty/Invoices/Delivery với STORE; CASH vẫn
được tạo các nút Tables/Loyalty/Products. Chuỗi tiêu đề chung “KHÔNG GIAN QUẢN LÝ”
và “Quản lý / Bán hàng” góp phần gây nhầm, nhưng không chứng minh role bị đổi.
UX mục3 quy định CASH đọc menu/ưu đãi và chọn bàn qua POS. BLL vẫn có kiểm quyền;
source các module ẩn thao tác sửa theo ADMIN, chưa phát hiện vượt quyền ghi.

Hướng sửa đề xuất: tổ chức navigation rõ theo vai trò trong MainForm; CASH tập trung
Bán hàng, Lịch sử hóa đơn, Giao hàng và các lối vào khách/tồn phục vụ bán được phép;
menu/công thức, ưu đãi, chọn bàn qua POS theo UX. Chỉnh nhãn shell theo công việc/role
để không gọi CASH là quản lý. Giữ nguyên AuthService/BLL/SQL/contract và POS hiện có.

Regression sau duyệt: menu/start page cả3role, CASH chọn bàn/khách/ưu đãi qua POS,
ADMIN giữ quyền quản trị, STORE không bán/giao; navigation không làm mất đơn/nháp.
Rủi ro chính: ẩn lối vào làm thiếu đường tới use case cần bán; kiểm đủ các lối POS
trước loại nút. Lịch sử PASS T07 trước G04 giữ nguyên, không lấy làm bằng chứng bản mới.
