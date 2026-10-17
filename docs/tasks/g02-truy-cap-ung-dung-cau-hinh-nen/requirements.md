# G02 — Truy cập ứng dụng và cấu hình nền

**Trạng thái:** Đã duyệt requirements/bộplan/đợttest; triển khai và nghiệm thu
G02 **PASS27/27IDgốc ngày17/10/2026**, B01–B07/PKCSđãsửa/regressionPASS,
Windowsidentitythật/setup-popup/restart exe đãPASS. Đã bàn giao Git theo yêu cầu:
commit`1ab09af368f320a598a577d22542e41563e5e432` đãpush`origin/main`.
Xem`acceptance-matrix.md`/`report.md`/`git-handoff.md`.
**Duyệt requirements:** Người dùng “duyệt” sau lần rà soát tài liệu ngày 09/10/2026.
**Ngày:** 09/10/2026.
**Yêu cầu khởi động:** Người dùng “ok triển khai g02”.
**Nguồn chuẩn:** `docs/KeHoach_QuanLiQuanCafe.md`, mục 10, 12 (T06–T08), 13;
`docs/UX_QuanLiQuanCafe.md`, UX01/UX02/UX09, quy ước UI và phiên làm việc.
**Phụ thuộc:** T05 thuộc G01; mốc bàn giao tham chiếu
`deabac108cbeddaa18126733ad18803b0626d7d7` trên `main`. Hiện trạng dưới đây
được đối chiếu source/schema và hồ sơ test, không suy ra từ tên commit.
**Thứ tự:** T06 → T07 → T08; một task con triển khai tại một thời điểm.

## 1. Mục tiêu

Chuyển từ mẫu kiểm chứng kết nối G01 sang ứng dụng có xác thực thật, phiên làm
việc và quyền BLL; khung điều hướng theo vai trò; nhóm danh mục nền đủ dùng cho
các phân hệ tiếp theo và cài DB mới không seed demo. Giữ phạm vi, đầu ra, phụ
thuộc và toàn bộ checklist nguyên bản, không coi requirements là duyệt code/test.

## 2. Task con, phạm vi và đầu ra

| Task | Mô tả / phạm vi nguyên bản | Quy mô / phụ thuộc | Đầu ra nguyên bản |
|---|---|---|---|
| T06 — Bootstrap admin, đăng nhập và cấp quyền | Dùng TAIKHOAN/VAITRO thay login hard-code; tách chức vụ và quyền app. AuthService, session, hash/verify, bootstrap một lần, logout; UI account editor đầy đủ ở T24. | M; T05 | Đăng nhập thật, admin đầu tiên có hash hợp lệ, authorization BLL. |
| T07 — Khung giao diện và điều hướng module | MainForm và quy ước bố cục app quản trị có POS. Sidebar, content panel, UserControl/dialog, loading, thông báo; chưa triển khai tất cả màn hình. | S; T06 | MainForm dùng được; style cơ bản và navigation theo vai trò. |
| T08 — Các danh mục nền cho toàn ứng dụng | Dữ liệu tham chiếu hợp lệ trước giao dịch. Danh mục sản phẩm, NCC, phương thức TT, đối tác, loại thu/chi, chức vụ, ca; không biến mỗi bảng thành module riêng. | M; T07 | Các tab quản trị và dữ liệu hệ thống phục vụ app cài mới không seed. |

### T06 — Hành vi cần có

- Login dùng account/nhân viên/role DB, C# hash/verify bằng
  `Microsoft.Extensions.Identity.Core`. Không mật khẩu mặc định, không tự đăng
  nhập, không lưu password/hash vào session hoặc log.
- Bootstrap chỉ được phép khi database chưa hoàn tất thiết lập quản trị lần đầu;
  không dùng việc thiếu admin hoạt động làm điều kiện duy nhất. Thành công tạo/bật
  đúng một admin hợp lệ và lưu bền vững trạng thái hoàn tất tại DB, nguyên tử cùng
  các thay đổi bootstrap. Thất bại rollback toàn bộ và cho phép thử lại hợp lệ.
- Hai session bootstrap đồng thời chỉ có một kết quả thành công. Restart app,
  admin bị khóa hoặc nhân viên nghỉ việc không mở lại bootstrap đã hoàn tất.
  Khôi phục quyền quản trị là quy trình riêng, không dùng bootstrap để reset password.
