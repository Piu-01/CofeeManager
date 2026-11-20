## Xác nhận của người dùng — 20/10/2026

Người dùng phản hồi sau khi nhận bản sửa: “ok test vậy mình thấy đủ ok”. Ghi nhận chấp nhận giao diện lịch/nút lịch bản mới trong phiên manual hiện tại, kết hợp bằng chứng bounds125% đã PASS. Phần lỗi visual B04 được người dùng chấp nhận đóng. Không suy rộng phản hồi này thành đã chạy bàn phím, Designer hoặc DPI100/150%; các ca đó giữ NOT RUN. Không thay đổi kết quả nghiệm thu toàn nhóm G03 hay các ca nghiệp vụ còn lại. Không sửa source hoặc dữ liệu DB trong lượt cập nhật này.
## 20/10/2026 — B04 sửa bổ sung sau duyệt

Popup đã đồng bộ theo native size; icon lịch thay nút chữ bị cắt. Build Debug/Release đạt;20unit+11logicngày+2bounds125%PASS. Native visual/keyboard editor và DPI100/150% chưa nghiệm thu. B04 chưa đóng. Biên bản: runs/g03-b04-followup/test-results.md (tính từ nhóm). Phiên test mới PID9684, cùng DB test, chờ đăng nhập; không ghi dữ liệu khuyến mãi.

# Kiểm GUI có hướng dẫn — 19/10/2026

## M36 — khuyến mãi lưu0% khác kịch bản, báoUIchọnngày — 20/10

Người dùngxácnhậnđãlưuKMvàyêucầusửachọnngàytrắng/lệchtheme. [M36-db.json](M36-db.json): KM20261020/Kiểm thử giảm 20%/NgayBD20/10/2026/NgayKT21/10/2026/PhanTramGiam0/receipt1. ChưaPASSrate20; cầnxácnhậnđãnhập20haygiữdefault0, khôngtựquykếtappbug/sửadata. [G03-B04](../../t13-uu-dai-diem/bug-report.md) đangđiềutraphầnôdatehaypopupcalendar, chưasửasource, chưatiếppreviewđòi20%trướckhilàm rõ.

## M35 — PASS xóa khách chưa có nguồn — 20/10

Người dùngxácnhậnđãxóaKH012. [M35-db.json](M35-db.json) KH012count0, giữCustomerCreate/CustomerDelete, KH011cònđúngtên/300/BAC. PASSnhánhxóakháchchưamốcT12-K07, khôngảnhhưởngkháchkhác; khôngđóngmọicaseT12chưa chạy.

M36 hướngdẫnƯuđãi/Điểm→Khuyếnmãi→ThêmKM20261020/Kiểm thử giảm 20%/từngày20/10/2026đến21/10/2026/giảm20→Lưu. MãđãkiểmDBchưadùng, fixturekhôngảnhhưởngdevelopment. Dialogdàicầncuộnvùnginputđểthấyphầntrăm; chờactual/SQL, chưaPASSM36.

## M34 — PASS GUI chặn xóa khách có mốc — 20/10

[M34-db.json](M34-db.json) KH011vẫncòn/đúngtên/300/BAC/mốc1/receipt4nhưtrước. PASSnhánhchặnxóakháchcómốcT12-K07; khôngsuyPASSmỗiloạiFKhistory/order từca này.

ẢnhXóakháchKH011cóthôngbáoràngbuộcchung “Dữ liệu không thỏa ràng buộc hoặc không đủ tồn; kiểm tra nguồn liên quan.”, dialogcòn. SQLM34-db.json kiểmkhách/điểm/hạng/mốc/receipt, khôngghi nhậnthôngbáocụthểvềmốc đãhiện. M35hướngdẫnHủy→chọnKH012(kháchNULLSDT/chưamởsổ)→Xóa→kiểmđúngmãLưu/xácnhận; expectedxóađược/giữreceipt, KH011khôngảnhhưởng. Chờactual, chưaPASSM35.

## M33 — PASS sửa hồ sơ khi có300điểm — 20/10

[M33-db.json](M33-db.json): KH011đúngtênmới/SDT0900000981, giữ300/BAC, OpeningPoints300/nguồn/KindManual/mốc1, receipt4tăng1profileupdate. PASSnhánhprofilekhôngđổisốdưkhicóđiểmT12-K01.

Người dùngxácnhậnđãlưutênKhách kiểm thử sau mở sổ. SQLM33-db.json kiểmtên/SDT/điểm/hạng/mốc/receipt. M34hướngdẫnchọnKH011→Xóa→đúngmãLưu/xácnhận. Expectedkhôngxóakháchđãcómốc, giữ300/BAC/mốc1/receipt4. Chờactual, chưaPASSM34.

