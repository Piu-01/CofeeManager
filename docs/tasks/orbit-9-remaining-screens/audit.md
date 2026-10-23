# Audit triển khai tám màn Orbit

Nguồn hình thức: docs/ui/orbit-9-html/README_FIRST.md, design-tokens.json, 01–09 HTML và PNG trong references/; đọc cùng bộ guide docs/ui/ theo AGENTS. Hình thức web được chuyển sang native WinForms .NET10.0-windows; không WebView/ảnh nền. Tiếp tục pilot; không sửa T01–T32, schema hoặc seed.

## Kiến trúc và mapping

- MainForm/AppShell/sidebar và 96DPI PerMonitorV2 của pilot giữ nguyên. Module factories/dependency injection giữ nguyên.
- OrbitManagementTheme tập trung palette, Segoe UI, font card/empty, icons. OrbitButton giữ native Click/focus/keyboard và timer hover/press dùng chung; không stardust.
- OrbitListWorkspace giữ header/title/primary/tabs/tools/meta/pager ngoài Body. Body chứa grid/cardpanel cuộn; row height cố định theo profile. OrbitSectionTabs dùng native Button, hỗ trợ phím trái/phải. OrbitViewSwitch nhóm hai nút native.
- OrbitRecordListControl trình bày tám màn, giữ query(skip50), cancellation/generation, sự kiện và guard actions. Product/Customer/Table/Loyalty giữ service, typed DTO/revision, CaptureWrite/Reconcile/validation/confirmation. Catalog giữ handler có tên và mapping 7 CatalogKind.
- OrbitDataGridView readonly profile, full row selection, mã/name stretch, currency/date/percent format; badge trạng thái menu và hạng khách; trang chính sách hạng không icon cạnh tên.
- Bảng bàn giữ thêm cột Khu vực/Đơn mở của chức năng cũ khi xem Danh sách; không bỏ để ép giống ảnh Sơ đồ. Catalog giữ nút Tìm riêng cạnh Tải/đối chiếu vì đã có chức năng thật. Không đưa dòng demo từ HTML vào sản phẩm.
- Paging tám màn dùng batch50 hiện có. Tổng cuối chỉ biết khi batch<50. Batch50 hiển thị chỉ số trang/…; có thể cần trang rỗng cuối với số dòng đúng bội50. Không thay contract để giả tổng. Inventory giữ paging/lọc snapshot7 của pilot.

## File thay đổi so baseline đầu đợt

15 file sửa,4 mới: xem runs/source-changes-final.csv; so runs/baseline-source.csv và source-r6.csv. Không có source thay đổi ngoài CafeManager.WinForms. Hash không chứng minh runtime backend đã test mọi use case; chỉ chứng minh source backend giữ nguyên. DB schema/migrations không sửa trong đợt này.

Forms/CustomerPickerDialog.cs: bỏ Apply theme cũ đệ quy để không ghi đè control Orbit, giữ CustomerSelected/DialogResult/ShowDialog.
Modules: Menu/ProductsControl.cs, Tables/TablesControl.cs, Customers/CustomersControl.cs, Loyalty/LoyaltyControl.cs, Catalogs/CatalogsControl.cs và Designer. Chỉ layout/binding giao diện/filter reset/selection đồng bộ. Thông tin khách đang chọn lấy từ DTO thật.
Theme/OrbitManagementTheme.cs; UI/OrbitButton.cs, OrbitDataGridView.cs, OrbitEmptyState.cs, OrbitFilterBox.cs, OrbitListWorkspace.cs, OrbitManagementTabs.cs, OrbitPagination.cs.
Mới UI/OrbitRecordListControl.cs, OrbitSectionTabs.cs, OrbitTableCard.cs, OrbitViewSwitch.cs.

## Dọn mã trong phạm vi

Đã bỏ trường layout/toolbar/TabPage/picker và cơ chế compact/reparent heading cũ trong CatalogsControl do lần thay này không còn dùng. Không xóa RecordListControl/OrbitManagementLayout: CustomerOrderList vẫn dùng, MainForm vẫn gọi Icon; xóa sẽ làm hỏng luồng còn dùng. Không xóa ảnh/reference/lịch sử của lần trước hoặc thay đổi ngoài baseline được giao. Không commit/push.

## Giới hạn và vấn đề mới

Editor SP005 hiển thị sai danh mục so với grid (đã Hủy, không ghi). Ghi tại bug-report.md; chưa đổi handler nghiệp vụ. Không chứng minh mọi hành động ghi chỉ bằng việc nút còn tồn tại. Lượt này kiểm chủ yếu UI và đọc SQL test; giao dịch ghi giữ kết quả G03 trước, không tự nâng nghiệm thu backend.

## Bổ sung R8
Font style/vẽ trực tiếp dùng DrawingFont trong OrbitManagementTheme:font pixel theo DeviceDpi, cache theo template/DPI. OrbitDataGridView đặt AlignmentMiddleLeft rõ để getter DefaultCellStyle không trả bản sao; ApplyMetrics cập nhật fontbody/header/code theoDPI. Badge,card,empty/avatar dùng cùnghelper. NativeLabel/control.Font vẫn do WinForms scale. Không đổi ProgramHighDpiMode hoặc épScale hệ thống.
So baseline cuối:16file sửa+4file mới, tổng20fileWinForms; xem runs/source-changes-r8.csv. BackendModels/Business/DAL0file đổi. Không xóa helper cũ còn được dùng. LượtR8hash: runs/source-r8.csv.

## Bổ sung R9
NavigationController giữ màncache parented trongcontent, ẩn/hiện thay Controls.Clear; WinForms truyềnDPI tới cảmànẩn, giữcachevàinput. OrbitSectionTabs giới hạnbuttonheight theoheighttrốngtrừthanhcuộnngangkhicần, xửlýlàmtròn125% vànarrow. Không đổiConfirmLeave/quyền/session/query.
Cuốicùng21fileWinForms (17sửa+4mới), thêmNavigation/NavigationController.cs; source-r9.csv vàsource-changes-r9.csv. Backend0fileđổi. R9Debug/Releaseclean,probe19screens+navigation/tabwidthPASS. NativeR9sauđăngnhậpvẫnPENDING.

## R10
ChỉthêmGrid.ColumnHeadersHeightChanged gọiPositionEmpty;khôngđểoverlaylistrỗngcheheaderkhiHandleCreated/DPIthaychiềucao. Finalsource-r10.csv/source-changes-r10.csv:21fileWinForms (17sửa+4mới),backend0. BuildDebug/Releaseclean,fullprobe19screensvàoverlayheaderresize144PASS. NativecaPointsđangchờlogin;Menu/tab/cache125/150vàđộnét150đãđượckiểmtratrênR9.

## R11
MainForm.Shown căn lại shell sau autoscale; OrbitSectionTabs.OnCreateControl relayout sau HWND con tạo xong. Không đổi Navigate/permission/session/query. Tổng22fileWinForms (18sửa+4mới), thêmForms/MainForm.cs so với R10; backend0fileđổi. Source-r11.csv/source-changes-r11.csv. Shell startup125 và fullprobe125PASS; native appđầyđủ R11sauđăngnhập cònPENDING.


## Chốt bàn giao native R11
Xác nhận app đầy đủ sau đăng nhập125%: shell/tab/Points empty và chọn KH001 PASS. Source so freeze R11: 0 file drift trong 167 file kiểm tra. Không thay source sản phẩm trong lượt native. Báo cáo và ảnh cuối đã cập nhật; giữ125% theo yêu cầu, không commit/push.
