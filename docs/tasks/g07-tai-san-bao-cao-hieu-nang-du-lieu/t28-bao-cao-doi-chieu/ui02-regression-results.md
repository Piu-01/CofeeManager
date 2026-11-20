# G07-UI02 — Hoàn thiện màn Tổng quan quản lý

09/11/2026. Người dùng đã làm rõ phạm vi là giao diện Tổng quan và duyệt bằng “ok”. **Đã triển khai,12/12 ca regression chức năng/layout kỹ thuật PASS, bản mới đã mở tại Tổng quan production.** Hình thức vẫn do người dùng kiểm, không tự nghiệm thu thị giác hoặc chốt toàn G07.

## Thay đổi và phạm vi

Chỉ sửa file sản phẩm `src/CafeManager.WinForms/Modules/Home/AdminDashboardControl.cs`:

- Header, ngày nghiệp vụ, ba card Doanh thu hôm nay/Đơn đang mở/Công nợ nhà cung cấp, dùng OrbitSurface và theme hiện có. Card co thành3/2/1 cột khi hẹp, giữ giá trị typed từ DashboardRow.
- Nhóm Truy cập nhanh với Tải lại/Báo cáo/Bán hàng/Công nợ, kích thước tính theo font/DPI của OrbitManagementTheme.Button, có wrap. RowStyles/ColumnStyles cấp vùng rõ ràng, nội dung cuộn khi cần.
- Vùng trạng thái và thời điểm cập nhật riêng; ghi chú doanh thu/ngày trả/nợ và giới hạn lợi nhuận. Lỗi/loading hiển thị “—”, không gán0 hoặc giữ số liệu cũ như kết quả mới.
- Dùng chung một request đang tải để chặn sự kiện Load/VisibleChanged gửi trùng, hủy khi ẩn/đóng, chặn kết quả muộn bằng generation/session và dispose đúng vòng đời. Các thay đổi này nằm trong phạm vi loading/hủy khi rời màn đã duyệt của UI02.

Không đổi SQL, BLL, DAL, contract, shell/sidebar hoặc helper dùng chung. Các thay đổi sẵn có của người dùng được giữ nguyên. Không commit/push.

## Môi trường và source

[Manifest](../runs/home-ui02_20261109_221240_d34dc2a4/manifest.json), Windows,SDK10.0.401,SQLServerlocalhost17.0.1000.7,WinForms DeviceDpi120=125%. DBtest riêng `CoffeeManager_Test_G08_20261109_221240_d34dc2a4_demo`, install demo/schema001–027 có marker. Bootstrap ADMIN fixture bằng password ngẫu nhiên chỉ ở bộ nhớ, runtimeHomeUiRuntime WITHOUT LOGIN/CafeManagerApp. Không ghi fixture vào development.

Nguồn/harness cố định trong lượt:source-audit.json có **SourceUnchanged=true,HarnessUnchanged=true**. DLL DefaultDebug/F5 giống nguyên byte với DLL kiểm trong harness:SHA2569D60E60EDA734732438843943776BE5281303EBAF44E1B2011AC64549014FCC2. App production đã đóng bình thường trước build mặc định, mở lại theo profile Development với DLL mới; không để người dùng xem binary cũ sau bàn giao.

Build Debug output riêng,Release output riêng và Debug mặc định đều **0warning/0error**; log trong thư mục run. Không suy PASS chức năng từ build.

## Kết quả regression

[Raw12ca và số đo](../runs/home-ui02_20261109_221240_d34dc2a4/results.json), mã harness ở Probe/Program.cs trong cùng run. Chuẩn bị harness xong trước snapshot; không thay source/expected trong lượt.

