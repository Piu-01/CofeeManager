# Đợt bug 15/11/2026 — ô tìm kiếm có viền vuông, bảng dữ liệu chừa khoảng trống

Người dùng báo kèm ba ảnh chụp app đang chạy (Thu chi, Công nợ nhà cung cấp,
Nguyên liệu & tồn kho, 125% DPI). Chưa sửa source; chờ duyệt đợt bug.
Hai lỗi dưới đây thuộc component dùng chung của Orbit nên ghi ở task này; màn
ảnh hưởng thuộc T19/T23 (G05) và T09 (G03), T06 (G06) — sẽ liên kết khi lập
báo cáo nhóm.

## Cập nhật 15/11/2026 — đã sửa theo duyệt, chờ duyệt đợt test

Người dùng duyệt cả hai. Đã sửa đúng phạm vi báo cáo, không đổi backend/SQL/contract:

- B05: `UI/OrbitTabControl.cs` `EnsureSelectedPage` bỏ `AppTheme.Apply(control)`,
  chỉ còn `control.Dock = DockStyle.Fill` trong Suspend/ResumeLayout. Trang lazy
  vẫn dùng control Orbit tự theme; hết ghi đè `BorderStyle/BackColor` của input.
- B06: `UI/OrbitDataGridView.cs` thêm profile `Finance`, `SupplierDebts`; nhánh
  `ApplyLedgerColumn` đo header + nội dung đã format theo font thật rồi đặt
  `Fill` với `MinimumWidth`/`FillWeight` theo số đo (không đặt `Width` trên cột
  Fill). `Modules/Finance/FinanceControl.cs` và `SupplierDebtsControl.cs` khai
  báo profile tương ứng; khai báo `AllCells` ở màn bị thay bằng cơ chế này.

Build Debug và Release PASS 0 warning 0 error. Đợt test kỹ thuật 15/11 đã chạy
([biên bản](runs/ui-b05-b06-20261115/test-results.md)): 125%/DeviceDpi120, hai
trang lazy không còn ô viền vuông, Thu chi 6/6 và Công nợ 8/8 cột lấp hết bề
rộng và 0 cột cắt chữ tại 1366/1400; unit 52/52 PASS. Đây **không** phải PASS
giao diện; hình thức vẫn do người dùng tự nghiệm thu. DPI 100%/150% và
Designer NOT RUN.

## ORBIT-UI-B07 — mật độ bảng dữ liệu nhỏ, chữ trong bảng nhỏ (yêu cầu 15/11/2026)

**Trạng thái:** đã sửa 15/11/2026 theo chuẩn màn Giao hàng; test kỹ thuật PASS
(125%: 16/16 profile dòng 46/header 40, 0 cột cắt chữ); chờ người dùng nghiệm
thu hình thức.
Người dùng chốt: tăng kích thước ô cho toàn bộ bảng Orbit và lấy bảng màn Giao
hàng làm chuẩn (dòng 46, header 40, chữ 9pt) — không tăng cỡ chữ thêm.

**Hiện tượng:** Ảnh Tài sản (125%) và Danh mục nền (100%) cho thấy dòng thấp
hơn hẳn màn Giao hàng/Thu chi; cùng một app mà độ đặc của bảng khác nhau vì mỗi
profile một chiều cao.

**Nguyên nhân (source trước sửa):** `UI/OrbitDataGridView.cs:72-76` —
`RowTemplate.Height` theo profile: Areas 28, Catalogs/Ranks 33, Customers 34,
Promotions/Points/OrderLines 36, còn lại 46; header 40 (OrderLines 28). Màn Tài
sản và Danh mục nền rơi vào profile `Catalogs` vì
`OrbitRecordListControl.Configure` gán `_ => OrbitGridProfile.Catalogs`.

**Đã sửa:**

- `OrbitDataGridView.ApplyMetrics`: `RowTemplate.Height` cố định 46 logical và
  `ColumnHeadersHeight` 40 logical cho mọi profile (bỏ nhánh compact và nhánh
  header 28 của OrderLines); header luôn `DisableResizing`.
- Dòng OrderLines: 46 logical, dòng có ghi chú 64 (trước 36/54);
  `OrderPanel.CancelNoteEdit` đổi theo 46/64 để không lệch khi thoát sửa ghi chú.
- Không đổi font ô/header, cột, binding, sự kiện, backend/SQL.

**Phạm vi:** mọi `OrbitDataGridView` (danh mục, tài sản, kho, nhân sự, POS, bảng
lịch sử trong dialog). Không đổi control ngoài bảng.

