# Kết quả kiểm tra Orbit — tám màn còn lại

Ngày21/10/2026. Lượt sản phẩm cuối R6, hash runs/source-r6.csv; drift0 sau các lượt cuối. .NET10.0.401, Windows, SQL Server localhost. DB duy nhất CoffeeManager_Test_G03_20261019_215457_29e552a9_demo; guard dbo.G03TestMarker.RunId=20261019_215457_29e552a9. Không ghi DB trong đợt này; login app do người dùng thực hiện thủ công. Không coi fixture SessionContext trong probe là kiểm thử đăng nhập.

Yêu cầu test/tự sửa là yêu cầu gốc Orbit, được kế thừa khi người dùng cho triển khai tám màn. Không mở lại nghiệm thu nghiệp vụ G03; không tick T01–T32.

| Mã | Dữ liệu/thao tác | Expected/Actual | Kết quả và bằng chứng |
|---|---|---|---|
| BUILD-D | dotnet build WinForms -c Debug --no-restore -m:1 -nr:false |0 warning/error|PASS build-r6-debug.log|
| BUILD-R | build UiProbe Release kéo project WinForms/Business/DAL/Models |0 warning/error|PASS build-r6-release.log,build-probe-r6-wait.log|
| RULES | xUnit RulesTests G03 + MaterialRulesTests + CatalogRulesTests |42/42|PASS rules-r3-retry.log; source rules/backend không đổi tới R6|
| BIND | 19 snapshot mỗi lượt (9màn,1catalog thêm,2role,7paging fixtures) | Menu11,Bàn10,Khu vực10,Khách11,KM12,Hạng10,Điểm0,Catalog10; đúng cột/readonly/count/rowheight |PASS probe-r6-96-settled.json,probe-final-120.json,probe-final-144.json|
| CAT-7 | Chuyển7 CatalogKind thật |rows10/10/10/2/12/10/10 và đúng cột của tab|PASS các JSON trên|
| FILTER-M | chọn DM002 |2 dòng SP005/SP006|PASS probe + native-r3-menu-filter-100-0.png|
| SEARCH-M | Native nhập SP005,Enter |1 dòng SP005,giá45.000₫|PASS native-r5-menu-search-100-0.png/.txt|
| EMPTY-M | tìm ORBIT_NO_MATCH_20261021 trong probe |0 row,overlay visible|PASS MenuEmptySearch trong JSON cuối|
| FILTER-T | chọn KV002, chờ query thật hết busy |2 bàn cùngKV002|PASS TableAreaFilter trong các JSON cuối|
| MODE-T | bấm Danh sách/Sơ đồ native/probe |grid/card đúng visibility|PASS native-r3-tables-list-100-0.png và TableListMode/TableMapMode|
| SELECT-T | chọn B0003 |CurrentRow B0003; chỉ cardB0003 Selected; statusHD20250410|PASS CardSelection trong JSON cuối; native-final-table-selected-100-0.png/.txt|
| PAGE | fixtures typed 0/1/49/50/51/100/101,ClickNext đến cuối |Seen bằngcount,khôngtrùng/mất,Next khóa khi biết cuối|PASS7/7 mỗi DPI trong JSON; fixture UI không chứng minh concurrency SQL|
| ROLES | read identity CASH/STORE, khởi tạo menu với service thật |CASH không ghi;STORE chỉCông thức;ADMIN đủaction|PASS role snapshots trong JSON; không tự login tài khoản role bằng GUI|
| EMPTY-P | chưa chọn khách trên app thật |icon/lời nhắc,header giữ,0giao dịch|PASS native-final-08-points-100-0.png|
| PICK-P | chọn KH001 bằng picker thật |identity3433/BKIM và4 giao dịch |PASS native-r3-points-selected-100-0.png/.txt; picker theme/chiều cao nút đã regression native-final-customer-picker-100-*.png|
| ORDER | xem8màn,kiểmaction từ JSON/capture |Sửa/Xóa liền nhau;khôngthiếuaction;hạngkhôngicon tên|PASS snapshots100% + JSON; native-final-07-ranks-100-0.png|
| SCROLL | Native Menu scroll xuốngSP981 |chỉbodycuộn,toolbar/header/footer đứng yên|PASS native-r5-menu-scroll-100-0.png; không giãnrow|
| RESIZE | host control logical1280×720 và1366×768 (trừsidebar/topbar) |body>0,pager khôngtràn,primarykhôngchồngtitle|PASS30case mỗi DPI; không thay thế resize MainForm thật ở mọi cỡ|
| GUI-100 | app thật client1366×768 (source và capture1368×800gồmframe/titlebar),Scale100% |đối chiếu9PNGgốc;giữnativecontrol,màu,padding,rowheight|Đã xem/chụp đủ9màn: native-final-01…09-100-0.png. Không tuyên bố pixel-perfect; tên/dữ liệu DB khác dữ liệu ảnh mẫu|
| DPI-125 | Settings100→125,app đang mở;probe mới DeviceDpi120 |bounds/binding/paging/resize|Probe PASS;ảnh native-final-menu-125 và inventory-125-settled có chữ mất nét sau liveScale. Chưa kết luận nguyên nhân/capture hayapp|
| DPI-150 | Settings125→150,app đang mở;probe mới DeviceDpi144 |bounds/binding/paging/resize|Probe PASS;ảnh native-final-menu-150 chữ mất nét. Chưa chốt độ nét native, chờ đối chiếu trực tiếp|
| EDIT-OPEN | NativeSP005→Sửa→Hủy |mởđúngsảnphẩm;Hủykhôngghi|Mở/Hủy PASS;Danh mục editor hiệnCa phe dùgridDM002:FAIL,bug-report.md,native-r5-menu-editor-cancel-before-*.png;khôngLưu|
| WRITE | Thêm/Sửa/Xóa/Lưu/CaptureWrite/confirmation của mọi module |giữ nguyên use case |NOT RUN ghi giao dịch đợt này;chỉaudit vàmở/Hủy editor. Không kế thừa giảPASS từ nút còn đó|
| DESIGNER | mở VisualStudioDesigner 9màn |khônglỗidesign-time|NOT RUN|

