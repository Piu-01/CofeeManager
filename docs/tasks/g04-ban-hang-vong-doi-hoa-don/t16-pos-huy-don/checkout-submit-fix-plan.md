# Đợt G04-UI05 và UX nhập tiền — 25/10/2026

Trạng thái: người dùng đã duyệt sửa và regression. Đã sửa ba file, Debug/Release
sạch;32/32 component,60/60 layout/binding và11/11 effect/input PASS.
Người dùng đã chốt qua Confirm thật, không lỗi/thối19000; SQL xác nhận thu81000
một lần và tiêu hao/kho khớp. T16-K03 PASS, G0434PASS/0FAIL/22BLOCKED.
Xem checkout-submit-test-results.md.

## Hiện tượng và bằng chứng

Người dùng làm ca HD20250411 rồi bấm xác nhận, app hiện unhandled exception:
“Forms that are not enabled cannot be displayed as a modal dialog box.”
Ảnh vẫn hiển thị khách đưa100000, thối19000 và các nút bị vô hiệu hóa.
SQL đọc sau lỗi: đơn vẫn Chua thanh toan, payable81000; không retry thanh toán.

Nguyên nhân đã tái hiện trên đúng DLL method-center-20261025, không gọi SQL:
StarDustOverlay constructor đặt Enabled=false; Burst gọi Show(owner), Form.Show
ném InvalidOperationException đúng chuỗi trong ảnh. Stack chỉ tới
StarDustOverlay.cs:42. CheckoutDialog.Confirm gọi SetBusy(true), stars.Burst
trước try/finally và trước service.CheckoutAsync nên exception thoát handler,
không gửi thanh toán và không mở lại controls. Các test SQL bridge trước đó gọi
service trực tiếp, không đi qua Burst; không chứng nhận đường click Confirm thật.

Chứng cứ tại ../runs/checkout-crash-investigation-20261025/:
reproduction.json, user-reported-exception.png, database-after-error.json, audit.json.

Yêu cầu bổ sung rõ của người dùng: ô Tiền mặt khách đưa mặc định bằng phần cash,
nhưng khi vừa vào ô phải tự chọn toàn bộ số để nhập đè. Source hiện chỉ có
TextChanged/Leave chuẩn hóa, không có SelectAll khi nhận focus/click lần đầu.
Đây là cải thiện thao tác nhập, không thay số thu hay cách tính tiền thối.

## Phạm vi sửa đề xuất

1. Sửa StarDustOverlay để Show hợp lệ, giữ không chiếm focus/không chặn chuột
   bằng cơ chế no-activate và hit-test hiện có; không đặt disabled rồi Show.
2. Không để lỗi hiệu ứng khóa luồng tiền: đưa bước hiệu ứng vào phạm vi xử lý
   trạng thái có finally mở controls; lỗi hiển thị không tự retry hoặc biến thành
   kết quả SQL unknown khi request chưa gửi. Hiệu ứng phụ không cản checkout hợp lệ.
   Giữ nguyên RequestId/token, validation, commit/reconcile và quyền.
3. Thêm helper chọn toàn bộ khi nhận focus vào control/input chung, opt-in ở
   Tiền mặt khách đưa. Keyboard Tab và click từ ô khác đều chọn toàn bộ;
   click tiếp khi đã trong ô vẫn cho đặt caret/chọn một phần để sửa. Không reset
   Text về mặc định trong khi nhập, không thay decimal/culture hoặc luồng validation.

File dự kiến: UI/StarDustOverlay.cs, Modules/Sales/CheckoutDialog.cs,
helper nhỏ trong UI hiện có hoặc một file behavior dùng chung. Không đổiSQL/DAL/
BLL/role; không refactor input của mọi form, không tắt hiệu ứng theo yêu cầu riêng.

## Regression sau duyệt cả đợt

- Đóng băng source/DLL sau sửa, giữ lỗi và snapshot cũ; Debug/Release.
- Burst thật trên WinForms: không exception, overlay không giữ focus/input,
  timer/hide/dispose an toàn; kiểm kỹ thuật, không QA thị giác.
- Bắt buộc kiểm đường handler Confirm thật có xác nhận, hiệu ứng và gửi SQL,
  không chỉ CaptureAllocations→SalesService trực tiếp. Nếu helper modal vẫn không
  nhập được, phối hợp người dùng bấm và agent snapshot SQL trước/sau, không giả PASS.
- 81000/100000 lưu81000/thối19000; một payment, kho/điểm/transaction đúng trong
  DB test riêng; mở lại/đối chiếu không thu lặp. Đơn manual chưa chốt hiện tại giữ
  nguyên đến khi người dùng làm ca; không thay bằng UPDATE hoặc gọiSQL để né GUI.
- Chọn hết khi Tab/click mới vào ô, nhập đè100000, giữ caret khi sửa tiếp,
  80000/abc/overflow, Leave-format, bank/mixed/zero, Enter chưa tựchốt,
  cancel/Quay lại, busy/unknown, hồi quy layout/input đã có tại125%.
- Nguồn lỗi hiệu ứng được xử lý không giữ busy vĩnh viễn; không tạo request mới
  sau lỗi chưa rõ/commit. Source cố định từng lượt, bug khác ngoài scope báo bổ sung.

Rủi ro: overlay nhận focus nếu bỏ disabled mà thiếu no-activate/hit-test; SelectAll
bị mouse-up đặt lại caret; catch quá rộng che lỗi hoặc xử lý sai trước/sau gửi.
Kiểm các rủi ro này trực tiếp, không tuyên bố đã qua từ source/build.

T16-K03 chuyển FAIL vì thao tác chốt thật bị exception, G04 thành33PASS/1FAIL/
22BLOCKED. Chưa suy PASS toànK04/K05 từ ảnh hoặc lời mô tả gộp các bước.