- Người dùng nhập password tại máy; không mật khẩu mặc định, không nhận qua chat,
  không lưu plaintext trong file/log/session. Quyền setup tách credential runtime;
  không đưa quyền migration vào cấu hình fallback. Bảng/cột/procedure và cơ chế
  khóa cụ thể là quyết định của plan T06, chưa chốt trong requirements.
- Tài khoản inactive hoặc nhân viên nghỉ bị từ chối; placeholder hash không
  phải mật khẩu hợp lệ. Rehash có kiểm soát; logout vô hiệu session cũ.
- Username trim nhất quán, uniqueness theo collation được chốt trong plan;
  giữ password đúng giá trị nhập, không trim password.
- BLL kiểm quyền khi gọi use case, độc lập menu; ADMIN/CASH/STORE theo ma trận
  mục 10 kế hoạch và mục 3 UX. Recheck trạng thái phiên/quyền chặn thao tác mới
  khi account bị khóa; không tuyên bố rollback giao dịch đã gửi SQL.
- Lỗi DB khác lỗi credential; UI che password, có loading/chặn gửi lặp,
  giữ input và hiển thị lỗi thân thiện.

### T07 — Hành vi cần có

- Một MainForm cho phiên hiện tại; hiển thị nhân viên/vai trò, tiêu đề màn,
  trạng thái và logout. Điều hướng trong content panel bằng UserControl, editor
  dạng dialog; không mở connection/event handler/control trùng khi đổi module.
- Menu/màn bắt đầu theo vai trò; module tương lai chỉ thể hiện trạng thái chưa
  triển khai phù hợp, không có giao dịch giả. CASH/STORE vẫn có màn bắt đầu hợp
  lệ dù POS/kho đầy đủ nằm ở nhóm sau.
- AppTheme tập trung màu/font/spacing theo Espresso/Kem/Caramel. Control native,
  bố cục Dock/Anchor/TableLayoutPanel; Designer save/close/reopen phải giữ hình
  thức trên bản source sau Save, kế thừa bài học B10.
- Filter/nháp được giữ trong phiên; hỏi **Ở lại / Bỏ nháp và rời** khi đổi màn,
  logout/đóng. Dialog hủy không ghi, lưu thành công refresh đúng màn; không xóa
  đơn đã lưu khi rời ứng dụng. Loading/rỗng/lỗi/xung đột/kết quả chưa biết phân biệt.
- Kiểm 1280×720, 1366×768 và DPI Windows thật 100/125/150%; bàn phím/focus,
  trạng thái disabled và cuộn truy cập được khi thiếu chỗ.

### T08 — Hành vi cần có

- Tab quản trị chung cho 7 danh mục: DANHMUC_SP, NHACUNGCAP, PHUONGTHUCTT,
  DOITACGIAOHANG, DANHMUC_THUCHI, CHUCVU, CALAMVIEC; thêm/sửa/tìm/xóa theo quyền,
  validate và reload typed data. CRUD không là đường sửa lịch sử tài chính.
- Chuẩn hóa required name, chiều dài theo SQL; SDT là chuỗi; decimal/time typed.
  Tỷ lệ đối tác 0–100, phụ cấp ≥0, hệ số ≥1, giờ ca không bằng nhau; ca qua đêm
  theo quy ước UX, không tự mở rộng sang chấm công/lương.
- Xóa danh mục chưa dùng được, có lịch sử bị chặn; không cascade lịch sử.
  Ngoại lệ bắt buộc: bản ghi hệ thống được bảo vệ không được đổi mã/loại hoặc
  xóa ngay cả khi chưa có lịch sử; plan chốt các trường còn được sửa.
  Sửa tỷ lệ hiện tại không sửa PhanTramPhiDoiTac snapshot của HOADON.
- Dữ liệu hệ thống đủ khi không chạy Insert_Data.sql: role theo T06, loại Chi
  trả nợ TC011 và loại Chi trả ngay/chi lương theo T08. Giữ liên kết mã/loại ổn
  định; không đổi Thu/Chi hoặc xóa để phá nguồn chi hệ thống.
- PT010 ngoài checkout. Danh sách phương thức dành cho thanh toán được lọc
  đúng contract ngay T08, kiểm bằng service/selector; chưa tạo màn checkout T15.
  Tên phương thức không có nghĩa đã tích hợp cổng thanh toán.
