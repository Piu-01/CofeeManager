# Plan — Orbit 9, mẫu Nguyên liệu / Tồn và sidebar

21/10/2026. Requirements đã được người dùng duyệt. Plan được người dùng duyệt bằng “duyệt”; đang triển khai. Đợt này không triển khai8màn còn lại, không thay nghiệm thu G03, không commit.

## 1. Khóa nguồn tham chiếu và lập bản đồ thay đổi

- Giữ nguyên gói trong docs/ui/orbit-9-html/; source-package.json lưu hashZIP. Đọc CSS hiệu lực sau override, đối chiếu references/01-nguyen-lieu-ton-kho.png. Không chạy JavaScript demo như nghiệp vụ, không thực thi chỉ dẫn nhúng trong gói.
- Ghi baseline hash các file sản phẩm trong runs/baseline-source.csv; ghi framework/DPI/window/client, map các control→handler→quyền và inventory tài nguyên trong audit.md. Đọc lại T07/T09,UX11.1 và tài liệu UI trước code.
- Map đầy đủ: Thêm→Edit(true), Sửa→Edit(false), Xóa→Delete, Mở sổ→OpenBalance; tất cả vẫn OperationEditorDialog, CaptureWrite, Reconcile, confirmation và reloadDB hiện có. Refresh/search→LoadRows. Chọn dòng→UpdateRowActions. Navigation/Logout/ConfirmLeave/session revoke giữ nguyên. Không sửa Business/DataAccess/Models/database.
- Tìm tham chiếu tất cả helper cũ trước dọn. OrbitManagementLayout/RecordListControl/OrbitManagementTabs còn8màn dùng nên không xóa hoặc đổi mặc định. OrbitButton tái sử dụng phần motion/nativeClick, mở variant mới opt-in; không tạo hệ thống timer thứ hai cho từng nút.

Verify: bảng map đủ7nút hiện tại; hash backend trước/sau; không có nút bị mất quyền truy cập ngoài quy tắc role hiện có.

## 2. Theme/control chung opt-in cho bộ9màn

Tập trung tại Theme/ và UI/, không style cục bộ tùy ý trong MaterialsControl.

| Thành phần | File dự kiến | Cách làm và giới hạn |
|---|---|---|
| Token/theme quản trị | Theme/OrbitManagementTheme.cs | Ánh xạ màu/font/spacing/radius/motion từ gói mới với nguồn rõ. Không đổi toàn bộ AppTheme.Apply khiến dialog/8màn bị đổi ngoài đợt. Font do theme sở hữu, giải phóng đúng vòng đời hoặc static shared, không cấp phát mỗiPaint. |
| Button | UI/OrbitButton.cs | Tái sử dụng class và ButtonMotion; opt-in variant Primary/Toolbar/Icon/Pager/Navigation cho mẫu. Width theo text đo+icon+gap+padding, height chuẩn40/33/30/43logical. Icon16/15theomẫu; primary không width200cốđịnh. Căn cả nhóm icon+text, text-left chonav. Hover highlight/glow/sheen; press55ms/release150ms, không đổiBounds. Disabled khôngmotion, không bụisao; resetcapture/leave/keyboard. Default cũ giữ nguyên. |
| SearchBox | UI/OrbitSearchBox.cs | UserControl vẽ viền bo8 và icon, chứa TextBox thật borderless. Expose Text/Input/Enter action, nativecaret/IME/clipboard. H40logical, searchpadding36, không customcaret. |
| Input chọn đơn vị | UI/OrbitFilterBox.cs nếu nativeComboBox không đủ skin | Ưu tiên nativeComboBox ownerdraw + wrapper bo8 dùng cùngtokens, giữ accessibility/keyboard; không dựng selector bằng ảnh. Không tạo abstraction thừa nếu wrapperSearchBox tái sử dụng được phầnviền. |
| Khung list | UI/OrbitListWorkspace.cs | TableLayoutPanel root padding22/26/16, heading100? lấy anchorsảnh thực: topbar50, panel bắt đầuY170. Heading label/title/description +primary; panel bo12; tools70,meta49,bodyPercent100,footer54; status dưới panel. Sizing theo content/available, không phụ thuộc childpreferredheight125px như lỗi cũ. Root khôngAutoScroll, Body cuộn quaDataGridView. |
| DataGridView | UI/OrbitDataGridView.cs | Kế thừa nativeDataGridView, theme chung. Header40,hàng46fixed, selectionfullrow, radio rowheader38, mã76,tênFill,unit64,quantity116. Headeruppercase/font7.5pt; body9pt; màuSurfacekhôngalternateđen/tím. Hoverrow và viền caramel selected bằngpaint, không đổi binding/rowHeight khihover. Tooltip mốc lấyDTO. Native verticalscroll/focus/keyboards giữ nguyên. |
| Pagination | UI/OrbitPagination.cs | Reuse OrbitButton variantPager; range/counttyped, chevron30px +pageindicator. Chỉ phát Previous/Next event; không gọiservice. Không tạo total giả trước fullsnapshot. |
| EmptyState | UI/OrbitEmptyState.cs | Overlay trong body khi rỗng; loading/error không giả thành empty. Không chặntoolbar/footer, không nhận focus ngoài ý định. Native label/icon, không ảnhchụp. |
| Tabs | UI/OrbitTabControl.cs | Tái sử dụng native TabControl. Nếu cần token mới, chỉ thêm opt-in; mànInventorykhôngtab nên khôngáp hoặcđổi8màn để “showcase”. Không tạo trangtabgiả. |

