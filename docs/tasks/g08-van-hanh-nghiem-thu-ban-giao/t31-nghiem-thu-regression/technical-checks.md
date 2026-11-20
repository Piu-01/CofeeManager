# T31 — Kiểm tra kỹ thuật chuẩn bị nghiệm thu

02/11/2026. Không chạy E2E nghiệm thu. Đã chuẩn bị:

- Ma trận trích đủ325ID duy nhất từ kế hoạch và44UX-ID, giữ nguyên mô tả và link nhóm sở hữu; kết quả trong lượt G08 đểNOT RUN, không xóa kết quả lịch sử.
- G08Probe build thành công với -m:1 -nr:false; bootstrap/login smoke ở T30 thành công. Có NU1900 do feed audit không truy cập được, không coi audit sạch.
- E2EProbe chứa các subcase SQL bán2dòng/2TT, nhập/trả2lần, kiểm kê stale, công qua đêm/chốt/chi, giao hàng/finance hash, CASH/STORE login/Finance denied/khóa phiên và transaction0:0. Chưa chạy; không gọi đó là PASS. Fixture audit giá/công thức chỉ thực thi trong RunAcceptance/Demo.
- FailureProbe chỉ chấp nhận mã lỗi nghiệp vụ52903 cho stale stocktake, không nuốt timeout. Fault trước/sau commit từng usecase và native E2E theo native-test-guide, thiếu điểm fault có bằng chứng phải BLOCKED.
- Source sản phẩm/migrations không sửa để phục vụ test. SQL state dùng capture biến trước SELECT; không xuất password/hash/connection string vào evidence.

Hình thức SKIPPED; chức năng125% chưa chạy native trong G08,100/150%NOT RUN. Đầu ra chuẩn bị T31 dùng được cho T32 tạo ứng viên; chưa chốt T31/325ca PASS.