Raw log thất bại giữ lại: build-r3-release.log (MSBuild trảFAILED0error, retry -m:1 PASS), probe-r3.log/build không thành công mặc định, probe-r3-execution.log SQLaccessdenied sandbox; rules-r3.log testhosttimeout sandbox. Chạy quyền phù hợp đúng DBtest/testthuần sau đóPASS; không bỏ logfail.

R2/R3/R4/R5 từng lượt kết thúc trước sửa source. R5 có tabellipsis trong đoảnh;R6 sửa đồng bộ viền chọn bàn. Probe-r6-96.json TableAreaFilter từng ghiFAIL(rows10) vì harness chỉ chờ label mà status SelectCode thay label khi query cònbusy. Harness bổ sung chờ fieldbusy/loading thật, không đổi expected hoặc source sản phẩm; probe-r6-96-settled.json đọc2dòngPASS,logđầu giữ nguyên.

Phần chưa chốt: lỗi danh mục editor SP005; độ nét native sau đổi DPI giữaphiên; ghiCRUD đầyđủ/Designer/resizeMainFormmọicỡ. Không đánh dấu toàn bộ task hoặc G03/T01–T32 hoàn tất chỉ từ lượt UI này.

## Lượt R8 — sửa font DPI dùng chung
R6 native125/150 xác nhận FAIL bởi người dùng: chữ app cũng mờ/đứt nét. Không coi là chỉ lỗi ảnh chụp. R7 đo thấy style body chưa cập nhật; giữ logFAIL. R8 source cố định tại runs/source-r8.csv.