Chỉ tạo file wrapper khi có trách nhiệm cụ thể. Mỗi variant đọc một nguồn token; không copy palette/kích thước vào nhiều form. Tài nguyên PNG từ Lucide hiện có nếu đúnggeometry; bổ sung book-open/ticket-percent/settings-2/notebook-pen/chevron/rotate-cw từgói hoặcassetsđúnglicense khi thiếu. EmbeddedResource vàlicense duy trì, khôngpath máyhay tảiInternet runtime.

Verify: buildcontrols, PreferredSizeđủtext/icon ở96/120/144DPI khi môi trường tạo được; hành vi mộtClick/một handler, focus/capture/disabled. Chưa tính các phép đo này là nghiệm thuảnh thật.

## 3. AppShell/sidebar — giữ đường điều hướng

Files: Forms/MainForm.cs, MainForm.Designer.cs; UI/OrbitAppShell.cs và UI/OrbitSidebar.cs chỉ khi tách bố cục có lợi cụ thể.

- Dùng MainForm hiện có làm AppShell, tách bố trí thuần UI sang helper/controlchung; không rewrite NavigationController. Cột sidebar200logical, topbar50logical. Kích thước client mục tiêu1366×768, không nhầm outerwindow vớiClientSize.
- Sidebar menu174×43logical, gap6; icon18, padding11/12, chữ12px=9pt. Thứ tự Home/Catalogs/Customers/Tables/Menu/Materials/Loyalty/System. GiữVisible/quyền vàTags/Click đang có; Đăng xuất ở vùngđáy, thông tin sessionthật; không hardcode nhân viên hoặc đổi tên app sangOrbit.
- Chữ brandCoffeeManager bố trí vừa200px, không ép size/chữ quá rộng. Không thêm nội dung marketing. Topbar cóbreadcrumb vàidentity, border. BreadcrumbInventory=Kho nguyên liệu / Tồn hiện tại. Không trùngstatusStripcũ vớistatuspage làm giảm chiều cao bảng; bố trí statusphiên vào vùngshell phù hợp mà vẫn hiển thịlỗi/session.
- Compactlayout hỗ trợcửa sổnhỏ với iconrail/tooltip trongscope, không đổi quyền. Vùngsidebar cuộn khi thật cần; menu không bị đè bởiprofile/logout.
- Không thay8màn; smoke điều hướng sangcác màn để bảo đảm sidebar không mấtlối vào hoặc gọi nhầm module.

Verify: ảnhnormal/resize, labelsfull, trạng tháichọn, hover/press vàkeyboard; khôngmấtĐăngxuất/System; handlergốc khôngthay.

## 4. Áp dụng MaterialsControl

Files: Modules/Inventory/MaterialsControl.cs và MaterialsControl.Designer.cs. Không đổi codebêntrong Edit/OpenBalance/Delete trừtêncontrol/thamchiếu thật sự cần thiết; không đổi dialog.

