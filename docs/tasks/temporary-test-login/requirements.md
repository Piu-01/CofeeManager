# Nút đăng nhập test tạm thời

22/10/2026 — requirements đã được người dùng duyệt bằng “ok”, gồm làm rõ nút bỏ qua tên/mật khẩu; plan tiếp theo đã được duyệt và triển khai. Chờ duyệt đợt test chức năng. Đây là tiện ích phát triển độc lập, không mở lại hoặc thay nghiệm thu T06/G02/G03.

## Mục tiêu

Theo người dùng làm rõ: thêm nút bấm vào thẳng app để test, **bỏ qua bước nhập và xác thực tên/mật khẩu**, không làm chức năng điền mật khẩu tự động. Giữ nguyên đăng nhập thường của dự án. Theo chốt bổ sung ngày22/10/2026, phải gỡ hoàn toàn tiện ích ở **cuối G08 / T32**, có bằng chứng kiểm lại sau gỡ thì G08 mới được PASS; không chỉ ẩn nút trong bản phát hành.

## Phạm vi đề xuất

1. Thêm một nút **Bỏ qua đăng nhập — TEST** tại LoginForm, tái sử dụng theme/control hiện có. Bấm một lần vào phiên ADMIN test, không cần điền tên hay mật khẩu. Không tự vào khi khởi chạy.
2. Chỉ biên dịch trong **Debug**, mặc định tắt. Chỉ bật khi launcher test truyền tùy chọn rõ ràng và xác minh server local, DB test cụ thể cùng marker đúng. Bản Release và kết nối DB sử dụng thật/development thông thường không có đường đăng nhập nhanh. Không chỉ dựa vào tên DB chứa chữ “test”.
3. Nút dùng helper test tạm đọc identity hiện tại của tài khoản ADMIN trong DB test để tạo SessionContext, **không gọi LoginAsync và không đọc/điền mật khẩu**. Identity phải đúng account có thật, đang hoạt động, nhân viên còn làm việc, role ADMIN và security stamp hiện tại; không tự gán role hoặc tạo tài khoản. Quyền BLL/SQL và kiểm phiên sau khi vào app vẫn chạy như cũ. Không sửa AuthService hoặc giảm xác thực của nút Đăng nhập thường.
4. Tái sử dụng luồng LoggedIn/ApplicationSessionContext để mở đúng một MainForm, xử lý đăng xuất, thu hồi phiên và role như đăng nhập thường. Giữ nguyên username/password/show-password/Đăng nhập thường và validation của chúng. Nút thường vẫn kiểm thử độc lập được, không được tính PASS xác thực từ nút test.
5. Tắt cả hai nút khi đang gửi; chặn gửi lặp, dispose phiên nếu cửa sổ đóng hoặc yêu cầu hết hiệu lực; lỗi tài khoản/marker giữ Login và không rơi sang DB khác. Không hiện chuỗi kết nối qua textbox hoặc thông báo lỗi.
6. Một launcher test rõ DB/marker/tùy chọn; giữ nguyên profile Development. Người dùng đã yêu cầu bổ sung profile **CoffeeManager.Test** riêng để chọn và chạy bằng F5; đọc manifest explicit, không tự vào app hoặc fallback DB khi lỗi. Hướng dẫn cách bật/tắt và danh sách file cần gỡ trước bàn giao, gồm cả profile Test. Thêm mục kiểm “đã gỡ đăng nhập test” vào hồ sơ tiện ích; chưa tự thay checklist325ca/T01–T32.

## Phân công kiểm giao diện và DPI

Đã cập nhật AGENTS.md theo chỉ dẫn người dùng: người dùng tự kiểm hình thức. Agent vẫn code tương thích100/125/150%, ưu tiên125%, chỉ kiểm chức năng/layout kỹ thuật ở125%. Không ép Program.cs về DPI cố định, không tự đổi Scale sang100/150%. Hình thức ghiSKIPPED theo phân công; không yêu cầu duyệt miễn test lại mỗi đợt. Không xóa lịch sử kiểm thử cũ.

## Giả định

- Một nút ADMIN đủ cho yêu cầu hiện tại; không tự thêm nút CASH/STORE hoặc đăng nhập nhanh vào DB sử dụng thật.
- Chỉ dùng DB test riêng đã xác minh. Nếu cần reset, phải xác định DB/fixture được phép; không tự thay account hoặc password của DB development.
- Tiện ích chỉ cần cấu hình kết nối DB test đã có, không cần password tài khoản ứng dụng hoặc file accounts.dpapi. Thiếu kết nối/identity hợp lệ thì giữ Login và báo cấu hình test chưa sẵn sàng.
- Chức năng test vẫn tuân thủ quyền thực và sự cho phép của công cụ thao tác. Nút test không được dùng làm cách vượt hạn chế của công cụ nếu công cụ đang cấm thao tác xác thực.

## Tiêu chí hoàn thành

- Đăng nhập thường vẫn chạy nguyên luồng và vẫn từ chối mật khẩu sai. Debug tắt flag hoặc DB sai/marker sai đều không tạo được phiên test. Release không biên dịch nút/helper bỏ qua đăng nhập.
- Debug bật đúng cấu hình test, để trống username/password rồi bấm nút TEST vẫn mở đúng một MainForm với identity ADMIN thực của DB test. Tài khoản khóa/nghỉ việc/không còn role ADMIN không tạo phiên. Không đọc password, không tự cấp quyền SQL hay sửa account để test PASS.
- Đăng xuất/quay Login, đóng trong lúc đang gửi và nhấn lặp được kiểm chức năng trên DB test riêng. Kiểm ở125%, không chạy kiểm thị giác.
- Build Debug/Release sạch; giữ hash source trong từng lượt test, lưu biên bản và phần chưa chạy. Chưa cấpPASS cho module hoặc nghiệm thu toàn dự án từ tiện ích.
- Có hướng dẫn gỡ tạm, kiểm Release không còn nút và ghi rõ phải gỡ trước bàn giao cuối; chưa giả định đã gỡ khi dự án chưa hoàn thành.

## Bước tiếp theo

Theo task-approval-workflow: duyệt requirements này → viết plan cụ thể theo các điểm gọi Auth/UI/DAL/launcher đang có → duyệt plan trước code tiện ích → duyệt đợt test sau triển khai. AGENTS.md đã được chỉnh ngay theo yêu cầu trực tiếp, không chờ tiện ích đăng nhập.
