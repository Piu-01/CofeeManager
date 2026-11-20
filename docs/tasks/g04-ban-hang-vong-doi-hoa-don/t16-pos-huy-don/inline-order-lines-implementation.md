# Đặt món trực tiếp trên POS — triển khai10/11/2026

Đã triển khai theo [plan bổ sung](inline-order-lines-plan.md) sau khi người dùng
chốt4 điểm cùng ngày. Trạng thái: **đã sửa source, chờ duyệt đợt test riêng**;
không thay checklist/ca nghiệm thu T14–T18, không ghi PASS chức năng hoặc hình thức.

## Thay đổi trong phạm vi

- `Modules/Sales/OrderPanel.cs`: bỏ cột `Note`/`Amount` khỏi bảng; lưới còn
  `Name`, `Quantity`, `Price` và cột unbound `Delete`. Bỏ hàng nút sửa/xóa/hủy;
  chuyển `Hủy đơn mở` lên header cạnh F3, grid nhận phần cao còn lại. Bắt click
  `−`/`+` và thùng rác, nháy đúp cột tên để mở editor ghi chú inline; editor là
  control con của panel (TextBox + Lưu/Hủy), Enter lưu, Esc hủy, giữ text khi lỗi.
- `Modules/Sales/SalesControl.cs`: bỏ hộp `Món`, bỏ `EditLine` và field nháp
  `productDraft`/`lineDraft`. Bấm card món thêm ngay1 đơn vị; cùng món+cùng giá
  snapshot+cùng ghi chú gọi `Update` tăng dòng cũ, khác thì `Add` dòng mới. Đơn đầu
  tại quán cần bàn, mang đi tạo ngay, giao hàng mở F3 một lần nếu thiếu thông tin.
  `−`/`+`/ghi chú dùng `LineAsync` action `Update`; thùng rác gọi `Delete` ngay,
  không hỏi; hủy cả hóa đơn giữ xác nhận cũ.
- `UI/OrbitDataGridView.cs`: profile `OrderLines` đo cột theo dữ liệu, header/cell
  cùng padding, `Price` căn phải; vẽ tên + ghi chú dưới, cụm `− n +`, icon thùng
  rác; dòng36/54 logical; header28 logical. Tooltip hướng dẫn thao tác.
- `Theme/OrbitManagementTheme.cs`: thêm nét icon `minus`, dùng lại `plus`/`trash`.
- `Modules/Sales/SalesUi.cs`: xóa `OrderLineDialog`, không còn cửa sổ nhập món.
- `docs/UX_QuanLiQuanCafe.md` mục7.2 và `docs/ui/WINFORMS_IMPLEMENTATION.md`:
  cập nhật luồng dòng món, hành vi click và mapping profile.

Không đổi Models/DAL/BLL/SQL/migration/procedure/quyền/snapshot/tổng tiền. POS vẫn
chỉ hiển thị `Phải thanh toán` từ SQL; `Amount` không còn trong lưới.

## Kiểm kỹ thuật trong triển khai

- Build Debug và Release: **0 warning, 0 error**.
- Smoke layout/paint kỹ thuật tại DPI120 (125%), kích thước logic1280×720 và
  1366×768, session đã revoke và DTO trong bộ nhớ: panel đơn431px (≈345 logical),
  grid nằm trong panel, header35px, dòng45/68/68px đúng36/54 logical, đủ4 cột,
  Price căn phải, hai nút header không chồng, editor mở trong panel và trả chiều
  cao dòng khi hủy. `DrawToBitmap` chạy qua painter không lỗi.
- Bằng chứng: [layout-smoke.json](runs/inline-order-lines-implementation-20261110/layout-smoke.json).

## Chưa chạy / rủi ro còn lại

- Chưa test chức năng SQL: thêm/gộp dòng, `−`/`+`, xóa, ghi chú, tạo đơn tại
  quán/mang đi/giao hàng, tổng và tiêu hao sau reload. Cần DB test riêng khi được
  duyệt đợt test.
- Chưa mô phỏng click thật/IME/focus ngoài smoke; chưa test100%/150%, hình thức do
  người dùng tự kiểm theo phân công.
- Rủi ro đã ghi trong plan: hit-area `−`/`+`, editor khi cuộn, gộp nhầm dòng khác
  ghi chú, bấm nhanh lúc busy, đơn giao vẫn cần F3.

## Bổ sung16/11/2026 — thao tác ghi chú trên dòng món

Thay hành vi “nháy đúp cột tên” ở mục Thay đổi trong phạm vi bằng yêu cầu người dùng
cùng ngày; phần còn lại của đợt triển khai10/11 giữ nguyên.

- `UI/OrbitDataGridView.cs`: theo dõi cả cột đang rê (`hoveredColumn`); rê vào cột
  `Name` của profile `OrderLines` đổi chữ sang Caramel, vẽ icon bút chì ở cuối ô và
  đặt con trỏ tay; rời lưới hoặc lưới bị disable thì trả `Cursors.Default`. Tooltip
  đổi thành “Bấm vào tên món để thêm ghi chú.”. Thêm `LineContentHeight` (46 khi
  trống, 64 khi có ghi chú, tính theo DPI) làm nguồn duy nhất cho phần nội dung;
  painter chỉ vẽ tên/số lượng/giá/thùng rác trong phần này nên phần dư bên dưới để
  trống cho editor.
