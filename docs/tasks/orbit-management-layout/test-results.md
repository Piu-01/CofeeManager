## Kết quả native cuối — 21/10/2026

B03: PASS cho hai nhãn còn lại tại125% trên app thật PID29492, DB test CoffeeManager_Test_G03_20261019_215457_29e552a9_demo. Mở sổ hiện đầy đủ, Sơ đồ hiện đầy đủ; toolbar không wrap/chồng footer sau tăng chiều rộng. Evidence runs/bugfix-b01-b04/native-label-final-materials-0.png và native-label-final-tables-0.png. Tab đầu Danh mục nền tự tải10dòng, không bấm Tải lại: native-label-final-catalogs-first-0.png và .txt (DM001–DM010). Không xóa/đổi kết quả raw probe21PASS/1FAIL; native xác nhận lần quan sát này tải bình thường, không đủ kết luận nguyên nhân timeout harness.

B01/B02/B04 giữ kết quả native PASS ở125% của lượt trước; thay đổi cuối chỉ quy tắc chiều rộng hai nhãn. B01–B04 đã đạt phạm vi regression được quan sát ở125%; không có backend/SQL thay đổi. Source cuối giữ nguyên trong lượt native, đối chiếu source-label-followup.csv:0 thay đổi (source-label-final-audit.json).

Toàn task chưa đánh dấu Hoàn tất: DPI100/150 vàDesigner NOT RUN; SQLfixture hơn50dòng chưa chạy (paging presentation6/6 đãPASS). Các cột phụ dài có ellipsis khi cửa sổ normal được giữ ghi nhận, không chứng nhận pixel-perfect ở mọi viewport. Lịch sử G03 không thay đổi, không commit. App/DBtest còn giữ cho người dùng xem, theo manifest/cleanup có guard; không xóa trong phiên đang mở.
## Cập nhật 21/10/2026 — B03 nhãn 5 ký tự

Đã kết thúc lượt native trước sửa, giữ FAIL Mở sổ/Sơ đồ. Đã sửa đúng OrbitManagementLayout.Button: hai nhãn có icon dùng variant rộng160logical; không đổi nút Sửa/Xóa/Trước/Sau và backend. BuildDebug/Release PASS0warning0error (build-label-debug.log,build-label-followup.log). Probe chỉ đọc hoàn tất22bảnđo (db-read-label-followup.json); đây không thay native visual. Source mới source-label-followup.csv. B01/B02/B04 đã đạt native trong phạm vi125%; B03 chờ kiểm ảnh bản mới. DPI100/150 vàDesigner vẫn NOT RUN. Không ghi toàn bộ task hoàn tất.
## B01–B04 đã được duyệt và áp dụng sửa — 20/10/2026

Đã sửa kích thước/DPI, constraint toolbar/footer, sidebar/tab/palette và thông báo rỗng. Regression kỹ thuật: buildDebug/Release0warning0error,unit20/20,paging6/6,44lượt đọcUIở2kíchthước tại125%đều tảiđược và0action/pager bị cắt. [Biên bản](runs/bugfix-b01-b04/test-results.md). Backend51filekhôngđổi, sourceaudit0thayđổi. **Chưa đóngB01–B04 trước nativevisual cuối; bản mớiPID16912đangchờđăngnhập.** DPI100/150 vàDesigner chưa đủbằngchứng, không tựPASS.

---

## Tiếp tục sau đăng nhập — native GUI, 20/10/2026

Người dùng xác nhận đăng nhập. Computer Use quan sát đúng CoffeeManager, ADMIN; chỉ dùng Sky cho thao tác ngoài process. Không thao tác auth, không ghi dữ liệu và không sửa source.

