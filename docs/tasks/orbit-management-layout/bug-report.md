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


