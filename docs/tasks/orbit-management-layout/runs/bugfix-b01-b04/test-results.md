# B01–B04 — sửa và regression ngày20/10/2026

**Đã duyệt đợt sửa/regression. Trạng thái: đã áp dụng sửa, chưa đóng bug vì chờ native visual sau đăng nhập.** Không thay backend, SQL hoặc dữ liệu. Source cuối tại source-final.csv; source-audit.json:0thayđổi trong lượt regression cuối; backend-audit.json:51file/0thayđổi.

## Thay đổi

- B01: OrbitManagementLayout giữ token logical cho mỗi nút, chuẩn hóa Size/Margin theo DeviceDpi ở lifecycle và layout; gán idempotent, không nhân tiếp kích thước đã scale. Sửa/Xóa ở các màn đều110×45 và nút chính250×50 tại125%.
- B02: các hàng có cột nhóm nút với chiều rộng tính từ control thật; tính chiều cao khi wrap, pager/primary không wrap. Đặt RowStyle rõ thay vì để childcontainer chiều cao mặc định125px chi phối hàng. Bỏ margin dọc thừa của ô tìm. Footer ở ngoài vùng cuộn.
- B03: sidebar224logical, nút200logical thay168 để giữ font mà không rút nhãn; đồng bộ lại khiresize/DPI. Tab flat phủ cả native tabrect rồi vẽ lại chữ/underline/focus; palette trong khung được áp nhất quán sau layout/theme cha. Cột mã/ngày/đơn vị được dành chiều ngang rõ hơn. Đây là chỉnh trong phạm vi bug đã duyệt, không đổi luồng navigation hoặc dialog.
- B04: hướng dẫn chưa chọn khách chỉ ở vùng rỗng trung tâm; count ghi trạng thái lựa chọn, status ghi Sẵn sàng, không hiển thị pager/tổng0 giả trước chọn khách. Mô tả lịch sử điểm riêng, tiêu đề gọn.

File sản phẩm thay đổi trong đợt: UI/OrbitManagementLayout.cs,UI/OrbitTabControl.cs,UI/RecordListControl.cs,Forms/MainForm.cs,Forms/MainForm.Designer.cs.

## Regression cuối với source cố định

| Ca | Kết quả | Bằng chứng/giới hạn |
|---|---|---|
| BuildDebug/Release | PASS0warning0error | build-debug.log,build-release-final.log |
| UnitG03/MaterialRules | PASS20/20 | unit.log,unit.trx |
| Pagingpresentation0/1/49/50/51/101 | PASS6/6 | paging-results.json,paging.log; fixture UI, không thay SQLfixture>50 |
| ReadUI3role tại client1280×720,DPI120 | PASS22/22bảnđo tải dữ liệu;0action/pagerclipping | db-read-1280.json/log |
| ReadUI3role tại client1366×768,DPI120 | PASS22/22bảnđo tải dữ liệu;0action/pagerclipping | db-read-1366.json/log |
| Đồng nhất nút125% | PASSphép đo | Materials/Products/Customers/Catalogs/Tables/Loyalty: Sửa/Xóa110×45, primary250×50 |
| Cardmap trong vùng cuộn | Không coi card ngoài viewport là actionclipping | Bộ đếm clipping chỉ loại cardbàn có chủ đích cuộn; toolbar/pager phải nằm trọn mọi ancestor |
| DPI100 thử bằng threadawareness | NOT RUN hợp lệ ở100% | db-read-dpi96.json thực tếDeviceDpi120,không96; không giả PASS100%. Rawcó1initialCatalogFAILharness; không dùng lượt này để chứng nhậnDPI hoặc thêmbugautoload |
| DPI150/nativeDesigner | NOT RUN | Chưa có bằng chứng ở môi trường này |
| Nativevisual cuối/sidebar/tab/theme/empty state | Chờ đăng nhập | BảnDebugmớiPID16912,đúngDBtestmanual; Sky thấy CoffeeManager·Đăngnhập. Không điều khiển auth |

Các lượt r1/tables-panels-r1/r2 là đo trong pha debug để xác định sizing, giữ nguyên log và source-r1.csv; không thay kết quả cuối bằng lượt cũ. Harness cówarningnullable riêng, build sản phẩm cuối sạch. BuildDebugđầu bị appcũ8496khóaexe: giữbuild-debug-locked.log. Cửa sổ đã đóng nhưng tiến trình còn chạy; đã kiểm manifest/path và dừng đúngPIDtest rồi buildlại, không dừng appkhác.

**Tài nguyên:** vẫn dùngDBtest CoffeeManager_Test_G03_20261019_215457_29e552a9_demo/marker20261019_215457_29e552a9; mọi probe chỉ đọc. Manifest manual cập nhậtPID16912. Giữ theo hạn21/10đểnative/regression; chưacleanup khi đang cần phiên. Câu hỏi đăng nhập thủ công đã gửi, chưa có xác nhận native cho bản sửa.