| Ca | Kết quả | Bằng chứng/giới hạn |
|---|---|---|
| Login xong, Danh mục tab đầu | PASS subcase tải dữ liệu | native-catalogs.txt/ảnh: ADMIN, DM001–DM010, footer1–10; không phải kiểm lại xác thực |
| Các màn theo ảnh | FAIL bố cục native | Đã mở Menu, Nguyên liệu, Khách, Bàn/sơ đồ, Khu vực, Khuyến mãi, Chính sách hạng, Lịch sử điểm và Danh mục; toolbar/pager/nhãn/chrome chưa đạt, B01–B04 |
| Navigation giữa các màn/tab trên | PASS các bước đã thao tác | Breadcrumb/tiêu đề và dữ liệu thay theo màn; chưa thay stress100lần/revoke/logout |
| Cuộn grid Menu | PASS subcase | native-products-scrolled-0.png: SP004–SP981, footer vẫn Trang1/1–11; scrollbar không tải trang khác |
| Resize/phóng lớn | FAIL giữ lỗi layout | native-points-maximized-0.png và native-products-maximized-settled-0.png vẫn cắt footer/nút; không phải chỉ do cửa sổ nhỏ |
| Empty history chưa chọn khách | FAIL trình bày; đúng thông điệp nghiệp vụ | Hướng dẫn đúng nhưng lặp3vùng; B04 |
| DPI100/150 và viewport chính xác1280×720/1366×768 | NOT RUN | Đã quan sát ở125%, cửa sổ thường/phóng lớn. Helper trả ảnh1222×711/1536×816; không gọi là đã kiểm đủ ma trận mục tiêu |
| Keyboard/search nhập liệu | BLOCKED kiểm đầy đủ | Helper accessibility có lúc thiếu/stale tree, không xác minh focus ổn định; không gõ mù. Chưa kết luận bug nhập liệu sản phẩm |
| CRUD/policy/adjust/reconcile smoke qua GUI | BLOCKED bởiB02 | Nút thao tác bị cắt; không gửi ghi mù hoặc thay kiểm GUI bằng lời gọi DAL |
| Designer native | NOT RUN | Chưa có phiên Designer được kiểm trong đợt này; constructorSTA không thay Designer |
| DB fixture>50, timeout/fault UI và 3role đăng nhập/native logout/stress | NOT RUN | Paging synthetic và identityfixture chỉ kiểm phần đã ghi, không thay các ca còn lại |

Ảnh/txt gốc lưu trong runs/acceptance-20261020/native-*.png và native-*.txt. Một số accessibility tree Sky chưa cập nhật ngay sau điều hướng; ảnh có trạng thái đã ổn định được đặt hậu tốsettled khi cần. Không dùng tree cũ để khẳng định dữ liệu màn mới. Source audit cuối0thayđổi.

**Kết luận đợt:** FAIL, chưa nghiệm thu UI. Đã có bằng chứng cho B01–B04; chờ duyệt một đợt sửa và regression. Không tự sửa trong lượt test. App vẫn trên DB test manual, chưa cleanup vì cần phục vụ regression và người dùng đang xem.

---
# Test layout Orbit — tiến độ20/10/2026

**Đợt test đã được người dùng duyệt. Source cố định:** runs/acceptance-20261020/source-frozen.csv. Sourceaudit0fileđổi. Windows/.NETSDK10.0.401, DPI120(125%). DBtest CoffeeManager_Test_G03_20261019_215457_29e552a9_demo, markerđãxácminh, khôngghidữliệu.

| Phạm vi | Kết quả hiện tại | Evidence/giới hạn |
|---|---|---|
| L01 compileDebug/Release | PASS0warning0error | build-debug.log của lượt test; build-release.log của implementation sourcecùnghash |
| Constructor/layoutcontrolSTA | 7/7PASS | constructor-results-v3.json; không phảiDesignerGUI |
| UnitG03/MaterialRules | 20/20PASS | g03-unit.trx/log |
| L03 nút cùngchức năng,cùngDPI | FAIL | Materials110×45vsProducts/Customers88×36; B01 |
| L05 pagingpresentation | 6/6PASS | paging-results.json; dữliệugiảinprocess0/1/49/50/51/101, khôngthaytestfixtureSQL>50 |
| ReadUIthật vớiDBtest vàidentityfixture3role | 22bảnđo:21đọcthànhcông,1CataloginitialFAILraw | db-read-results.json; đoDataSource/bounds/buttons, khônglàlogin/quyềnmutate/visualPASS |
| Catalogtáikiểmđộclập | 7/7tabđọcđược | catalog-recheck.json; tabđầu10dòng, chưađủkếtluậnbugautoload |
| L02/L04nativevisual/keyboard,DPI100/150 vàDesigner | ChờGUI/NOTRUN | Cầnappthật vàDesigner, khôngsuytừharness |
| L06–L10loadingfault/filter/writeCRUD/rolesmutate/logout/stress | Chưakiểmđầyđủ | Readfixturekhôngthaythếsmokeghi, khôngtựPASS |