## M32 — PASS GUI chặn mở sổ điểm lần2 — 20/10

[M32-db.json](M32-db.json): KH011vẫn300/BAC/OpeningCount1/ReceiptCount3nhưM31. PASSnhánhmanualchặnmởsổlại/giữinputT12-K05; khôngtựtickcáccađồngthờitừGUIbước này.

ẢnhKH011300/nguồnKiểm thử mở sổ điểm, appbáo “Khách đã có mốc hoặc giao dịch; không thể mở sổ lại.”, dialog/inputgiữ. SQLM32-db.json kiểmsốdư/mốc/receipt. M33hướngdẫnHủy/bỏnháp→chọnKH011→Sửa, chỉ đổitênKhách kiểm thử sau mở sổ→Lưu. Expectedgiữ300/BAC/SDT0900000981/mốc1, receiptUpdate1lần. ChờactualM33, chưaPASSM33.

## M31 — PASS mở sổ300điểm và hạngBAC — 20/10

[M31-db.json](M31-db.json): điểm/OpeningPoints300, BACngưỡng300/giảm3, nguồnđúngKiểm thử mở sổ điểm/Manual/Actor1/mốc1/receipt3(create+profile+open). PASSnhánhmởsổvàđúngngưỡng300T12-K04/K05; khôngtựtickcácngưỡngkhác/rollbackfault từGUIbước này.

ẢnhngườidùngKH011300điểm/BAC/3%. SQLM31-db.json đối chiếuđiểm/hạng/ngưỡng/mốc/nguồn/receipt. M32hướngdẫnchínhKH011→Mởsổđiểm lại300/nguồnKiểm thử mở sổ điểm→Lưu/xácnhận. Expectedtừchốiđãcómốc, giữinput; DB300/BAC/mốc1/receipt3khôngđổi. Chờactual, chưaPASSM32.

## M30 — PASS GUI chặn SDT trùng — 20/10

[M30-db.json](M30-db.json): KH012vẫnSDTNULL/điểm0/DONG/receipt1; KH011giữ0900000981/0/DONG/receipt2. Khôngghi thêm. PASSnhánhtrùngSDTthậtT12-K02, khôngđóngmọicaseGUIchung.

ẢnhKH012nhập0900000981báo “Mã hoặc dữ liệu duy nhất đã tồn tại; tải lại để đối chiếu.” Giữinput/dialog, khôngcoiđãhiệnthôngbáoSDTcụthể. SQLM30-db.json kiểmcảKH011/KH012/receipt. M31hướngdẫnHủy/bỏnháp→chọnKH011→Mởsổđiểm, điểm300, nguồnKiểm thử mở sổ điểm→Lưu/kiểmxácnhậnđúngKH011/300rồiOK. Expectedđiểm300/hạngBACtheopolicyseed/ngưỡng300; chờactualvàSQL, chưaPASSM31.

## M29 — PASS sửa hồ sơ KH011 — 20/10

[M29-db.json](M29-db.json) KH011đúngtênđổivàSDT0900000981giữ0đầu, điểm0/DONGgiữ, receipt2(create+update); KH012khôngđổiNULL/0/DONG/receipt1. PASSnhánhprofilekhôngsửasốdưT12-K01/giữ0đầuT12-K02.

Người dùngxácnhậnđãLưu; SQLM29-db.json kiểm tên/SDT/điểm/hạng/receiptKH011vàKH012đốichứng. M30hướngdẫnchọnKH012→Sửa, giữtênvàđiềnSDT0900000981(đãthuộcKH011)→Lưu. ExpectedtừchốitrùngSDT, giữinput; DBKH012vẫnNULL/0/DONG/receipt1vàKH011khôngđổi. Chờactual, chưaPASSM30.

## M28 — PASS nhiều khách có SDT NULL — 20/10

Người dùngxácnhậnđãlưukháchthứhai. [M28-db.json](M28-db.json): KH011vàKH012mãkhác, tênđúng, cảhaiPhoneIsNulltrue/điểm0/DONG/receipt1mỗikhách. PASSnhánh2NULL T12-K02, khôngsuyPASStrùngSDTthậtchưachạy.

M29 hướngdẫnchọnKH011→Sửa, tênKhách kiểm thử GUI đổi tên, SDT0900000981→Lưu. SDTfixtuređãđọcDBchưadùng. Expectedgiữ0đầu, điểm0/DONGkhôngđổi; chờactual/SQL, chưaPASSM29.

## M27 — tạo khách KH011 qua GUI — 20/10

**M27PASS:** [M27-db.json](M27-db.json) KH011đúngtên, PhoneIsNulltrue, điểm0, hạngDONGngưỡng0, receipt1. PASScreatekhách/NULL/hạngkhởiđầu; chưađủđóngT12-K01/K02vìcònprofileupdate/2NULL/trùngSDT.

