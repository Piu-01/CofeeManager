## Kết quả native cuối — 21/10/2026

B03: PASS cho hai nhãn còn lại tại125% trên app thật PID29492, DB test CoffeeManager_Test_G03_20261019_215457_29e552a9_demo. Mở sổ hiện đầy đủ, Sơ đồ hiện đầy đủ; toolbar không wrap/chồng footer sau tăng chiều rộng. Evidence runs/bugfix-b01-b04/native-label-final-materials-0.png và native-label-final-tables-0.png. Tab đầu Danh mục nền tự tải10dòng, không bấm Tải lại: native-label-final-catalogs-first-0.png và .txt (DM001–DM010). Không xóa/đổi kết quả raw probe21PASS/1FAIL; native xác nhận lần quan sát này tải bình thường, không đủ kết luận nguyên nhân timeout harness.

B01/B02/B04 giữ kết quả native PASS ở125% của lượt trước; thay đổi cuối chỉ quy tắc chiều rộng hai nhãn. B01–B04 đã đạt phạm vi regression được quan sát ở125%; không có backend/SQL thay đổi. Source cuối giữ nguyên trong lượt native, đối chiếu source-label-followup.csv:0 thay đổi (source-label-final-audit.json).

Toàn task chưa đánh dấu Hoàn tất: DPI100/150 vàDesigner NOT RUN; SQLfixture hơn50dòng chưa chạy (paging presentation6/6 đãPASS). Các cột phụ dài có ellipsis khi cửa sổ normal được giữ ghi nhận, không chứng nhận pixel-perfect ở mọi viewport. Lịch sử G03 không thay đổi, không commit. App/DBtest còn giữ cho người dùng xem, theo manifest/cleanup có guard; không xóa trong phiên đang mở.
## Cập nhật 21/10/2026 — B03 nhãn 5 ký tự

Đã kết thúc lượt native trước sửa, giữ FAIL Mở sổ/Sơ đồ. Đã sửa đúng OrbitManagementLayout.Button: hai nhãn có icon dùng variant rộng160logical; không đổi nút Sửa/Xóa/Trước/Sau và backend. BuildDebug/Release PASS0warning0error (build-label-debug.log,build-label-followup.log). Probe chỉ đọc hoàn tất22bảnđo (db-read-label-followup.json); đây không thay native visual. Source mới source-label-followup.csv. B01/B02/B04 đã đạt native trong phạm vi125%; B03 chờ kiểm ảnh bản mới. DPI100/150 vàDesigner vẫn NOT RUN. Không ghi toàn bộ task hoàn tất.
## B01–B04 đã được duyệt và áp dụng sửa — 20/10/2026

Đã sửa kích thước/DPI, constraint toolbar/footer, sidebar/tab/palette và thông báo rỗng. Regression kỹ thuật: buildDebug/Release0warning0error,unit20/20,paging6/6,44lượt đọcUIở2kíchthước tại125%đều tảiđược và0action/pager bị cắt. [Biên bản](runs/bugfix-b01-b04/test-results.md). Backend51filekhôngđổi, sourceaudit0thayđổi. **Chưa đóngB01–B04 trước nativevisual cuối; bản mớiPID16912đangchờđăngnhập.** DPI100/150 vàDesigner chưa đủbằngchứng, không tựPASS.

---

## Kết luận nghiệm thu UI20/10/2026 — FAIL

Đã kiểm native sau khi người dùng đăng nhập. Bố cục chưa đạt ảnh: kích thước nút lệchDPI; toolbar/pager bị cắt; nhãn/sidebar/tab/chrome chưa đúng; empty state lặp. [Đợt bugB01–B04](bug-report.md), [biên bản](test-results.md). Build/unit20/constructor7/pagingpresentation6đạt không thay thế kết quả visualFAIL. Các ca phụ thuộc nút bị cắt ghiBLOCKED, ma trậnDPI/Designer/phần chưa chạy giữNOTRUN. Source vẫn cố định, chưa sửa. Chờ duyệt debug/regression cả đợt.

## Đợt test20/10đang tiến hành — cóFAIL

Đãduyệttest. BuildDebug/Releaseđạt;constructor7/7,unitG0320/20,pagingpresentation6/6PASS. Pháthiện [B01 kích thước nút theoDPI](bug-report.md); chưa sửa source. ReadUIthật3role đã cóevidence, nativeGUI/Designer/chức năngghi chưađầyđủ. Xem [test-results.md](test-results.md). AppPID8496chờđăngnhập trênDBtest cũ; sourceaudit0thayđổi. Các nhãn chờduyệttestbêndưới làlịchsử.

# Bàn giao triển khai bố cục quản lý Orbit — 20/10/2026

**Trạng thái:** Requirements và plan đã được người dùng duyệt; đã triển khai, chờ duyệt đợt test riêng. Chưa nghiệm thu giao diện theo ảnh, chưa xác nhận PASS luồng hoặc DPI từ build.

## Đã triển khai

