# Plan — Hộp nghiệp vụ nổi trong MainForm

**Lịch sử:** plan container/card này đã được triển khai và dùng thử nhưng không
đúng kiểu Form con user mong muốn. Quyết định mới là native Windows modal,
theo native-dialog-adjustment-plan.md đã duyệt26/10/2026. Không dùng plan cũ để
ghi đè yêu cầu mới; evidence host cũ được giữ, không chuyển expected thànhnative.

Requirements và toàn bộ plan (gồm chọn hết nội dung input) đã được người dùng
duyệt26/10/2026. Trạng thái: ĐÃ TRIỂN KHAI, CHỜ DUYỆT ĐỢT TEST riêng.
Một đợt triển khai tuần tự, sau đó trình một đợt test riêng.
Không đổi SQL, Models/DAL/BLL, role hoặc nghiệp vụ T01–T32.

## 1. Kết quả rà soát và quyết định kỹ thuật

MainForm đã có content/UserControl; không thay thành MDI. Có13file gọi
ShowDialog, gồm cả dialog máy in cần giữ native. Form nghiệp vụ hiện có chứa
validation, result, Close/FormClosing/Shown và dispose; giữ control/layout đang
dùng và chuyển chúng thành nội dung child trong host, tránh viết lại mọi editor.

Thêm một OrbitDialogHost trên vùng client MainForm và một lớp nền nhỏ cho hộp
nghiệp vụ. Child Form dùng TopLevel=false, borderless, không taskbar/minimize/
maximize; host giữ thẻ tiêu đề, nút đóng, bounds và stack. Chỉ host này được mở
hộp app. Caller await kết quả, không Application.DoEvents, ShowDialog hoặc vòng
lặp message lồng để giả modal. Không bật Enabled=false cho toàn MainForm vì sẽ
khóa luôn hộp con; khóa nền/layout bên dưới và chặn điều hướng bằng trạng thái
host, chỉ hộp trên cùng nhận input. Không tạo lớp nền nghiệp vụ hoặc serviceDI
mới cho phần trình bày.

Trả kết quả qua task hoàn tất đúng một lần khi hộp thực sự được phép đóng. Hộp
đang close/dispose/logout không được callback muộn ghi vào màn cũ. Caller vẫn
sở hữu using/dispose sau await; host tháo parent/subscription trước trả kết quả.
Giải quyết cả lỗi khởi tạo/Shown, đóng MainForm và session revoke để task không
treo. Các button hiện chỉ gán DialogResult cần chuyển sang hành động hoàn tất
rõ ràng; child Form không được dựa vào auto-close của ShowDialog.

Xác nhận bỏ nháp trong FormClosing hiện đồng bộ phải chuyển thành guard đóng
bất đồng bộ: chặn đóng ban đầu, mở hộp xác nhận trên stack, chỉ đóng sau đồng ý;
không await trong async void FormClosing rồi để form đóng trước kết quả. Duy
trì busy/unknown/session policy cụ thể của từng editor.

## 2. Triển khai tuần tự

### Bước1 — Host, vòng đời và hộp xác nhận dùng chung

Files dự kiến: `UI/OrbitDialogHost.cs`, `Forms/OrbitDialogForm.cs` (mới),
`Forms/MainForm.cs`/Designer nếu cần bố trí root, `Navigation/NavigationController.cs`,
`Forms/OrbitConfirmationDialog.cs`, `UI/OrbitDialogLayout.cs`,
`Modules/Sales/SalesUi.cs` và token/theme chỉ nếu cần tái dùng giá trị sẵn có.

- Host phủ vùng client, panel nền khóa tương tác, card co tối đa theo client ở
  DPI hiện tại, title/close và phần nội dung không cắt footer. Dùng Orbit theme.
- Stack giới hạn theo luồng thật, hộp con quay về đúng hộp cha, phục hồi focus
  và enabled trước đó, không enable bừa toàn bộ control sau khi đóng.
- MainForm chặn navigate/logout/shortcut xuống nền khi host mở; đóng MainForm
  xử lý nháp/busy/unknown của hộp trên cùng. Session revoke giải phóng đúng thứ
  tự, không cố giữ dialog của phiên đã mất quyền hoặc giả rollbackSQL.
- Một API await cho mở hộp và một API xác nhận dùng cùng host; helper tìm MainForm
  bằng parent chain thay vì dựa Owner của top-level Form. Native owner dùng HWND
  cửa sổ chính hợp lệ. Startup/Login ngoài MainForm giữ đường xác nhận phù hợp
  vòng đời của chúng, không rơi vào fallback mở top-level cho editor nghiệp vụ.
