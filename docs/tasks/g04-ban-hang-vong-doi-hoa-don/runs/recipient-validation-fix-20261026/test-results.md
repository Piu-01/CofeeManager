# G04-UI06 — kiểm sau sửa 26/10/2026

Người dùng duyệt sửa/regression. Source sản phẩm chỉ đổi OrderHeaderDialog.Save:
kiểm tên/SDT/địa chỉ, ErrorProvider đúng từng trường và focus lỗi đầu tiên, giữ
Normalize BLL; errors.Clear mỗi lần áp dụng. Không đổi SQL/role/quy tắc tài chính.

| Phần | Kết quả |
|---|---|
| Build Debug/Release | 0 warning / 0 error cả hai |
| Dialog thật, runtime SQL/AuthService trong DB riêng schema016 | 40/40 PASS |
| Tên/SDT/địa chỉ trống và khoảng trắng, SDTabc | Lỗi đúng ô/focus, input giữ, Result null |
| Nhiều ô sai | Dấu lỗi từng ô, focus ô đầu |
| Khôi phục hợp lệ | Hết dấu lỗi cũ, trả header typed đúng |
| Shipabc | Lỗi đúng ship; sửa hợp lệ thành công |
| Ba kênh/lookup/cancel/loading | Hồi quy kế thừa harness đã chạy lại đạt |
| Source trong lượt test |208 file,0 thay đổi |
| DPI | DeviceDpi120 (125%);100/150% NOT RUN |
| Native F3 bằng thao tác người dùng | Đang chờ; chưa nâng T18-K01 PASS |
| Hình thức | SKIPPED theo phân công |

Bằng chứng: results.json, source-audit.json, build-debug.log, build-release.log,
dll-hash.json. Fixture actorNRV01 được gỡ; transaction0:0; không tạo hóa đơn mới.
Harness gọi Save qua reflection trên dialog thật; không mô tả là input chuột thật.
Đã mở bản Debug mới bằng đăng nhập thường, cùng DBtest checkout; không đổi profile
DPAPI, không tự đăng nhập, không ghi mật khẩu. Native cần kiểm lại474 và tìm421
trong Giao hàng (không có kết quả). Giữ FAIL ban đầu trong hồ sơ bug/test.

## Native regression đã xác nhận

Người dùng trả lời đúng hết: dấu lỗi/focus đúng ô tên/SDT/địa chỉ, bỏ input sửa
và tìm421 Mang đi trong Giao hàng không có dòng. SQL after-native-474.json xác
nhận thông tin người nhận giữ nguyên, Chưa thanh toán/Đang giao81000,0payment/
0tiêu hao. G04-UI06 đóng sau regression; T18-K01 PASS.

Tổng ID G04:40PASS/0FAIL/16BLOCKED. T18:8/8ID PASS theo bảng kết quả, kế thừa
SQL các đợt trước và native lượt ưu tiên26/10. Không coi đây là nghiệm thu toàn
G04;16ca T16/T17 còn thiếu giữ nguyên, không chuyển thành PASS hoặc xóa checklist.
Theo yêu cầu user ưu tiên phụ thuộc để triển khai G05 trước: T18 là phụ thuộc
trực tiếp của T19/G05, phần này đã đạt. Có thể bắt đầu requirements/bộ plan G05;
chỉ code G05 sau duyệt đúng các gate đó. Không tự đánh G04 Hoàn tất.