- Build Debug app:0warning/0error (build-r8-debug.log). Release qua UiProbe:0warning/0error (build-probe-r8.log). Diagnostic project có warningNU1900 vì không lấy được vulnerability feed NuGet; không lỗi compile. Lần build-dpi-r7 đầu bị khóa DLL do probe đang mở; đã đóng và retry, giữ log gốc.
- DpiProbe app thật không DB:100→125→150→100. Cell font pixel12→15→18→12; header10→12.5→15→10; row46→58→69→46; đo Nguyên liệu71×15→88×20→106×25→71×15. Evidence dpi-live-r8.jsonl. Không còn giữ font9pt cũ khi đổi DPI. Không dùng phép đo này thay cho kiểm tra độ nét app đầy đủ.
- UiProbeR8 mới tại96 và144DPI:19screen,30resize,7catalogtab,7boundarypage,filterMenu/Table,empty,mode/card selection đều PASS; khôngexception. JSON probe-r8-96.json/probe-r8-144.json. Source backend so baseline:0file đổi. Không ghi DB.
- Đã khôi phục WindowsScale100%. AppDebugR8 mở lại đúngDBtest, PID16592, chờ đăng nhập thủ công để kiểm tra native sau sửa. Hình thức nativeR8 và độ nét150%:PENDING, không đánh dấuPASS từ probe.

## Lượt R9 — điều hướng cache và tab sau đổiDPI
R8native100%: đã xem/chụp đủ9màn,client1366×768; native-r8-01…09-100-0.png. R8live125 phát hiện tab cóscrollbar và quay màncache bị giữDPI cũ (FAIL); nguyên nhân/bằngchứng ởbug-report. Kết thúcR8trước sửa, không xóaảnhFAIL.
R9freeze source-r9.csv;21fileWinForms thayđổi so baseline (17sửa+4mới),backenddrift0. Bổ sung Navigation/NavigationController.cs chỉ giữcontrolparented/ẩn/hiện, không sửa quyền/ConfirmLeave/factory/service.
- BuildDebug/Release:0warning/error (build-r9-debug.log,build-r9-release.log).
- UiProbeR9 tại96DPI:19screens,30resize,7CatalogTab,7ServerPaging,filter/empty/card/mode đềuPASS;khôngexception (probe-r9-96.json).
- ActualNavigationController trongprobe:Products→Customers→Products, chỉtạo2page, cả2giữparent,chỉ1visible,inputtìmkiếm còn nguyên:PASS (probe-navigation-r9.json). Không tự đăng nhập bằngGUI;fixtureSessionContext chỉphụctestđiềuhướng/quyềnđọc.
- Tabwidth160cóscrollngangcầnthiết,buttonkhôngtrànchiềucao;900khôngscroll:PASS cùngJSON.
- DpiProbe nativekhôngDB125→150:form/root/cached/grid/tabs đều120→144,kểcảmànẩn;đổimànvẫn144;tabsScrollfalse:PASS dpi-cache-r9.jsonl. Nativecapturechẩnđoánkhôngthaythếappnghiệpvụ.
- WindowsScaleđãkhôiphục100%. AppR9PID38516 đãmởđúngDBtest,đangchờđăngnhậpthủcôngđểxácnhậnnativeliveDPI. Độnét125/150 vànativeR9cuối:PENDING. GhiCRUD/Designer/bugSP005vẫngiữnguyêntrạngtháichưakiểmchứng/OPEN.

## Lượt R10 — overlay header và chốt DPI
R9native: đã kiểmtracacheMenuvàtabKhuyếnmãiở125/150;khôngcònscrollbarchetab,khôngcònmàncachethu nhỏ. Evidence native-r9-02-menu-125/150,native-r9-06-promotions-125/150. Người dùng xácnhậntrựctiếp150% “Chữ trên app đã rõ”. ĐộnétMenu150%PASS theo người dùng;khôngcoiảnhSkymấtnétlàactualappFAIL.
R9nativePoints mới mởsauDPIcóoverlaycheheader,đãghiFAILtrướcsửa. R10thêmduynhất Grid.ColumnHeadersHeightChanged→PositionEmpty trongOrbitRecordListControl. Khôngsửafont/handlerghi thêm.
R10Debug/Release0warning/error (build-r10-debug.log/build-r10-release.log). Probeoverlay144:HeaderHeight90→OverlayTop90PASS,khôiphụcheaderchuẩnEmptyHeaderOffsetPASS (probe-overlay-r10-144.json). Fullprobe96:19screen,30resize,7CatalogTab,7ServerPaging,filter/empty/card/modePASS,2EmptyHeaderOffsetPASS,khôngexception (probe-r10-96.json). Hashsource-r10.csv;backenddrift0,21fileWinFormsso baseline.
Windows đã về100%,probewindowđãđóng;appR10PID33244mởđúngDBtestđangchờđăngnhậpthủcôngđểchụpcaPointsnativecuối. Khôngchốtcaseoverlaynativechưachạy. SP005categoryeditorvẫnOPEN,ghiCRUD/Designerchưarun.