- ADMIN quản trị cấu hình tài chính; CASH/STORE chỉ được đọc/thao tác trong
  quyền đã nêu, service từ chối ghi trái quyền dù bỏ qua menu.

## 3. Nghiệm thu — giữ đủ 27 ca nguyên bản

Các dòng sau giữ checklist T06–T08; riêng mô tả T06-K01 được sửa đồng bộ với
kế hoạch theo yêu cầu về điều kiện bootstrap. Giữ nguyên ID và nhãn U/I/M.
Không tick PASS khi mới triển khai, không thay kiểm SQL Server bằng SQLite.

### T06 — 10 ca

- `T06-K01` [M] Bootstrap chỉ khi database chưa hoàn tất thiết lập quản trị lần đầu; tạo/bật đúng một tài khoản admin với mật khẩu người dùng nhập, lưu bền vững trạng thái hoàn tất tại DB nguyên tử cùng dữ liệu bootstrap. Không dùng việc thiếu admin hoạt động làm điều kiện duy nhất.
- `T06-K02` [I] Bootstrap lần hai bị chặn, không reset password hoặc tạo admin trùng.
- `T06-K03` [U/M] Đúng mật khẩu thành công; sai/username không có/placeholder hash thất bại an toàn.
- `T06-K04` [I/M] Tài khoản HoatDong=0 hoặc nhân viên DangLamViec=0 bị từ chối.
- `T06-K05` [U] Verify cần rehash cập nhật hash đúng, không lưu plaintext.
- `T06-K06` [U] ADMIN/CASH/STORE được/chặn đúng từng nhóm quyền, kể cả gọi service không qua menu.
- `T06-K07` [M] Password không hiện ở log/session; UI che ký tự.
- `T06-K08` [M] Đăng xuất xóa session và đóng màn hình có quyền; không thao tác bằng session cũ.
- `T06-K09` [M] Lỗi DB khi login không biến thành login thành công hoặc thông báo sai password đơn thuần.
- `T06-K10` [I] Tên login trùng theo collation đã chọn bị chặn; chuẩn hóa khoảng trắng nhất quán.

### T07 — 7 ca

- `T07-K01` [M] Login thành công vào MainForm, hiển thị đúng tên nhân viên/vai trò.
- `T07-K02` [M] Menu theo role; không tồn tại hai MainForm/session khác nhau ngoài ý định.
- `T07-K03` [M] Đổi module nhiều lần không mở connection hoặc giữ event handler/control trùng.
- `T07-K04` [M] Resize/DPI 100% và 125% không che menu, nút và cột chính.
- `T07-K05` [M] Nhấn thao tác đang loading không gửi lặp; xong/lỗi đều mở lại trạng thái nút đúng.
- `T07-K06` [M] Dialog hủy không ghi; dialog lưu refresh đúng module.
- `T07-K07` [M] Đóng app khi có input chưa lưu hỏi bỏ/lưu phù hợp; đơn đã lưu không bị xóa.

### T08 — 10 ca

- `T08-K01` [I/M] Thêm/sửa/tìm từng danh mục hợp lệ, mở lại app vẫn có dữ liệu.
- `T08-K02` [I/M] Tên rỗng/whitespace/quá dài, SDT NCC trùng, FK sai bị từ chối rõ.
- `T08-K03` [I] Tỷ lệ đối tác -1/>100, phụ cấp âm, hệ số <1 và giờ ca bắt đầu=kết thúc bị chặn.
- `T08-K04` [I] Tỷ lệ 0/100 và ca qua đêm được xử lý đúng quy ước.
- `T08-K05` [I] Xóa danh mục chưa dùng được; danh mục có lịch sử bị từ chối, không cascade lịch sử.
- `T08-K06` [I] TC011 và loại chi trả ngay/chi lương hệ thống tồn tại khi không chạy seed demo.
- `T08-K07` [I/M] Người dùng không đổi Thu/Chi của danh mục đã gắn chi hệ thống; không xóa phương thức đã dùng.
- `T08-K08` [M] PT010 không xuất hiện ở danh sách checkout; tên phương thức không có nghĩa đã tích hợp online.
- `T08-K09` [U/M] CASH/STORE không sửa giá trị cấu hình tài chính hoặc quyền không được cấp.
- `T08-K10` [I] Sửa tỷ lệ hiện tại không đổi snapshot phí đối tác của đơn cũ.

