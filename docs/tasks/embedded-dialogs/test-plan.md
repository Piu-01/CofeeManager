# Đợt kiểm hộp nổi và chọn hết input

Trạng thái: ĐỢT TEST ĐÃ ĐƯỢC NGƯỜI DÙNG DUYỆT26/10/2026; đang thực hiện; duyệt triển khai không thay
duyệt test. Source cuối tại runs/implementation-20261026/source-final.csv,
DLLhash tại dll-hashes.json. Không sửa source trong lượt nghiệm thu.

Phạm vi đầy đủ ED01–ED13 ở plan.md, gồm:

1. Hộp nổi/stack/focus/khóa nền/sidebar, trả kết quả, resize và đóng cửa sổ chính.
2. Thêm/sửa/hủy/picker lồng nhau ở danh mục/nguyên liệu/menu/công thức/bàn/khách/
   ưu đãi/điểm; nháp, lỗi, busy, unknown và session logout/revoke.
3. F3validation, F4mở đúng một lần, F8chốt/xung đột/đối chiếu; detail/giao hàng/
   quay lại POS; hỏi in/Không/Có→hủy/PDF/lỗi sau commit và preview/in lại.
4. Chọn hết input click/Tab/rời-vào lại, caret các click tiếp, text/tiền/SL/ghi chú/
   combo gõ/grid, dynamic/reused, paste/IME, readonly và dropdown chỉ chọn.
5. Ba role, dispose/lifetime, client1280×720/1366×768 tại125%; hình thức user kiểm,
  100/150%NOT RUN, không tự đổi Windows Scale.

Các giao dịch dùng SQL Server thật, DB test riêng. Đề xuất tiếp tục DBcheckout
có marker20261025_181739_98e5df59; phải kiểm ownership/schema/permission restoration
và số tài nguyên trước ghi, tạo fixture riêng, không chốt lại các đơn đã trả.
Baseline tiền/kho/điểm/receipt và snapshot dòng/header trước/sau, dùng hai session
cho ca cạnh tranh thực tế bị ảnh hưởng. Không tác động DBdevelopment/profile.
Không mở thêm DB thứ4; ghi retain/cleanup cho tài nguyên đợt test đã duyệt.

Agent kiểm phần tự làm được trên control thật, không biến đọc source/handler
reflection thành bằng chứng click native. Gom các bước cần người dùng thực hiện
khi công cụ GUI hạn chế, ghi kết quả từng phần. Hình thức SKIPPED theo phân công.
Khi FAIL ghi bug, tiếp tục phần độc lập, tổng hợp đợt debug để duyệt; không sửa
source giữa lượt test. Ghi report/test-results đầy đủ kể cả còn BLOCKED.