Ảnh người dùngKháchhàngcótrạngtháiĐãlưuKH011vàdòngKH011/Khách kiểm thử GUI/SDTtrống/điểm0/DONG/ưuđãi0. SQLđốichiếuM27-db.json xácnhậnNULLvàngưỡnghạng. Người dùnghỏimáynàymìnhcóthểlàmkhông: giải thích giớihạn ởinputmodal helperđãcóreproducer, khôngnóikhôngtruycậpđượcmáyhoặckhônglàmđượcSQL/code. KhôngtựđóngGUI/PIDđangkiểmtay.

## M26 — PASS xóa khu vực đã hết bàn — 20/10

Người dùngxácnhậnđãxóaKV981. [M26-db.json](M26-db.json): AreaCount0/AreaTableCount0, giữAreaCreate/AreaDelete. PASSnhánhxóakhuvựckhôngcònbàn, kếtthúcluồngCRUDfixtureB0981/KV981; khôngđóngT11vìcòncaquyền/trạngthái/refresh/DPIkhác.

M27hướngdẫnKháchhàng→Thêm, tênKhách kiểm thử GUI, SDTđểtrống→Lưu. Mãkháchtựsinh, khôngnhậpmã/điểm/hạng. Expectedđiểm0/hạngngưỡng0/PhoneNULL. Chờmãhoặcảnhkếtquảđểđốichiếuđúngentity, chưaPASSM27.

## M25 — PASS xóa bàn chưa có lịch sử — 20/10

Người dùngxácnhậnđãxóabàn. [M25-db.json](M25-db.json): B0981TableCount0/ViewCount0; KV981còn1, AreaTableCount0; giữ3receiptTableCreate/TableUpdate/TableDelete. PASSnhánhxóabànchưadùngT11-K06, khôngsuyPASSxóabàncólịchsử.

M26hướngdẫnTabKhuvực→chọnKV981→Xóa→đúngmãLưu/xácnhận. Expectedxóađượckhuvựcđãhếtbàn, khôngảnhhưởngKVkhác, receiptAreaDeletegiữlại. Chờactual, chưaPASSM26.

## M24 — PASS chặn xóa khu vực có bàn — 20/10

[M24-db.json](M24-db.json): KV981vẫn còn, B0981vẫnthuộcKV981, receiptarea1/bàn2nhưtrước. PASSnhánhkhóaxóaFKT11-K06; thôngbáochungchưađượcđánhgiávisual/UXđạttoànbộ.

Ảnh người dùngXóaKV981báo “Dữ liệu không thỏa ràng buộc hoặc không đủ tồn; kiểm tra nguồn liên quan.” Đây làthôngbáoràngbuộcchung, khôngghi nhận appđãhiệnthôngbáocụthể“cònbàn”. DBđốichiếuM24-db.json kiểmkhuvực/bàn/receiptgiữnguyên. M25hướngdẫnHủy→tabBàn/Sơđồ→chọnB0981→Xóa→đúngmãLưu/xácnhận. Expectedxóađượcbànchưacólịchsử; KV981cònvàkhôngcònbàn, receiptbànDeleteđượcgiữ. Chờactual, chưaPASSM25.

## M22 input được xác nhận; M23 sửa khu vực — 20/10

**M23PASS:** [M23-db.json](M23-db.json) B0981đúngKV981/Khu kiểm thử GUI, tênbàngiữ, viewTrong/khôngđơn, AreaTableCount1/ReceiptCount2(create+update). PASSnhánhchuyểnquan hệbànkhuvực, chưađủreload/restart/cảma trậnT11-K01.

Người dùng xác nhậnM22đãgiữmặcđịnhTrongnha/KV001, nênDBkhớpinputthực, khôngcóbằngchứngmappingbug. SauđóxácnhậnđãSửaB0981sangKhu kiểm thử GUI. SQLđối chiếuM23-db.json kiểmMaKV/quan hệ/view/receipt. M24hướngdẫnchuyểntabKhuvực→chọnKV981→Xóa→kiểmđúngmãLưu/xácnhận; expectedbịtừchốiKVđangcóbànB0981, giữquan hệ/lịch sử. Chờactual, chưaPASSM24.

## M22 — bàn đã tạo nhưng khu vực khác kịch bản — 20/10