## 4. Đối chiếu hiện trạng và phần chưa được triển khai

| Nguồn đã đọc | Xác nhận từ source/schema/hồ sơ | Khác biệt hoặc giới hạn xác nhận |
|---|---|---|
| Program.cs, TableStatusService.cs và danh sách source src | Composition root mở StartupForm, đọc CAFEMANAGER_APP_CONNECTION; có query bàn async và dịch lỗi | Chưa có AuthService/session/authorization, LoginForm/MainForm/AppTheme hay CRUD T08. Yêu cầu thiết kế trong kế hoạch không phải chức năng đã có. |
| DB_Schema.sql: TAIKHOAN/VAITRO/NHANVIEN/CHUCVU | Login VARCHAR(50) unique, hash VARCHAR(255), FK nhân viên/vai trò, HoatDong mặc định 0, DangLamViec mặc định 1; nhân viên bắt buộc có chức vụ | Chưa có trạng thái hoàn tất bootstrap hoặc contract rehash; collation login không được khai báo tường minh tại cột. Không suy ra collation của DB đang chạy từ script. |
| Insert_Data.sql: VAITRO/TAIKHOAN | Có role ADMIN/CASH/STORE; admin/thungan/kho đều HoatDong=0 và hash placeholder | Là dữ liệu demo, không là bootstrap thành công; không tự bật tất cả account. |
| Insert_Data.sql: DANHMUC_THUCHI; sp_TraCongNo trong schema | Seed TC005 là Luong nhan vien, loại Chi; TC011 là Tra no nha cung cap, loại Chi; procedure trả nợ dùng cứng TC011 | TC005 mới là danh mục trong seed, chưa có contract chi lương T26. Chưa tìm thấy mã chi trả ngay hệ thống được xác lập trong schema/seed/migration/contract G01; không gán TC008 theo tên. |
| PHUONGTHUCTT/seed và contract G01 | PT010 có tên Diem thuong doi trong seed; ngoài phạm vi theo kế hoạch | Chưa có dữ liệu/cơ chế biểu thị hỗ trợ phương thức hay selector G02. Tên phương thức không chứng minh hỗ trợ. |
| Migration001 và docs/database/data-contract.md | sp_CapMa chỉ HOADON/DANHMUC_SP/NHANVIEN; role SQL CafeManagerApp có GRANT/DENY nguồn hiện tại | Chưa có migrations T06/T08, quyền setup/bootstrap hay contract CRUD/selector. CafeManagerApp là role DB, không phải ADMIN/CASH/STORE của app. |
| report.md G01 và b10-test-results.md | Hồ sơ ghi 45/45 ca và regression UI PASS; lưu bằng chứng, hạn chế dữ liệu grid demo được nêu rõ | Một số dòng lịch sử còn ghi chờ commit/chưa commit. Mốc Git bàn giao là tham chiếu riêng; lần rà soát này không chạy lại nghiệm thu G01 hoặc kiểm kê DB runtime. |

Chỉ có migration001 trong thư mục migrations hiện tại. Việc đọc script không
chứng minh migration đã áp dụng trên mọi DB. Collation thực tế, dữ liệu admin
trên DB đích và trạng thái cấu hình hiện tại phải được xác nhận trong plan/đợt
kiểm tra được duyệt, không suy diễn từ seed hoặc kết quả DB test cũ.

SDT của NHACUNGCAP hiện NOT NULL và UNIQUE; SDT NHANVIEN optional, có unique
index lọc NULL. Subcase NULL phải kiểm đúng contract từng bảng, không áp dụng
quy tắc optional của nhân viên sang NCC. Thiết lập/restore/cài đặt đầy đủ ở T30;
G02 phải chốt bước setup tối thiểu để bootstrap với quyền riêng rồi dùng runtime.

## 5. Quyết định bắt buộc trước khi duyệt cả bộ plan

