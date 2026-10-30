# Đợt test Lịch sử hóa đơn V4

Trạng thái: USER ĐÃ DUYỆT đợt test bằng “duyệt” ngày26/10/2026, sau bàn giao
triển khai. Ma trận đầy đủ IH01–IH10 tại [plan](plan.md).

1. Cố định source/binary theo manifest triển khai; kiểm hash trước/sau. Nếu cần
   harness riêng thì lưu riêng, không sửa source sản phẩm trong lượt test.
2. Kiểm query trên SQL Server thật, DB test đang sở hữu
   CoffeeManager_Test_G04_20261025_181739_98e5df59_checkout, marker
   20261025_181739_98e5df59, schema016. Xác minh trạng thái thực tế và ownership
   trước thao tác; không tạo DB thứ4 hoặc ghi fixture vào development.
3. IH02/IH03/IH05: hai loại ngày, đầu/cuối ngày, ngày sai/range đảo; tìm mã/tên/SDT,
   Enter/kính lúp, draft chưa submit, đổi filter/refresh/clear, query đến muộn,
   cancel/error; tập kết quả phải khớp SQL, input được giữ, không 0 giả/stale action.
4. IH04:0/1/49/50/51/100/101 dòng, tổng thật và nút trang cuối, đổi tập khiến trang
   hiện tại rỗng. Fixture riêng trong DB test, ghi allowlist và cleanup, không
   thanh toán đơn fixture. Không suy PASS các bộ dữ liệu chưa chạy.
5. IH06–IH08: WinForms thật tại125%, code/ngày/tiền typed, readonly, select-all,
   Tab/Enter/date popup, enable action và mở đúng chi tiết, owner/taskbar/modal,
   paid/canceled khóa tài chính; mở POS chỉ theo contract. Không checkout/in ra
   máy thật. Snapshot tiền/kho/điểm/thanh toán trước/sau phải bất biến.
6. IH09/IH10: quyền ADMIN/CASH/STORE và regression module dùng chung workspace,
   date/grid/pager/search; chỉ các đường chức năng bị ảnh hưởng. Giữ checklist325ID,
   tham chiếu T17-K01/K02/K04/K05/K07/K08/K09, không đổi expected gốc.
7. Người dùng kiểm hình thức; agent ghi SKIPPED theo phân công, không chụp ảnh
   đối chiếu. DPI100/150% NOT RUN; không đổi Windows Scale. Thiếu GUI/quyền/dữ liệu
   ghi đúng phần BLOCKED và gom yêu cầu manual, không suy PASS từ service.
8. Lưu kết quả, lỗi và tài nguyên còn giữ. Dọn đúng fixture/principal/process mới
   thuộc đợt, giữ các đơn và manifest native cũ theo ownership riêng. Bug mới ghi
   tại task T17/báo cáo nhóm và chờ duyệt đợt bug trước sửa/regression.

Đợt này không tự tiếp tục thanh toán476–478 hoặc đánh dấu phần native/in cũ đã đạt.