Người dùngxácnhậnđãtạobàn. [M22-db.json](M22-db.json): B0981/Bàn kiểm thử GUI, MaKV=KV001/Trong nha, viewTrong/khôngđơn, receiptTableCreate1lần. KịchbảngợiýKV981/Khu kiểm thử GUI nênchưaPASSquan hệđúnginput. CầnxácnhậnngườidùngđãchọnKV981haygiữmặcđịnhTrongnha; khôngtựsửadữliệu hoặcquykếtmappingbug. CodeTableList.Editlấyarea.SelectedValuekhiCaptureWrite, mặcđịnhDataSourcechọndòngđầu; chưađủbằngchứngbugapp. Phầnentitycreate/viewTrongcóbằngchứng, quan hệđangchờxácnhận.

## M21 — PASS tạo khu vực KV981 — 20/10

[M21-db.json](M21-db.json): KV981/Khu kiểm thử GUI/TableCount0/ReceiptCount1khớpxácnhậnngườidùng. PASSbướctạoareaT11-K01, chưađủcảCRUD/reload.

Người dùng xácnhậnđãtạokhuvực; SQLđối chiếuM21-db.json đểxácnhậnmã/tên/receipt. M22hướngdẫn tabBàn/Sơđồ→ThêmB0981/Bàn kiểm thử GUI/chọnKhu kiểm thử GUI→Lưu. NếulookupchưahiệnKV981, tải lạihoặcđổimànrồiquaylại; ghi actual lookup nếuphải làm thêm, khôngsuyPASStựrefresh. ExpectedB0981liênkếtKV981/trạngtháiTrống/khôngđơn. Chờactual/SQL, chưaPASSM22.

## M20 — PASS chặn nguyên liệu trùng — 20/10

Người dùng báo “Nguyên liệu bị trùng”. [M20-db.json](M20-db.json): SP981Dangban, recipegiữNVL01=0.0200/NVL02=0.0180, receipt3khôngđổi. PASSnhánhvalidationtrùngNVL/khôngghipayloadT10-K02; khôngcóảnhmới, dựaxácnhậnngườidùng+SQL. Các boundary/role/visualkhácchưachạygiữriêng, khôngđóngT10.

M21 hướngdẫnHủy/bỏnháp→Bàn/Khuvực→tabKhuvực→ThêmKV981/Khu kiểm thử GUI→Lưu. PreflightKV981/B0981đềuchưatồn tại. Chờactual/SQL, chưaPASSM21. T09/T10cáccachungcònthiếuvẫngiữchưanghiệmthu.

## M19 — PASS từ chối định mức0, giữ công thức cũ — 20/10

Ảnh người dùngeditorSP981cóNVL01lượng0/NVL02=0,018, báo “Định mức phải dương.” vàgiữinput. [M19-db.json](M19-db.json) recipeNVL01=0.0200/NVL02=0.0180, receipt3, stock96.7400/98.1600khôngđổi. PASSnhánhGUIvalidation lượng0/khôngghicảpayloadT10-K02; khônggọiđâylàenginefaultrollback vìBLLchặntrướcghi. SQLfaultrollbackT10-K03cóevidenceđợttrước.

M20hướngdẫnHủy/bỏnháp→mởCôngthứclạiSP981→thêmdòngCa phe hatNVL01lượng0,0100(giữ2dòngcũ)→Tab/Lưu. Expectedtừchốitrùngnguyênliệu, DBgiữ2dòng/receipt3. Chờactual, chưaPASSM20. ĐánhgiávisualB03vẫntạmgáctheoýngườidùng.

## Xác nhận input M17 và M18 bật bán — 20/10

**M18PASS:** [M18-db.json](M18-db.json) SP981Dangban/25000, recipegiữNVL01=0.0200/NVL02=0.0180, tồn96.7400/98.1600khôngđổi, receiptProductUpdate1lần (tổng3). PASSnhánhbậtbánkhiđủrecipe; khôngnghiệmthuPOS/checkoutG04.

Người dùng xác nhận đãnhậpngượclượngso kịchbảngốc: NVL01=0,0200/NVL02=0,0180. VìvậyDBsauLưukhớpinputthực, khôngcóbằngchứngappđảolượng. Ghi khácbiệt fixture rõràng, khôngsửaexpectedđểnébug; nhánhmappingđúnginputPASS theo xácnhận+SQL. Các visual/unitimmediate/xóadòng chưađủxácnhận vẫnchưađóngB03.

Người dùng xácnhậnđãbậtbánSP981. SQLđốichiếuM18-db.json đểghi actual trạngthái/côngthức/tồn/receipt. M19kếtiếp: mởCôngthứcSP981chỉđọckiểm2dòngvừaLưu, sửađịnhmứcNVL01thành0vàLưu; expectedtừchốicảpayload, DBgiữ0.0200/0.0180vàkhôngtăngreceipt. ChờactualM19, chưakếtluận.

## Sau sửaB03 — người dùng đãLưu, tạm gác đánh giá UI

