# Triển khai Orbit UI trên WinForms .NET 10

17/10/2026 · Hướng dẫn triển khai, chưa phải code đã nghiệm thu trên Windows.

## 1. Hợp đồng thiết kế

Giữ MainForm + UserControl, C# / SQL Server và kiến trúc dự án. HTML là mẫu để nhìn/đo/tương tác, không yêu cầu chuyển ứng dụng sang web hoặc tự nhúng WebView2. Không giả định CSS opacity, transform, blur và shadow có đối tượng tương đương trên mọi WinForms control.

Đầu ra đúng phải có bố cục, màu, chữ và cảm giác gần mẫu, đồng thời vẫn có bàn phím, DPI, focus và nhập liệu ổn định. Sai khác do font renderer cần ghi lại, không lặng lẽ thay thiết kế.

## 2. Thành phần dùng chung

Bổ sung26/10/2026, đã làm rõ sau dùng thử: hộp nghiệp vụ dùng Form modal Windows
có titlebar/X native, không thêm taskbar riêng. `OrbitDialogHost` là bộ quản lý
owner/stack/result chung, không còn panel/container. Caller await ShowAsync;
chỉ bộ quản lý gọi ShowDialog trên UI thread, Windows khóa owner. Dialog đầu
thuộc MainForm, dialog lồng thuộc hộp cha. Guard CanCloseAsync giữ kết quả đóng
khi WinForms hủy close để hỏi bỏ nháp. Không DoEvents/polling hoặc UI thread mới.
`OrbitConfirmationDialog.ShowAsync` dùng cùng đường này. Native PrintDialog nhận
HWND của hộp đang hoạt động; StarDustOverlay là tool window hiệu ứng không nhận
input/taskbar, không là hộp nghiệp vụ. Không dùng bitmap scaling để thay DPI.

Hành vi nhập chung ở `OrbitField.ApplyInputBehavior`/`SelectAllOnEntry`: chọn hết
khi mới vào bằng click/Tab, không chọn lại mỗi TextChanged hoặc mỗi click trong
cùng lần focus. Gắn idempotent, theo dõi control động/editor grid; bỏ qua readonly
và ComboBox chỉ chọn. AppTheme, field theme và host/MainForm dùng cùng helper.
Kiểm hồi quy theo hồ sơ `docs/tasks/embedded-dialogs/`; build/smoke không chứng
nhận nghiệm thu các màn đã chuyển.

| Thành phần đề xuất | Trách nhiệm |
|---|---|
| AppTheme / ThemeTokens | Parse tokens một lần hoặc ánh xạ hằng C# có nguồn; cấm magic number màu/motion rải rác. |
| OrbitButton | Normal/hover/pressed/focus/disabled/busy; vẽ bóng, sheen, ripple; một đường kích hoạt chung. |
| ProductCardControl | Vẽ ảnh, tên/mô tả/footer giá và dấu cộng cố định tâm; xử lý AddRequested một lần. |
| OrbitTextField | Container có nhãn/viền/focus; TextBox native cho caret, IME, clipboard và tiếng Việt. |
| OrbitBadge | Nhãn trạng thái; màu không thay chữ. |
| OrderPanel | Dòng món, bước tăng giảm, tổng SQL; footer thanh toán tiếp cận được. |
| OrbitDialog | Modal xác nhận/editor; focus vào nội dung phù hợp, trả focus khi đóng. |
| MotionCoordinator | Stopwatch + timer UI chung; quản lý trạng thái hover/press/reveal, không timer mỗi hạt. |
| StarDustService | Opt-in từng button; phát hạt ở overlay cấp form, không gắn toàn bộ nút. |

Giữ DataGridView/ComboBox native ở nghiệp vụ cần nhập liệu; custom paint phần header/selection/border hợp lý. Không custom vẽ lại toàn bộ accessibility của các control phức tạp chỉ để giống web.

### 2.1. Trang danh sách — control và API đã có 26/10/2026