- Hỗ trợ Esc/Tab/ShiftTab/Enter theo từng hộp; Enter không tự chốt. Lọc phím trên
  host không nuốt input dropdown, NumericUpDown hay calendar đang sử dụng.

Kiểm tra kỹ thuật: build; prototype hộp cha/con thật ở125%, trả OK/Cancel một
lần, close bị từ chối, resize, không thêm top-level window. Đây là smoke triển
khai, chưa đánh PASS ma trận nghiệm thu.

### Bước2 — Editor và picker dùng chung/các module quản lý

Bổ sung hành vi nhập dùng chung theo yêu cầu người dùng26/10: mở rộng
`UI/OrbitField.cs`/SelectAllOnEntry hiện có và gắn tại các điểm tạo/theme input
chung (`AppTheme`, `OrbitManagementTheme`/field factory, editor grid theo source
thực tế). Hỗ trợ TextBoxBase, phần text của NumericUpDown và ComboBox cho gõ;
readonly/disabled/dropdown chỉ chọn được bỏ qua. Gắn idempotent để ApplyTheme
nhiều lần/control động/editor grid tái sử dụng không nhân event. Chọn hết khi
Enter và mouse-up đầu khắc phục caret native; sau đó giữ caret/selection của
người dùng. Giữ IME/multiline/paste và tránh chọn lại sau mỗi TextChanged.
Rà mọi input của sidebar modules, POS, dialog và login/setup có thể nhập tay.

Files: `Forms/OperationEditorDialog.cs`/Designer, `CatalogEditorDialog.cs`/Designer,
`CustomerPickerDialog.cs`; callers trong `Modules/Catalogs`, `Inventory`, `Menu`,
`Tables`, `Customers`, `Loyalty` (rà path thực tế trước sửa).

- Đổi caller sang await host; handler đồng bộ mở picker đổi async đúng nơi.
- Giữ CaptureWrite/reconcile/requestId/ResultCode/rowRevision, nháp và lỗi SQL.
- Hộp xác nhận nằm trên editor; cancel giữ input. Picker trong preview/loyalty/
  editor quay về cha đúng selection, không làm mất filter danh sách.
- Các dialog cấu hình hiện có được mở từ màn hệ thống nếu có sẽ được rà caller,
  đưa vào cùng host; bootstrap/login/startup không nhập nhằng vào phạm vi này.

Kiểm tra kỹ thuật: thêm/sửa/hủy, error giữ input, picker lồng, dirty close và
lookup fail; build. Chưa chạy giao dịch nghiệm thu không được duyệt.

### Bước3 — POS, hóa đơn/giao hàng và preview/in

Files: `Modules/Sales/SalesControl.cs`, `SalesUi.cs`/OrderLineDialog,
`OrderHeaderDialog.cs`, `OpenOrdersDialog.cs`, `CheckoutDialog.cs` và layout,
`InvoiceDetailDialog.cs`, `InvoicesControl.cs`, `DeliveryControl.cs`,
`Forms/InvoicePrintPreviewForm.cs`, `Printing/InvoicePrinting.cs`,
`UI/StarDustOverlay.cs` chỉ khi cơ chế parent/input hiệu ứng cần đổi theo host.

- F3/F4/F8, nhập/sửa món và xem giao/hóa đơn dùng cùng host. Giữ toàn bộ data/
  validation và states đã có; UI06 tiếp tục lỗi đúng ô.
- F4doubleclick/mở đơn trả result rồi đóng đúng hộp trước tảiPOS; không một lần
  click mở hai hộp hoặc để hộp cũ phủ POS. Quay lạiPOS từ detail cũng như vậy.
- Detail→preview, F3→thêm/chọn khách, checkout→xác nhận là stack có thứ tự;
  cha không đóng khi con đang mở, back giữ input khi chưa áp dụng.
- Checkout giữ previewToken/revision/requestId/unknown/reconcile, Confirm một
  lần. Burst chỉ nút Confirm, overlay hiệu ứng không là cửa sổ nghiệp vụ độc
  lập và không nhận input. Không đổi timing commit để phục vụ animation.
- Sau paid: đóng checkout, hỏi in trong host; native PrintDialog có owner đúng;
  chọnKhông/hủy/lỗi vẫn reset POS như contract, không checkout lại khi in.
- Preview vẫn dùng InvoicePrintDocument và reset BeginPrint; không tạo payment
  hay tính lại tiền. Dialog chọn printer/lưuPDF do Windows mở là ngoại lệ đã duyệt.

Kiểm tra kỹ thuật: build và đường mở/đóng/result, không chốt lại các đơn đã trả.

### Bước4 — Rà hết caller và tài liệu bàn giao