**Tài nguyên/GUI:** đãđóngphiênappcũđểbuildDebugbằngComputerUse. Phiên29040khôngcòncửasổởlầnquansátkếtiếp; chưacóbằngchứngnguyênnhân, log.NETkhôngtrảcrashCoffeeManagerphùhợp. ĐãmởlạiphiênPID8496, titleCoffeeManager·Đăngnhập, cùngDBtest, manifestmanualcậpnhật. Câu hỏi đăngnhập thủcôngđãgửi; khôngtựthao tácauth. DBgiữđếnhạn21/10theomanifest, chưacleanupvìtestcònđangtiếp.

**Lịch sử harness:** constructor.loglỗicúphápđomới, constructor-v2doubleownership khônghợplệ, constructor-v3hợplệ. unit.log/trx chạytoànprojectthiếuenvSQLfixtures nên63PASS/461FAILcủaguard; khônggọi461bug. DbReadProbe cówarningnullablechỉtrongharness, khôngphảiwarningbuildsảnphẩm. GridẩnTablescủalượttổnghợpkhôngdùngđểkếtluậnbềrộngKhuvực; selectorđãđượcthuhẹpvềtabđangchọntrongharnesssauđó.

**Bước tiếp:** nativeGUI sau đăngnhập, tiếptụccácphầnđộclập; tổnghợpbug/reportcuốiđợt trước xin duyệtdebug. KhôngsửaB01tronglượttest.



## Native regression sau đăng nhập — 20/10/2026

Source giữ cố định trong lượt native, source-final.csv đối chiếu không thay đổi. App thật trên DB test đã nêu; ảnh cửa sổ normal 1222×711 logical, DeviceDpi120 theo phép đo cùng môi trường. Không nhận đây là bằng chứng native100/150%.

- B01 PASS trong phạm vi125%: cùng variant kích thước đã đo, nút chính đầy đủ ở các màn quan sát.
- B02 PASS: toolbar/rowactions/footer nằm ngang, không chồng hoặc mất nút; chuyển map/list bàn được. Cuộn nội dung riêng, footer ngoài vùng cuộn.
- B03 FAIL còn nhãn Mở sổ hiển thị M... (native-fixed-materials-0.png), Sơ đồ hiển thị Sơ... (native-fixed-tables-map-0.png). Sidebar và tab đã hết nhãn cắt/border sáng. Cột trạng thái bàn vẫn rút chữ tại cửa sổ normal; giữ ghi nhận giới hạn chiều rộng.
- B04 PASS: native-fixed-points-0.png chỉ một hướng dẫn ở trung tâm, count Chưa chọn khách hàng, không pager/tổng0 giả.
- Đã xem Customers, Menu, Materials, Tables map/list/Area, Promotions/Policy/Points, Catalogs/Product/Payment. Ảnh native-fixed-* là bằng chứng, không chỉ screenshot harness.

Điều tra B03 còn lại: OrbitManagementLayout.Button dùng Text.Length > 5 để chọn rộng160logical, khiến Mở sổ và Sơ đồ đều5kýtự nhận88logical. Icon và padding vẽ chữ làm thiếu chiều ngang; phép đo control bounds không kiểm được ellipsis bên trong. Hướng sửa trong B03 đã duyệt: xếp hai nhãn này vào variant rộng160, không đổi kích thước Sửa/Xóa/Trước/Sau hoặc logic. Kết thúc lượt test này trước sửa, lưu FAIL; cần lượt source/regression riêng sau sửa.

Probe nhãn mới thực tế21PASS/1FAIL: Catalogs tab Sản phẩm lần tải đầu chưa nhận Rows trong cửa sổ chờ harness, tương tự hiện tượng raw trước đó; chưa kết luận nguyên nhân sản phẩm. Giữ nguyên FAIL, không tính22PASS. Cần xác nhận native tab đầu sau đăng nhập. App test mớiPID29492.