| Ca | Expected/thao tác | Actual |
|---|---|---|
|UI02.Binding|ReportService thật trên SQLtest;3giá trị khớp DashboardRow, tiền vi-VN, thời điểm đọc|PASS|
|UI02.Layout.1280x720|3card/4nút đủpreferredsize, card không kẹp con, label đủpreferredheight|PASS,125%,3cột|
|UI02.Layout.1366x768|Như trên tại viewport thứ2|PASS,125%,3cột|
|UI02.Layout.920x720|Co card khi hẹp; giữ input/button đúng vùng cha|PASS,125%,2cột|
|UI02.Layout.620x720|Card xuống một cột, nội dung cuộn được qua containerAutoScroll|PASS,125%,1cột|
|UI02.Layout.440x720|Panel hẹp tương đương vùng nội dung cửa sổ nhỏ|PASS,125%,1cột|
|UI02.LargeValueLayout|Giá trị tiền synthetic18chữsố trên label tại panelhẹp; kiểm preferredheight và tải lại từSQL|PASS; chỉ kiểm trình bày số lớn, không claim đã tạo doanh thu đó|
|UI02.ErrorAndRetry|SessionInitializer ném lỗi SQL thật chỉ trênfixture; rồi bỏ fault và Tải lại|PASS:error riêng,3giá trị—,refresh mởlại,retry thànhcông|
|UI02.PendingDuplicateAndHide|WAITFOR trongconnectionfixture; yêu cầulặp khi pending,rời rồi quay lại|PASS:khônggửiđúp,refreshkhóa,hủy khiẩn,khônglateaggregate,quaylạitảimới|
|UI02.DisposeWhilePending|Dispose2lần tronglúcquerychậm|PASS:task kếtthúc,khôngObjectDisposedupdate|
|UI02.RevokedDuringLoad|Session thật bịrevoke khi chờ|PASS:khônghiệnaggregate,refreshkhóa;khôngthayca logoutnative|
|UI02.Navigation|MainForm/NavigationController thật,3modulefactoryspy;PerformClick từng nút rồi quayHome|PASS:đúngReports/Sales/Debt,mỗifactory1lần,khôngmởtrùng|

Các nút ở125% đều cao50devicepixel, bằng GetPreferredSize; đây là đo bounds/chức năng, **không phải nghiệm thu hình thức/spacing**. Không dùng screenshot đối chiếu để ghi PASS. Native các màn đích nghiệp vụ không được suy PASS từ spy module.

## Native production và bàn giao

Mở đúng exe Debug mới, bấm ADMIN bằng tọa độ screenshot hiện tại (chỉ định vị input). Sau đó đọc AX trên MainForm xác nhận3vùng card độc lập,4hành động truy cập nhanh,ngày/thời điểm đọc và “Đã cập nhật số liệu”. [Bằng chứng](../runs/home-ui02_20261109_221240_d34dc2a4/native.json). Giá trị binding được kiểm trực tiếp qua control trênSQLfixture;AX label có AccessibleName riêng nên không dùng AX để chứng nhận số tiền native.

**App để mở tại Tổng quan mới để người dùng xem hình thức.** Không chụp/so ảnh màn dashboard, không kiểm hover/spacing. Hình thức SKIPPED theo phân công;DPI100/150%NOT RUN,không đổi WindowsScale. Không từ lượt này nâng các checklistnative khác hoặc chốt325ID/G07/G08.

## Tài nguyên và trạng thái

[Resource audit](../runs/home-ui02_20261109_221240_d34dc2a4/resource-audit.json):1DBtest tạo/1dọn/0còn,0processprobe;khôngtạoSQLlogin/Windowsaccount/credentialDPAPIfixture. Appdevelopment của người dùng được mở lại có chủ ý,khôngdọn phiên này. Developmentschema027 và dữ liệu được giữ nguyên bởi fixUI;khôngmigrationmới.

Không phát hiện bug mới trong12ca. G07-UI02 đã xử lý theo phạm vi triển khai và kiểm chức năng/kỹ thuật;hình thức chờngườidùngđánhgiá. Lịch sửG07-DEV01 và cácblocker khác giữ nguyên.
