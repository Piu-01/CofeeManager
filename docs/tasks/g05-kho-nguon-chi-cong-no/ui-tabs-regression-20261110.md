# G05/G06-UI — Tab Kho và Nhân sự

Ngày10/11/2026. Người dùng duyệt cả đợt sửa/đo/regression bằng “duyệt”, sau đó
yêu cầu “tiếp tục”. Phạm vi theo [báo cáo điều tra](ui-tabs-investigation-20261109.md).
**Đã sửa;100/100 kiểm tra chính và12/12 kiểm tra bổ sung PASS trên module thật.**
Hình thức do người dùng tự kiểm; không nâng nghiệm thu toàn G05/G06/T01–T32.

## Thay đổi trong source

Chỉ ba file thuộc đợt này:

- `UI/OrbitTabControl.cs`: bổ sung AddLazyPage opt-in. Giữ tất cả nhãn tab;
  lần chọn đầu tạo nội dung, theme một lần trước khi gắn vào cây control,
  Dock.Fill, cache instance; dispose theo TabPage. Các caller không dùng API
  này giữ hành vi cũ. Không thay WndProc/vẽ chrome hiện có.
- `Modules/Inventory/InventoryControl.cs`: dùng OrbitTabControl với
  FlatManagementTabs=true, cấu hình chrome trước khi có handle; bốn nhãn
  Tồn kho/Nhập hàng/Xuất kho/Kiểm kê giữ nguyên. Tồn kho nhận Dock.Fill từ helper,
  không còn ancestor150×150. Gom layout, không dựng cả bốn body ngay khi mở.
- `Modules/HumanResources/HumanResourcesControl.cs`: dùng cùng helper, giữ
  điều kiện schema/quyền, PendingOperation scope và đủ bốn nhãn. Theme chrome
  trước khi gắn; bỏ AppTheme.Apply toàn cây sau khi các page đã hiện. Mỗi page
  vẫn là control nghiệp vụ cũ, giữ filter/input/query và vòng đời modal.

Không đổi BLL/DAL/SQL, lifecycle đọc của các danh sách, sidebar label, quyền,
giao dịch kho/lương hoặc cơ chế freshness hiện có. Trace không chứng minh tải
trùng ở ba danh sách chứng từ Kho nên không sửa handler Load/VisibleChanged.

## Môi trường và fixture

- Windows, .NETSDK10.0.401, net10.0-windows/PerMonitorV2; DeviceDpi120=125%
  thực. Desktop1920×1080; client1280×720,1366×768,800×640.
- SQL Server thật localhost; một DB test riêng
  `CoffeeManager_Test_UiTabs_20261110_b00bb153`, marker UiTabsTestMarker với
  RunId20261110_b00bb153; baseline→seed một lần→migrations001–024.
- Runtime database user UiTabsRuntime, WITHOUT LOGIN, member CafeManagerApp;
  factory chạy EXECUTE AS USER trên từng connection không pooling. Session
  lấy identity fixture ADMIN/STORE/CASH, PermissionService/BLL/DAL thật.
  Không dùng DBdevelopment hoặc DB đang sử dụng.
- Fixture bootstrap admin bằng PasswordService/hash ngẫu nhiên trong bộ nhớ;
  bật ba account demo chỉ trong DB test. Không kiểm login nhập mật khẩu và
  không lưu mật khẩu/hash/connection string vào raw result.

## Đo trước/sau và source cố định

Artifacts: [native-tabs-20261110](runs/native-tabs-20261110/).

Đã đo bản gốc đầu tiên: Nhân sự1314ms, heartbeat gap1220ms. Trong lúc làm,
workspace có thay đổi đồng thời về ngôn ngữ/dashboard ở nhiều file khác.
Không sửa đè chúng. Bản chụp đầu thiếu resource/ngôn ngữ, rồi gặp lỗi namespace
do phần việc khác chưa hoàn thiện; các log build lỗi được giữ nguyên.

Để so sánh có ý nghĩa, tạo FrozenBefore/FrozenAfter với phần source chung
giống nhau. FrozenBefore khôi phục cách ghép eager/native tabs/thiếu Dock của
ba file từ source đã điều tra, giữ wrapper ngôn ngữ của phần việc đồng thời;
FrozenAfter chứa đúng bản sửa. Hai bản chỉ khác ba file nêu trên. Đo bằng cùng
runner/fixture, cùng ngôn ngữ mặc định tiếng Việt. Không chỉnh source snapshot
hoặc expected trong lượt test. 305file FrozenAfter không đổi trong cả lượt
chính và bổ sung; hash ba file trong workspace khớp bản đã kiểm.

| Mở lần đầu | Trước | Sau | Ý nghĩa |
|---|---:|---:|---|
| Nhân sự ADMIN — elapsed |668,5ms|232,8ms|Giảm công việc khi mở module |
| Nhân sự — heartbeat gap lớn nhất |605,8ms|174,8ms|UI ít bị giữ liên tục hơn trong lượt đo |
| Nhân sự — lệnh procedure bắt đầu |11|7|EmployeeLookups từ3lần còn1; vẫn kiểm quyền |
| Nhân sự — layout events theo instrumentation |667|49|Không re-theme/recreate cả cây đang hiện |
| Nhân sự — page đã tạo lúc đầu |4|1|Chỉ dựng body được chọn |
| Kho ADMIN — elapsed |202,7ms|273,6ms|Không có bằng chứng mở Kho nhanh hơn |
| Kho ADMIN — layout events |94|51|Giảm dựng/layout không cần thiết |
| Kho — page đã tạo lúc đầu |4|1|Giữ đủ4nhãn, chưa dựng3body còn lại |