Mỗi quyết định dưới đây phải có phương án cụ thể được agent đề xuất, lý do,
tác động SQL/DAL/BLL/UI, file/migration/fixture liên quan và ca kiểm chứng.
Đưa vào plan task sở hữu và dẫn từ plans.md; không để “quyết định khi code”.
Nếu chưa chốt hoặc còn mâu thuẫn contract thì bộ plan chưa đủ điều kiện duyệt.
Requirements này quy định kết quả cần đạt, chưa chọn bảng/cột/procedure mới.

### 5.1. T06 — Bootstrap DB sạch, DB demo và chuyển phiên bản

- DB sạch: đề xuất cách tạo chức vụ tối thiểu, role, nhân viên, account admin;
  liệt kê trường người cài nhập, giá trị hệ thống và toàn bộ dữ liệu tạo trong
  cùng transaction với trạng thái hoàn tất. Không cấp quyền app từ chức vụ.
- DB demo: đề xuất cách chọn đúng nhân viên/account kích hoạt, xử lý inactive,
  placeholder hash và nhân viên nghỉ; không tự bật mọi account seed.
- Đã có admin hợp lệ nhưng thiếu trạng thái bootstrap do chuyển phiên bản:
  chốt cách nhận diện/xác nhận hoàn tất an toàn, không reset hash, không cho
  người gọi chiếm quyền quản trị qua bootstrap; chỉ kiểm HoatDong là không đủ.
- Chốt lưu bền vững trạng thái, transaction/lock, rollback, cạnh tranh hai
  session, retry/đối chiếu nếu kết quả ghi chưa biết; admin bị khóa không mở lại.
- Chốt cách chạy setup bằng quyền riêng, quyền được cấp, đóng setup sau hoàn
  tất và đăng nhập bằng credential runtime. Không fallback sang migration user.
- Nêu ranh giới với khôi phục quyền quản trị riêng và editor T24; không triển
  khai editor nhân sự/tài khoản đầy đủ ở G02.

### 5.2. T08 — Ma trận quyền từng danh mục

Plan T08 phải có ma trận cho từng ADMIN/CASH/STORE, mỗi vai trò tách rõ
đọc/thêm/sửa/xóa và quyền mở tab quản trị. Đọc phục vụ use case là contract
riêng, phải liệt kê các trường trả về; không suy từ quyền đọc sang được xem
mọi trường tài chính. Không để ô ma trận chưa quyết định khi xin duyệt bộ plan.

| Danh mục phải có hàng riêng trong ma trận | Phạm vi trường/quyền đọc phải phân biệt |
|---|---|
| DANHMUC_SP | Quản trị danh mục và danh sách phục vụ chọn/lọc món |
| NHACUNGCAP | Quản trị NCC và lookup phục vụ kho/nợ; giới hạn trường theo use case |
| PHUONGTHUCTT | Quản trị phương thức và selector phương thức được hỗ trợ |
| DOITACGIAOHANG | Lookup mã/tên và quyền đọc/sửa tỷ lệ tài chính |
| DANHMUC_THUCHI | Quản trị loại Thu/Chi, mã hệ thống và lookup trong thao tác được cấp |
| CHUCVU | Lookup mã/tên và quyền đọc/sửa phụ cấp, hệ số |
| CALAMVIEC | Quản trị ca và lookup giờ ca theo quyền use case |

BLL phải thực thi ma trận kể cả gọi service trực tiếp, chặn ghi cấu hình tài
chính của CASH/STORE và quyền chưa được cấp; menu không là ranh giới kiểm quyền.

### 5.3. T08 — Mã hệ thống và mã danh mục

- Đối chiếu TC005/TC011 với chứng cứ ở mục 4; chốt mã chi lương, trả nợ, trả
  ngay, loại Chi, mã được bảo vệ và trường được phép sửa. Mã chi trả ngay còn
  thiếu contract, phải đề xuất rõ trước duyệt plan.
- Cấm đổi mã/loại hoặc xóa bản ghi hệ thống, kể cả chưa có lịch sử. Chốt cơ chế
  bảo vệ và xử lý xung đột dữ liệu sẵn có, không ghi đè cấu hình hợp lệ.
- Chốt migration DB sạch/DB demo và chạy lại không tạo trùng, không thay lịch
  sử tài chính; phân biệt missing, hợp lệ và mã tồn tại nhưng sai loại/ý nghĩa.
- Chốt nhập/cấp mã cho từng danh mục trong cả 7 bảng, kiểm trùng, giới hạn và
  tính bất biến khi sửa. sp_CapMa G01 không hỗ trợ các mã ngoài contract hiện có.
