# Plan bổ sung T18 — đồng bộ giao diện trang Giao hàng

Ngày26/10/2026. User đã duyệt phạm vi/plan bằng “duyệt”; đã triển khai,
build Debug/Release sạch và smoke kỹ thuật48/48 tại125%. Đợt test riêng đã duyệt
và chạy:55/55giao hàng,108/108shared/native,53/53invoice,18/18xUnit; chờ manual/
hình thức và giữ các giới hạn subcase chưa chạy tại kết quả.
Thuộc G04/T18, bổ sung requirements/plan nhóm đã có, không tạo requirements task
con hoặc thay checklist325ID. Áp dụng skill task-approval-workflow của dự án.

## 1. Mục tiêu và căn cứ

Đồng bộ trang Giao hàng với chuẩn danh sách vừa được người dùng chốt trong
docs/ui/HuongDan_UI_Orbit_Cafe.md mục11, WINFORMS_IMPLEMENTATION.md mục2.1.
Tham chiếu bố cục tìm kiếm/bộ lọc/footer của Lịch sử hóa đơn V4 và bảng giãn vùng
nội dung của Khách hàng. Giữ nghiệp vụ kế hoạch T18, UX mục9/UX-K21/K22 và quyền
ADMIN/CASH/STORE hiện hành.

Source đã đọc:

- DeliveryControl kế thừa OrbitRecordListControl, Configure còn caption dài và
  eyebrow mặc định REFERENCE WORKSPACE; filters180+180 nằm cùng hàng tìm kiếm.
  Có nút Áp dụng, chưa gắn auto-filter, chưa bật UseStackedSearch.
- DeliveryRow đã có TotalCount; màn chưa gắn TotalCountSelector nên vẫn dùng
  server pager suy từ số dòng. Chuyển dùng tổng sẵn có, không cần migration.
- Grid chưa có profile Delivery riêng; sáu cột là mã/người nhận/SDT/địa chỉ/
  tiến độ/thanh toán. Delivery và PaymentStatus là hai trạng thái độc lập.
- NavigationController gán Dock.Fill cho module, Workspace và Grid cũng có
  cơ chế fill. Vì vậy chưa có bằng chứng kết luận cụ thể ancestor nào gây
  hiện tượng bảng nửa trang; phải đo bounds cả chuỗi sau ghép, không khẳng định
  một dòng Dock.Fill tự giải quyết. Không tái hiện thị giác thay người dùng.
- DeliveryDetailDialog hiện nằm cùng file DeliveryControl.cs, là native modal
  có owner qua OrbitDialogHost; không giả định có file dialog riêng.

## 2. Phạm vi và giao diện đích

Chỉnh trang danh sách Giao hàng. Dialog Chi tiết giữ nội dung/nghiệp vụ/cơ chế
native hiện có, chỉ kiểm hồi quy đường mở/đóng và trả POS; không thiết kế lại
dialog trong đợt này. Không thêm ngày lọc, partner filter, timeline, hoàn tiền,
thao tác hủy mới hoặc SQL/Models/DAL/BLL contract mới.

| Vùng | Thiết kế đích |
|---|---|
| Header |DELIVERY WORKSPACE; tiêu đề Giao hàng; mô tả ngắn tra cứu/chuyển tiến độ. Giữ thông tin “chỉ trạng thái hiện tại” ở mô tả/chi tiết để không gây hiểu nhầm timeline. |
| Hàng tìm kiếm |Ô Mã hóa đơn / số điện thoại người nhận giãn hết chiều ngang còn lại, kính lúp và làm mới ở phải. |
| Hàng bộ lọc |Lọc theo; tiến độ giao và trạng thái thanh toán. Plain filter theo theme, tự wrap khi thiếu ngang. Không chép cụm ngày từ hóa đơn vì contract giao hàng chưa có. |
| Hàng hành động |Chi tiết / Bước tiếp và Kiểm tra kết quả ở hàng riêng căn phải; không đặt nút chuyển trạng thái trực tiếp trong grid. |
| Bảng |Readonly, giãn hết ngang và cao còn lại trong card, kể cả ít dòng/rỗng; không chiều cao theo số dòng hoặc nửa màn hình. Mã/SDT đủ đọc, địa chỉ dùng phần ngang còn lại; cuộn ngang khi cần. |
| Footer/status |Range và pager ở đáy card, trạng thái tải ở dưới; không summary phía trên bảng. |

## 3. Các bước triển khai sau duyệt

### Bước1 — Ghép layout/toolbar và hành vi tìm lọc

File chính: src/CafeManager.WinForms/Modules/Sales/DeliveryControl.cs.

