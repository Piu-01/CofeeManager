# Đợt bug editor công thức — G03-B03 — 20/10/2026

## Sau duyệt — đã áp dụng sửa, chờ regression GUI

Người dùng duyệtđợtsửa/regression. ChỉsửaProductsControl.cs vàOperationEditorDialog.cs (MarkDirty cho xóa nháp). Recipegrid dùngFlatcombocell/placeholder, màucontrolđangedit vàDrawItemcombo hẹp; cellcóviền, cộtlượngcógợiý/tooltip/cănphải, unitread-onlynềnriêng; commitcombodirtyrồicậpnhậtunit theoCell.Value. Thêmnútxóadòng choADMIN, canceldòngnew hoặcDeleteDataRowView, đánhdấunháp; STOREkhôngcónút/khôngđượcedit. KhôngđổiSQL/BLL/contract, khôngtrừtồn.

Debug/ReleasebuildPASS0warning0error; unitG03/MaterialRulesTests20/20PASS. [Biênbản](bug-test-results.md). Chưatuyênbố hếttrắng/tràn hoặcunitngay từbuild; cần người dùng kiểm native. TrạngtháiB03: **Đã sửa — chưa đóng, regression GUI chờ**. Source sửa cố định trong regression theo source-frozen.csv; lỗi mới ghi nhận trước sửa.

**Mức độ P2. Trạng thái:** Đã điều tra bằng ảnh/source, chờ duyệt cả đợt sửa và regression. Chưa sửa source trong lượt test. Đây là bug sản phẩm GUI, khác lỗi helper modal trước đó.

## Hiện tượng và bằng chứng

Người dùng gửi4ảnh khi kiểmM17/SP981: ôcombo nguyênliệu chưa chọn trắng nổi trên nền tối, trạngtháiediting/nonditing lệchmàu; báo khungtrắngbịtràn; đơnvị chưa hiện ngay sauchọn, phải bấm tiếp; ôĐịnhmức trống không rõ có thể nhập; thêmnhầm dòng không thấy thao tácxóa. Ảnh1códòngđangchọnCa phe hat nhưngđơnvịtrống; ảnh2/3đãcó0,0180Kg/0,0200Lit; ảnh4thêmdòngnhầmCa phe hat. Không tính ảnh2làbằngchứng đãLưu.

Ảnh gốc người dùng: codex-clipboard-f9cd83ae-7fda-43df-bf60-1aaed814c3e5.png; d413365f-2121-4302-b411-9869a41a8338.png; bd8ff4f2-2e7e-4d1f-848f-b13ec564fc3a.png; 640e8b07-65e0-41d6-a083-6e63ee1363fb.png trong attachmentchat20/10. Không sửa ảnh. [SQL chỉ đọc](../runs/manual-g03-20261019/M17-investigation-db.json): SP981Ngungban/recipekhôngcó/receipt1, nên dữliệutrongeditorlànháp. Truyvấnđầu dùngnhầmDinhLuonglỗi; sau đối chiếuschema dùngDinhMuc, không cówriteSQL điều tra.

## Nguyên nhân đã thấy và giới hạn

1. ProductsControl.Recipe dùng DataGridViewComboBoxColumn mặc định, không cấu hình renderer/FlatStyle/editor theme. AppTheme.ApplyGrid chỉ đặt stylechung và hàng xen kẽ, không theme EditingControlShowing hoặc overridecellcombo. Đây là khoảngtrống cấu hình có bằngchứngsource, phù hợp ảnh nềntrắng. Màu xen kẽđen/tím bảnthân là theme hiện có; lỗi cần sửa là phân biệt editable/read-only/selection vàeditortrắng, không loại mọi màu xen kẽ vô cớ. Cơchế render/clip gây cảmgiác“tràn” chưa đo riêng, không khẳng định bounds tràn bằng source.
2. Unit chỉ cập nhật trong CellValueChanged đọc DataRowView. Không có CurrentCellDirtyStateChanged/commit khi combo đổi, vìvậy chưa chủđộng chốt selection để đồngbộunit ngay. Đây là nguyênnhân khảdĩ rõ từsource cho độtrễ; cần regression native xácnhận sựkiện/dòngmới thayvì chỉ suy đoán.
3. CộtĐịnhmức là DataGridViewTextBoxColumn mặcđịnh, themechung SingleHorizontal, chưa có dấu hiệu/tooltip/hướngdẫn nhập riêng. Ảnh xácnhận khó nhậnbiết vùng editable. Không đềxuất numericcontrol mới nếu native cell cấu hình đủ.
4. AllowUserToDeleteRows=true choADMIN nhưng không có nútXóadòng, FullRowSelect hoặc hướngdẫnphímDelete. Vìvậy không chínhxác nếu nói không thể xóa bằngbànphím; thiếu là thao tácxóa dễ khámphá và đángtin trên dòngnháp/đangedit. STOREReadOnly và khôngđượcxóaphảigiữnguyên.

## Đợt sửa đề nghị — duyệt một lần