Tồn kho trước bị co, sau giãn hết nên diện tích hiển thị/vẽ khác nhau; không
co lại hoặc giấu grid để cải thiện số đo. Timings là một cặp chẩn đoán trên
máy này, có tác vụ khác chạy nền, không phải benchmark T29 hoặc cam kết FPS.
Raw có cả lần đầu, chuyển tab, chuyển lại và reenter. Các timing warm chỉ là
tham khảo vì runner nhận biết DataSource có sẵn; không suy là toàn bộ query
refresh đã hoàn tất. Kết luận hiệu năng dựa vào mở lần đầu, số lần gọi thực
và số page/layout; không tự đổi contract/filter snapshot nguyên liệu.

## Kết quả chức năng

| ID cục bộ / tham chiếu | Expected/actual | Kết quả |
|---|---|---|
| UI01 — T20/T24, T07-K02 | ADMIN đủ4tab mỗi module; STORE4tab Kho; CASH không vào Kho/Nhân sự | PASS |
| UI02 — T07-K04 | Dùng OrbitTabControl.FlatManagementTabs=true; không native TabControl mặc định ở2caller | PASS cấu hình/native control; không nghiệm thu pixel/viền thay user |
| UI03 — T07-K04 | Nội dung mọi tab Dock.Fill và nhận đủ vùng page ở ma trận3client/role | PASS;6FAIL thiếu Dock của baseline đã hết |
| UI04 — T07-K03 | Chỉ1page được dựng ban đầu; chọn lần đầu có binding thật, quay lại cùng instance; module cache giữ nguyên | PASS |
| UI05 — T07-K03/K04 | Mũi tên native đổi tab; đủ nhãn; input nháp tìm kiếm giữ khi đổi tab; không tạo lại body | PASS |
| UI06 — trạng thái đọc | Stock có dữ liệu; tìm không khớp cho rỗng; SQL error là lỗi rõ, không giả0; phục hồi tải lại được | PASS |
| UI07 — query lifecycle | Đọc bị giữ ở initializer rồi đổi tab nhanh: request cũ bị hủy, không đè trang hiện tại; quay lại tải được | PASS |
| UI08 — HR lỗi/khôi phục | EmployeeList bị DENY thật ở runtime test: lỗi rõ/DataSource null; REVOKE rồi refresh cùng page nhận dữ liệu | PASS |
| UI09 — modal/session | Modal có owner MainForm và chặn chuyển module; revoke trong lúc load đóng/dispose an toàn | PASS |
| UI10 — dữ liệu test | UI đọc không đổi hash các trường kho/hóa đơn/nhân viên/phiếu nhập được đối chiếu; owner transaction0:0 | PASS; không suy là nghiệm thu mọi giao dịch ghi |
| Hình thức, ảnh, hover/spacing | Người dùng tự nghiệm thu | SKIPPED theo phân công |
| DPI100/150%, nhập/chốt chứng từ/lương/unknown sau ghi, login thật | Không thuộc lời gọi ghi trong lượt này | NOT RUN; không nâng PASS task nghiệp vụ |

Raw: [before.json](runs/native-tabs-20261110/before.json) có88/97PASS,9FAIL
đã biết (3caller chrome và6ca Dock); [after.json](runs/native-tabs-20261110/after.json)
100/100PASS; [supplement.json](runs/native-tabs-20261110/supplement.json)12/12PASS.
Số này là kiểm component/biến thể, không phải112ID Txx-Kxx. Bản baseline đầu
giữ ở original-before.json. Source/binary hash tại source-audit.json; số đo tại
comparison.json, bao gồm các số không cải thiện, không chỉ chọn số tốt.

Trong lượt lỗi SQL, chỉ DENY tạm sp_MaterialList/sp_EmployeeList cho user của
DB test; finally REVOKE để về quyền role ban đầu. Không đổi expected để né lỗi,
không sửa source/fixture trong lượt chính; lượt bổ sung riêng có fault setup
được kiểm marker và allowlist. Không sửa procedure/migration hoặc ghi tiền/kho.

## Build và tài nguyên

- Snapshot Debug/Release:0warning/0error. Workspace Debug cuối cũng0warning/
  0error ở product-debug-final.log. Snapshot là binary kiểm module, không là
  package bàn giao thay cho các phần việc đang sửa đồng thời.
- Workspace Release hiện **BLOCKED** bởi các thay đổi đồng thời ngoài đợt này:
  CS1503 string→FormattableString tại các caller G05Display.Text đang được đổi
  bởi phần ngôn ngữ. Xem product-release-final.log. Trước đó có lỗi resource
  en.json/namespace và dashboard trong build workspace; không vá ngoài scope.
- Log còn ghi lỗi build công cụ trùng EmbeddedResource của các snapshot con;
  đã sửa csproj runner trước khi test bằng cách tắt default resource glob.
  CS0176 Padding.Empty trong helper được sửa thành new Padding(0) ở bước
  compile, trước lượt sau sửa. Không che hoặc xóa các log lỗi ban đầu.
- Tạo1DB/1database-only user,0SQLlogin/0Windowsaccount. Đã xác minh marker,
  đóng forms/process, dọn đúng DB/user này; fixture.json Status=Cleaned,
  Created=true/Cleaned=true, không có tài nguyên giữ lại. DB sẵn có giữ nguyên.
- Giữ artifacts/manifest/raw evidence; binary và snapshot được Git ignore.
  Chưa commit/push. Chưa tuyên bố hết mọi giật của Kho hoặc nghiệm thu hình thức.