- Giữ base OrbitRecordListControl, query service.ListAsync và các giá trị trạng
  thái hiện tại. Set header/description/AccessibleName theo màn giao.
- Dùng AddFilter(combo,width,plain:true), nhãn Lọc theo, UseStackedSearch với
  noun “đơn giao” và empty message đúng module; bỏ nút chữ Áp dụng.
- Kính lúp/Enter submit từ khóa, không TextChanged search; SelectedValueChanged
  của hai filter tự ResetAndReload khi control sẵn sàng. Filter/paging/refresh
  dùng appliedSearch của base, không lấy Search.Text draft trong query.
- Không gắn thêm handler kính lúp/refresh riêng. New search/filter reset trang0,
  giữ input khi lỗi và bảo vệ cancellation/generation hiện có.
- Chi tiết chỉ bật khi có dòng hợp lệ và không busy; giữ writing/unknown guard
  hiện tại. Kiểm tra kết quả phải dùng requestId/pendingCode cũ, không phụ thuộc
  dòng vừa chọn hoặc vô tình tạo yêu cầu mới do đổi bộ lọc. Nếu cần chỉnh cách
  enable hiển thị thì dùng hook/predicate nhỏ chung, không thay protocol reconcile.

Verify triển khai: compile; đối chiếu handler một lần, đường query dùng q/skip
typed, không gọi ChangeAsync từ filter hoặc phím Enter ô tìm.

### Bước2 — Profile bảng Delivery dùng chung

File: UI/OrbitDataGridView.cs, DeliveryControl.cs; Theme/OrbitManagementTheme.cs
chỉ khi cần icon/style có sẵn chưa được expose, không tạo theme màn mới.

- Thêm profile Delivery opt-in vào grid chung, chọn tại màn Giao hàng; không
  tái dùng InvoiceHistory vì cột/ý nghĩa khác, không đổi mặc định profile cũ.
- Code đo đủ10ký tự/header theo DrawingFont/DPI, min khoảng125logical và freeze
  nếu phù hợp; Phone là chuỗi giữ0đầu, min khoảng130; Recipient min150;
  Address fill/min200; Delivery/PaymentStatus mỗi cột min khoảng145. Đây là
  mức khởi đầu kỹ thuật, dùng số đo text thực để tránh cắt nội dung cần đọc,
  không lấy pixel mẫu HTML làm tọa độ cố định.
- Tái sử dụng painter badge hiện có cho hai loại trạng thái; tiến độ giao và
  thanh toán có chữ riêng. Đã giao không đồng nghĩa Đã thanh toán. Chọn màu từ
  theme chung, không tạo switch màu rải rác hoặc paint handler copy từng màn.
- Địa chỉ/tên dài được xem đầy đủ qua tooltip/chi tiết hiện có hoặc bổ sung
  tooltip nhỏ dùng chung nếu cần; không tăng toàn bộ rowheight làm mất vùng bảng.
- Nếu tổng minwidth vượt viewport thì grid cuộn ngang, footer/action vẫn hiện;
  selection/readonly/accessibility giữ theo grid dùng chung.

Verify triển khai: grid readonly; đo Code/Phone, trạng thái có chữ; profile cũ
không bị đổi cột/màu. Hình thức người dùng kiểm sau triển khai.

### Bước3 — Tổng thật và giãn vùng bảng

File: DeliveryControl.cs; UI/OrbitListWorkspace.cs chỉ khi đo chứng minh lỗi
layout chung, và NavigationController/MainForm chỉ khi có bằng chứng ancestor
không giãn trong đường mở thật. Ưu tiên sửa đúng cấp, không refactor shell.

- Gắn TotalCountSelector từ DeliveryRow.TotalCount (0 khi rỗng), tái dùng base
  đã có range/page/Next logic cho tổng thật và fallback trang cũ rỗng.
- Đo chuỗi content→DeliveryControl→workspace→card→Body→Grid ở client1280×720,
  1366×768 tại125%. Card nhận phần nội dung còn lại; Body.Bottom sát vùng footer
  với padding theme. Giảm số dòng không làm Body/Grid co chiều cao.
- Giữ toolbar/action/footer ở hàng riêng, không tăng fixedHeight để giả full;
  filters wrap thì chỉ toolbar tăng và Body nhận phần dư. Không đổi Windows
  Scale hoặc nhân DPI lần hai. Không thay shared default khiến màn khác đổi.

Verify triển khai: build Debug/Release và smoke bounds kỹ thuật dùng app thật,
bao gồm ít dòng/rỗng/resize. Không ghi PASS hình thức hoặc T18 từ smoke.

