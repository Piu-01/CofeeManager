# Ma trận44UX-ID — T31

Nguồn mô tả giữ nguyên UX. Truy vết trách nhiệm theo kế hoạch mục12.4, từng ca chạy/biên bản được cập nhật sau duyệt đợt test.

| UX-ID | Thao tác gốc | Expected gốc | G08 tích hợp |
|---|---|---|---|
| UX-K01 | Login đủ ba role | Đến đúng màn, menu và quyền đúng | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K02 | Sai mật khẩu so với mất DB | Hai thông báo khác nhau, không mở MainForm sai | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K03 | Đăng xuất khi có nháp | Có lựa chọn ở lại/bỏ; session và ngữ cảnh bị xóa khi thoát | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K04 | Khóa tài khoản đang có phiên | Thao tác mới bị chặn khi kiểm tra lại, không bypass quyền | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K05 | Chọn bàn trống rồi rời, chưa thêm món | Không để hóa đơn rỗng chiếm bàn | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K06 | Chọn bàn có đơn | Tải đúng đơn, không tạo thêm | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K07 | Thêm món, sửa lượng, hai ghi chú khác nhau | Dòng và tổng đúng, focus thuận tiện, không gộp sai | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K08 | Hai phiên sửa cùng đơn | Báo xung đột, tải lại, không ghi đè | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K09 | Khách mới/vãng lai/đổi khách | Không hồ sơ lẻ dùng chung, preview theo đúng khách | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K10 | Có hạng và chọn KM | Nguồn giảm đúng, không cộng dồn, không có Dùng điểm | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K11 | Preview đang tải/thất bại | Không thanh toán theo số cũ | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K12 | Thu 81.000, tiền mặt đưa 100.000 | Thu 81.000, thối 19.000; hiển thị đúng | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K13 | Chia 30.000 + 51.000, thiếu/dư phân bổ | Chốt đúng khi đủ; lỗi tại bảng khi lệch | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K14 | Giảm 100%, ship 0 hoặc ship >0 | Luồng 0 ₫ không tạo dòng 0; còn ship phải trả | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K15 | Preview đổi lúc xác nhận | Hiện số mới, yêu cầu xác nhận lại | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K16 | Thiếu kho khi checkout | Giữ đơn mở, báo rõ và không báo đã thu | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K17 | Double-click/Enter/F8 lặp | Chỉ một yêu cầu chốt; phím không chốt trực tiếp | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K18 | Timeout trước/sau commit | Có đối chiếu, không retry mù, trạng thái phản ánh DB | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K19 | SQL thành công nhưng in lỗi | Giữ kết quả đã trả, chỉ In lại | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K20 | Hủy đơn mở và thử hủy đơn đã trả | Luồng mở hợp lệ; đơn trả không cho hủy tài chính | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K21 | Giao hàng thiếu người nhận/địa chỉ | Chặn chốt, lỗi đúng trường | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K22 | Chuyển tiến độ đơn giao đã trả | Tiền/điểm/tồn không đổi, trạng thái riêng rõ | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K23 | Lịch sử đổi filter/ngày/đơn nhiều trang | Kỳ rõ, paging đúng, chi tiết và in dùng snapshot | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K24 | Nhập nháp rồi bỏ | Không tăng tồn/chi/nợ, có hỏi bỏ nháp | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K25 | STORE nhập so với ADMIN trả một phần | STORE không trả ngay; ADMIN thấy chi/nợ đúng nguồn | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K26 | Nhập NVL trùng, đơn vị g/kg | Focus dòng cũ; đơn vị và lượng không gây hiểu sai | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K27 | Xuất thiếu một NVL hoặc trả hàng | Không chốt một phần; không hứa tự cấn nợ | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K28 | Kiểm kê ô trống, lệch có/không lý do | Chặn số chưa đếm và thiếu lý do theo policy | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K29 | Kho đổi sau snapshot | Yêu cầu lấy/đếm lại, không tự chốt bằng dữ liệu cũ | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K30 | Chốt kiểm kê lần hai/header reset | Không có đường thao tác lại trên UI | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K31 | Trả nợ vượt, đồng thời, timeout | Reload/đối chiếu, không chi trùng, STORE chỉ xem | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K32 | Editor thu chi nguồn hệ thống | Chỉ đọc/mở nguồn, không sửa hoặc ghi lần hai | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K33 | Điều chỉnh điểm và sửa chính sách | Lý do/preview rõ, số dư chỉ đọc, không đổi lịch sử đơn | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K34 | Ca qua đêm/overlap/thiếu giờ ra | Ngày giờ rõ, lỗi xử lý trước chốt lương | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K35 | Chốt lương rồi đổi hồ sơ/chi lần hai | Snapshot giữ, khóa kỳ, không chi trùng | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K36 | Lương 0/chốt chưa chi | Nhãn phân biệt đúng, không tạo phiếu 0 | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K37 | Báo cáo rỗng, lỗi DB, query trả muộn | Không dùng 0 giả, không hiển thị số của filter cũ | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K38 | Tài sản/khấu hao | Nhãn tham khảo, không coi lưu hồ sơ là ghi chi | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K39 | Tab/Enter/Esc/F2/F3/F4/F8 | Thứ tự focus đúng, không chốt nguy hiểm hoặc mất nháp | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K40 | 1280×720, 1366×768; DPI 100/125/150% | Không cắt text/nút/footer, grid đọc và cuộn được | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K41 | Tên tiếng Việt dài, ghi chú nhiều dòng, tiền lớn | Không vỡ layout; dấu, đơn vị, số không bị mất | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K42 | Query chậm, đổi màn, đóng dialog | UI responsive; kết quả không cập nhật control đã đóng | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K43 | Đóng app/mở lại có đơn mở | Đơn được tải từ DB; không auto-hủy/auto-thu | Kết quả/subcase xem test-results T31; chưa kiểm đủ toàn ID trong G08, giữ lịch sử task sở hữu |
| UX-K44 | Kết nối/sao lưu ở bản publish | Thông báo dễ hiểu, quyền đúng, không lộ bí mật | PASS — T30-K07/B01 regression; kế thừa backup/quyền không bị ảnh hưởng; gate T32 tổng thể vẫn BLOCKED. |

