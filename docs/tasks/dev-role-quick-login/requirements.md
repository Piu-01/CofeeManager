# Đăng nhập nhanh theo role khi development

Ngày 09/11/2026. Requirements, plan, test và sửa DQ-B01 kèm regression đã được người dùng duyệt. Regression:57/57 subcase và142unit PASS; DQ-B01 đã đóng trong phạm vi kiểm. Native còn BLOCKED/chưa đủ. Chưa nghiệm thu toàn tính năng.

## Mục tiêu

Người phát triển bấm một nút ADMIN, CASH hoặc STORE tại LoginForm để vào đúng phiên và màn bắt đầu, không phải nhập lại tài khoản/mật khẩu mỗi lần kiểm thử.

## Phạm vi đề xuất

- Bổ sung khu vực ghi rõ “Đăng nhập nhanh — DEV” với ba nút ADMIN/CASH/STORE trong chế độ development được bật rõ ràng. Chạy bình thường và bản Release/publish không bật tiện ích.
- Cấu hình một lần ba tài khoản hiện có và mật khẩu tương ứng trên máy dev; lưu riêng theo Windows user và DB bằng DPAPI, không lưu vào repo, source, log hoặc cấu hình bàn giao. Không tự đặt lại mật khẩu hay tạo tài khoản trên DB đang dùng.
- Mỗi nút thực hiện xác thực thật qua AuthService.LoginAsync, kiểm tra role trả về khớp nút trước khi phát sự kiện đăng nhập. Không tạo SessionContext giả hoặc thay role của tài khoản.
- Dùng lại luồng pending/cancel/generation của LoginForm: chỉ một yêu cầu đang chạy, khóa cả nút thường và ba nút nhanh; đóng form hoặc logout không nhận callback cũ. Mật khẩu không nằm trong session.
- Tài khoản khóa/nghỉ việc, sai mật khẩu, sai role, thiếu cấu hình hoặc lỗi DB hiển thị lỗi phù hợp; không tự chọn một tài khoản khác hay hạ điều kiện xác thực.
- Logout về LoginForm, cho phép chọn role khác bằng nút. Quyền, điều hướng và kiểm tra phiên dùng cơ chế hiện có.
- Ghi checklist gỡ toàn bộ tiện ích/cấu hình dev trước bàn giao cuối G08/T32. Giữ nguyên lịch sử gỡ tiện ích TEST ngày 24/10 và 325 ID nghiệm thu hiện có.

## Giả định và ranh giới

- Phạm vi đã duyệt: bấm nút để đăng nhập nhanh bằng tài khoản thật, cấu hình credential một lần trên máy dev.
- Không sửa schema, migration, dữ liệu nghiệp vụ hoặc password tài khoản hiện tại; nếu thiếu tài khoản role, báo rõ và dùng chức năng quản trị hiện có.
- Tiện ích dùng phục vụ phát triển; các ca nghiệm thu luồng nhập mật khẩu vẫn thực hiện bằng đăng nhập thường.
- Đây là bổ sung độc lập liên quan T06 và gate T32. Yêu cầu mới cho phép đề xuất tiện ích dev trở lại, nhưng không hủy điều kiện gỡ trước bàn giao.

## Tiêu chí hoàn tất

1. Sau cấu hình một lần, từng nút vào đúng identity/role và màn bắt đầu ADMIN/CASH/STORE, không cần nhập lại.
2. Xác thực, khóa/nghỉ việc, sai role và quyền BLL vẫn có hiệu lực; lỗi giữ LoginForm và cho phép thử lại phù hợp.
3. Click lặp, đóng khi chờ, logout/đăng nhập role khác không sinh hai phiên hoặc callback muộn.
4. Credential được bảo vệ local, tách DB/Windows user; không xuất plaintext vào source/log/package.
5. Chế độ thường và Release/publish không có đường bật đăng nhập nhanh. Checklist T32 yêu cầu gỡ source/helper/hooks/cấu hình trước bản bàn giao cuối.
6. Build phù hợp; đợt test chức năng được duyệt riêng và lưu kết quả thực. Hình thức do người dùng kiểm, agent kiểm chức năng tại DPI125%, không đổi Windows Scale.

## Bước tiếp theo

Đã triển khai, test và sửa/regression DQ-B01. Xem report.md và bug-regression-results.md; phần còn lại là bằng chứng native chưa đủ, không có đợt bug mới chờ duyệt.