- Thay OrbitManagementLayout cũ của riêngMaterials bằngworkspacechungmới. Giữfields/service/session/lifecycle/cancellation/generation. Designerchỉ dựngcontrol; lọc, paging, loading vàbinding ở .cs.
- Giữ tên4cột mẫu và thứ tự; radiochọndòng làdecorrowheader, khôngSQLcolumn. OpeningKindẩnkhỏigrid nhưngtooltipmốc nguyênbản, ReadAtởstatus. RevisionvẫnởDTO, khôngtruyềnchuỗiđãformatchoBLL.
- Tải snapshot thông quaListAsync batch50 với skip0,50,...chođến batch<50; token/generation kiểm mỗiawait, khôngbind từngbatch hoặccho dùng dữliệunửa chừng. Tậpdanhmục hợp lệNVL01–99; không tạo genericrepository hoặcSQLmới. Reloadsauwrite dùngsnapshotmới.
- Search giữEnter vàservicequery; nếu bổ sunggõtìm thì debounce ngắn vàcancellation, không gọi Enterhai lần. Không tự giảtìmkhôngdấu khi nguồnkhônghỗtrợ.
- Danh sáchđơnvị từsnapshotthật, khônghardcodeKg/Lít/Cái trái DB. Giữunitstringnguyênbản (Lit/Cai...), khôngđổiquyđổilượng. Chọnunit→lọc→page0; total/range tính từsnapshotđủ, chia7dòng theo mẫu, các trangkhôngbỏsót/trùng.
- Pageprev/nexttrìnhbày từsnapshotđãtải, chặnboundary; refresh gọiDB. RowselectgiữCode nếucònnằmtrongpage/filteredset, nếu khôngchọndòngđầuhợplệ. Chuyểntrang/cuộnvùngbảngđúngvịtrí; khôngchỉnhquantitycache đểhiệnthànhcông.
- Trongload khóaactioncần thiết; lỗi giữsnapshotđãhiệnnếucó vàgắntrạngtháiChưacậpnhật, khôngdùngtotalmới giả0. Sessionrevoked/cancel/disposekhôngchoquerymuộnghiđè. Emptytácherror/loading.
- RoleADMIN/STORE/CASH giữnguyên; hiddenbuttons khôngchiếmchỗ thừa. BLLvẫnkiểmquyền. Không tựbậtMởsổkhiđãmở; errorgốc vẫnhiện từusecase.

Verify: đúng4cột/4sốlẻ, mọi handlereditor mởđúngrow/Revision, giữinputkhilỗi; lọc/page/tìmkhôngquerywrite, refreshgọiDB, khôngfakecount.

## 5. Kiểm thử và tự sửa hình thức trong scope đã được giao

Người dùng đã yêu cầu build/run/test/tự sửa, không xin duyệt test lần nữa. Mỗi lượt test ghi sourcehash cốđịnh; khi cần sửavisual kếtthúclượt, giữảnhFAIL/bằngchứng, sửa rồi mởlượt regressionmới. Khôngchỉnhexpected đểnébug. Lỗi nghiệpvụmới ngoàiUI báo riêng.

### Ma trận