Người dùng xác nhận đãLưu và yêu cầukiểmtiếp, tạm gácgiao diện. [SQLsauLưu](../g03-b03-fix/db-after-manual-save.json) có2dòngNVL01=0.0200Kg,NVL02=0.0180Lit; khácgiátrịkịchbảngốcNVL01=0.0180/NVL02=0.0200. Chưa biết doinputmanualkháchayappgánnhầm, cầnxácnhận input trướcghiPASSđúngpayload hoặcbug mới; khôngsửadữliệu/expectedđể né.

ĐãchứngminhLưurecipequaappcóhiệuứng: mộtProductReplaceRecipereceipt ngoàiProductCreate; khôngtrừkho(NVL01=96.7400,NVL02=98.1600đúngbaseline),SP981vẫnNgungban/25000. ChỉPASSnhánhkhôngtrừtồn/lưumộtlần, chưaPASSmappingquantityđúnginput. B03phầnnativevisual/unitimmediate/xóadòng/DPIchưađủxácnhận, giữchưađóngtheoyêucầugácUI, khôngtựcoiđạt.

## M17 — FAIL phần UX editor, lưu công thức NOT RUN — 20/10

Người dùng báo4nhómlỗi vàgửi4ảnh: combo trắng/lệchmàu/cảmgiáctràn, unitcậpnhậtchậm, ôlượngkhó nhậnbiếteditable, thiếuthaotácxóadòngnhầm. Đãđốichiếusource vàDB: SP981chưacórecipe/receipt1. [G03-B03](../../t10-san-pham-cong-thuc/bug-report.md) gộp hướngsửa/regression, chờduyệtđợtbug; chưasửa code. M17GUIUXFAIL; phầnLưu2dòngchưachạy, ca bậtbán saurecipeBLOCKED. Khôngthêmchỉdẫn workaroundDelete đểcoiUXPASS. Các bướctrướcPASSgiữ nguyên.

## M16 — PASS chặn bật bán thiếu công thức — 20/10

Người dùng banđầu không thấyTrạngthái do vùnginputcuộn; đãhướngdẫncuộnxuống, ảnhsau cho thấyĐangbánvàthông báo “Món cần công thức trước bật bán; sản phẩm có lịch sử không thể xóa.” Inputgiữ/dialogcòn. [M16-db.json](M16-db.json) xácnhậnSP981vẫnNgungban/25000/recipe0/receipt1. PASSnhánhGUIguard thiếu recipeT10-K04; không suyPASScheckoutG04hoặc mọi layout/DPI.

M17 hướngdẫnHủy/bỏnháp, chọnSP981→Côngthức, thêm2dòngNVL01(Ca phe hat)0,0180Kg vàNVL02(Sua tuoi)0,0200Lit; đơnvịtựlấy, bấmTabracellđểchốt rồiLưu. Expected2dòngđúngquantity/unit, trạngtháivẫnNgungban, khôngtrừkho. Chờactual/SQL; chưaPASSM17.

## M15 — PASS tạo sản phẩm ngừng bán — 20/10/2026

Ảnh người dùng hiển thịSP981/Cà phê thử GUI/DM001/25.000₫/Ngừng bán. [M15-db.json](M15-db.json) đúngmarker: GiaBan25000/TrangThaiNgung ban/RecipeCount0/ReceiptCount1. PASS bướcmetadata createADMIN củaT10-K01; chưa đủ toànca thêmnhiềudòngcôngthức.

M16 hướng dẫnchọnSP981→Sửa→chỉ đổiTrạng tháiĐangbán→Lưu. Expectedtừchối vìchưacôngthức, giữinput; DBvẫnNgungban/recipe0/receipt1. Chờactual, chưaPASSM16. Khôngchạythanh toán/POSG04chưa triểnkhai.

## M14 — PASS xóa nguyên liệu chưa dùng

Người dùng xác nhậnđãxóa. [M14-db.json](M14-db.json): NVL83entity0/mốc0, receipt3giữlại MaterialCreate/MaterialUpdate/MaterialDelete. PASSxóachưadùng/giữreceipt nhánhT09-K08. Không đóngT09: cònrole/timeout/stale/refresh/DPI/Designer/cácboundarychưađủ. LuồngcơbảnM01–M14có kết quả từngbước; M09vẫnchờxácnhậnclickLưu.

M15 chuẩn bịluồngmenuT10: đãđọccontrol và kiểmSP981chưatồntại. Hướngdẫn Menu/Công thức→Thêm SP981/Cà phê thử GUI/danhmụcCà phê/giá25000/Ngừngbán→Lưu, chưabậtbán/chưacôngthức. Chờactual vàSQL; khôngPASSM15trướckếtquả. Phầnrole/regressionchungT09vẫnđểchưa chạy, không bỏchecklist.