Hành vi/bố cục bắt buộc xem [guide mục11](HuongDan_UI_Orbit_Cafe.md#11-chuẩn-trang-danh-sách-dùng-chung--26102026).
Các thành phần dưới đây **đã tồn tại trong source**, khác bảng đề xuất ban đầu
ở trên. Đường dẫn tính từ `src/CafeManager.WinForms/`. Đọc implementation hiện
hành trước dùng; không tạo lại chỉ vì đang code một module mới.

| Thành phần/API | File | Cách tận dụng |
|---|---|---|
| AppTheme |`Theme/AppTheme.cs` |Theme nền/control native và hành vi input chung; không rải mã màu/font riêng. |
| OrbitManagementTheme |`Theme/OrbitManagementTheme.cs` |Màu Background/Surface/Input/Text/Muted/Caramel/Lilac, font, Px/DrawingFont; Button(style,icon), DialogInput/DialogGrid. Các style nút Icon/Action/Primary/Secondary/Danger/Pager dùng đúng vai trò. |
| OrbitListWorkspace |`UI/OrbitListWorkspace.cs` |Header, card, SearchActions, Filters, PrimaryActions/RowActions, Body, Pagination, Status; Dock.Fill. StackedTools=true xếp search hết hàng + filter hàng dưới, wrap và tính phần Body còn lại. Không tự bọc thêm container cao50% hoặc fixedHeight chặn workspace. |
| OrbitRecordListControl |`UI/OrbitRecordListControl.cs` |Base của danh sách: Configure, AddFilter, AddAction, query async, cancellation/generation, loading/actionstate. UseStackedSearch(noun,emptyDescription) bật layout mới và tách draft/applied keyword, gắn kính lúp và chuyển nút refresh có sẵn. Không đăng ký thêm handler tìm/refresh trùng. |
| OrbitSearchBox |`UI/OrbitSearchBox.cs` |TextBox native và style tìm kiếm dùng chung. Qua base control dùng Search; đặt placeholder/AccessibleName đúng nghiệp vụ. |
| OrbitFilterBox |`UI/OrbitFilterBox.cs` |Bọc ComboBox chỉ chọn; LogicalWidth và Plain cho filter phẳng theo mẫu. AddFilter(combo,width,plain:true) trong base tự bọc. Không tự vẽ dropdown khác cho từng màn. |
| OrbitDateField/OrbitDateRangeField |`UI/OrbitDateField.cs`, `UI/OrbitDateRangeField.cs` |DateField giữ typed Value/parser dd/MM/yyyy/calendar native; DateRangeField ghép hai instance hiện có, tên Từ/Đến, layout chung. Không nhân đôi parser hoặc mất handler/giá trị khi ghép. |
| OrbitDataGridView/OrbitGridProfile |`UI/OrbitDataGridView.cs` |Style/selection/paint badge và profile theo loại bảng. InvoiceHistory dành cho hóa đơn; Delivery dành cho giao hàng: Code freeze/đo mã, Phone chuỗi, Address fill, badge Delivery và PaymentStatus riêng; tooltip tên/địa chỉ đầy đủ. Chọn profile đúng module, không copy paint handler riêng. |
| OrbitPagination |`UI/OrbitPagination.cs` |SetPage(page,50,total,noun) khi có tổng thật; SetServerPage khi contract chưa có tổng; SetPending cho loading/lỗi. Chỉ số trang đo theo text, không fixed width cắt nhiều chữ số. |
| TotalCountSelector |`UI/OrbitRecordListControl.cs` |Hook opt-in lấy tổng typed từ DTO; cùng điều khiển pager và enable action. Không hardcode tổng hoặc sửa query SQL khi chưa được giao contract. |
| OrbitEmptyState |`UI/OrbitEmptyState.cs` |Trạng thái rỗng; TitleText/HistoryPrompt theo nhu cầu, thông điệp đúng module. Không dùng empty để che lỗi đọc. |
| OrbitButton |`UI/OrbitButton.cs` |Nút có style/icon từ management theme; reuse icon search/refresh/printer/left/right. Icon action cần tên truy cập/tooltip; không thêm bụi sao mặc định. |
| OrbitField |`UI/OrbitField.cs` |ApplyInputBehavior/SelectAllOnEntry dùng chung, idempotent cho input động/grid editor; không chép handler SelectAll mỗi màn. |
| OrbitDialogHost/OrbitDialogForm |`UI/OrbitDialogHost.cs`, `Forms/OrbitDialogForm.cs` |ShowAsync có owner/stack/result, native modal/title/X/no taskbar; không tự tạo cửa sổ nghiệp vụ độc lập hoặc host panel giả. |
| OrbitSurface |`UI/OrbitSurface.cs` |Khung card bo góc nhỏ dùng chung theo theme/DPI, tô nền ngoài theo parent; caller sở hữu layout/control và đặt children trong viền. Không là cửa sổ/dialog hoặc framework điều hướng. |
| OrbitBadge |`UI/OrbitBadge.cs` |Nhãn trạng thái có chữ, viền/nền/text theo palette; caller gán Text/ForeColor, không nhúng nghiệp vụ thanh toán vào badge. |

POS gọn30/10, cập nhật10/11 dùng SalesControl + ProductCardControl/OrderPanel hiện
có. Các helper mới ở trên được dùng cho menu/bàn/card món/panel đơn; không sao chép
theme riêng. OrbitGridProfile.OrderLines opt-in: readonly, cột `Name` fill,
`Quantity`, `Price`, `Delete`; bỏ `Note`/`Amount` khỏi hiển thị. Ghi chú vẽ chữ nhỏ
dưới tên, dòng46 logical khi trống và64 khi có ghi chú; `Quantity` vẽ `− n +`,
`Delete` vẽ icon thùng rác, click do OrderPanel xử lý. `Price` đo theo giá snapshot,
căn phải; header/cell cùng padding, header OrderLines28 logical. Editor ghi chú là
control con của OrderPanel (TextBox + nút lưu/hủy), không mở dialog, không tính tiền
trong painter.

Cập nhật16/11: mở editor bằng **một** click vào tên món hoặc dòng ghi chú bên dưới
(cùng ô `Name`). Khi rê chuột vào cột `Name`, chữ đổi màu Caramel và con trỏ thành tay;
không vẽ icon bút chì; tooltip nhắc “Bấm vào tên món để thêm ghi chú.” Dòng đang sửa
được nới thêm30 logical và editor đặt ngay dưới phần nội dung của dòng, không đè lên
tên/số lượng/giá; painter chỉ vẽ nội dung trong chiều cao46/64 logical nên phần dư dành
cho editor. `LineContentHeight` là nguồn duy nhất cho chiều cao nội dung dùng chung giữa
painter và OrderPanel. Không đủ chỗ dưới nội dung (lưới thấp, dòng cuối): editor đóng và
dòng trả lại chiều cao, không để khoảng trống không có ô nhập.

Gợi ý ghi chú (16/11): `NoteSuggestionPanel` là control con nổi của OrderPanel, đặt ngay
dưới editor (trên editor nếu dưới thiếu chỗ, luôn nằm trong panel). Danh sách gộp mục
cố định của quán với ghi chú đã dùng đọc qua `sp_SalesNoteSuggestions`; gõ tới đâu lọc
tới đó, ưu tiên mục bắt đầu bằng từ khóa rồi mới tới mục chứa từ khóa, tối đa6 dòng.
↑/↓ đổi mục chọn, Enter nhận mục rồi lưu, Esc đóng danh sách trước và lần Esc sau mới
hủy editor; click chuột vào mục cũng nhận và lưu. Mở editor không tự bung danh sách.
Lịch sử chỉ là phụ trợ: lỗi đọc không chặn POS, và ghi chú vừa lưu được đưa lên đầu
danh sách ngay trong phiên.
OrderPanel nhận DTO, chỉ Payable ở POS; đầy đủ breakdown tại CheckoutDialog.
Nháp/preview chưa hợp lệ/unknown hiển thị — và nhãn đúng, không tự gán0₫.
Kích thước order330–345logical, menu nhận phần dư; root/modal luồng cũ giữ nguyên.

`Modules/Sales/InvoicesControl.cs` là ví dụ ghép layout V4 đã triển khai:
Configure → profile cột/tổng thật → AddFilter → UseStackedSearch → AddAction.
`Modules/Sales/DeliveryControl.cs` đã dùng cùng chuỗi với profile Delivery và
DeliveryRow.TotalCount. Hook IsActionAvailable(command) ở base mặc định true;
module có thể khóa action theo writing/unknown, rồi gọi UpdateActions khi trạng
thái thay đổi. Reconcile không requires-row vì dùng pendingCode/requestId cũ,
không dùng dòng hiện chọn. Các helper mới đã compile/smoke kỹ thuật, không tự
đồng nghĩa các màn đã PASS chức năng hoặc hình thức.
Trong lớp con của OrbitRecordListControl:

```csharp
// Sau Configure và cấu hình cột/filters theo contract của module.
UseStackedSearch("hóa đơn", "Thử thay đổi khoảng ngày hoặc điều kiện tìm kiếm.");
TotalCountSelector = rows => rows.Count == 0 ? 0 : ((SalesOrderRow)rows[0]!).TotalCount;
// Nối SelectedValueChanged/ValueChanged của filter vào ResetAndReload.
// Base dùng appliedSearch; không đọc Search.Text trong callback query/filter.
```

Đoạn trên chỉ minh họa API trong lớp con với DTO hóa đơn đã có, không là template
ép DTO/filters/permission của mọi module thành hóa đơn. Base hiện bật chế độ mới
theo opt-in, chưa tự chuyển mọi màn cũ; màn code/chỉnh tiếp phải chủ động dùng nó
theo chuẩn được chốt. ShowTools/ShowMeta/Compact chỉ đặt khi đúng use case.

**Chuỗi bố cục phải giãn đầy đủ:** MainForm content → UserControl module → tab
container (nếu có) → OrbitRecordListControl/workspace → card → Body → grid.
Grid Dock.Fill trong Body không đủ nếu một ancestor còn fixedHeight hoặc chia
50/50. Kiểm lần lượt ancestor, Dock/Anchor, RowStyle Percent100 cho hàng fill
và thứ tự add/z-order; toolbar/footer có hàng đo riêng, vùng bảng nhận phần dư.
Không dùng AutoSize theo số dòng cho vùng bảng, không cho toàn workspace cuộn
làm pager biến mất. Khi thiếu ngang, filter wrap và grid cuộn ngang; không giảm
font/cắt mã/đẩy footer ra ngoài để giả vừa.

Không thay màu/token bằng giá trị của HTML demo; tiếp tục dùng tokens/theme
chung. Bounds do WinForms scale không nhân DPI lần hai. Agent chỉ kiểm layout
kỹ thuật/chức năng tại125%, hình thức người dùng kiểm; quy định này ưu tiên
các đoạn workflow chụp ảnh/DPI chung lịch sử phía dưới.

### 2.2. Sửa/xóa trực tiếp trên dòng — 22/11/2026

Hành vi bắt buộc xem [guide mục11.5](HuongDan_UI_Orbit_Cafe.md#115-sửaxóa-trực-tiếp-trên-dòng--22112026).
Không dùng chế độ edit native của `DataGridView`: DTO là positional record immutable
và lưới ReadOnly tự paint, nên sửa ô bằng **editor overlay** đặt đúng ô, ghi qua
service rồi tải lại và chọn lại dòng theo mã. UI không chứa SQL/nghiệp vụ.

| Thành phần/API | File | Cách tận dụng |
|---|---|---|
| OrbitCellEditor |`UI/OrbitCellEditor.cs` |Panel overlay một ô cho TextBox/ComboBox/CheckBox theo theme; viền Lilac, kích thước qua `OrbitManagementTheme.Px`; Enter/Tab/Esc và select-all qua `OrbitField`; không nhận click xuyên xuống lưới. |
| OrbitRowEditor |`UI/OrbitRowEditor.cs` |Engine dùng chung nối lưới + ô editor: mô tả cột bằng `InlineColumn(Property, Kind, Read, Convert, Options, CanEdit)`, màn cấp delegate Save/Delete/Reconcile/Reload/RowKey/Status. Quản lý một editor mở, commit khi click ô khác/Enter/Tab/blur có guard gửi lặp, Esc hủy. |
| OrbitDataGridView.InlineEditable/InlineCursor |`UI/OrbitDataGridView.cs` |Hook opt-in theo ô: hover đổi màu chữ/viền badge sang Caramel và đổi con trỏ; mặc định null giữ hành vi cũ cho mọi màn chưa chuyển. Không tự sửa nền dòng đang chọn. |
| OrbitDataGridView.RowBlocked |`UI/OrbitDataGridView.cs` |Hook theo chỉ số dòng để vẽ veil + vạch trái cho dòng khóa kết quả chưa xác định. |
| AddRowDeleteColumn |`UI/OrbitRecordListControl.cs` |Cột `Delete` 44 logical px, icon thùng rác; click xóa đúng dòng, không cần chọn trước. `RowDeleteRequested`/`RowActivated` cũ không dùng cho các màn đã chuyển. |
| OrbitRecordListControl.RowWriteBusy/IsBusy |`UI/OrbitRecordListControl.cs` |Trạng thái ghi inline cho base: chặn `UpdateActions`, dừng reload chồng, làm cơ sở cho `StateChanged` của engine và `IsWriting` của màn Designer. |

- Màn Designer (`CatalogsControl`, `MaterialsControl`) dựng engine trực tiếp trên `Grid`
  và `Workspace.Body`; màn kế thừa base dùng helper `CreateInlineEditor`. Không chép
  handler click/hover riêng từng màn.
- Lỗi thường giữ editor + giá trị, báo ở status. Chỉ "kết quả chưa xác định" mới khóa
  dòng và đặt nút **Đối chiếu** căn giữa dòng; đối chiếu qua `ReconcileAsync`/`ReceiptAsync`
  sẵn có. Danh mục chưa có `@RequestId` cho Update/Delete nên đối chiếu bằng `GetAsync`
  + so `Revision`/giá trị hoặc còn tồn tại — khoảng trống đã ghi nhận, không thêm migration.
- Xóa theo ma trận xác nhận ở guide11.5; xóa ngay chỉ báo status, không toast/undo. Quyền
  vẫn do service quyết định; ẩn nút không thay kiểm tra quyền.
- Không thêm bảng sửa/`OperationEditorDialog` cho trường đơn giản đã inline; dialog thêm
  mới và dialog chuyên biệt (công thức, tài sản Thêm/Yêu cầu chờ/Chi tiết) giữ nguyên.

## 3. DPI, font và bố cục

- Chốt cấu hình DPI ở đầu ứng dụng theo cấu trúc project .NET 10 đang có; ưu tiên cấu hình thống nhất PerMonitorV2 khi đã kiểm chứng. Không trộn cấu hình .NET Framework/app.config cũ vào project hiện đại.
- Đồng bộ AutoScaleMode giữa các form/container; không đổi tùy tiện giữa Font và Dpi trong cùng cây control.
- JSON dùng logical px tại 96 DPI; device px = logical × DeviceDpi / 96 cho custom painting. Bounds do WinForms autoscale thì không nhân lần nữa.
- Font JSON là CSS px từ reference; pt tương đương = px × 72/96. Ví dụ 14px = 10,5pt; kiểm tra PreferredSize vì renderer WinForms khác browser. Không lấy 14px làm 14pt.
- Dock/Anchor/TableLayoutPanel cho shell; menu sản phẩm co giãn 2–3 cột theo bề rộng dùng được, không chỉ theo độ phân giải vật lý.
- Tên món tối đa vùng hai dòng; mô tả vùng hai dòng; footer giá/cộng cùng baseline. Tên dài không đẩy footer lệch; tooltip/editor cung cấp nội dung đầy đủ.
- Ô dấu cộng 32×32 ở reference, hai nét 12×2px và 2×12px vẽ từ cùng tâm, stroke theo DPI. Không rotate glyph '+' và không nested button bên trong Button.
- Hình có tay cầm phải căn giữa theo toàn silhouette, không chỉ theo thân ly.
- Test 1280×720, 1366×768, DPI100/125/150%; bàn phím, tên tiếng Việt dài và số tiền lớn. DPI cao có thể cần giảm số cột/panel bàn riêng, không giảm font đến khó đọc.

## 4. Vẽ hover/press và chuyển động

1. Paint nền, border, bóng trong/ngoài, ảnh/text rồi focus ring; không vẽ glow sáng đè chữ.
2. Hover product: nâng phần render khoảng 3px, viền/glow tím, spotlight theo con trỏ. Không đổi Bounds của cả grid mỗi MouseMove.
3. Press: cập nhật visual ngay ở MouseDown hoặc kích hoạt bàn phím; 55ms lún, 150ms trả về. MouseUp ngoài nút, capture lost, focus loss phải reset visual, không giữ trạng thái lún.
4. Kích hoạt nghiệp vụ qua duy nhất một đường; MouseDown chỉ vẽ, không vừa gọi nghiệp vụ ở MouseDown vừa gọi ở Click.
5. Timer UI khoảng 16ms chỉ là mục tiêu tick; lấy tiến độ từ Stopwatch, không đếm frame. Không hứa đạt 60fps khi chưa đo Windows thật.
6. Một coordinator invalidate vùng thay đổi; chỉ chạy timer khi có animation. Không Thread.Sleep/DoEvents/vòng lặp repaint chặn UI.
7. Cache ảnh bóng/glow có biên để tránh cắt; custom control có AllPaintingInWmPaint/OptimizedDoubleBuffer/UserPaint phù hợp. Double buffer không tự làm native child HWND trong suốt.

Dọn Pen/Brush/Font/GraphicsPath/Bitmap/Timer do mình sở hữu; không dispose e.Graphics hoặc shared resource chưa hết dùng. DPI đổi phải rebuild raster cache, không scale bitmap nhỏ nhiều lần.

## 5. Bụi sao — opt-in và overlay

Ví dụ cấu hình, không phải code hoàn chỉnh:

```csharp
confirmPaymentButton.EnableStarDust = true;
openPaymentButton.EnableStarDust = false;
addProductButton.EnableStarDust = false;
```

Mặc định false. Không suy ra từ Primary/Danger/Confirm. Nút mới chỉ có nếu người dùng chỉ định.

- 40 hạt, radius75–215 logical px, sao10–19px/hạt3–7px, lifetime850–1250ms. Caramel/lilac/cream theo JSON.
- Tính origin = form.PointToClient(button.PointToScreen(localClick)); keyboard dùng tâm nút. Không dùng screen coordinates trực tiếp để vẽ vào client.
- Overlay phải nằm trên các sibling và vùng cuộn, không chỉ trong clip của Button; pointer/click phải đi xuyên. WinForms child Panel transparent không tự bảo đảm vừa thấy native HWND dưới vừa vẽ phủ mọi thứ. Cần prototype phương án overlay cấp form; nếu cần cửa sổ overlay layered owned form, kiểm tra z-order, không activate, hit-test xuyên, reposition/resize/DPI/minimize và dispose.
- Không tạo 40 Label/Control. Vẽ tất cả hạt trên một surface bằng một timer; ngân sách tối đa280 hạt. Xóa hạt cũ theo lifetime; không để overlay tồn tại khi đóng/chuyển form.
- Cho các action hợp lệ đi qua, không che thông báo lỗi. Disabled/busy/validation fail không phát. Khóa gửi lặp trước khi gọi BLL.
- Bụi sao là phản hồi nhấn, không bằng chứng SQL đã commit. In lỗi sau commit không thu tiền lại. Timeout phải đối chiếu mã đơn, không retry mù.

## 6. Cuộn hai cấp

Reference có cả menu cuộn và trang dài phía dưới. Cuộn menu: reveal opacity0,4→1 + dịch16px/360ms, stagger45ms. Cuộn trang: opacity0,3→1 + dịch22px/520ms, stagger65ms; thanh tiến độ thật và nền dịch nhẹ. Không smooth-scroll hijack bánh xe.

WinForms không có opacity tùy ý cho Panel chứa nhiều child HWND. Reveal text/card nên render trên custom surface hoặc snapshot ngắn; không thay đổi Opacity toàn Form mỗi lần cuộn. Với danh sách lớn, chỉ animate các card mới vào viewport, giới hạn số đang chạy; virtualize hoặc tái sử dụng item nếu số liệu đòi hỏi. Không làm nội dung vô hình nếu animation bị lỗi.

Trang component ở cuối reference phục vụ đối chiếu, không yêu cầu thêm trang marketing vào app. Với nghiệp vụ, áp dụng motion vào vùng nội dung dài thật; giữ footer thanh toán, focus và vị trí cuộn.

## 7. Trình tự làm việc với agent

1. Đọc README, guide, tokens; chạy reference offline, xem ảnh/video.
2. Kiểm tra source/AGENTS của dự án; không phá Designer, quyền hoặc scope task đang duyệt.
3. Theme → button/card → shell/POS → xác nhận thanh toán → overlay bụi sao → cuộn.
4. Chạy Windows, chụp cùng viewport và trạng thái. So sánh card alignment, text contrast, padding/size, focus, glow, press/release, full-page scroll và clip hạt.
5. Sửa visual trong scope được giao, ghi rõ sai khác còn lại. Không áp dụng hàng loạt sang module khác trước khi POS đạt đối chiếu.
6. Không tick nghiệm thu Txx/UX-Kxx chỉ từ screenshot hoặc build thành công. Giữ quy trình duyệt/test/commit của dự án.

## 8. Tiêu chí kiểm tra

| Ca | Kết quả cần đạt |
|---|---|
| Hover/leave nhanh | Không rung layout, không kẹt glow/pressed. |
| Nhấn/giữ/thả ngoài, keyboard | Visual reset, action không chạy hai lần. |
| Double click xác nhận | Một lệnh nghiệp vụ, disabled chặn burst tiếp theo. |
| Các nút thường | Không bụi sao; hover/press vẫn có. |
| 40 hạt sát mép/vùng cuộn | Bay ngoài nút, không clip nội bộ, không nhận chuột. |
| Cuộn dài/filter/đổi màn | Không mất focus, không nhảy về đầu ngoài chủ đích; query muộn không ghi đè. |
| 100 lần đổi module | Không tăng liên tục GDI handles/timer/ảnh; theo dõi số liệu thực. |
| Tên dài/tiền lớn/DPI150% | Không cắt tiếng Việt, giá và CTA; card cùng baseline. |
| SQL chậm/lỗi/timeout/in lỗi | Trạng thái đúng contract, không success giả hoặc checkout lại. |

Nguồn kỹ thuật chính thức (tham khảo ngày17/10/2026):
- https://learn.microsoft.com/en-us/dotnet/desktop/winforms/forms/autoscale
- https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-reduce-graphics-flicker-with-double-buffering-for-forms-and-controls
- https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.timer
- https://motion.dev/docs/scroll (tham chiếu hành vi web; không là dependency WinForms)