**Rủi ro/regression:** dòng cao hơn giảm số dòng thấy ở cửa sổ 720px; POS
OrderLines/CheckoutDialog tính chiều cao theo `RowTemplate.Height` nên dialog cao
hơn, phải kiểm không tràn; Payment giữ nguyên 46/40 nên không đổi. Kiểm tại 125%:
tất cả màn có bảng, chọn dòng, nháy đúp, sửa ghi chú POS, Payment, phân trang;
100/150% và Designer giữ NOT RUN theo quy định.

## ORBIT-UI-B05 — ô tìm kiếm hiện thêm khung vuông bên trong (tab Tồn kho/Nhân sự)

**Trạng thái:** đã sửa 15/11/2026; test kỹ thuật PASS (125%, 0 ô viền vuông);
chờ người dùng nghiệm thu hình thức.

**Hiện tượng:** Ở màn Nguyên liệu & tồn kho, bên trong khung tìm kiếm tròn còn
một hình chữ nhật góc vuông viền xám, nền sáng hơn khung (đúng như ảnh người
dùng gửi). Cùng control này ở Thu chi/Công nợ không bị. Ô vuông xuất hiện cả
khi ô chưa focus, không phải hiệu ứng focus.

**Nguyên nhân:** `UI/OrbitTabControl.cs:46-51` — `EnsureSelectedPage` gọi
`AppTheme.Apply(control)` cho module được tạo lazy. `Theme/AppTheme.cs:44-49`
đệ quy toàn cây và với mọi `TextBox` đặt `BackColor = AppTheme.Surface`
(#211B28) và `BorderStyle = FixedSingle`, ghi đè `OrbitSearchBox.Input`
(`UI/OrbitSearchBox.cs:7` đặt `BorderStyle.None`, `BackColor = Input` #17131D)
và phần vẽ khung tròn trong `OnPaint`. Vì vậy lỗi chỉ có ở các trang dựng qua
`AddLazyPage` (4 tab Kho, 4 tab Nhân sự), không có ở Thu chi/Công nợ (control
gắn trực tiếp, không đi qua `AppTheme.Apply`).

**Bằng chứng:** dựng harness ngoài repo tham chiếu chính project WinForms, chứa
`OrbitTabControl` + `MaterialsControl()` thật, chụp cửa sổ: đo được
`border=FixedSingle back=(33,27,40)` trên TextBox tìm kiếm và ảnh cho ra đúng
khung vuông như ảnh người dùng. Khi dựng `MaterialsControl` trực tiếp (không
qua tab) thì `border=None back=(23,19,29)` và không có khung. Đây là bước tìm
nguyên nhân, không phải nghiệm thu hình thức.

**Hướng sửa đề xuất:** bỏ lời gọi `AppTheme.Apply(control)` cho nội dung lazy
(page Orbit tự định theme; chrome tab đã được `AppTheme.Apply(tabs)` cấu hình
trước khi có handle), hoặc thay bằng bước chỉ gán `Dock=Fill`. Không nên để
`OrbitSearchBox` tự "sửa lại" style sau theme vì sẽ che lỗi gốc và các control
Orbit khác vẫn bị ghi đè.

**Phạm vi ảnh hưởng:** 4 tab Kho (Tồn kho/Nhập hàng/Xuất kho/Kiểm kê) và 4 tab
Nhân sự đang nhận nhầm palette cũ: lưới đổi màu header/nền, panel đổi nền,
TextBox/ComboBox đổi viền. Cần kiểm lại các màn này sau khi sửa.

**Regression/rủi ro:** thứ tự theme từng được thêm để nội dung tab "được theme
một lần" (theo ui-tabs-regression-20261110); nếu gỡ phải soát 8 tab, dialog mở
từ các tab và trạng thái loading/rỗng/lỗi. Rủi ro chính là màn nào còn dựa vào
palette AppTheme sẽ mất màu — cần rà trong cùng đợt duyệt.

## ORBIT-UI-B06 — bảng Thu chi/Công nợ chừa khoảng trống bên phải và cắt nội dung cột

**Trạng thái:** đã sửa 15/11/2026; test kỹ thuật PASS (125%: cột `Fill` lấp hết
bề rộng, 0 cột cắt chữ tại 1366/1400); chờ người dùng nghiệm thu hình thức.

**Hiện tượng:** Lưới Thu chi có dải rỗng lớn bên phải tiêu đề GHI CHÚ (vùng nền
bảng tiếp tục tới mép card). Đồng thời nội dung cột bị cắt: "TC2…",
"12/11/2026 0…", "Mua nguyên li…", "Mua da, trái c…". Công nợ nhà cung cấp
tương tự: cột TRẠNG THÁI kết thúc giữa card, bên phải là khoảng trống, cột MÃ
NỢ hiển thị "CN2…". Màn Nguyên liệu (cùng component lưới) thì lấp đầy nên nhìn
không thấy.

**Nguyên nhân:** hai lớp ghi đè nhau.
1. `Modules/Finance/FinanceControl.cs:31` và
   `Modules/Finance/SupplierDebtsControl.cs:16` khai báo mọi cột
   `AutoSizeMode = AllCells`.
2. Nhưng `UI/OrbitDataGridView.cs:239` gọi `ApplyMetrics()` ở
   `DataBindingComplete`; với profile mặc định (`Inventory`), dòng 138-142 đặt
   lại `AutoSizeMode = Fill` chỉ cho cột có property trùng tên "Name", các cột
   còn lại về `None` và `Width = ColumnWidth(property)` (76/64/116 logical).
   Thu chi/Công nợ không có cột "Name" (Date/Type/Category/Amount/Note và
   Supplier/PurchaseCode/Original/Paid/Remaining/Due/Status) nên **không cột
   nào giãn** và mọi cột bị ép 116-120 logical (~145-150 px ở 125%), hẹp hơn
   nội dung thật.

**Bằng chứng:** ảnh người dùng; đo ảnh Thu chi thấy dải tiêu đề lưới chỉ tới
x≈910 trong khi vùng lưới/card rộng tới ≈1370, phần còn lại là nền Surface;
ảnh Công nợ tương tự với ~250 px trống. Trong code, `ColumnWidth` (dòng 227-238)
không có nhánh nào cho các property của hai màn này.

**Hướng sửa đề xuất:** cho lưới tài chính/công nợ bộ đo cột riêng (như
`ApplyInvoiceColumn`): đo độ rộng theo nội dung thật cho Mã, Ngày, Phiếu nhập,
Hạn, Trạng thái; căn phải cho Nợ gốc/Đã trả/Còn nợ/Số tiền; chọn một cột dài
(Ghi chú ở Thu chi, NCC ở Công nợ) làm `Fill` để lấp phần còn lại, thay vì để
cột cuối rơi vào khoảng trống. Có thể thêm `OrbitGridProfile.Finance/SupplierDebts`
để `ApplyMetrics` không ghi đè cấu hình cột của hai màn.

**Regression/rủi ro:** ảnh hưởng toàn bộ lưới dùng chung nếu sửa trong
`ApplyMetrics`; phải kiểm lại các màn đang dựa vào bề rộng mặc định (Nguyên
liệu, Nhập hàng, Xuất kho, Kiểm kê, Danh mục, Sản phẩm, Khách hàng, Bàn, Ưu
đãi/Điểm). Giữ nguyên format/căn phải tiền, không đổi query/binding, không đổi
cột hiển thị. Rủi ro thấp nếu chỉ thêm profile mới opt-in cho hai màn tài chính.

## Kết quả native cuối — 21/10/2026

B03: PASS cho hai nhãn còn lại tại125% trên app thật PID29492, DB test CoffeeManager_Test_G03_20261019_215457_29e552a9_demo. Mở sổ hiện đầy đủ, Sơ đồ hiện đầy đủ; toolbar không wrap/chồng footer sau tăng chiều rộng. Evidence runs/bugfix-b01-b04/native-label-final-materials-0.png và native-label-final-tables-0.png. Tab đầu Danh mục nền tự tải10dòng, không bấm Tải lại: native-label-final-catalogs-first-0.png và .txt (DM001–DM010). Không xóa/đổi kết quả raw probe21PASS/1FAIL; native xác nhận lần quan sát này tải bình thường, không đủ kết luận nguyên nhân timeout harness.

B01/B02/B04 giữ kết quả native PASS ở125% của lượt trước; thay đổi cuối chỉ quy tắc chiều rộng hai nhãn. B01–B04 đã đạt phạm vi regression được quan sát ở125%; không có backend/SQL thay đổi. Source cuối giữ nguyên trong lượt native, đối chiếu source-label-followup.csv:0 thay đổi (source-label-final-audit.json).

Toàn task chưa đánh dấu Hoàn tất: DPI100/150 vàDesigner NOT RUN; SQLfixture hơn50dòng chưa chạy (paging presentation6/6 đãPASS). Các cột phụ dài có ellipsis khi cửa sổ normal được giữ ghi nhận, không chứng nhận pixel-perfect ở mọi viewport. Lịch sử G03 không thay đổi, không commit. App/DBtest còn giữ cho người dùng xem, theo manifest/cleanup có guard; không xóa trong phiên đang mở.
## Cập nhật 21/10/2026 — B03 nhãn 5 ký tự

Đã kết thúc lượt native trước sửa, giữ FAIL Mở sổ/Sơ đồ. Đã sửa đúng OrbitManagementLayout.Button: hai nhãn có icon dùng variant rộng160logical; không đổi nút Sửa/Xóa/Trước/Sau và backend. BuildDebug/Release PASS0warning0error (build-label-debug.log,build-label-followup.log). Probe chỉ đọc hoàn tất22bảnđo (db-read-label-followup.json); đây không thay native visual. Source mới source-label-followup.csv. B01/B02/B04 đã đạt native trong phạm vi125%; B03 chờ kiểm ảnh bản mới. DPI100/150 vàDesigner vẫn NOT RUN. Không ghi toàn bộ task hoàn tất.
## B01–B04 đã được duyệt và áp dụng sửa — 20/10/2026

Đã sửa kích thước/DPI, constraint toolbar/footer, sidebar/tab/palette và thông báo rỗng. Regression kỹ thuật: buildDebug/Release0warning0error,unit20/20,paging6/6,44lượt đọcUIở2kíchthước tại125%đều tảiđược và0action/pager bị cắt. [Biên bản](runs/bugfix-b01-b04/test-results.md). Backend51filekhôngđổi, sourceaudit0thayđổi. **Chưa đóngB01–B04 trước nativevisual cuối; bản mớiPID16912đangchờđăngnhập.** DPI100/150 vàDesigner chưa đủbằngchứng, không tựPASS.

---

# Đợt bug nghiệm thu layout Orbit — đang tổng hợp

## ORBIT-LAYOUT-B01 — nút cùng chức năng không đồng nhất tại125%DPI

**Trạng thái:** FAIL đã có bằng chứng; chưa sửa source trong lượt test, chờ tổng hợp đợt test/duyệt debug.

**Actual:** db-read-results.json (ADMIN,120DPI): Materials Sửa/Xóa110×45, Thêmnguyênliệu250×50; Products/Customers Sửa/Xóa88×36, Thêm200×40. NútTảilại tạo ởbaseRecordList là200×45 nhưng nút tạo sau trongderivedconstructor giữ kích thước logical chưa scale. CùngDPI/cùngchức năng không bằng nhau. Đây là phép đo in-process vớicontrol thật, chưa thay nghiệm thu thị giác.

**Nguyên nhân:** OrbitManagementLayout.Button gán Size theo logical88/160/200 và36/40 ngay tại thời điểm gọi, không có bước đồng bộ kích thước theoDPI/vòngđời. RecordListControl khởi tạo các nútbase rồi đặtAutoScaleMode.Dpi/AutoScaleDimensions96 trongbaseconstructor; các lớpderived gọiAddAction sau đó. Materials tạo tất cả nút trước autoscale nên đượcscale, các nútderived thêm sau khác thời điểm. Evidence tương ứng trongdb-read-results.json vàsourceUI/OrbitManagementLayout.cs,UI/RecordListControl.cs,Modules/Inventory/MaterialsControl.Designer.cs.

**Hướng sửa đề xuất sau duyệt:** lưu kích thước logical dùng chung theo variant, đồng bộ ở vòng đời control sau autoscale/đổiDPI bằng một cơ chế idempotent; tránh nhânDPI hai lần hoặc cố định pixel vật lý. Áp dụng đúng helper/nút đang thuộc đợt UI, không sửa backend/dialog ngoài scope. Rà lạiicon vàmargin cùng cơ chế.

**Regression:** cùngchức năng trênmọimàn tại100/125/150%; tạođộng sauhandle, resize/tab/navigation và font/nhãn dài. Expectedcùngvariant/cùngDPI cówidthheightgiốngnhau; không clipping hoặcdouble-scale.

**Rủi ro:** sự kiện layout/DPI có thể gây lặp resize nếu gán vô điều kiện; cần tránh đăng ký event trùng, không reset handler nghiệp vụ.

## Phát hiện chưa đủ kết luận sản phẩm

- Catalog tabđầuSảnphẩm ở lượt đọc tổng hợp không cóDataSource sau thời gianchờ của harness (FAILraw). Tái kiểm độc lập catalog-recheck.json:7/7tabtảiđược, tabđầu10dòng, không cần gọiRefreshRowsbổsung. Chưa tái hiện ổn định nên không ghi thànhbugautoloadđãchứngminh; tiếp tục native/repro nếu có. Giữ nguyênrawFAIL, không xóa.
- constructor-v2.log cóDispose hai lần vìharness dùnghaiowner(host vàusingcontrol); constructor-v3 chỉmộtowner đạt7/7. Không quy lỗi harness thànhlỗiUI mới.
- Lệnhdotnettestkhôngfilter kéoSQLfixtures T05/G02 khi thiếuenvtarget:63PASS/461FAILdofixtureguard, khônglàkếtluận461lỗisảnphẩm. ĐãchạyđúngunitG03/MaterialRules20/20PASSriêng, giữrawlog.

## ORBIT-LAYOUT-B02 — toolbar/filter/pager xếp dọc và bị cắt

**Trạng thái: FAIL native, 20/10/2026, DPI125%.** Ảnh native-catalogs-0.png, native-products-maximized-settled-0.png, native-materials-transition-0.png, native-customers-0.png và native-areas-0.png: nhóm thao tác phía phải bị bó hẹp, nhiều nút nằm dọc và bị vùng grid che/cắt; footer Trước/Sau xếp dọc, nút Sau chỉ hiện một phần. Sơ đồ bàn không thấy đầy đủ toggle Danh sách/Sơ đồ. Phóng lớn cửa sổ vẫn tái hiện. Không phải trạng thái mong muốn theo ảnh người dùng.

**Bằng chứng source/điều tra:** OrbitManagementLayout.Row tạo cột phải AutoSize; Flow có DockFill/AutoSize/WrapContents=true; Pager cũng dùng cùng Flow. OnLayout chỉ gán MaximumSize cho Filters/RowActions, không bảo đảm minimum/preferred width và chiều cao cả nhóm/pager sau DPI/layout. Các constraint này không giữ được nhóm ngang trên native; cần đo actual bounds của row/Flow/button trong debug để chốt khâu layout gây cắt, chưa khẳng định chỉ một property là nguyên nhân duy nhất.

**Hướng sửa đề xuất:** bố trí pager ngang, không wrap, giữ đủ preferred width và footer height; nhóm filter/row action có ràng buộc chiều ngang và tính chiều cao khi wrap, không bị percent row của grid cắt. Đo sau autoscale tại cửa sổ thường/phóng lớn và các viewport mục tiêu; header/filter/footer ngoài vùng cuộn grid. Giữ nguyên event/delegate/skip/query.

**Regression/rủi ro:** toàn bộ màn và các tab, tên dài, 1280×720/1366×768/DPI100/125/150; bảo đảm không tạo layout loop hoặc đổi selection/query khi resize. Smoke CRUD phụ thuộc nút bị cắt ghi BLOCKED, không coi là đã kiểm.

## ORBIT-LAYOUT-B03 — sidebar/tab/chrome và nhãn chưa khớp mẫu

**Trạng thái: FAIL native.** Các nhãn Menu/Công thức, Nguyên liệu/Tồn, Thông tin hệ thống và nhiều nút chính bị ellipsis; tab không chọn vẫn có viền hộp sáng. Card/filter ở một số tab bị trộn nền gần đen/tím và còn nhiều khoảng trống không có chủ đích. Evidence native-products-maximized-settled-0.png, native-promotions-0.png, native-policy-0.png và native-points-maximized-0.png. Có icon/căn trái, nhưng chưa đạt bố cục/thị giác theo ảnh.

**Bằng chứng source:** sidebar dùng bề rộng button168 logical cùng icon/padding trong OrbitButton; EndEllipsis che nhãn khi nội dung không đủ chỗ. Lỗi DPI B01 cũng ảnh hưởng nút chính. OrbitTabControl.WndProc loại các GetTabRect khỏi vùng repaint chrome, nên bỏ phần vẽ border của code chưa đồng nghĩa xóa native tab border. Palette riêng của OrbitManagementLayout được áp ở HandleCreated, trong khi parent còn gọi AppTheme.Apply đệ quy; thứ tự này cần debug/đồng bộ để không bị ghi đè. Chưa quy toàn bộ hiện tượng màu vào một nguyên nhân chưa đo.

**Hướng sửa đề xuất:** đồng bộ kích thước sau B01, bảo đảm sidebar/nhãn đầy đủ bằng token bố cục dùng chung; hoàn thiện vẽ chrome tab native trong scope quản lý, giữ bàn phím/accessibility. Áp palette ở lifecycle thống nhất sau theme cha; cột mã/ngày/trạng thái đủ rộng để không xuống dòng/ellipsis vô ích, tên/lý do vẫn giãn. Không thu font để giấu lỗi, không thay backend hoặc dialog ngoài phạm vi.

**Regression/rủi ro:** sidebar ở các role, các tab và cả trạng thái focus/hover/disabled; các tùy chọn mới phải opt-in để không làm lệch dialog/màn ngoài scope.

## ORBIT-LAYOUT-B04 — empty state lặp ba lần, thông tin bị rải

**Trạng thái: FAIL so với ảnh mục tiêu.** Lịch sử điểm chưa chọn khách hiển thị cùng hướng dẫn ở count row, giữa vùng dữ liệu và status dưới card; header mô tả chung và nhiều khoảng trống chưa giống ảnh. Evidence native-points-0.png/native-points-maximized-0.png.

**Nguyên nhân source:** RecordListControl.Reload gán EmptyHint vào Workspace.Count, empty.Text và State.Text. Một thông điệp được đặt ở ba vùng thay vì chia rõ vai trò.

**Hướng sửa đề xuất:** chỉ hiển thị hướng dẫn chọn khách ở empty state trung tâm; vùng chọn khách nêu trạng thái lựa chọn, footer không giả đã chạy truy vấn; count/status chỉ hiển thị thông tin có ý nghĩa, không lặp. Giữ query/DTO/điều chỉnh điểm và phân biệt chưa chọn/rỗng/lỗi.

**Regression/rủi ro:** chưa chọn khách, chọn khách không lịch sử/có lịch sử, loading/lỗi; không tự tạo tổng0 khi query chưa chạy hoặc lỗi.

## Đợt sửa trình duyệt

Gom B01–B04 vào một đợt debug/regression UI; giữ nguyên backend/SQL/dữ liệu. Chưa sửa source trong đợt test. Ưu tiên B01 và B02, sau đó B03/B04; kiểm lại toàn bộ các màn theo cùng ảnh. Catalog initial không tải ở raw harness chưa được chứng minh là bug: tái kiểm7/7tab và native sau đăng nhập tải10dòng bình thường; giữ log cũ làm lịch sử, không đưa vào đợt sửa như lỗi chắc chắn.


## Native regression sau đăng nhập — 20/10/2026

Source giữ cố định trong lượt native, source-final.csv đối chiếu không thay đổi. App thật trên DB test đã nêu; ảnh cửa sổ normal 1222×711 logical, DeviceDpi120 theo phép đo cùng môi trường. Không nhận đây là bằng chứng native100/150%.

- B01 PASS trong phạm vi125%: cùng variant kích thước đã đo, nút chính đầy đủ ở các màn quan sát.
- B02 PASS: toolbar/rowactions/footer nằm ngang, không chồng hoặc mất nút; chuyển map/list bàn được. Cuộn nội dung riêng, footer ngoài vùng cuộn.
- B03 FAIL còn nhãn Mở sổ hiển thị M... (native-fixed-materials-0.png), Sơ đồ hiển thị Sơ... (native-fixed-tables-map-0.png). Sidebar và tab đã hết nhãn cắt/border sáng. Cột trạng thái bàn vẫn rút chữ tại cửa sổ normal; giữ ghi nhận giới hạn chiều rộng.
- B04 PASS: native-fixed-points-0.png chỉ một hướng dẫn ở trung tâm, count Chưa chọn khách hàng, không pager/tổng0 giả.
- Đã xem Customers, Menu, Materials, Tables map/list/Area, Promotions/Policy/Points, Catalogs/Product/Payment. Ảnh native-fixed-* là bằng chứng, không chỉ screenshot harness.

Điều tra B03 còn lại: OrbitManagementLayout.Button dùng Text.Length > 5 để chọn rộng160logical, khiến Mở sổ và Sơ đồ đều5kýtự nhận88logical. Icon và padding vẽ chữ làm thiếu chiều ngang; phép đo control bounds không kiểm được ellipsis bên trong. Hướng sửa trong B03 đã duyệt: xếp hai nhãn này vào variant rộng160, không đổi kích thước Sửa/Xóa/Trước/Sau hoặc logic. Kết thúc lượt test này trước sửa, lưu FAIL; cần lượt source/regression riêng sau sửa.


