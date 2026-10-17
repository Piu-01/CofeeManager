# Plan T07 — Shell, theme và điều hướng

Trạng thái: Hoàn tất; bộplan đãduyệt, nghiệm thu T07 7/7IDgốc PASS17/10/2026. ĐãbàngiaoGit,xem`../git-handoff.md`. Phụ thuộc T06 auth/session đã nghiệm thu.
Nguồn: kế hoạch T07; requirements G02; UX02/UX09/mục3–6; guide README,
index.html và preview.html. Không SQL migration tại T07.

## 1. Bước và file

| Bước | File dự kiến trong src/CafeManager.WinForms | Đầu ra / kiểm tra kỹ thuật |
|---|---|---|
| 1 | Theme/AppTheme.cs | Palette/font/spacing, helper native button/grid; style round-trip được |
| 2 | Forms/MainForm.cs/.Designer.cs/.resx | Sidebar, topbar/session, content, logout; constructor Designer không SQL |
| 3 | Modules/Home/SessionHomeControl.*, Modules/System/SystemInfoControl.* | Hai màn có thật dùng được ở3role, không giao dịch/module tương lai |
| 4 | Navigation/NavigationController.cs, Navigation/NavigationTarget.cs | Lazy một instance mỗi module/phiên, guard quyền và nháp |
| 5 | Forms/LoginForm.*, Program.cs, ApplicationSessionContext.cs | Vòng Login→Main→logout→Login, một MainForm, guard await/disposal |
| 6 | UI/ModuleState.cs, UI/OperationStatePresenter.cs, UI/IUnsavedChanges.cs | Mẫu trạng thái và hỏi bỏ nháp dùng ở màn hiện có |
| 7 | tests/CafeManager.Tests/UI/*, tools/ui/Verify-G02.ps1; README | Chuẩn bị phép đo/test UI và hướng dẫn, chưa chạy nghiệm thu |

NavigationController phục vụ đúng shell này, không module registry/plugin hoặc
generic UI framework. IUnsavedChanges có contract cụ thể HasUnsavedChanges/
DiscardChanges cho editor thật T08, không tạo interface rỗng.

## 2. Menu, màn bắt đầu và style

| Role | Màn bắt đầu | Menu G02 |
|---|---|---|
| ADMIN | Trang phiên; sau T08 vào Danh mục nền | Trang phiên, Danh mục nền (chỉ nối khi T08 có control), Thông tin hệ thống, Đăng xuất |
| CASH | Trang phiên ghi “Bán hàng chưa triển khai” | Trang phiên, Thông tin hệ thống, Đăng xuất |
| STORE | Trang phiên ghi “Kho chưa triển khai” | Trang phiên, Thông tin hệ thống, Đăng xuất |

Không mở POS/kho giả, không dựng UserControl cho tất cả task sau. Các lookup
quyền CASH/STORE của T08 test service; chưa có màn use case thì không thêm màn
chỉ để mở lookup. SystemInfo chỉ tên app/SDK build/schema version và tình trạng
kết nối; không connection string/hash/SQL error hoặc tên login migration.

Topbar có tên nhân viên/role, tiêu đề, trạng thái kết nối và logout. Không dùng
chức vụ thay role. SessionHome không tổng doanh thu/tồn0 giả khi chưa có dữ liệu.
ADMIN chỉ vào màn thật sau T08. Menu và BLL dùng cùng permission enum T06.

AppTheme: Espresso#241814, primary#4B2E23/hover#633E2D, accent#C58B52,
background#F5EFE6, surface#FFFCF8, text#291E18, border#DDD0C2,
selected#EAD8C3; Segoe UI10pt, title17pt, nhóm12pt; spacing4/8/12/16/24 ở96DPI.
Sidebar200px cơ sở, layout co giãn, native TableLayoutPanel/Dock/Anchor, vùng
cuộn khi thiếu chỗ. Không cỡ fixed whole Form làm che nút ở150%; minimum shell
640×400 cơ sở với kiểm chứng sau DPI scaling. Status có chữ, AccessibleName,
mnemonic/tab rõ. Designer layout giữ riêng, helper style runtime chỉ UI, không SQL.
Không thêm asset mới: icon/text native hiện có đủ, không phụ thuộc Internet.

## 3. Lifecycle, nháp và trạng thái

- ApplicationSessionContext quản lý LoginForm và MainForm hiện tại; login
  successful/generation valid mới mở MainForm, ẩn Login. Đóng Main không để
  process/chủ phiên thứ2 sống ngoài ý định. Logout đóng/dispose Main/controls,
  revoke phiên T06 và trở lại Login mới; không mở lại object phiên cũ.
- Module instance lazy một lần/phiên, giữ filter khi quay lại; chỉ attach event
  handler một lần, controller detach/dispose khi logout/đóng app. Connection
  chỉ mở trong repository call, navigation không giữ SQLconnection.
- Mỗi navigate recheck role/trạng thái/stamp trước chuyển nội dung; late result
  phải khớp session generation + request version + chưa dispose. Hủy request
  cũ không chứng minh rollback SQL; chỉ bỏ UI update muộn.
- Nháp editor: đang nhập hỏi Ở lại/Bỏ nháp và rời. Nếu muốn lưu, ở lại và dùng
  Lưu tường minh; không Enter/chuyển màn tự commit. Dialog Hủy không ghi. Lưu
  thành công chỉ refresh control sở hữu và giữ filter. Sau khi T08 nối, đây là
  màn thật để nghiệm thu T07-K06/K07, không editor demo ghi giả.
- Đang gửi write: chặn gửi lặp và chuyển màn/đóng gây mất đối chiếu; thông báo
  đang chờ. Nếu timeout/error unknown, giữ editor/request ID và bắt đối chiếu
  trước retry. Cho đóng không giả success/rollback, ghi hướng dẫn quay lại đối
  chiếu. Không thêm hoàn tiền/giao dịch POS để test bất biến đơn đã lưu.

| Mẫu chung | Hiển thị/hành vi | Nơi áp dụng G02 |
|---|---|---|
| Loading | Chữ đang tải/gửi, khóa nút gửi, hủy read nếu hỗ trợ | Login, recheck/navigation, danh mục T08 |
| Empty | Chữ không có dữ liệu; không lỗi | Grid T08 kết quả0 |
| Error | Thông báo thân thiện, giữ input/rows cũ, Thử lại | Login/read T08 |
| Conflict | Bản ghi thay đổi/quyền phiên đổi; không overwrite, reload hoặc Login | CAS danh mục/rehash/session |
| Unknown write result | Không nói đã lưu; đối chiếu key/request ID trước gửi lại | Setup T06 và CRUD T08 |
| Saved | Kết quả sau xác nhận commit, refresh owner | Dialog T08 |

## 4. Kiểm tra kỹ thuật và fixture

Sau đầu ra T06, build shell và smoke login→trang phiên→logout ở runtime DB test,
ghi technical-checks.md, không PASS T07-Kxx. T08 nối danh mục sau đó mới có đủ
fixture hủy/lưu/nháp để chạy ma trận nghiệm thu khi được duyệt.

UI dùng app thật, Windows principal runtime như G01; credential nhập tại máy.
DB test G02 có3role active và nhân viên đang làm, password tạm sinh/nhập cục bộ.
Loading/error dùng endpoint hỏng hoặc SQL session2 giữ lock trên nguồn test,
không ngủ UI thread. Giữ nguồn test/source cố định, trả DPI125% sau mỗi lượt.
Designer mở không cần connection. Đối chiếu hash trước/sau Save; nếu source
serializer đổi, chụp manifest mới/build/test bản sau Save, không chốt từ ảnh cũ.

## 5. Nghiệm thu sau duyệt đợt test

| ID | Thao tác / dữ liệu | Expected / evidence |
|---|---|---|
| T07-K01.a | Login cả ADMIN/CASH/STORE | Main đúng tên NV/role, trang đầu đúng, screenshot/process identity |
| T07-K02.a | Menu3role, thao tác direct navigate trái quyền | Đúng menu, BLL/guard chặn, không Main thứ2 |
| T07-K02.b | Login/logout10 vòng, account role đổi/khóa khi Main mở | Đúng1Main/phiên; phiên cũ bị đóng và revoke, không control/handler còn sống |
| T07-K03.a | ADMIN đổi Trang phiên↔Danh mục50 vòng (=100 chuyển), STORE Trang phiên↔Thông tin50 vòng | Sau lần lazy đầu, số instance mỗi control không tăng; visible tree không nhân bản |
| T07-K03.b | Sau100 chuyển, gọi load1 lần và logout, forced GC trong test harness | 1request cho1click; event callback1lần; disposed controls không retained ngoài weakref; pool/request SQL không tăng bởi navigation |
| T07-K04.a | 1280×720 và1366×768, DPI100%/125% | Không che menu/nút/cột; bounds+ảnh native riêng6combo tổng cùng .b |
| T07-K04.b | Hai kích thước DPI150%, resize/minimize/restore | Cột/nút chính nhìn/scroll được, không chấp nhận height bị clamp |
| T07-K04.c | Main/Login/Bootstrap và editor T08 save/close/reopen Designer | Giữ layout/style, không DB query; ảnh/property/hash/binary sau Save |
| T07-K04.d | Tab/ShiftTab/mnemonic, disabled/loading/modal | Focus đúng, không gửi trùng, không Enter tự chốt |
| T07-K05.a | Read slow, clicklặp10 lần, hủy/read lỗi rồi tải lại | Không gửi lặp; UI responsive; sau completion/error/cancel nút đúng |
| T07-K05.b | Logout/đóng trong read pending rồi session2unlock | Kết quả muộn không hiện màn/phiên cũ; không disposed exception |
| T07-K06.a | Mỗi editor T08 nhập rồi Hủy/X | DB không đổi; state owner giữ |
| T07-K06.b | Lưu editor ở tab NCC khi tab khác đã lazy | Chỉ NCCrefresh, filter giữ, không cập nhật lẫn module |
| T07-K07.a | Input chưa lưu, điều hướng/logout/đóng, chọnỞ lại/Bỏ nháp | Ở lại giữinput, bỏ nháp không ghi; không autosave |
| T07-K07.b | Có HOADON mở từ fixture rồi đóng app | Header/chi tiết giữ nguyên, không gọi hủy; không tạo POS giả |

Đếm control/handler bằng harness UI cùng process test (STA) với weakref/counter
callback/test service observer, không thêm module hoặc telemetry sản phẩm chỉ
để đo. Connection evidence dùng SQL DMVs theo app/test session, phân biệt pool
idle hợp lệ với connection sống do navigation. Ghi môi trường/count trước-sau.
T07-K04.a tách thành .a.100.1280/.a.100.1366/.a.125.1280/.a.125.1366;
.b tách .b.150.1280/.b.150.1366. Mỗi viewport có ảnh/bounds/actual/status riêng.
.c tách từng Form/control có Designer, .d tách3DPI; không dùng PASS viewport
khác thay ca thiếu. T07-K01.a vàK02.a cũng tách3role, K06.a tách7editor.
Mọi subcase bắt buộc; actual/status hiện NOT RUN. Nghiệm thu T07 sau T08 có
editor thật, lưu cạnh task và report nhóm, không chỉ build hoặc HTML preview.