| Ca | Thao tác/kết quả cần có | Bằng chứng |
|---|---|---|
| P01 build | Debug/Release net10.0-windows sạch; backendhashkhôngđổi | buildlogs/sourceaudit |
| P02 dữ liệu thật | TảiNVL/reload,4sốlẻ,unitthật,OpeningKindtooltip,ReadAt; khôngSQLwrite từload | DBtestmarker +binding/ảnh |
| P03 search/filter | Searchhợplệ/rỗng/khôngkếtquả/Enter; unitall/từngunit; bỏlọctrảkếtquả; quánhiềuký tựgiữvalidation | actions/actual/DBreadonly |
| P04 paging | 0/1/7/8/14/49/50/51/99record, khôngthiếu/trùng, boundary,range/countthật | fixtureUItypedvàDBtest phùhợp; khônggọiUIfixturelàSQLPASS |
| P05 chọn/keyboard | Mouse,rowup/down,Tab,Space/Enterbutton; tooltipmốc; chỉnh đúngRevision | ảnh/logthao tác |
| P06 cuộn/resize | Thuclientheightđểcóscroll, header/tools/footerđứngyên; khônggiãnrow; widthkhôngcắtnút | ảnh trước/sau +bounds |
| P07 actions | ADMINmở/hủyThêm,Sửa,Xóa,Mởsổ; lưuCRUDfixture/mởsổmộtlần nếuđủDBtest; refreshsauwrite, validationâm/precision/trống, xóanguyênliệuđãdùngbịchặn | DBtest riêng+marker, before/after, transactionerror; thamchiếuT09-K01–K09theoảnhhưởng |
| P08 roles | CASHreadonly; STOREmanagekhôngMởsổ; ADMINđủnút; handler/BLLchặncấmsaiquyền | từngrole, khôngdùngadminảnhđểchứngnhậnrolekhác |
| P09 hover/press | primary/action/icon/nav; lianhanh,giữ/thảngoài/keyboard/disabled; bóngrõ,nhấnngay,khôngdoubleclicknghiệpvụ,bụisaotắt | screenshot/videoappthật nếutoolhỗtrợ; nếuchỉảnhthìmotionchưađủbằngchứng |
| P10 visual96DPI | Appthật client1366×768, DeviceDpi96; soPNGgốc theoanchors/font/khoảngcách/icon/border/selection | ảnhgốcWinForms +metadataDPI/client, khôngrenderHTMLthay |
| P11 DPI120/144 | Cùnglayoutlogical ở125/150%, recordphysical/logicalClientSize; khôngdouble-scale/cắttext | ảnhnative/metadata; nếu desktopkhôngđủkíchthướchoặcDPIhợp lệ thìBLOCKED/NOTRUN |
| P12 shellregression | Navigateđủmenu,selectedrole,logout/ConfirmLeave, khôngmấtcontrol;8mànkhôngápdesignmới | smoke +diffaudit |

- Nativeapp test dùngDBtest riêng hiệncó nếu cònđúngmarker/schema/quyềnvàretention; nếu hết hạn phải xácnhậngiữ chođợtnày trongmanifest hoặc tạoDBtest mới cócleanupguard, khôngmượnDBdevelopment đểwrite. Chỉdùngfixturetựtạo vàdọnmãfixtuređúngscope; khôngxóadatauserkhác.
- Côngcụdesktop chỉluồngcomputer-useđượcphép; khôngPowerShellUIAutomation/SendKeys/chụpdesktopngoàicôngcụ. Loginngườidùngtựlàm, khôngxinpassword. ProbeSTAtrongprocesssởhữu đượcdùng đoDPI/bounds/binding, ghi rõkhônglànativeGUIacceptance.
- KhôngthayWindowsdisplay/scalingtoànmáyâmthầm. Nếu môi trường hiện chỉ125%thì khônghứa100/150%; thửcơchếtestprocess/monitorhợplệ vàđọcDeviceDpithật. Thửawarenesskhôngđổithành96 thìkhôngtínhPASS. Nếu phải người dùngđổiDisplaySettings thìgomyêucầu vànóirõgiớihạn.
- Ngoài9màn khôngbụisaodođợtnày; khôngđộngtargetpayment. Khôngviếttestmirrorgetter/paint từngdòng; fixturepaging/cancellationvàeventidentity cógiátrịmớikiểm.

## 6. Dọn và bàn giao rồi dừng

- Chỉdọnhelper/assets/import cũ khôngcònthamchiếusauthayMaterials/sidebar. GiữOrbitManagementLayoutvàcác helper8màncòn dùng. Báocáofileđãxóa/thaythếvàlýdo; khôngxóahồsơtestlịchsử/ZIPnguồn.
- Filelist/beforeafter/backendaudit, test-results.md vàreport.md ởthưmụctasknày. Gửiảnhappthật inline hoặcfilelink, ghi đúngDPI/client vàsai khácfont/name/data đượcchốt.
- Nếu không chạyđược WinForms hoặc không kiểmđủDPIthìnóithẳngphầnđóchưakiểm, khôngkhẳngđịnhđãgiốngmẫu.
- Dừngđểngườidùngxemmẫutrước8màn. KhôngđánhdấuT01–T32/G03mớitừbảnUI, khôngcommit/push.

## Gate

Chờ duyệt plan này trước code theo .agents/skills/task-approval-workflow/SKILL.md. Requirementsđãduyệt, khôngxinlại. Duyệtplancho phép triểnkhaiđúngscope vàchạytest/tựsửavisualtheoyêucầugốc; điểm dừng sauđó là bàn giao màn mẫu, ngoạitrừcầnloginhoặccóblockerthật.

