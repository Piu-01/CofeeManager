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

## T07-SIDEBAR-20261109 — Header sai vị trí và menu thiếu phân nhóm

**Trạng thái cập nhật09/11/2026 sau làm rõ của người dùng: nhóm chỉ là label,
không thu gọn/ẩn tab; đã sửa và regression299/299PASS tại125%.** Xem
[biên bản label và giới hạn kiểm](sidebar-labels-test-results.md). Phương án
accordion ở dưới đã được chỉ dẫn mới thay thế;297/297PASS cũ giữ làm lịch sử.
Hình thức người dùng tự kiểm. Các mục bên dưới giữ nội dung điều tra/đề xuất
trước sửa; không dùng các dòng “chưa chạy” lịch sử thay kết quả mới.
Đây là bổ sung của shell T07, không mở lại requirements/plan toàn bộ G02 hoặc
thay lịch sử nghiệm thu và checklist T01–T32.

### Hiện tượng và bằng chứng

- Người dùng gửi ảnh ngày09/11/2026: phía trên sidebar có khoảng trống lớn;
  tên CoffeeManager và nhãn KHÔNG GIAN QUẢN LÝ xuất hiện giữa Bán hàng và
  Giao hàng. Nhiều mục nghiệp vụ xếp liên tục, không có nhóm.
- Ảnh là bằng chứng người dùng cung cấp; chưa có số đo bounds, DPI hoặc
  xác nhận binary/commit của cửa sổ trong ảnh. Không suy diễn đã tái hiện.
- Source tham chiếu: working tree trên HEAD
  `8a38f8691d37089feec149b71a8f5718d37167f3`; MainForm.cs đang có thay đổi
  sẵn từ trước lượt này. Khi sửa phải giữ các thay đổi đó.

### Nguyên nhân và giới hạn kết luận

- `UI/OrbitSidebar.cs:6,22–53`: lớp kế thừa FlowLayoutPanel, gọi
  `base.OnLayout(e)` rồi tự SetBounds cho header, từng button và footer.
  Cùng một cây control chịu cả flow layout và bố trí thủ công; thay đổi
  bounds/visibility/AutoScrollMinSize tiếp tục tác động layout. Đây là xung
  đột cơ chế bố trí cần loại bỏ. Chưa có trace runtime để xác định chính
  xác lượt layout nào tạo ra vị trí header trong ảnh.
- `OrbitSidebar.cs:28,35–40`: nhận diện hai label header bằng thứ tự trong
  Controls thay vì tham chiếu rõ ràng. Header còn nằm chung vùng AutoScroll
  với menu và footer; header không áp cùng offset cuộn như các button.
- `OrbitSidebar.cs:31–34`: khoảng đầu menu tính từ số nút và chiều cao cửa
  sổ, trong khi title đặt tại vị trí riêng; không có vùng header độc lập.
- `Forms/MainForm.cs:93–105`: tạo menu phẳng theo role, thêm từng button trực
  tiếp vào sidebar. Không có dữ liệu nhóm hoặc control tiêu đề nhóm, nên
  việc thiếu phân nhóm được xác nhận từ source.

### Phạm vi và cách sửa đề xuất để duyệt

1. Chia sidebar thành ba vùng: header cố định trên cùng, menu cuộn ở giữa,
   footer phiên/Đăng xuất cố định dưới cùng. Dùng Panel/TableLayoutPanel và
   Dock theo vùng; chỉ một cơ chế bố trí trong mỗi container. Bỏ cơ chế
   đoán header qua thứ tự label và công thức khoảng trống theo số nút.
2. Giữ Trang phiên là lối vào riêng ngay đầu menu. Gom toàn bộ mục hiện có:

   | Nhóm | Mục con theo thứ tự |
   |---|---|
   | Bán hàng | Bán hàng; Lịch sử hóa đơn; Giao hàng; Bàn / Khu vực |
   | Khách hàng & ưu đãi | Khách hàng; Ưu đãi / Điểm |
   | Menu & kho | Menu / Công thức; Nguyên liệu / Tồn; Kho |
   | Tài chính & báo cáo | Thu chi; Công nợ; Tài sản; Báo cáo |
   | Quản trị | Nhân sự; Danh mục nền; Thông tin hệ thống |

   Đây là nhóm điều hướng, không đổi màn hình/use case. Nhãn Tồn phục vụ bán
   của CASH và các tên hiện tại vẫn theo role.
3. Header nhóm có chevron, có thể mở/thu bằng click, Enter hoặc Space; mặc
   định mở nhóm chứa màn đang chọn, các nhóm khác thu gọn. Khi Trang phiên
   đang chọn, mở Bán hàng cho ADMIN/CASH hoặc Menu & kho cho STORE. Mở nhóm
   khác thu nhóm trước để giảm chiều dài; không tự chuyển màn khi mở nhóm.
   Nhóm không có mục được phép xem thì ẩn. Đường điều hướng từ nơi khác
   phải mở đúng nhóm và đưa mục đang chọn vào vùng nhìn.