- Không triển khai nhập hàng T20 hoặc chi lương T26 trong G02.

### 5.4. T08 — Phương thức thanh toán

- Chốt tiêu chí hỗ trợ và contract selector checkout; loại PT010 tại service/
  contract, không chỉ ẩn control và không quyết định bằng tên phương thức.
- Chốt phương thức mới ADMIN thêm được dùng ngay hay phải cấu hình/xác nhận
  hỗ trợ; nơi lưu/quản trị và cách bảo vệ điều kiện này phải rõ trong plan.
- Chốt dữ liệu cài sạch/demo, hành vi selector với phương thức không hỗ trợ,
  ID lạ hoặc PT010. T08 test selector/service; không tạo checkout UI T15.

### 5.5. T06 — Username, phiên và rehash

- Chốt bộ ký tự username, chiều dài, trim, phân biệt hoa/thường, collation/
  uniqueness và cách xử lý dữ liệu cũ. UI/C#/SQL thống nhất; không trim password.
- Chốt thời điểm recheck HoatDong/DangLamViec/role trước thao tác mới và khi
  điều hướng; không tin role cache/menu. Lỗi recheck không cấp quyền giả.
- Logout vô hiệu tham chiếu session/service cũ; async trả muộn không mở lại
  MainForm hoặc cập nhật màn của phiên đã logout. Chốt lifecycle/guard cụ thể.
- Rehash phải tránh ghi đè hash mới khi password đã đổi sau lúc đọc. Chốt
  concurrency contract và hành vi login nếu cập nhật không còn hợp lệ.
- Chốt integration test cập nhật rehash trên SQL Server thật và race đổi
  password; unit test verify riêng không thay chứng cứ ghi DB.

### 5.6. T07 — Shell, UI và giới hạn nhóm

- Chỉ shell và các màn T06–T08. Chốt màn bắt đầu CASH/STORE hợp lệ, ghi rõ
  chức năng chưa triển khai; không tạo giao dịch POS/kho giả hoặc dựng toàn bộ
  module tương lai. Rời app không xóa đơn đã lưu là bất biến, không là lý do
  triển khai POS sớm để test.
- Chốt mẫu chung loading/rỗng/lỗi/xung đột/kết quả chưa biết và áp dụng khi màn
  hiện có cần; chặn gửi lặp, giữ input/dữ liệu cũ, xử lý nháp phù hợp UX.
- AppTheme theo guide Espresso/Kem/Caramel, font/spacing/native controls. Chốt
  Designer save/close/reopen, keyboard/focus, 1280×720/1366×768, DPI100/125/150%
  thật với bằng chứng riêng cho bản sau Save. Preview HTML không là test app.

## 6. Subcase bắt buộc trong bộ plan và biên bản

Giữ nguyên 27 ID T06–T08 và 325 ID gốc toàn dự án. Các subcase dưới đây là
bắt buộc, không tùy chọn, không thay checklist gốc. Plan gán hậu tố như
T06-K02.a, ghi fixture/thao tác/expected; biên bản ghi actual và kết quả riêng
PASS/FAIL/BLOCKED/NOT RUN cho từng subcase, không gộp để che ca chưa chạy.

