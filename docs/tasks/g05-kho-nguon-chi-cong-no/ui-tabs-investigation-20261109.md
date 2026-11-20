# Điều tra tab Kho/Nhân sự —09/11/2026

**Cập nhật10/11/2026: đã duyệt, sửa và regression100+12 kiểm tra PASS.**
Xem [kết quả/hiệu năng/giới hạn build](ui-tabs-regression-20261110.md).
Nội dung dưới đây giữ bản điều tra trước sửa, không thay kết quả cập nhật.
Chỉ đọc source/tài liệu và hai ảnh người dùng gửi; chưa sửa source, chạy app,
đo hiệu năng hoặc chạy test mới. Không quy kết SQL chậm khi chưa có số đo.

## Hiện tượng và chủ sở hữu

- Ảnh Kho: nền thanh tab/viền sáng màu trắng; Tồn kho chỉ có một phần nội
  dung ở góc trên trái, phần còn lại trống. Người dùng báo khi click bị giật.
- Ảnh Nhân sự/Chấm công: cùng thanh tab/khung trắng; người dùng báo tải nhiều,
  giật cả màn hình. Không có trace thời gian/CPU/query của phiên trong ảnh.
- [G05-UI-20261109](t20-nhap-kho/bug-report.md#g05-ui-20261109--tab-kho-viền-trắng-nội-dung-tồn-co-và-giật-khi-mở)
  sở hữu caller InventoryControl và cách ghép Tồn kho.
- [G06-UI-20261109](../g06-nhan-su-cong-luong/t24-nhan-vien-tai-khoan/bug-report.md#g06-ui-20261109--tab-nhân-sự-còn-chrome-sáng-và-khởi-tạo-nặng)
  sở hữu caller HumanResourcesControl; ảnh hưởng các trang T24–T26.

## Bằng chứng source và mức kết luận

1. **Nguyên nhân cấu hình tab đã xác định:** InventoryControl.cs:10 và
   HumanResourcesControl.cs:22 dùng `new TabControl`. AppTheme.cs:59–65,130–145
   chỉ gắn owner draw và tô `DrawItemEventArgs.Bounds` của nhãn tab; không thay
   phần chrome còn lại của native TabControl. Vì vậy các caller này chưa dùng
   đường xử lý chrome tối đang có tại UI/OrbitTabControl.cs. Ảnh người dùng
   phù hợp cấu hình này; chưa kiểm pixel hoặc nghiệm thu thị giác thay user.
2. **Nguyên nhân Tồn kho bị co đã xác định từ source:** InventoryControl.cs:10
   thêm trực tiếp `new MaterialsControl(...)` vào stock.Controls mà không đặt
   Dock/Anchor. MaterialsControl.Designer.cs không đặt Dock/Size cho chính
   UserControl. `grid.Dock=Fill`/workspace bên trong không làm ancestor giãn.
   Khi mở Materials riêng qua NavigationController thì controller đặt Dock.Fill,
   nhưng đường mở bên trong tab Kho không đi qua bước đó.
3. **Phần việc UI có thể góp phần giật, chưa có định lượng:** InventoryControl
   khởi tạo đồng loạt4trang con trong constructor; HumanResourcesControl:20–28
   chờ đọc context rồi khởi tạo tối đa4trang con và gọi AppTheme.Apply(this).
   Constructor/layout/theme nằm trên UI thread, các caller không có đợt ghép
   cây control được SuspendLayout/ResumeLayout bao quanh. Không suy rằng cả4
   trang đều query SQL cùng lúc chỉ từ việc tạo control.
4. **Ứng viên tải lại cần đo:** PurchasesControl.cs:20, StockIssuesControl.cs:19,
   StocktakesControl.cs:19 đăng ký cả Load và VisibleChanged để gọi LoadRows;
   mỗi lần gọi hủy request cũ rồi bắt đầu request mới. Chưa trace thứ tự event
   để khẳng định tải trùng trên máy user. MaterialsControl.cs:26 đặt loaded=false
   khi ẩn, lần hiện lại tải lại; LoadRows:39–45 duyệt mọi batch50 để tạo snapshot
   phục vụ lọc/phân trang tại UI. Đây là hành vi hiện tại, không tự đổi thành
   chỉ lấy50dòng vì có thể làm sai filter/count.
5. OrbitRecordListControl.cs:61–68 có guard ready cho Load/VisibleChanged;
   không gộp mọi trang Nhân sự vào kết luận tải trùng. OrbitField input helper
   cũng có ConditionalWeakTable guard, chưa có bằng chứng nhân handler chọn hết.

299/299 của đợt sidebar trước là kiểm shell với body module mô phỏng; không
bao phủ các control Kho/Nhân sự thật và không có phép đo độ giật của chúng.

## Đề xuất sửa và đo trong một đợt sau duyệt

1. Hai caller dùng lại OrbitTabControl với FlatManagementTabs=true, theme
   nền/page theo Orbit. Giữ đủ nhãn tab và chọn tab trực tiếp; không accordion,
   không ẩn nhãn tab, không tạo một hệ thống tab thứ tư.
2. Đặt MaterialsControl Dock.Fill ngay tại caller tab Kho; kiểm đủ chuỗi
   container tới workspace/grid và clamp bounds ở125%.
3. Đo bản trước sửa và bản sau sửa bằng thời gian tạo control/layout, UI timer
   heartbeat, event Load/VisibleChanged và số/lifetime lời gọi đọc dữ liệu.
   Tách mở lần đầu, đổi tab lần đầu, đổi lại tab đã mở, rời module rồi quay lại.
   Không ghi password/hash/connection string vào evidence.
4. Dựa trên đo: khởi tạo nội dung khi tab được mở lần đầu, cache instance trong
   phiên; ghép cây control trong một đợt layout, tránh áp theme lại toàn bộ
   subtree đã cấu hình. Hợp nhất/guard tải trùng nếu trace chứng minh; giữ
   loading/lỗi, cancel/generation, refresh sau ghi, freshness cần thiết và
   trạng thái nháp/unknown/quyền. Không bỏ refresh bằng cache mù.
5. Phạm vi chính: InventoryControl.cs, HumanResourcesControl.cs; chỉ sửa lifecycle
   đọc tại các trang Kho hoặc OrbitRecordListControl khi có bằng chứng đo.
   OrbitTabControl chỉ sửa nếu phát hiện thiếu sót thật, không đổi mặc định
   toàn app. Giữ BLL/DAL/SQL/contract, sidebar label, module khác.

## Regression và rủi ro

- Debug/Release; MainForm với **module Kho/Nhân sự thật**, DB test riêng có
  schema đủ024 trở lên, không DB sử dụng thật. Có manifest/allowlist/cleanup
  cho fixture thuộc lượt; ưu tiên runner/fixture sẵn có hợp lệ. Không nâng
  development hay giữ/xóa DB ngoài phạm vi lượt.
- ADMIN/STORE/CASH theo quyền; đủ tab; lần đầu/chuyển lại/chuyển module, bàn
  phím Tab/mũi tên, focus, giữ filter/input, không tạo control hoặc handler trùng.
- Bounds kỹ thuật1280×720/1366×768 và cửa sổ hẹp tại125%; Tồn kho giãn hết;
  loading/rỗng/lỗi/cancel/query muộn; hủy/nháp/busy/unknown không bị vượt qua.
- So timing/heartbeat/query count trước/sau với cùng fixture/môi trường;
  không báo mượt hoặc quy nguyên nhân SQL chỉ từ cảm giác/build.
- Hình thức/viền do người dùng nghiệm thu; agent không chụp ảnh so sánh.
  DPI100/150% chưa chạy ghi NOT RUN, không đổi Windows Scale.
- Rủi ro thực tế: lazy page làm lệch Load/VisibleChanged hoặc cache giữ dữ liệu
  cũ; đổi tab phải giữ query lifecycle, quyền và cập nhật sau ghi. Không xử lý
  bằng Application.DoEvents, ép DPI, WS_EX_COMPOSITED toàn app hoặc chặn UI chờ SQL.

Nếu phát hiện nguyên nhân mới ngoài phạm vi trên, ghi bằng chứng và báo duyệt
bổ sung; không tự đổi SQL/index hoặc mở đợt tối ưu toàn app.
