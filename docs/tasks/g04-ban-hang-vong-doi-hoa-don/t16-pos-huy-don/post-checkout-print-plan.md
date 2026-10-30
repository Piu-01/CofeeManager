# Bổ sung G04 — Hỏi in và về đơn mới sau thanh toán

Ngày25/10/2026. Yêu cầu người dùng: thay thao tác Xem/In ở POS bằng hỏi in sau
thanh toán; chọn có hoặc không xong đều trở về trạng thái đơn mới.
Trạng thái: người dùng đã duyệt bằng “ok đó”; đã triển khai và buildDebug/Release
0warning/0error. Chờ duyệt đợt test riêng, chưa chạy chức năng/in/ghiDB phần mới.
Xem post-checkout-print-implementation.md và post-checkout-print-test-plan.md.
Thuộc T16/T17 trong nhóm G04 hiện có, không tạo requirements task con hoặc đổi ID.

## Yêu cầu và tiêu chí hoàn tất

1. Chỉ khi đã xác nhận hóa đơn Da thanh toan từ SQL mới hỏi một lần:
   “Thanh toán thành công. Bạn có muốn in hóa đơn không?” với Có/Không.
2. Có mở hộp chọn máy in/in native hiện có rồi gửi in khi người dùng đồng ý.
   Đây là ý hiểu của “Có thì in”: không bắt đi qua màn chi tiết hoặc preview trước.
   Không mở cửa sổ chọn máy in khi chọn Không.
3. Sau Không, in thành công, hủy hộp in hoặc lỗi in, POS trở về nháp trống tương
   đương bấm Đơn mới: xóa mã/order đang chọn, dòng món, khách/ưu đãi và input nháp.
   Không tự tạo hóa đơn rỗng trong DB. Giữ hành vi bộ lọc/loại đơn như Đơn mới
   hiện có, không thêm quy tắc đổi loại/bộ lọc ngoài yêu cầu này.
4. Bỏ nút Xem/In khỏi POS. Tra cứu/in lại hóa đơn cũ vẫn ở Lịch sử hóa đơn.
   In lần đầu từ câu hỏi sau chốt không gắn nhãn IN LẠI; in từ lịch sử vẫn có nhãn.
5. Lỗi in hiển thị rõ đã thanh toán nhưng chưa in được, có thể in lại từ lịch sử;
   không rollback, thu lại, gọi checkout hay trừ kho lần nữa. Hủy in cũng không
   đổi hóa đơn. Nếu chỉ refresh bàn/đọc chi tiết sau commit lỗi thì không báo
   thanh toán thất bại hoặc giữ POS ở đơn đã chốt.
6. Hủy Checkout, lỗi trước commit hoặc kết quả còn unknown giữ đơn hiện tại;
   không hỏi in hoặc xóa input như thể đã thanh toán. Giữ khóa gửi lặp/phím/modal.

## Bằng chứng hiện trạng

SalesControl.Checkout hiện giữ dialog.ResultOrder trong order, ShowOrder(order)
và báo “Xem/In để tra cứu hoặc in lại”; nút invoice nằm trong orderPanel.Actions.
NewOrder chỉ reset khi !busy/!unknown nên không gọi nguyên xi lúc Checkout đang
busy; cần dùng phần reset chung đúng trạng thái, không bỏ guard để né vấn đề.
InvoicePrintPreviewForm hiện có PrintDialog/DemandPrintAsync/document.Print và
catch lỗi in. InvoicePrintDocument đã có tham số reprint=true; tái sử dụng.
UX mục8 hiện ghi kết quả có In/Xem chi tiết/Đơn tiếp theo, cần cập nhật đúng luồng mới.

## Kế hoạch triển khai trong phạm vi duyệt

1. SalesControl.cs: gỡ action invoice/XemIn và wiring chỉ phục vụ nút này;
   giữ điều hướng lịch sử/mở đơn. Tách phần reset nháp nhỏ dùng lại cho nút Đơn
   mới và kết thúc thanh toán. Chốt dữ liệu đơn đã trả vào biến cục bộ để in,
   tách kết quả đã commit khỏi lỗi tải bàn/chi tiết/in.
2. Sau dialog trả kết quả paid xác nhận, hỏi Có/Không. Nhánh Có lấy snapshot
   InvoiceService và kiểm quyền in hiện có, mở PrintDialog với đúng hóa đơn đã
   trả; reset POS trong đoạn kết thúc luôn chạy cho nhánh paid, kể cả lỗi/hủy in.
   Giữ thông báo mã đơn/thu/thối ở status để thu ngân tham khảo sau khi nháp trống.
3. Tái sử dụng đường in qua helper UI/Printing nhỏ dùng chung từ logic đã có
   trong InvoicePrintPreviewForm. Preview/history gọi helper cùng quyền và xử lý
   lỗi, lần in mới truyền reprint:false. Không tạo print service ở Business có
   WinForms control; không thêm framework/interface chung không cần thiết.
4. Cập nhật UX mục kết quả sau thanh toán và kế hoạch T16/T17 liên quan.
   Giữ toàn bộ56ID/325ID và lịch sử kiểm. Requirements/plans nhóm liên kết bổ sung.
   Không sửa SQL/migration/DAL/BLL/request/token/role hay bổ sung lưu tiền đưa/thối.

File dự kiến: Modules/Sales/SalesControl.cs, Forms/InvoicePrintPreviewForm.cs,
helper nhỏ ở Printing; InvoicePrintDocument chỉ nếu cần nối tham số đã có.

## Đợt test đề nghị sau triển khai (chờ duyệt test riêng)

- Đóng băng source mới; build Debug/Release. Agent kiểm chức năng125%, hình thức
  do người dùng kiểm,100/150% NOT RUN; không thay expected/checklist cũ.
- Paid→Không: một câu hỏi, không mở print, POS nháp trống, DB đúng một checkout.
- Paid→Có: hộp in đúng chứng từ; in xác nhận/hủy/lỗi máy in đều về nháp, tài chính
  và tiêu hao/điểm không đổi so ngay sau commit. Không tự gửi job in giấy/PDF nếu
  người dùng chưa chọn đích; ca gửi in thật phối hợp người dùng chọn máy/đích.
- Phân biệt trước/sau commit: stale/thiếu kho/cancel/unknown không xóa đơn;
  lỗi tải bàn/chi tiết sau commit giữ thông điệp đã thanh toán và reset được.
- Không còn Xem/In trên POS; lịch sử mở đúng đơn, in lại có nhãn, không dựng tiền
  đưa/thối chưa lưu; in nhiều lần không tạo payment hoặc tiêu hao.
- 81000/100000, mixed/bank/0₫, tạo đơn tiếp theo ngay sau reset, không giữ khách/
  ưu đãi/dòng cũ; Enter/F8/modal không gửi lặp. SQL Server DB test riêng, snapshot
  tiền/kho/điểm trước/sau; đánh dấu rõ component và thao tác người dùng thật.

Rủi ro chính: reset quá sớm làm mất dữ liệu in; reset bị guard busy chặn; lỗi in
bị hiểu thành lỗi checkout; lần in đầu nhầm nhãn IN LẠI. Không đưa thay đổi này
vào lượt test cũ đang giữ source cố định. G04 hiện34PASS/0FAIL/22BLOCKED, chưa
nghiệm thu phần thay đổi mới từ việc lập kế hoạch.