## M13 — PASS đổi đơn vị trước phát sinh

Người dùng xác nhận đãđổi. [M13-db.json](M13-db.json): NVL83đơnvịg,tồn0.0000,mốc0,receipt2(create+update). PASSnhánhGUIđổiđơnvịkhi chưa có nguồn thuộcT09-K06, dựa xác nhận thao tác+SQL; không có ảnhmới.

M14 hướng dẫnchọnNVL83→Xóa, kiểm đúngmã→Lưu/xácnhận. Expectedxóađược vìchưa dùng, entity/mốc0 nhưngreceiptdeletegiữlạitheocontract; agentđốichiếuSQLsauphảnhồi. Chờactual, chưaPASSM14.

## M12 — PASS lưu lại sau validation đơn vị trống

Người dùng xác nhậnđãlưu. [M12-db.json](M12-db.json) kiểm đúngmarker: NVL83/Nguyên liệu kiểm tra đơn vị/Kg/tồn0.0000/mốc0/receipt1. PASSbướctạo sau bổsung đơnvịhợplệ; không giữpendingpayloadđơnvịtrống.

M13 hướng dẫnchọnNVL83→Sửa, chỉ đổiKgthànhg→Lưu, khôngmởsổ. Expectedđổiđược vìchưacómốc/côngthức/giaodịch, tồn0/mốc0; chờactual/SQL, chưaPASSM13.

## M11 — PASS từ chối đơn vị trống

Ảnh người dùng:Thêm nguyên liệuNVL83/tênNguyên liệu kiểm tra đơn vị/đơnvịtrống; lỗi “Đơn vị: bắt buộc, tối đa 20 ký tự, không chứa ký tự điều khiển. (Parameter 'Đơn vị')”, mã/tên giữ nguyên. [M11-db.json](M11-db.json): MaterialCount0/OpeningCount0/ReceiptCount0. PASSvalidationđơnvịtrống nhánhT09-K04; chưa suyPASSmaxđộdài/focus/ErrorProvider riêng.

M12 hướng dẫn cùngdialog bổsungKg→Lưu, không mởsổ. ExpectedNVL83đượctạo,tồn0/mốc0/receipt1; chờactual rồi kiểm đổiđơnvịtrướcphátsinh và xóachưadùng. ChưaPASSM12.

## M10 — PASS ghi chính xác4sốlẻ sau validation

ẢnhNVL82/tồn1,2345/Manual. [M10-db.json](M10-db.json) xác nhậnSoLuongTon=Quantity=1.2345, UnitSnapshotKg, OpeningCount1, ReceiptCount2(create+open). Nguồn thực tếKiểm thử số âm vẫn giữ từM08, khác nhãn gợi ýM10 nhưng hợp lệ và được lưu đúng input; không tự sửa fixture hoặc báo nguồnđúngnhãngợiý. PASSprecision4lẻ và chuyển từpayloadsốâm lỗi sanggiátrịhợp lệ. Chưa có xác nhận riêng người dùng đãbấmLưu khi0,00001 ởM09 nên M09giữchờxácnhận; không suyPASS từ lần thành công này.

M11 hướng dẫnThêmNVL83/TênNguyên liệu kiểm tra đơn vị/đểtrốngĐơn vịchuẩn→Lưu. Expectedtừchối đơnvịtrống, giữinput, DBkhôngcóNVL83/khôngreceipt. Chờactual, chưaPASSM11.

## M09 — đối chiếu lượng5sốlẻ

Ảnh người dùng trả lời bướcM09 cóNVL82/lượng0,00001/thông báo giới hạn4sốlẻ; nguồn thực tế vẫnKiểm thử số âm, khác nhãn nguồn gợi ý nhưng không đổi mục tiêu precision. [M09-db.json](M09-db.json) lưu đối chiếu; tồn0/mốc0/receipt1. Ảnh tĩnh không phân biệt được thông báo mới với thông báo giữ từM08 vì nội dunggiốngnhau; cần người dùng xác nhận đãbấmLưu sau sửa lượng trước kết luậnM09PASS.

M10 kế tiếp khiM09đãbấmLưu: đổi lượng1,2345, nguồnKiểm thử giữ 4 số lẻ→Lưu/xác nhận đúng1,2345Kg/NVL82. Expected ghiđúng1.2345, mốc1/receipt2, không mang pendingpayload lỗiM08/M09. Chờactual; chưaPASSM10.

## M08 — PASS GUI từ chối số âm

[M08-db.json](M08-db.json): tồn0.0000, OpeningCount0, ReceiptCount1 nhưM07; không có ghi mới. PASS nhánh từ chối sốâm/giữinput; chưa kiểmfocus/ErrorProvider gầntrường bằng thao tác riêng.

