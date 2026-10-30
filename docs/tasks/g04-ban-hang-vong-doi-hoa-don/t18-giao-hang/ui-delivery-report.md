# UI Giao hàng — triển khai26/10/2026

Đã triển khai và chạy đợt chức năng được user duyệt riêng26/10/2026:55/55kiểm
giao hàng,108/108native/shared regression,53/53invoice regression,18/18Sales xUnit.
Source cố định; không đổi số ca gốcT18/G04. Chờ xác nhận manual/hình thức,
giới hạn từng ca tại [kết quả](ui-delivery-test-results.md).

## Thay đổi

- DeliveryControl dùng workspace/theme/control có sẵn: header DELIVERY WORKSPACE,
  title Giao hàng, mô tả giữ giới hạn chỉ lưu hiện trạng; ô tìm hết hàng và hai
  icon, hai filter plain hàng dưới tự áp dụng; bỏ nút Áp dụng.
- Tách nháp tìm kiếm/từ khóa đã submit qua UseStackedSearch. Chi tiết cần dòng
  hợp lệ; Kiểm tra kết quả chỉ mở khi unknown/pending. Hook IsActionAvailable
  mặc định true ở base cho phép module đồng bộ enable theo writing/unknown,
  không đổi requestId/pendingCode, SQL hoặc contract ghi.
- Profile Delivery mới trong OrbitDataGridView, không dùng InvoiceHistory:
  Code đủ10ký tự/frozen, Phone giữ0đầu, Recipient/Address có tooltip, Address fill,
  badge tiến độ và thanh toán có chữ riêng. Pager lấy DeliveryRow.TotalCount.
- Không sửa MainForm/NavigationController/OrbitListWorkspace: chuỗi fill hiện
  có đủ giãn sau ghép. Profile cũ không có cột fill vì bảng không có Name;
  so cùng sáu cột/bounds cho oldColumns962px, không fill, viewport1049px.
  Profile mới cột Address fill/minwidth, dùng đủ ngang hoặc cuộn ngang khi cần.
  Đây là bằng chứng phân bổ ngang, không kết luận nguyên nhân chiều cao của
  ảnh cũ chưa đo. Body/Grid hiện nhận toàn vùng giữa actions và footer.

## Kiểm kỹ thuật trong pha triển khai

Build Debug/Release:0 warning/0 error. Smoke48/48 invariant kỹ thuật tại DeviceDpi120
với MainForm/DeliveryControl thật, session/service null và DTO trong bộ nhớ,
không query/Auth/SQL hoặc chuyển giao. Client1280×720/1366×768: page fill content,
grid fill Body, Body sát footer, search còn đủ ngang, filter/action/pager nằm trong
ancestor;0/1/50dòng không đổi Body.Size. Readonly/profile/AddressFill/Code/Phone đúng
trong phạm vi trình bày. Không test thị giác/chụp ảnh hoặc ghi PASS thay user.

Chiều cao Body thực đo181px ở1280×720,229px ở1366×768, sau header/tool/actions/
footer. Smoke đầu có49PASS/1FAIL do harness tự đặt Body>=200px; plan không có
min200 và invariant fill đều đạt. Giữ layout-smoke-initial.json/smoke-initial.cs.txt;
bản cuối ghi số đo này thành observation, không thay source để che lỗi hoặc đổi
expected tài chính. Hình thức/mức thoải mái vùng xem chờ người dùng xác nhận.
Smoke build đầu tự include source-before gây lỗi harness; log giữ nguyên, project
đã explicit Compile Program.cs, không thay source ứng dụng vì lỗi tooling đó.

Bằng chứng trong
[run triển khai](../runs/delivery-ui-implementation-20261026/manifest.json):
build-debug.log/build-release.log, layout-smoke.json, source-before/final.csv,
changed-files.csv và dll-hashes.json. Chỉ3file source WinForms thay đổi;
không SQL/Models/DAL/BLL. Trong pha triển khai không tạo/ghi DB hoặc mở app mới;
đợt acceptance sau duyệt có fixture/SQL transition được tự dọn và appPID6748 cho
người dùng kiểm. Không commit. Binary V4 cũ giữ riêng; lượt test V4 đã đóng theo
hash cũ, manual còn thiếu không chuyển thành PASS trên bản này.

Các phần query/filter/paging, pending reconcile, native/detail/role và regression
đã chạy trong phạm vi ghi tại kết quả; còn manual/hình thức và các giới hạn
timeout thực/DPI/paid fixture mới chưa chạy. Không phát hiện lỗi sản phẩm trong
những ca đã thực hiện. Xem
[test-plan](ui-delivery-test-plan.md) và [kết quả](ui-delivery-test-results.md).
