# Điều tra: đã mở đơn nhưng còn hộp chọn — 25/10/2026

## Kết quả kiểm sạch

Người dùng xác nhận: “Hộp đã đóng hết, chỉ còn POS đúng HD20250421” sau khi
đóng hộp cũ, F4 đúng một lần rồi tìm/doubleclick. Nhánh mở/đóng hộp sạch PASS
theo thao tác thật; tình huống hộp còn lại phù hợp trạng thái test bị chồng từ
công cụ. Không cần sửa SelectOrder hoặc tự thêm Close thừa. Không sửa source.
Những câu “đang chờ” dưới đây giữ lịch sử điều tra trước phản hồi này.

Người dùng gửi ảnh: POS bên dưới đã có món, hộp Đơn đang mở còn phía trước.
Chưa sửa source; không tự kết luận Close trong SelectOrder bị hỏng.

## Bằng chứng

- Logg04-continue-pager/native-refreshed.json trước đó có hai ownedwindow
  Đơn đang mở (accessibilityindices517 và124) sau toolPressReturn nhắm rootPOS.
  Đây là lượt nhập sai target của công cụ đã được ghi, không PASSnativeEnter.
- Capture hiện tại cùng MainWindow11537712 chỉ còn hộp124, searchOO..., tổng53,
  currentHD20250471; POS bên dướiHD20250421/Chua thanh toan, status21:32.
  Hộp124 và các giá trị trùng hộp cũ trong log trước. Khớp tình huống hộp đangchọn
  đãđóng nhưng còn hộp chồng bên dưới, không đủ chứng minh doubleclick một hộp
  sạch không gọiClose. Không đổ lỗi cho người dùng đã mở nhiều lần.
- SelectOrder nguồn đặtSelectedCode,DialogResult.OK rồiClose; callerOpenOrders
  dùngusing/ShowDialog rồiOpenOrderAsync. Component chọn/doubleclick đơnlẻ đãPASS.
  Caller chỉguardbusy/unknown, không có cờ một-instance hộp chọn. Chốngreentrancy
  là đề xuất riêng nếu cần, không tự sửa khi chưa được duyệt.
- Agent thử click nútClose513 đúng từstate mới; helper trả“coordinate input
  geometry is unavailable”. Không gửi thêmEnter/Esc vàoPOSroot để tránh chồng hộp.

## Cách kiểm tiếp

Đã nhờ người dùng đóng hộp còn lại bằngX, F4đúngmộtlần, tìmHD20250421 rồi
doubleclick. Expected: không còn hộp vàPOSđúngmã. Đang chờ kếtquả táchbiệt này.
Nếu đạt, ghi phục hồi trạngthái test bị côngcụlàmchồng; không vá Close thừa.
Nếu vẫn còn hộp trên trạngtháisạch, điều tra/repro/gom hướngsửa trước debug theo
quytrình bug. Không dùng lỗihelper hoặc ảnh hiện tại để suyca độc lập FAIL/PASS.

Evidence user-report.png làảnh do người dùng gửi, không agentcaptureQA;
current-ui-before-clean-check.json làaccessibility khôngscreenshot.
Không gửi checkout/in, không đổiSQL/role/source. PC03PDF chờ sau khi làm rõ
trạngthái hộp chọn, chưa đánh PASSoutput chưa có.