Ảnh người dùng:NVL82,lượng-0,0001,nguồnKiểm thử số âm; thông báo “Lượng không âm, tối đa 14 số nguyên và 4 số thập phân. (Parameter 'Lượng')”, dialog/input giữ nguyên. SQL đối chiếuM08-db.json. Nhánhvalidation sốâm T09-K04; không suyPASS phầnprecision chưa chạy. ChuỗiParameter là chi tiết kỹ thuật hiển thị hiện có, chưa sửa trong lượt test.

M09 hướng dẫn trong cùngdialog đổi lượngthành0,00001, nguồnKiểm thử quá 4 số lẻ→Lưu. Expectedtừchối quá4sốlẻ, không âm thầm làm tròn/ghi0; giữinput, tồn0/mốc0/receipt1. Chờactual, chưaPASSM09.

## M07 — PASS tạo fixture NVL82

Người dùng xác nhận đãtạoNVL82. [M07-db.json](M07-db.json) kiểm đúngmarker: NVL82/Cà phê kiểm thử chưa mở sổ/Kg/tồn0.0000/mốc0/receipt1. PASSbướctạo theo xác nhận người dùng+SQL; không có ảnhGUI mới cho bước này.

M08 hướng dẫnchọnNVL82→Mởsổ, lượng-0,0001, nguồnKiểm thử số âm→Lưu. Expectedbịvalidationtừchối, giữinput và khôngmốc/khôngghi tồn. Chờactual/ảnh, chưaPASSM08. Quá4sốlẻ kiểm sau đối chiếuM08, không hướng dẫnchốt giá trị hợp lệ trong bước này.

## M06 — PASS chặn xóa nguyên liệu đã mở sổ

Ảnh người dùng: dialogXóa nguyên liệuNVL81, từchối “Đã có mốc mở sổ, công thức hoặc giao dịch; không thể thực hiện thao tác này.” [M06-db.json](M06-db.json) xác nhậnNVL81còn, tênđúng/Kg/tồn0.0180/mốc1/receipt3 giữ nguyên. PASS nhánhGUI xóaNVLđãcómốc thuộcT09-K08, không kết luận xóaNVLchưa dùng hoặc mọiFK từ nhánh này.

M07 hướng dẫnHủy→ThêmNVL82/Cà phê kiểm thử chưa mở sổ/Kg→Lưu. Expectedtồn0/mốc0; dùng kiểmvalidationmởsổ sốâm và quá4sốlẻ. Chờactual, chưaPASSM07.

## M05 — PASS khóa đơn vị sau mở sổ

Ảnh người dùng cho thấy dialogSửa nguyên liệuNVL81, inputđơn vịg còn giữ, báo đãcómốc/côngthức/giaodịch và từchối. [M05-db.json](M05-db.json) xác nhận DBvẫnKg, tồn/Quantity0.0180, UnitSnapshotKg, tên/nguồn/Kindkhôngđổi, mốc1/receipt3 nhưM04. PASS nhánhGUI đổi đơn vị sau mốc thuộcT09-K06; không xem lỗi dự kiến là bug.

M06 hướng dẫnHủy/bỏnháp dialogSửa, chọnNVL81→Xóa, kiểm đúngmã rồiLưu/xác nhận. Expected không xóa được nguyên liệu đã có mốc; NVL81/tồn/mốc/receipt giữ nguyên. Chờactual, chưaPASSM06.

## M04 — PASS sửa tên không đổi tồn

Ảnh người dùng cóNVL81/tênđãđổi(rút gọn bằng…)/Kg/0,0180/Manual. [M04-db.json](M04-db.json) xác nhận tên đầy đủCà phê kiểm thử GUI đổi tên, tồn/mốcQuantity0.0180, UnitSnapshotKg/Source/Kind giữ nguyên, OpeningCount1, ReceiptCount3(create+open+rename). Không mất ký tự tên trongDB. M04PASS nhánhrename thuộcT09-K06.

M05 hướng dẫnNVL81→Sửa→chỉ đổi đơn vịKg thànhg→Lưu. Expected từ chối do đã có mốc; giữinput/dialog; SQL đơn vị vẫnKg, tồn0.0180, mốc1/receipt3. Chờactual, chưaPASSM05.

## M03 — PASS mở sổ lại bị từ chối

[M03-db.json](M03-db.json): tồn0.0180, OpeningCount1, ReceiptCount2 — giữ nguyên so vớiM02. M03PASS nhánhGUIchặn mở sổ lần2/giữinput; không thay trạng thái các subcase khác chưa chạy.