- Rà `ShowDialog`, `Show`, `DialogResult`, `FindForm`, `Owner`, MessageBox,
  OnFormClosing/Shown, AcceptButton/CancelButton và các fire-and-forget handler.
  Mọi editor/detail app trong phạm vi phải qua host; ghi danh sách ngoại lệ OS/
  startup, không chặn bằng regex rồi suy PASS toàn app.
- Cập nhật `docs/UX_QuanLiQuanCafe.md`, `docs/ui/WINFORMS_IMPLEMENTATION.md` và
  hồ sơ task này; giữ nguyên các checklist gốc. Build Debug/Release sạch.
- Đánh dấu bằng chứng G03/G04 cũ được kế thừa về SQL/BLL nhưng UI bị ảnh hưởng
  cần hồi quy, không tự nghiệm thu host mới từ ảnh hoặc kết quả cũ.
- Trình đợt test; không commit hoặc thay development DB/profile tự động.

## 3. Ma trận test sau duyệt riêng

| Mã | Thao tác | Kết quả cần có |
|---|---|---|
| ED01 | Mở mọi editor/detail app theo inventory, resize/minimize/restore | Hộp nằm trong client, không thêm top-level/taskbar nghiệp vụ, không cắt hành động |
| ED02 | Click/Tab/shortcut/sidebar khi hộp mở | Chỉ hộp trên cùng nhận input, focus không xuống nền |
| ED03 | Cha→con→xác nhận→quay lại, hủy từng cấp | Input/filter/selection của cha còn, result đúng một lần, focus khôi phục |
| ED04 | Dirty Esc/X/Back/AltF4/MainForm close, đồng ý/từ chối | Không mất nháp ngoài ý muốn, không đệ quy/treo task, đúng guard |
| ED05 | Busy/unknown/error/reconcile và session revoke/logout | Chặn gửi/đóng sai, giữ đường đối chiếu, callback muộn không chạm control cũ |
| ED06 | Editor/catalog/menu/kho/bàn/khách/loyalty thêm/sửa/hủy/lỗi | Typed request/result và SQL contract giữ nguyên; refresh đúng màn |
| ED07 | F3 validation UI06 và thêm/chọn khách lồng nhau | Lỗi/focus đúng trường, cancel giữ input, không thêm cửa sổ |
| ED08 | F4 tìm/paging/Enter/doubleclick, mở từ history/giao về POS | Đúng mã, đóng hết hộp cần đóng, không nhân dialog hoặc handler |
| ED09 | F8 tiền sai, Enter/doubleclick, success/conflict/unknown | Không tự chốt/thu lặp, hiển thị trạng thái thật, tiền/kho/điểm/receipt đúng |
| ED10 | Paid→hỏi inCó/Không, hủy nativeprint, lỗi đọc/in, historypreview | POS reset đúng, native owner đúng, dữ liệu tài chính không đổi khi in lại |
| ED11 | CASH/ADMIN/STORE; đóng/mở hộp nhiều lượt | Quyền giữ nguyên, không rò form/control/timer/event, không giữ dữ liệu phiên cũ |
| ED12 | Client1280×720/1366×768 tại125%, hộp dài/nhiều dòng | Bounds/focus/scroll/footer dùng được; hình thức user kiểm,100/150%NOT RUN |
| ED13 | Click/Tab vào ô có giá trị, rời/vào lại; text/tiền/số lượng/ghi chú/combo gõ/grid editor | Chọn hết ở lần vào; gõ/paste thay đúng, click tiếp vẫn đặt caret, readonly/dropdown chỉ chọn giữ nguyên; IME không bị gián đoạn, input động/reused không nhân handler |

Lưu test-results.md: source/DLLhash, schema/DBmarker, thao tác/expected/actual,
PASS/FAIL/BLOCKED/NOT RUN từng ca/subcase và bằng chứng. Test tài chính trên SQL
Server DBtest riêng, snapshot trước/sau và hai session cho các ca cạnh tranh bị
ảnh hưởng; tận dụng DB hiện có theo giới hạn tài nguyên, không tạo DB tùy tiện.
Bug mới điều tra/ghi riêng, duyệt đợt sửa trước debug; giữ source cố định mỗi lượt.

## 4. Rủi ro và kiểm soát

Nguy cơ chính là FormClosing/result/Owner đổi nghĩa khi Form không còn top-level,
keyboard đi xuyên nền, parent bị disable luôn hộp, task await không hoàn tất hoặc
doubleclick gọi hai handler. Giải quyết ở cơ chế host và từng caller; không che
lỗi bằng try/catch rộng, Application.DoEvents, timer chờ DialogResult hay tự đóng
hộp bất chấp busy. Nếu cần đổi contract nghiệp vụ thì dừng đúng phần đó để chốt
scope; không thêm migration vì yêu cầu chỉ là UI.
