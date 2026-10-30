# Tiếp tục G04 theo yêu cầu người dùng25/10/2026

Kết quả sau phản hồi người dùng: T16-K03 FAIL do unhandled exception khi Confirm.
Đã tái hiện G04-UI05 trong StarDustOverlay.Show, trước gửiSQL. DB vẫnopen81000,
0payment,0tiêu hao,kho trước/sau bằng nhau. Bằng chứng/lượt điều tra kế tiếp tại
../checkout-crash-investigation-20261025/. Source207file không đổi trong đợt test;
chưa sửa bug. Tổng cập nhật33PASS/1FAIL/22BLOCKED; các chữ “đang chờ” dưới là lịch sử.

Người dùng xác nhận “ok rồi” phần căn giữa và yêu cầu test tiếp. Áp dụng đợt test
G04 đã duyệt; source207file đóng băng, không sửa sản phẩm/migration trong lượt này.
App thật là runs/method-center-20261025/Debug/CafeManager.WinForms.exe,
cửa sổ592796, CASH đã đăng nhập bình thường bởi người dùng. DB riêng
CoffeeManager_Test_G04_20261024_152759_12204643_manual, marker kiểm đúng,
schema016. Không chạy test trên development.

| Nhánh | Thao tác / expected | Actual | Kết quả |
|---|---|---|---|
| T16-K05, F8 | Từ POS đơnHD20250411, F8 chỉ mở preview | Modal Xác nhận thanh toán mở, payable81000, cash81000, confirm bật; không ghi thanh toán | PASS nhánh; chưa PASS toànK05 |
| T16-K04 nhập thiếu | Thử set_value80000 ở cash field rồi kiểm disable | Helper báo element165 not available in cached app state; làm mới window và click vẫn coordinate input geometry unavailable | BLOCKED transport; không coi lỗi sản phẩm |
| Input bàn phím trong modal | Focus đọc được cash; Ctrl+A/type80000 phải đổi ô | Đọc lại vẫn81000; không chứng minh input đã đến modal | NOT CONCLUSIVE, không PASS validation từ lần thử này |
| Trạng thái DB trước và sau mởF8 | Chưa thanh toán,81000,0payment | Đúng; snapshot kho trước chốt được lưu | PASS đối chiếu read-only |
| Source | Không thay source |207file giữ nguyên, xem progress.json | PASS audit |
| Hình thức ô giữa | Người dùng trực tiếp xác nhận OK | Chỉ ghi nhận xác nhận người dùng cho phần căn giữa | USER CONFIRMED, không agent visual PASS |

Đã gửi một yêu cầu thao tác manual: cash80000/abc/overflow phải bị chặn,100000
thối19000,Enter không chốt,Quay lại rồiF8 lại vàchốt. Đang chờ người dùng trả kết
quả; chưa nâng T16-K03/K04 hoặc thay tổng33PASS/0FAIL/23BLOCKED.

Evidence accessibility không có screenshot: before-invalid-input.json,
cash-short80000.json,cash-short80000-settled.json,modal-target-refreshed.json.
Snapshot trướccheckout: before-checkout.json. Câu lệnh đọc snapshot thành công;
bước format output terminal sau đó từng dùng ConvertFrom-Json dư vào mảng đã parse
và báo lỗi; file snapshot gốc đầy đủ, đọc lại xác nhận được. Không là lỗi sản phẩm.
Không chốt đơn bằngSQL để thay cho thao tác GUI còn thiếu.