| Truy vết ID gốc | Subcase bắt buộc phải phân rã trong plan |
|---|---|
| T06-K01/K02 | DB sạch, DB demo, admin hợp lệ thiếu trạng thái khi chuyển phiên bản; hai session bootstrap đồng thời chỉ một thành công; lỗi giữa transaction rollback toàn bộ rồi retry hợp lệ; restart không mở lại; admin khóa/nghỉ sau bootstrap không mở lại |
| T06-K03/K05/K10 | Username biên ký tự/độ dài/trim/hoa-thường theo collation đã chốt; không trim password; hash placeholder bị từ chối; rehash ghi DB thật, race password mới không bị ghi đè |
| T06-K04/K06/K08, T07-K02 | Khóa account/đổi role/nhân viên nghỉ trong phiên; gọi service trực tiếp bị chặn; tham chiếu session/service cũ sau logout; async trả muộn không phục hồi phiên/màn cũ |
| T08-K01/K02/K03/K04/K05 | CRUD đủ 7 danh mục, mở lại/persist; tìm tiếng Việt; tên rỗng/whitespace/quá dài; SDT NULL theo contract từng bảng và trùng; mã trùng/FK sai; biên tỷ lệ/phụ cấp/hệ số/ca qua đêm; xóa chưa dùng và có lịch sử |
| T08-K06/K07/K10 | Dữ liệu hệ thống DB sạch/demo, migration chạy lại; xung đột mã; mã hệ thống không đổi mã/loại/xóa ngay cả chưa có lịch sử; giữ cấu hình hợp lệ và snapshot phí đơn cũ |
| T08-K08/K09 | Selector theo tiêu chí hỗ trợ đã chốt, PT010 bị loại, phương thức mới và ID không hỗ trợ; quyền đọc trường/quản trị từng danh mục, gọi service trực tiếp ở cả 3 role |
| T07-K03/K05/K06/K07 | Chốt số lượt đổi module cụ thể cùng số đo control/event handler/connection trước-sau; modal/loading/gửi lặp; hủy không ghi/lưu refresh; nháp khi rời/logout/đóng |
| T07-K04, UX-K39/UX-K40 | Hai kích thước ở DPI100/125/150%; keyboard/focus; Designer round-trip và kiểm binary/source sau Save; riêng ca150% và Designer có bằng chứng độc lập |

SQL trên engine thật, DB test riêng; hai session cho concurrency và đối chiếu
toàn bộ dữ liệu/trạng thái/transaction sau lỗi. Không dùng SQLite hay thay
expected để né lỗi. UI cần WinForms thật, không chỉ ảnh guide/DTO demo.

## 7. Quy trình và hồ sơ

1. Duyệt requirements G02 **một lần**.
2. Viết đủ plan dưới `t06-bootstrap-dang-nhap-phan-quyen/`,
   `t07-khung-giao-dien-dieu-huong/`, `t08-danh-muc-nen/`; chỉ mục `plans.md`.
   Duyệt **cả bộ plan một lần** trước triển khai.
3. Triển khai tuần tự T06→T07→T08, SQL→DAL→BLL→UI với task có SQL. Báo blocker
    nếu đầu ra task trước chặn task sau; chưa ghi nghiệm thu PASS.
   Được build, kiểm tra compile SQL/migration và smoke check tối thiểu trong
   phạm vi đã duyệt để xác nhận đầu ra dùng được cho task sau. Ghi riêng là
   kiểm tra kỹ thuật (lệnh/môi trường/kết quả), không thay checklist nghiệm thu
   hoặc tự đánh dấu PASS Txx-Kxx. Đổi scope/contract ngoài duyệt phải báo để
   người dùng quyết định trước khi tiếp tục.
4. Xong nhóm → chờ duyệt **đợt test một lần**. SQL Server thật, DB test riêng,
   fixture DB sạch và seed, UI WinForms thật; source cố định trong từng lượt.
5. Biên bản từng task cạnh plan; báo cáo nhóm ở `report.md`, giữ PASS/FAIL/BLOCKED/
   NOT RUN. Không sửa source/migration giữa lượt kiểm thử nghiệm thu. Bug-report
   tại task sở hữu, tổng hợp đợt bug và chờ duyệt trước debug; duyệt debug bao
   gồm regression đúng phạm vi ảnh hưởng. Bug mới ngoài đợt cần duyệt bổ sung.
6. Chỉ commit/push khi được giao riêng cho bàn giao G02; yêu cầu Git G01 trước
   không được suy rộng thành cho phép tự commit mọi nhóm sau.

## 8. Điều kiện hoàn tất

Đủ đầu ra nguyên bản của T06–T08, **27/27 ca bắt buộc PASS** và toàn bộ subcase
bắt buộc theo plan có bằng chứng; không lỗi trọng yếu, đủ biên bản và commit bàn
giao theo kế hoạch. Migrations chạy được trên version G01, giữ dữ liệu/lịch sử
cũ; cài DB mới không seed có bootstrap và danh mục hệ thống hợp lệ.
Không ghi Hoàn tất từ build hoặc source review. G03 phụ thuộc đầu ra T08.

Lần sửa tài liệu trước không phải duyệt triển khai. Requirements sau đó đã được
duyệt riêng; tiếp theo chờ duyệt cả bộ plan, rồi duyệt đợt nghiệm thu riêng.
