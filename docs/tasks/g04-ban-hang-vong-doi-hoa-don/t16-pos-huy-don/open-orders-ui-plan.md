# Bổ sung giao diện Đơn đang mở — 25/10/2026

Người dùng gửi Orbit_DonDangMo.zip, yêu cầu chỉnh màn này và cột mã hóa đơn phải
hiện toàn bộ mã. Người dùng đã duyệt bằng “duyệt'”; đã triển khai và buildDebug/
Release sạch. Đợt test riêng đã duyệt:59+7+32 kiểm traPASS, source208file cố định;
đang chờ người dùng xác nhận native/hình thức, không suy toànG04PASS. Xem
open-orders-ui-implementation.md và open-orders-ui-test-plan.md.
ThuộcT16/G04 hiện có; không tạo requirements task con hoặc thay56ID/325ID.

## Phạm vi và chuẩn hiển thị

- Bám bố cục PNG/HTML người dùng: header Đơn đang mở và hướng dẫn, thanh tìm/
  tổng kết quả, bảng cuộn nội bộ, footer dòng đang chọn/phân trang/Mở đơn.
  Giữ native chrome và theme Orbit hiện có, không sửa nềnPOS/sidebar.
- Giữ năm cột Hóa đơn/Ngày lập/Kênh/Khách/Phải trả. Mã đầy đủ nhưHD20250419,
  không HD2… hoặc chỉ xem được bằng tooltip. Footer cũng hiện đầy đủ mã chọn.
- Ngày giờ dd/MM/yyyy HH:mm, tiền81.000 ₫ căn phải; tên kênh tiếng Việt và badge
  như mẫu. Dữ liệu lấyDB, không copy5dòng/ngày/tiền demo củaZIP.
- Ưu tiên OrbitDialogLayout/OrbitField/OrbitButton/OrbitDataGridView; mở rộng
  profile riêng cho Đơn đang mở, không đổi kích thước cột của các module khác.

README/PNGZIP ghi rõ mã demo đã bị cắt và giữ…do không biết mã thật. Yêu cầu mới
của người dùng có ưu tiên: app phải hiện mã thật đầy đủ, không giữ ellipsisdemo.
Bản tham chiếu và manifest nguồn tại docs/ui/open-orders/; HTML không chạy nhưapp
và không phải bằng chứng WinForms. Không nhúng JSdemo/blur nền hoặc cần Internet.

## Nguyên nhân cột mã hiện tại

OpenOrdersDialog tạo SalesUi.Grid mà không chỉ định profile. OrbitDataGridView
mặc địnhInventory, ColumnWidth(Code)=76logicalpx, padding ngang26px và fontCode,
không đo text10ký tự. ApplyMetrics chạy lại khi binding/DPI nên chỉnh Width một
lần trong form có thể bị ghi đè. Cần metrics thuộc đúng profile và tồn tại sau
tải trang/resize/DPI, không chỉ tăngWidth tạm thời.

## Kế hoạch triển khai

1. OpenOrdersDialog.cs: bố trí lại header/search/count, bảng vàfooter bằngshared
   dialog/theme; giữ service.OpenAsync, tìmEnter, phân trang50, doubleclickmởđơn.
   Loading/rỗng/lỗi tách rõ; khóa Mở đơn khi chưa có lựa chọn hợp lệ hoặc đangtải.
   Trước/Sau khóa theo trang/tổng thật, không theo5dòngdemo. Lỗi giữ input vànhãn
   chưa cập nhật, không giả0kết quả. Esc đóng; Enter ởtìm chỉtìm, ởbảngmởdòngchọn.
2. OrbitDataGridView.cs: thêm profileOpenOrders read-only. Cột Code có minimum
   theo độ rộng đo mã đầy đủ/font/DPI +padding, tối thiểu đủHD99999999; không
   co mất mã khi binding/resize. Cột ngày giờ và tiền có minimum riêng, Kháchgiãn.
   Cửa sổ hẹp dùng cuộn bảng; cân nhắc cố định cột mã để đọc lúc cuộn ngang.
   Badgekênh dùng paintinghelper đãcó, scopeOpenOrders, không đổi profile khác.
3. Dùng Dock/Anchor/TableLayoutPanel; vùngbảng nhận phầncòn lại của shell cố định,
   không đặt Percent100 trong containerAutoSize gây giãn vượtviewport nhưUI03.
   Header/search/footer tiếp cận được ở125%, không giảm font để giấu clipping.
4. Cập nhật guideREADME và chỉ mục nhóm; giữ contract/role/BLL/DAL/SQL. Không thêm
   bộ lọc backend hoặc đổi nghiệp vụ đơn mở theo mẫu demo. BuildDebug/Release.

File dự kiến: Modules/Sales/OpenOrdersDialog.cs, UI/OrbitDataGridView.cs.
Theme/sharedlayout chỉ sửa khi cần extension nhỏ và không đổi default ngoài scope.

## Đợt test sau triển khai — chờ duyệt riêng

- Đóng băng source/DLL mới; thật125%,1280×720/1366×768 và cửa sổhẹp. Đo text mã
  đầy đủ với font/DPI thật và padding, bounds cell/footer; sau binding/page/resize
  không bị thu hẹp. Ngày giờ/tiền không truyền chuỗi format vềSQL.
- Tìm thấy/không thấy, tìmEnter, chọn/đổi dòng, doubleclick/Enter mở đúng mã,
  Esc/đóng không sửa hoặc hủy đơn. Cókháchtrống, tênViệtdài, mã10ký tự, tiền lớn.
-0/1/>50kếtquả, paging50/1, nútTrước/Sau/Mởđơnđúngstate; loading/lỗi/responsecũ
  không ghi đè kếtquảmới hoặc mở nhầm dữ liệu. DBtest riêng nếu cần fixture.
- Hồi quyprofilePayment/Inventory/cácgridđãcó, bảo đảmread-only và editor không
  đổi ngoài scope. Giữ test hỏiin/reset đã có ở binary cũ, không vá source trong
  lượt test. Ghi rõ agentcomponent hay người dùng thao tác thật.
- Hình thức do người dùng xác nhận; agent không chụpảnhQA. DPI100/150%NOT RUN,
  không đổiWindowsScale. Chỉ sửa sau duyệt phạm vi; test riêng sau triển khai.

Rủi ro: metrics lại ghi đèWidth saubinding; cộtfixed làmhẹpKhách/tiền; header/
footer bịcắt khi narrow. Ưu tiên bảo vệ mã và cuộn đúng vùng, không dựa tooltip.