- Giới hạn editorcôngthứcT10 và helpertheme hẹp nếu cần. Đọc bộguide docs/ui theoAGENTS trước code. Theme đồngnhất cellcombo và controlđangedit, placeholderchọnnguyênliệu rõ, kiểm dropdown/focus/selection/disabled/resize/DPI, không đổistyle toànapp.
- Commit selectioncombo đúng thờiđiểm rồi cậpnhậtunitread-only ngay; đồngbộ dòngmới/dòngđangedit/thaynguyênliệu; bỏ chọn không giữunitcũ, không tự quyđổi quantity.
- Làm ôĐịnhmức có affordance nhập rõ và hướngdẫn dấuphẩy/4sốlẻ; vẫn typeddecimal và validationhiệncó. Không mặcđịnhlượnghợplệ hoặc đổiexpected để né lỗi.
- Thêm thao tác **Xóa dòng** rõ choADMIN trong editor: xử lýEndEdit/CancelEdit/phầnnew-row, chọn lại dòng hợp lý; Deletekeyboard tươngđương nếu có; STOREkhông mutate. Chỉ xóa dòngcôngthứcnháp, không xóaNGUYENVATLIEU hay ghiDB đếnLưu.
- Giữ nguyên Save toànrecipe nguyên tử/receipt/revision, quyềnBLL/SQL, financialsnapshot và migrations. Không refactor toànmodule, không đổi nghiệp vụ/contract.

## Regression trong cùng đợt đề nghị

NativeADMIN: mở trống→chọnNVL01unitKg xuất hiệnngay→nhập0,0180→chọnNVL02unitLit/ngay→nhập0,0200→thêmdòngnhầm→xóadòng→Lưu2dòng→mởlại; đổinguyênliệu/unit; giữinput khi lượngsai/trùngNVL; xóa dòngđangedit/dòngmới/hàngcuối vàhủy nháp. STOREchỉđọc, nútxóaẩn/disabled vàserviceguardkhôngđổi. Kiểm celltrống/selected/editing/dropdown khôngtrắng/clipping, keyboardTab/Delete, resize/DPI theo matrixtask. Đối chiếuSQL2recipe chínhxác, khôngtrừkho, receiptmộtlần, snapshotcũkhôngđổi; unit/build theoảnhhưởng. HelperGUI vẫn cóblocker, không camkết PASSnative từ build/source.

**Rủi ro:** commit sớm phát recursion/dirtyevent hoặc bỏ dòngcuối chưaendEdit; stylechung ảnhhưởnggridkhác; phímDelete xóa nhầmcell/dòng; defaultnewrow lọt request. Giới hạn handler/cellstyle hẹp, kiểm các nhánh trên. Không tự vátrong lượt test; ca không phụthuộc lỗi có thể tiếp tục, M17/M18bậtbán saurecipe đangBLOCKED bởiUIFAIL.

# Đợt bug UI editor công thức — 21/11/2026 — chờ duyệt

**Mức độ P2 (chỉ UI editor công thức, không sai dữ liệu). Trạng thái:** đã điều tra bằng ảnh người dùng + source; **đã áp dụng sửa 21/11/2026, chờ người dùng kiểm native**. Ảnh: `c03f015b-94d1-4983-9422-61865064fe70.png` (dialog Công thức · Cappuccino, 21/11).

**Đã áp dụng:** `ProductsControl.Recipe` (combo `DropDownList` + `DropDownWidth` theo cột, padding/fill weight cột, cột `Delete` icon thùng rác theo dòng + hover đỏ, bỏ nút xóa hàng loạt, bảng nằm trong panel cao theo số dòng tối đa 8 dòng rồi cuộn, dialog co theo nội dung); `OrbitManagementTheme.DialogGrid` thêm nhánh cột tên `Delete` = 44px không padding (cùng quy ước `OrbitDataGridView`). Không đổi SQL/BLL/quyền. Build Debug 0 error (build sang OutDir tạm vì app đang chạy khóa bin).

## Hiện tượng

1. Dòng nguyên liệu đang sửa là combo editable kiểu `DropDown`: chữ "Ca phe hat" nằm trong một ô viền nhỏ (~2/3 bề ngang cột), tách rời nút mũi tên nằm sát mép phải cell, giữa còn khoảng trống. Các dòng không sửa hiển thị chữ + mũi tên sát mép, không cùng hình thức với dòng đang sửa. Danh sách xổ xuống không khớp bề ngang dòng dữ liệu.
2. Không có thao tác xóa từng dòng tại dòng; chỉ có một nút full-width "Xóa dòng đang chọn" nằm dưới bảng.
3. Bố cục: bảng cao cố định 300px nên với 3–4 dòng còn khoảng trống lớn; nút xóa full-width cộng khoảng hở dưới nút tạo thêm vùng trống vô nghĩa trước hàng Lưu/Đối chiếu/Hủy.

## Nguyên nhân (có dẫn chứng source)