### Bước4 — Hồ sơ và bàn giao test

Files: docs/ui/README.md và WINFORMS_IMPLEMENTATION.md cập nhật profile Delivery
thực sự đã có sau code; docs/UX_QuanLiQuanCafe.md mục9 cập nhật hành vi tìm/lọc
của giao hàng thay Apply cũ; hồ sơ plan/report/test-results tại T18 và report nhóm.

- Trước sửa product source, đóng biên bản lượt V4 theo sourcehash hiện tại,
  giữ các xác nhận manual còn thiếu là chưa chạy/chờ user, không chuyển thành
  PASS trên binary giao hàng mới. Không sửa source trong lượt test đang mở.
- Sau triển khai trình một đợt test riêng, không chạy nghiệm thu từ duyệt plan.
  Bug phát hiện trong test ghi T18/bug-report.md, gom duyệt trước sửa/regression.
- Không commit; không đổi checklist/đếm PASS cũ. T18 từng8/8 thuộc binary cũ,
  không tự coi layout/filter/profile mới đã được nghiệm thu từ lịch sử đó.

## 4. Đợt test dự kiến sau duyệt riêng

| ID bổ sung | Ca và expected |
|---|---|
| DG01 |Bounds125% ở hai client, ít/nhiều/0dòng, resize: bảng/card nhận phần còn lại; search hết hàng, filter dưới, footer/action không mất. Hình thức user kiểm. |
| DG02 |Tìm mã/SDT0đầu qua kính lúp và Enter; gõ chưa submit không query; ô trống submit mới clear; không chuyển giao từ Enter. |
| DG03 |Hai filter đổi là autoapply bằng từ khóa đã submit; draft khác vẫn giữ; paging/refresh không submit nháp. Query muộn không đè kết quả mới. |
| DG04 |0/1/49/50/51/100/101 đơn giao SQLtest: total/range/page đúng, trang cuối đủ50khóaSau; fallback khi trang rỗng không lộ dữ liệu cũ. |
| DG05 |Mã dài/SDT/tên/địa chỉ dài; hai badge đúng nghĩa, readonly; no selection/loading/error thì không mở nhầm dòng hoặc giả0. |
| DG06 |Chi tiết native owner/title/X/no taskbar; mở hóa đơn/đơn paid, canceled và đường POS đúng contract. Chuyển Chờ→Đang→Đã chỉ sau xác nhận; reload giữ bộ lọc. |
| DG07 |writing/unknown/reconcile/requestId và stale conflict: lọc/refresh không gửi lặp/chuyển sai đơn; receipt cũ đối chiếu đúng pendingCode. Hai session theo ca T18 bị ảnh hưởng. |
| DG08 |ADMIN/CASH/STORE; regress History/Customers/Menu/Tables cho workspace/grid/pager dùng chung. Không đổi quyền hoặc financial state khi chỉ đọc/xem. |

Tham chiếu T18-K01–K08 theo ảnh hưởng, UX-K21/K22; giữ mọi expected gốc. Fixture
trên SQL Server thật, DBtest riêng có ownership/marker hiện hữu; không DBthứ4,
không developmentfixture. Đọc lại tình trạng DB/app trước tạo dữ liệu, không
chốt đơn476–478 còn giữ từ lượt cũ. Dùng đơn giao fixture riêng cho chuyển bước;
snapshot tiền/kho/điểm/dòng/thanh toán/ưu đãi trước-sau phải không đổi khi chuyển
tiến độ. Tự dọn fixture đúng allowlist, giữ log/hash; không hoàn tiền/in thật.
Agent kiểm chức năng/bounds125%, visual SKIPPED theo phân công,100/150% NOT RUN
nếu chưa chạy; không ảnh so sánh/Designer visual. Tổng hợp kết quả trong report
nhóm, không thay trạng thái G04/G05 chỉ vì build hoặc thêm plan này.

## 5. Rủi ro thực tế và giới hạn

Shared painter/layout có thể ảnh hưởng module khác: thay theo profile/opt-in và
regression đúng phần thay. Filter mới autoquery dễ gặp trạng thái giao đang
ghi/kết quả chưa biết: giữ guard và pending identity, không đổi contract ghi.
Nguyên nhân “bảng nửa trang” chưa được đo tại GUI hiện tại: ghi bounds trước/sau,
nếu cần sửa ngoài vùng file đã nêu thì báo bằng chứng và cập nhật phạm vi trước
làm. Bản kế hoạch ban đầu chưa code/test; trạng thái triển khai thực tế xem
[báo cáo](ui-delivery-report.md), không dùng mô tả plan làm bằng chứng nghiệm thu.
