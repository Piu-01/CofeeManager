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