1. `ProductsControl.Recipe` dựng cột `DataGridViewComboBoxColumn` và chỉ theme màu khi `EditingControlShowing` ([ProductsControl.cs:48–62](D:/Workspace/Nam_3/HK1/CoffeeManager/src/CafeManager.WinForms/Modules/Menu/ProductsControl.cs)). Control đang sửa giữ `DropDownStyle` mặc định = `DropDown` (cho gõ tự do), nên native combo vẽ thêm ô edit bên trong + nút mũi tên tách rời; không đặt `DropDownWidth` nên popup rộng theo control chứ không theo cột. `DefaultCellStyle` của cột này là style mới rỗng nên mất padding 10px mà các cột khác có qua `OrbitManagementTheme.DialogGrid` → chữ lệch trái so với hàng.
2. Nút xóa hàng loạt tạo thủ công ở [ProductsControl.cs:65–79](D:/Workspace/Nam_3/HK1/CoffeeManager/src/CafeManager.WinForms/Modules/Menu/ProductsControl.cs); bảng không có cột thao tác theo dòng. Codebase đã có mẫu icon thùng rác theo dòng dùng `OrbitManagementTheme.DrawIcon(...,"trash",...)` + hover đỏ ([OrbitDataGridView.cs:345–351](D:/Workspace/Nam_3/HK1/CoffeeManager/src/CafeManager.WinForms/UI/OrbitDataGridView.cs), [OrbitRecordListControl.cs:106–120](D:/Workspace/Nam_3/HK1/CoffeeManager/src/CafeManager.WinForms/UI/OrbitRecordListControl.cs)).
3. Grid đặt `Height=300` cố định trong row Percent, dialog khai báo `Height=620`; `OrbitDialogLayout.FitToContent` dùng chiều cao bảng cố định 390 logical khi body có bảng ([OrbitDialogLayout.cs:104–118](D:/Workspace/Nam_3/HK1/CoffeeManager/src/CafeManager.WinForms/UI/OrbitDialogLayout.cs)) nên không co theo số dòng thực.

## Đợt sửa đề xuất (chỉ trong `ProductsControl.Recipe`, tận dụng helper theme đã có)

1. **Combo khớp dòng:** khi `EditingControlShowing` cho combo, đặt `DropDownStyle=DropDownList` (chỉ chọn từ danh sách, không gõ tự do — `DataError` hiện có vẫn giữ), `FlatStyle=Flat`, `DropDownWidth` theo bề ngang cột; giữ `DrawItem` themed cho danh sách xổ xuống; gán padding 10px + căn trái cho cột Nguyên liệu; cân lại `FillWeight` (Nguyên liệu rộng, Định mức vừa, Đơn vị hẹp, cột xóa cố định 44px).
2. **Thùng rác mỗi dòng:** thêm cột cuối `Delete` không tiêu đề, read-only, không sort; vẽ icon "trash" muted, hover đỏ bằng `OrbitManagementTheme.DrawIcon` (cùng pattern `OrbitDataGridView`); click xóa đúng `DataRowView` của dòng đó (CancelEdit nếu dòng mới, Delete nếu dòng có sẵn), `MarkDirty`, focus lại grid; chỉ ADMIN có cột này. Gỡ toàn bộ nút "Xóa dòng đang chọn". Phím Delete sẵn có vẫn giữ cho ADMIN.
3. **Bố cục:** bỏ `Height=300` và `Height=620`; đặt grid trong panel cao = header + (số dòng + dòng nhập mới) × chiều cao dòng, có trần (khoảng 8 dòng) rồi cuộn; panel không phải field giãn nên dialog tự co theo nội dung, không còn khoảng trống dưới bảng/nút xóa. Vì grid nằm trong panel, tự gọi `dialog.MarkDirty()` ở `CellValueChanged`/xóa dòng để không mất cờ nháp mà `AddField` đang gắn trực tiếp cho grid.

## Regression dự kiến

ADMIN: mở Cappuccino 3 dòng → đổi nguyên liệu cập nhật đơn vị ngay → sửa định mức → xóa dòng giữa / dòng mới / dòng cuối bằng icon → thêm dòng → Lưu rồi mở lại đúng dữ liệu; không ghi DB trước Lưu; đóng khi có nháp vẫn hỏi bỏ nháp. STORE: không có cột xóa, form read-only, không lưu. Combo không còn ô edit lồng, danh sách xổ rộng khớp cột, Tab/Enter/DPI 125% bình thường. Không đổi SQL/BLL/contract.

## Rủi ro

Đổi `DropDownStyle` sang `DropDownList` có thể ảnh hưởng thao tác gõ nhanh của người quen gõ tên nguyên liệu (chấp nhận vì trước đây gõ sai chỉ bị `DataError`); canh chiều cao panel theo DPI cần đo lại ở 125%; click icon trên dòng nhập mới phải hủy dòng nháp chứ không tạo bản ghi rỗng. Chỉ ảnh hưởng editor công thức, không đụng grid màn khác.