## Kiểm tra 125% theo yêu cầu người dùng ngày 21/10/2026
Đã chuyển Windows Scale từ100% sang125% bằng Display; không đổi resolution hoặc cài đặt khác. Source giữ nguyên R10, drift0.
- probe-overlay-r10-120.json: DeviceDpi120; tăng header lên75px thì overlay.Top75px, PASS. Khôi phục header chuẩn, EmptyHeaderOffset PASS.
- probe-r10-120.json:19snapshot,30resize,7tab danh mục,7biên phân trang; lọc Menu/Bàn, trạng thái rỗng, chọncard và chuyểnmode đều PASS;2EmptyHeaderOffset PASS;0exception/FAIL.
- Đây là kiểm tra control/binding trong process trên DB test có marker, không phải ảnh app nghiệp vụ đã đăng nhập. NativeR10 vẫn BLOCKED tại màn Đăng nhập; đã nhờ người dùng đăng nhập, không tự thao tác xác thực.
- Tạm giữ Scale125% theo yêu cầu test để người dùng đăng nhập và tiếp tục ca native; chưa trả100% khi lượt này còn chờ.

## Lượt R11 — khởi tạo cửa sổ ở 125%
R10 native 125% xác nhận header Points không còn bị overlay che (native-r10-08-points-125-0.png). Khi cửa sổ MainForm được tạo sau lúc đổi Scale, sidebar ban đầu bị autoscale thêm một lần; resize đưa về đúng200logical. Tab khởi tạo cũng cần relayout sau khi HWND con có DPI cuối. Đã kết thúc lượt R10 trước sửa.
R11 chỉ thêm ArrangeNavigation ở Shown của MainForm và PerformLayout ở OnCreateControl của OrbitSectionTabs. Build Debug/Release đều0warning/error:build-r11-debug.log,build-r11-release.log. Source cố định source-r11.csv;22file WinForms (18sửa+4mới), backend drift0.
- probe-shell-r11-120-retry.json: MainForm thật DeviceDpi120,sidebar250physical=200logical;tabheight48physical,khôngscrollbar và không tràn:PASS. Đây là shell kiểm tra riêng có fixtureSessionContext, không phải kiểm thử đăng nhập.
- Log shell lần đầu có ObjectDisposedException do harness dùng using catalog bên cạnh shell sở hữu catalog, gây Dispose trùng. Đã bỏ using của catalog trong harness; giữ log probe-shell-r11-120.json. Không sửa source sản phẩm để né lỗi. Retry khôngexception.
- probe-r11-120.json:19snapshot,30resize,7CatalogTab,7biênServerPaging,lọcMenu/Bàn,empty,cardselection/mode và2EmptyHeaderOffset đềuPASS;0exception/FAIL.
- R9 native Menu/tab/cache125/150 đãPASS về bố cục; người dùng xác nhận chữ trên app150% đãrõ. R10nativePoints125PASSheader. R11nativekhởi tạo125đangchờđăng nhập để xácnhận;appPID15316 đãmởđúngDBtest. GiữScale125% theo yêu cầu hiện tại.

## Xác nhận native R11 cuối — 21/10/2026, Scale 125%
Người dùng đăng nhập admin thủ công, app Debug R11 PID15316 trên DB test CoffeeManager_Test_G03_20261019_215457_29e552a9_demo. Giữ nguyên source R11 trong lượt này; không ghi DB.