- `Modules/Sales/OrderPanel.cs`: bỏ `CellDoubleClick`; click một lần vào `Name` gọi
  `BeginNoteEdit`. Dòng đang sửa cao thêm30 logical, cuộn tới dòng nếu ngoài vùng
  nhìn, editor đặt ngay dưới `LineContentHeight` của dòng (không đè lên tên/số
  lượng/giá); hủy/lưu trả chiều cao về46/64 theo ghi chú mới.
- Chưa chạy lại smoke hay test chức năng cho thay đổi này; chỉ compile
  `CafeManager.WinForms` Debug thành công0 warning/0 error (app đang mở giữ khóa
  file build của solution). Cần đóng app/Visual Studio trước khi build lại để chạy
  bản mới, và chờ duyệt đợt test riêng.
- Rủi ro thêm: editor có thể che dòng kế khi dòng cuối màn hình (đã chặn bằng kiểm
  biên và ẩn editor, chỉ cuộn khi dòng ngoài vùng nhìn); hit-area tên món rộng hơn
  nên dễ mở editor khi định chọn dòng — chờ người dùng kiểm thực tế.

Đợt test16/11 đã chạy (người dùng duyệt): lượt đầu37 PASS/1 FAIL (L2 — lưới thấp,
dòng cuối mở ghi chú thì editor ẩn nhưng dòng vẫn nở). Theo duyệt, đã sửa
`OrderPanel` (bỏ lề dọc2 khi đặt editor; không đặt được thì `CancelNoteEdit` trả
chiều cao) và regression41/41 PASS. Ghi nhận G04-POS-05 trong
[bug-report](bug-report.md). Biên bản:
[note-editor-inline-test-results.md](note-editor-inline-test-results.md), bằng chứng
[runs/note-editor-inline-20261116](runs/note-editor-inline-20261116/note-editor-smoke.json).

## Bổ sung16/11/2026 — bỏ icon, mở từ ghi chú, gợi ý ghi chú

Theo yêu cầu người dùng sau khi dùng thử (duyệt cùng ngày):

- `UI/OrbitDataGridView.cs`: bỏ vẽ icon bút chì khi rê chuột; chỉ đổi chữ sang Caramel
  và giữ con trỏ tay. Bấm vào tên hoặc dòng ghi chú (cùng ô `Name`) đều mở editor.
- `database/migrations/031_sales_note_suggestions.sql`: thêm
  `sp_SalesNoteSuggestions @Actor,@Stamp,@Search,@Take` trả tối đa50 ghi chú khác
  nhau từ `CHITIET_HD.GhiChu`, sắp theo số lần dùng; quyền ADMIN,CASH như các proc
  bán hàng. Không trả dòng/đơn, không ghi dữ liệu.
- `SalesRepository.NoteSuggestionsAsync` + `SalesService.NoteSuggestionsAsync` (đọc
  qua `Run`, kiểm quyền Sales như các truy vấn POS khác).
- `SalesControl`: tải lịch sử ghi chú trong `ReloadAll` (lỗi đọc bị bỏ qua để không
  chặn POS) và đưa ghi chú vừa lưu lên đầu danh sách trong phiên.
- `OrderPanel`: thêm `NoteSuggestionPanel` nổi dưới editor (trên editor nếu dưới
  thiếu chỗ), gộp mục cố định của quán với lịch sử, lọc khi gõ, tối đa6 dòng. ↑/↓
  chọn, Enter nhận và lưu, Esc đóng danh sách trước rồi lần sau mới hủy, click chuột
  nhận mục. Editor `KeyFilter` chặn phím trước handler Enter/Esc cũ.

Kiểm chứng trong pha triển khai (chưa phải đợt test được duyệt):

- Build `CafeManager.WinForms` Debug temp OutDir:0 error (chỉ cảnh báo MSB3277
  WindowsBase sẵn có).
- Migration031 áp vào DB test `CoffeeManager_Test_G04_20261111_draftflush` (đang ở
  schema30); gọi proc với actor NV001 trả5 ghi chú lịch sử, lọc `da` trả đúng3 mục.
- Smoke UI mở rộng thêm N1–N11: gợi ý hiện/lọc/ưu tiên mục cố định, nằm dưới editor
  và trong panel, có mục lịch sử, ↑/↓+Enter nhận và lưu, Esc đóng dropdown rồi hủy,
  click nửa dưới ô tên mở editor, mở editor không tự bung danh sách. Tổng52 PASS,
  0 FAIL tại DPI120.
- Đợt test16/11 (người dùng duyệt): 53/53 ca UI và6/6 ca SQL/DAL/BLL PASS tại
  DPI125%. Service đọc lịch sử đã chạy trên DB test riêng (actor CASH đọc đúng, STORE
  bị từ chối quyền). Biên bản:
  [note-suggestions-test-results.md](note-suggestions-test-results.md).
- Chưa chạy app đăng nhập thật end-to-end; chưa test100%/150%, IME gõ dấu, hình
  thức/hover do người dùng tự kiểm.