4. Tiếp tục giao diện Orbit, font/màu/icon từ theme/resources hiện có; tên
   nhóm nhỏ, mục con thụt vào và trạng thái chọn rõ. Không giảm font để nhét
   mọi mục. Chế độ compact giữ các lối vào bằng icon có tooltip/tên truy cập,
   có phân cách nhóm và cuộn; header/footer không chồng vùng menu.
5. Giữ nguyên menu theo ADMIN/CASH/STORE, startup target và mọi module đã nối;
   tái dùng button/handler hiện tại. Cập nhật cách duyệt button/đánh dấu mục
   chọn trong MainForm khi button chuyển vào container con. Giữ NavigationController,
   xác nhận nháp, guard busy/modal, logout và quyền BLL. Không thay SQL/DAL/BLL.

File dự kiến: `UI/OrbitSidebar.cs`, `Forms/MainForm.cs`,
`Forms/MainForm.Designer.cs`; chỉ thêm token/helper nhỏ ở theme khi thực sự cần.
Không tạo framework navigation hoặc mở rộng UI bên trong module nghiệp vụ.
Giả định người dùng muốn nhóm thu gọn để giảm độ dài menu, không chỉ thêm
nhãn giữa danh sách dài. Đây là điểm cần duyệt trong đề xuất này.

### Kiểm chứng sau khi được duyệt sửa/debug

- Build Debug/Release; ghi là kiểm tra kỹ thuật, không thay nghiệm thu.
- Regression WinForms thật tại125%: thứ tự vùng/control và bounds sau mở,
  resize, cuộn menu, mở/thu nhóm, chuyển compact và quay lại; kiểm1280×720,
  1366×768 và cửa sổ hẹp trong giới hạn working area thực tế.
- Đối chiếu đủ mục và startup của3role với menu hiện hành; không thêm quyền
  hoặc mất mục; ẩn nhóm rỗng. Kiểm Tab/Shift+Tab, Enter/Space, selected item,
  tooltip/tên truy cập khi compact và mỗi click chỉ điều hướng một lần.
- Kiểm điều hướng ngoài sidebar mở đúng nhóm, giữ module/filter/nháp, ở lại
  khi từ chối bỏ nháp, guard modal/busy và Đăng xuất còn tiếp cận được.
- Lưu `sidebar-ui-test-results.md` cạnh hồ sơ task, có expected/actual,
  môi trường/source và PASS/FAIL/BLOCKED/NOT RUN. Duyệt sửa/debug bao gồm
  regression trong phạm vi bug theo quy trình, không mở đợt nghiệm thu toàn G02.
- Agent không kiểm hình thức/chụp ảnh đối chiếu; ghi SKIPPED do người dùng tự
  kiểm. DPI100%/150% chưa chạy ghi NOT RUN, không đổi Windows Scale.

### Rủi ro và giới hạn

Chuyển button vào container con có thể làm các vòng lặp
`sidebar.Controls.OfType<Button>()` bỏ sót mục chọn hoặc style; cần sửa đúng
những chỗ phụ thuộc này. Mở/thu nhóm và chuyển compact có thể làm mất focus
hoặc vị trí cuộn, nên nằm trong regression. Không có thay đổi dữ liệu DB.
Chưa chạy app/test, không ghi PASS mới hoặc kết luận đã sửa từ việc đọc source.
Không gộp tối ưu ngoài lỗi vào đợt này.

## T07-HOME-20261110 — Trang phiên trùng màn nghiệp vụ ở CASH/STORE

**Trạng thái:** đã sửa theo hướng người dùng duyệt ngày10/11/2026; kiểm native
R01–R05 chờ người dùng thực hiện. Xem
[biên bản](home-role-test-results.md).

**Hiện tượng:** ảnh người dùng gửi cho thấy sidebar STORE có mục "Tổng quan"
trỏ vào tab Kho (trùng lối vào Khóa) và CASH có "Trang phiên" tĩnh không có
thao tác. Đây là hai lối vào trùng màn đích, gây nhiễu điều hướng.

**Nguyên nhân (từ source):** `MainForm` luôn đưa `homeButton` vào sidebar và
màn mở đầu rơi về `NavigationTarget.Home` cho mọi role; `Program.cs` map Home
sang `SessionHomeControl` với CASH/STORE, trong khi role đó chỉ dùng màn nghiệp
vụ bên dưới.

**Hướng đã duyệt:** CASH/STORE bỏ nút Trang phiên và vào thẳng Bán hàng/Kho;
ADMIN giữ nút với nhãn "Tổng quan"; `SessionHomeControl` giữ làm fallback cho
route Home, không xóa file. Không đổi quyền, module hay điều hướng nghiệp vụ.

**Phạm vi sửa:** `Forms/MainForm.Designer.cs` (nhãn), `UI/OrbitSidebar.cs`
(`HomeVisible`), `Forms/MainForm.cs` (ẩn home theo role, màn đích theo role),
`en.json` (key "Tổng quan"). Build Release PASS; Debug BLOCKED do app đang chạy
khóa file bin; visual/DPI100/150 NOT RUN.