| Ca | Expected | Actual / bằng chứng | Kết quả |
|---|---|---|---|
| Khởi tạo shell ở125% | Sidebar200logical, không avatar thừa | Sidebar đúng kích thước, profile nằm cuối; native-r11-09-methods-125-0.png | PASS |
| Tab khởi tạo Danh mục/KM | Đủ nhãn, không scrollbar khi đủ ngang | Phương thức10 dòng, KM12 dòng; native-r11-09-methods-125-0.png, native-r11-06-promotions-125-0.png | PASS |
| Points chưa chọn khách | Header đủ6cột, empty bên dưới | Không overlay che header; native-r11-08-points-empty-125-0.png | PASS |
| Chọn KH001 qua dialog thật | Đọc đúng khách và lịch sử, không ghi dữ liệu | KH001 / Nguyen Minh Khoa /3433/BKIM;4giao dịch, mã9/8/4/1; native-r11-08-points-selected-125-0.png và .txt | PASS |
| Footer Points | 1–4/4, trước/sau khóa | Đúng nội dung và trạng thái disabled trong accessibility tree | PASS |

Thao tác click theo index trên owned modal bị công cụ từ chối do cache, chưa tác động app; chuyển sang tọa độ từ screenshot đã quan sát và chọn thành công. Không coi đây là lỗi sản phẩm. Ảnh Sky ở DPI cao bị giảm nét; không dùng ảnh đó để phủ nhận xác nhận trực tiếp của người dùng rằng chữ app150% rõ.
Kết thúc ca native được yêu cầu. Giữ Windows125% theo yêu cầu test hiện tại. CRUD ghi toàn bộ, VisualStudioDesigner và SP005 editor category vẫn NOT RUN/OPEN như báo cáo; không tuyên bố mọi nghiệp vụ PASS.

## Phân trang Inventory50 — 22/10/2026
Source sản phẩm chỉ đổi PageSize7→50 trong MaterialsControl.cs. Người dùng duyệt fix bao gồm regression. Release build thành công0warning/0error. Probe mới InventoryPagingProbe chạy control WinForms thật trong process bằng fixture MaterialRow trong bộ nhớ, không SQL/không dữ liệu thật/không đăng nhập tự động; không thay thế ảnh app đầy đủ.
Lệnh: dotnet run --project docs/tasks/orbit-9-remaining-screens/runs/InventoryPagingProbe/InventoryPagingProbe.csproj -c Release --no-restore -- <output-json>.
FirstPage0/14/49/50/51, InternalScrollAvailable, ScrollReachesRow50 (toolbar/footer không di chuyển), NextPage51, PreviousPage, FilterResetsAndCounts:10PASS. Resize1080/1166:2FAIL vì footer/body giao1px125%; sốdòng50/chiềucaodòng58px vẫnđúng. Kết quả đầu inventory-paging-50-first-results.json, bổ sung số đo inventory-paging-50-diagnostic.json. Source sản phẩm giữ cố định giữa hai lượt; chỉ thêm diagnostics vào harness, giữ assertion và expected ban đầu. Chưa kiểm lại app đầy đủ sau đăng nhập hoặc chụp ảnh bản thay50; không tuyên bố mọi caPASS. Không sửa lỗi rounding ngoài phạm vi.
Lần chạy đầu sandbox không tạo project.assets.json; build no-restore báoNETSDK1004. Restore/build/run quyền cho phép thành công; không phải lỗisource.

## 22/10/2026 — Hộp chọn khách chỉ làm nhiệm vụ chọn
Nguyên nhân đã xác nhận trước sửa: CustomerPickerDialog nhúng CustomersControl(picker:true), nhưng constructor đăng ký các action quản lý trước nhánh picker; vì vậy modal có handler thật Thêm/Sửa/Xóa/Mở sổ/Hóa đơn. Backend vẫn DemandAsync theo quyền; đây là phạm vi UI quá rộng, không phải vượt quyền.
Người dùng duyệt sửa sau giải thích. CustomersControl hiện nhánh picker đăng ký Chọn khách/Đóng rồi return trước action quản lý. CustomerPickerDialog gắn CancelButton để Escape đóng. Lịch sử điểm không có Khách vãng lai; Preview ưu đãi truyền allowGuest=true giữ lựa chọn vãng lai cần cho luồng cũ. Trang Khách hàng không đổi handler/quyền/query; chọn khách vẫn GetAsync đối chiếu trước trả kết quả. Không thay backend/SQL.
Release build0warning/0error. CustomerPickerProbe:6/6PASS kiểm cấu trúc nút với fixture ADMIN/CASH và allowGuestfalse/true, CancelButton và trang quản lý vẫn giữ action. Không đăng nhập, không kết nối DB. Đây không phải test quyền backend hoặc thao tác modal trên app đầy đủ; chưa kiểm GUI sau đăng nhập. Bằng chứng runs/customer-picker-actions-results.json; probe không show form để tránh query/ghiDB.
File sản phẩm: Forms/CustomerPickerDialog.cs, Modules/Customers/CustomersControl.cs, Modules/Loyalty/LoyaltyControl.cs.