## Native regression sau đăng nhập — 20/10/2026

Source giữ cố định trong lượt native, source-final.csv đối chiếu không thay đổi. App thật trên DB test đã nêu; ảnh cửa sổ normal 1222×711 logical, DeviceDpi120 theo phép đo cùng môi trường. Không nhận đây là bằng chứng native100/150%.

- B01 PASS trong phạm vi125%: cùng variant kích thước đã đo, nút chính đầy đủ ở các màn quan sát.
- B02 PASS: toolbar/rowactions/footer nằm ngang, không chồng hoặc mất nút; chuyển map/list bàn được. Cuộn nội dung riêng, footer ngoài vùng cuộn.
- B03 FAIL còn nhãn Mở sổ hiển thị M... (native-fixed-materials-0.png), Sơ đồ hiển thị Sơ... (native-fixed-tables-map-0.png). Sidebar và tab đã hết nhãn cắt/border sáng. Cột trạng thái bàn vẫn rút chữ tại cửa sổ normal; giữ ghi nhận giới hạn chiều rộng.
- B04 PASS: native-fixed-points-0.png chỉ một hướng dẫn ở trung tâm, count Chưa chọn khách hàng, không pager/tổng0 giả.
- Đã xem Customers, Menu, Materials, Tables map/list/Area, Promotions/Policy/Points, Catalogs/Product/Payment. Ảnh native-fixed-* là bằng chứng, không chỉ screenshot harness.

Điều tra B03 còn lại: OrbitManagementLayout.Button dùng Text.Length > 5 để chọn rộng160logical, khiến Mở sổ và Sơ đồ đều5kýtự nhận88logical. Icon và padding vẽ chữ làm thiếu chiều ngang; phép đo control bounds không kiểm được ellipsis bên trong. Hướng sửa trong B03 đã duyệt: xếp hai nhãn này vào variant rộng160, không đổi kích thước Sửa/Xóa/Trước/Sau hoặc logic. Kết thúc lượt test này trước sửa, lưu FAIL; cần lượt source/regression riêng sau sửa.
## Cập nhật 21/10/2026 — B03 nhãn 5 ký tự

Đã kết thúc lượt native trước sửa, giữ FAIL Mở sổ/Sơ đồ. Đã sửa đúng OrbitManagementLayout.Button: hai nhãn có icon dùng variant rộng160logical; không đổi nút Sửa/Xóa/Trước/Sau và backend. BuildDebug/Release PASS0warning0error (build-label-debug.log,build-label-followup.log). Probe chỉ đọc hoàn tất22bảnđo (db-read-label-followup.json); đây không thay native visual. Source mới source-label-followup.csv. B01/B02/B04 đã đạt native trong phạm vi125%; B03 chờ kiểm ảnh bản mới. DPI100/150 vàDesigner vẫn NOT RUN. Không ghi toàn bộ task hoàn tất.

Probe nhãn mới thực tế21PASS/1FAIL: Catalogs tab Sản phẩm lần tải đầu chưa nhận Rows trong cửa sổ chờ harness, tương tự hiện tượng raw trước đó; chưa kết luận nguyên nhân sản phẩm. Giữ nguyên FAIL, không tính22PASS. Cần xác nhận native tab đầu sau đăng nhập. App test mớiPID29492.
## Kết quả native cuối — 21/10/2026

B03: PASS cho hai nhãn còn lại tại125% trên app thật PID29492, DB test CoffeeManager_Test_G03_20261019_215457_29e552a9_demo. Mở sổ hiện đầy đủ, Sơ đồ hiện đầy đủ; toolbar không wrap/chồng footer sau tăng chiều rộng. Evidence runs/bugfix-b01-b04/native-label-final-materials-0.png và native-label-final-tables-0.png. Tab đầu Danh mục nền tự tải10dòng, không bấm Tải lại: native-label-final-catalogs-first-0.png và .txt (DM001–DM010). Không xóa/đổi kết quả raw probe21PASS/1FAIL; native xác nhận lần quan sát này tải bình thường, không đủ kết luận nguyên nhân timeout harness.

B01/B02/B04 giữ kết quả native PASS ở125% của lượt trước; thay đổi cuối chỉ quy tắc chiều rộng hai nhãn. B01–B04 đã đạt phạm vi regression được quan sát ở125%; không có backend/SQL thay đổi. Source cuối giữ nguyên trong lượt native, đối chiếu source-label-followup.csv:0 thay đổi (source-label-final-audit.json).

Toàn task chưa đánh dấu Hoàn tất: DPI100/150 vàDesigner NOT RUN; SQLfixture hơn50dòng chưa chạy (paging presentation6/6 đãPASS). Các cột phụ dài có ellipsis khi cửa sổ normal được giữ ghi nhận, không chứng nhận pixel-perfect ở mọi viewport. Lịch sử G03 không thay đổi, không commit. App/DBtest còn giữ cho người dùng xem, theo manifest/cleanup có guard; không xóa trong phiên đang mở.