Người dùng gửi ảnh dialogMở sổ nguyên liệuNVL81 với thông báo “Đã có mốc mở sổ, công thức hoặc giao dịch; không thể thực hiện thao tác này.” Lượng hiển thị00,0180 (tương đương0,0180 theo parser), nguồnKiểm thử GUI G03 được giữ; dialog chưa đóng, Lưu/Hủy khả dụng, Đối chiếu disabled. Đây là kết quả lỗi nghiệp vụ dự kiến, không phải bug mới. SQL đối chiếu tạiM03-db.json.

M04 hướng dẫn đóng dialog bằngHủy/xác nhận bỏnháp nếu hỏi, chọnNVL81→Sửa, chỉ đổi tênCà phê kiểm thử GUI đổi tên; giữKg→Lưu. Expected rename thành công, tồn0,0180 và mộtmốc không đổi. Chờactual, chưaPASSM04.

## Kết quả hiện hành M02 — PASS mở sổ0,0180Kg

Người dùng gửi ảnhNVL81/tồn0,0180/mốcManual. [M02-db.json](M02-db.json) đối chiếu đúngmarker: SoLuongTon=Quantity=0.0180, UnitSnapshotKg, SourceKiểm thử GUI G03, KindManual, ActorAccountId1, OpeningCount1. ReceiptCount2 tương ứng mộtcreate và mộtopen; không phải hai lần mở sổ. M02PASS cho lượng/đơn vị và lưu dấu vết; chưa đóng task hoặc thay toàn bộ subcaseT09-K01.

M03 hướng dẫn thử mở sổ lại chínhNVL81 cùng lượng0,0180/nguồnKiểm thử GUI G03. Expected bị từ chối đã mở sổ, giữ dialog/input để xử lý; tồn0,0180, OpeningCount1/ReceiptCount2 không đổi. Chờ thông báo actual, không sửa source trong lượt test.

## Kết quả hiện hành M01 — PASS bước tạo, chờ mở sổ

Người dùng gửi ảnh danh sách cóNVL81/Cà phê kiểm thử GUI/Kg/tồn0,0000, mốc trống. Đối chiếu SQL chỉ đọc đúngDB/marker tại [M01-db.json](M01-db.json): tên/đơn vị đúng, SoLuongTon0.0000, OpeningCount0, ReceiptCount1. M01PASS; đây mới là bước tạo củaT09-K01, chưa PASS toànID vì chưa kiểm4sốlẻ sau mở sổ. Không sửa source/fixture để đạt kết quả.

M02 đã hướng dẫn: chọnNVL81→Mở sổ, lượng0,0180, nguồnKiểm thử GUI G03, bấmLưu và kiểm xác nhận đúng0,0180Kg/NVL81 rồiOK. Chờ actual/ảnh và đối chiếu DB; chưa đánh dấuM02PASS. Các nhãnNOTRUN bên dưới là kế hoạch ban đầu.

Người dùng chọn hướng kiểm tay GUI; agent chuẩn bị fixture/đối chiếu SQL/ghi biên bản. Tiếp tục đợt test G03 đã được duyệt; source cố định, phát hiện bug ghi nhận chờ duyệt trước sửa. Không thay47ID gốc, không tick từ hướng dẫn.

DB baseline demo→009 qua setup chuẩn đã kiểm Completed/admin/runtime; manifest ghi tên+marker+PID, giữ để kiểm qua nhiều lượt đến21/10/2026, cleanup.ps1 kiểm đúng tên/marker/PID path. Không dùng DB development. source-before.csv là snapshot bắt đầu lượt này. Các dữ liệu bên dưới là fixture test, không phải số dư thật.

## Bước M01 — T09-K01: tạo mới

Người dùng đăng nhập ADMIN vào app vừa mở, chọn Nguyên liệu/Tồn→Thêm. Nhập NVL81/Cà phê kiểm thử GUI/Kg→Lưu. Expected danh sách có nguyên liệu mới tồn0, không tự mở sổ. Trạng thái NOT RUN/chờ người dùng; chưa có bằng chứng actual. Sau phản hồi, agent đọc DB tên/đơn vị/tồn/mốc/receipt trước hướng dẫn mở sổ.

## Bước M02 — T09-K01/K05: mở sổ

Chỉ thực hiện sau đối chiếuM01. Chọn NVL81→Mở sổ, lượng0,0180, nguồnKiểm thử GUI G03, xem nội dung xác nhận rồi ghi. Expected0,0180Kg tương ứng18g, một mốc và receipt, không quy đổi ngầm. Trạng thái NOT RUN; chưa hướng dẫn gửi thao tác này cho người dùng.

Kết quả47ID chưa đổi20PASS/27BLOCKED; các ca thủ công sẽ ghi actual từng bước, không báoPASS cảtask từ một luồng.