## Kiểm tra native hộp chọn khách — 22/10/2026, đang chờ đăng nhập
Người dùng yêu cầu “kiểm đi”, đã duyệt kiểm tra native. Debug mới nhất build0warning/0error. Không có app mở ban đầu; đã chạy launcher DB test CoffeeManager_Test_G03_20261019_215457_29e552a9_demo, PID3172. Sky xác nhận cửa sổ “CoffeeManager · Đăng nhập”. Công cụ không thao tác xác thực, đã nhờ người dùng đăng nhập thủ công.
Các ca dự kiến: picker Lịch sử điểm không có action quản lý/vãng lai; tìm KH001; chọn trả đúngKH001 và tải4giao dịch; đóng/Escape không thay khách; trang Khách hàng giữ action theoADMIN; Preview ưu đãi có vãng lai nhưng không quản lý. Chưa thao tác ca native khi còn màn đăng nhập, tất cả PENDING, không ghi DB.

## Kết quả native picker sau đăng nhập — 22/10/2026
User đã đăng nhập admin thủ công. App Debug PID3172, DB test CoffeeManager_Test_G03_20261019_215457_29e552a9_demo, giữ source cố định, không ghi DB. Sky window984854 CoffeeManager.

| Ca | Expected | Actual | Kết quả |
|---|---|---|---|
| Picker Points không có quản lý | Chỉ Chọn khách/Đóng ngoài tìm/tải lại/phân trang | Không có Thêm/Sửa/Xóa/Mở sổ/Hóa đơn hoặc Khách vãng lai; ảnh native-picker-clean-20261022-0.jpg và tree.txt | PASS |
| Chọn KH001 | Trả đúng khách và lịch sử | KH001/Nguyen Minh Khoa/3433/BKIM,4giao dịch9/8/4/1; native-picker-points-return-20261022-0.jpg/.txt | PASS |
| Trang Khách hàng chính | Giữ action quản lý ADMIN | Thêm/Mở sổ điểm/Hóa đơn/Sửa/Xóa vẫn có; native-picker-management-page-20261022-0.jpg/.txt | PASS |
| Đóng picker không chọn lại | Không thay khách hoặc mất lịch sử | Đóng về KH001/3433/BKIM, vẫn4giao dịch; native-picker-close-keeps-selection-20261022-0.jpg/.txt | PASS |
| Tìm kiếm trong picker | Nhập tên và lọc | Sky báo focus ô tìm nhưng type_text không xuất hiện chữ sau hai lần đặt focus; Enter gửi vào cửa sổ cha làm reload. Không có input thực vào ô để kết luận lỗi app | BLOCKED bởi công cụ |
| Escape | Đóng không đổi khách | CancelButton đã qua probe cấu trúc; chưa thao tác native do routing phím vào cửa sổ cha | NOT RUN native |
| Preview ưu đãi / Khách vãng lai | Giữ vãng lai, không quản lý | Đã qua probe cấu trúc6/6 trước; chưa mở lại native trong lượt này | NOT RUN native |

Lần capture đầu khi app bị che trả ảnhCodex dù treeCoffeeManager; đã activate app và quan sát lại trước input, không dùng ảnhđó làmchứngcứ. Click Chọnkhách đầu chỉ focus; quan sátmới rồi clicklại trả kếtquảđúng. Không xácnhận tìmkiếm PASS khi chữ không được nhập. Kết thúc lượt native, app để ở Lịch sử điểmKH001; không đổi WindowsScale hoặc source.