- MainForm: sidebar200 logicalpx, toàn bộ mục có icon theoDPI, chữ căn trái; navigation active/các mục theo role và đăng xuất giữ nguyên. Topbar tách breadcrumb và danh tính session; bố cục hẹp giữ cơ chế compact có điều chỉnh breakpoint.
- OrbitManagementLayout/RecordListControl: header + hành động chính; hàng search/filter; toolbar thao tác dòng; vùng dữ liệu co giãn và footer pager riêng. Cột tên/lý do hoặc cột đại diện nhận phần rộng còn lại, cột số căn phải; chưa có total thì không hiển thị tổng trang giả.
- Nút chính200×40, CRUDngắn88×36, nhãn dài160×36 logicalpx; icon milk20/24/32 embedded, primary icon đổi sang màu chữ tối theo palette. Cùng chức năng dùng cùng token ở các màn. Quiet/AlignTextLeft củaOrbitButton là opt-in, default cũ giữ nguyên.
- Menu/khách hàng áp dụng qua RecordListControl; nguyên liệu có layout tương ứng và vẫn giữ mốc mở sổ/N4/quyền hiện có.
- OrbitManagementTabs: header/hành động chính ở trước tab; tab nhẹ theo ảnh áp dụng Bàn/Khuvực và Ưuđãi. Danh sách/Sơđồ bàn chiếm chung một vùng, card chọn đồng bộ dònggrid, giữ usecase chọn bàn hiện có.
- Policy không pager giả, lịch sử điểm có hướng dẫn chọn khách và vùng rỗng; không tạo truy vấn giả hoặc dữ liệu giả.
- Catalog giữ Kind/compact picker/reconcile/editor/guard hiện có, chuyển controls sang khung danh sách mới; không đổi handlerCRUD.
- Phân trang vẫn50dòng, footer báo TrangN/khoảng dòng đã tải; khóa Trước ở đầu, Sau khi lô<50. Khi đủ50 backend chưa biết có trang sau; giữ đường tải tiếp theo contract. Cuộn chỉ xem các dòng trong vùng dữ liệu.

## Kiểm chứng triển khai

| Phần | Kết quả | Bằng chứng |
|---|---|---|
| Release compile cuối | PASS:0warning/0error | runs/implementation/build-release.log |
| Hash backend so mốc trước đợt UI | 51file so sánh,0thayđổi | runs/implementation/backend-audit.json |
| Source bàn giao | Đã lưu hash sau triển khai | runs/implementation/source-final.csv |
| Native GUI/Designer/ảnh/DPI/keyboard/paging/smoke quyềnCRUD | NOT RUN — chờ duyệt đợt test | test-request.md |

Build trong quá trình triển khai chỉ kiểm compile, không là đợt nghiệm thu đã chạy. Không thay Business/DataAccess/Models/SQL/migrations; không ghiDB hoặc đổi dữ liệu development. Không restart phiên test đang mở/không tự đăng nhập, không commit/push.

## Phần cần xác minh trong test

Layout dùng chung nhiều màn, tab reparentheader và Catalogcompact cần kiểm lifecycle/selection/querycancel. Cần đo kích thước nút cùng chức năng và so từng màn với10ảnh ở1280×720,1366×768,DPI100/125/150. Rà native keyboard/scroll/pager/hơn50dòng,3role và các usecase đang nối. Nếu thiếu quyền/công cụ ghi đúngNOT RUN/BLOCKED; không tự PASS từ dữ liệu G03 cũ. Không ghi đè quyết định nghiệm thu G03.




## Native regression sau đăng nhập — 20/10/2026

Source giữ cố định trong lượt native, source-final.csv đối chiếu không thay đổi. App thật trên DB test đã nêu; ảnh cửa sổ normal 1222×711 logical, DeviceDpi120 theo phép đo cùng môi trường. Không nhận đây là bằng chứng native100/150%.

- B01 PASS trong phạm vi125%: cùng variant kích thước đã đo, nút chính đầy đủ ở các màn quan sát.
- B02 PASS: toolbar/rowactions/footer nằm ngang, không chồng hoặc mất nút; chuyển map/list bàn được. Cuộn nội dung riêng, footer ngoài vùng cuộn.
- B03 FAIL còn nhãn Mở sổ hiển thị M... (native-fixed-materials-0.png), Sơ đồ hiển thị Sơ... (native-fixed-tables-map-0.png). Sidebar và tab đã hết nhãn cắt/border sáng. Cột trạng thái bàn vẫn rút chữ tại cửa sổ normal; giữ ghi nhận giới hạn chiều rộng.
- B04 PASS: native-fixed-points-0.png chỉ một hướng dẫn ở trung tâm, count Chưa chọn khách hàng, không pager/tổng0 giả.
- Đã xem Customers, Menu, Materials, Tables map/list/Area, Promotions/Policy/Points, Catalogs/Product/Payment. Ảnh native-fixed-* là bằng chứng, không chỉ screenshot harness.

Điều tra B03 còn lại: OrbitManagementLayout.Button dùng Text.Length > 5 để chọn rộng160logical, khiến Mở sổ và Sơ đồ đều5kýtự nhận88logical. Icon và padding vẽ chữ làm thiếu chiều ngang; phép đo control bounds không kiểm được ellipsis bên trong. Hướng sửa trong B03 đã duyệt: xếp hai nhãn này vào variant rộng160, không đổi kích thước Sửa/Xóa/Trước/Sau hoặc logic. Kết thúc lượt test này trước sửa, lưu FAIL; cần lượt source/regression riêng sau sửa.

Probe nhãn mới thực tế21PASS/1FAIL: Catalogs tab Sản phẩm lần tải đầu chưa nhận Rows trong cửa sổ chờ harness, tương tự hiện tượng raw trước đó; chưa kết luận nguyên nhân sản phẩm. Giữ nguyên FAIL, không tính22PASS. Cần xác nhận native tab đầu sau đăng nhập. App test mớiPID29492.

