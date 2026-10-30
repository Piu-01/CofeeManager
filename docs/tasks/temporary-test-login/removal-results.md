# Gỡ bypass đăng nhập — 24/10/2026

Người dùng yêu cầu gỡ ngay tiện ích; không chờ G08. Đã gỡ hoàn toàn đường bypass
khỏi source sản phẩm, hooks, profile và tools đang dùng. Không commit/push.

## Thay đổi

- Xóa LoginForm.TemporaryTestLogin.cs, Configuration/TemporaryTestLogin.cs và
  DAL TemporaryTestTarget.cs.
- Gỡ hooks trong Program/ApplicationSessionContext/LoginForm; giữ AuthService,
  LoginAsync, xử lý loading/hủy, xóa password sau thành công và LoggedIn.
- Xóa tools/ui/Start-TestLogin.ps1, Grant-TestLoginMarker.ps1 và profile
  CoffeeManager.Test. Development vẫn dùng đăng nhập thường như trước.
- Launcher manual G04 không còn tùy chọn ADMIN bypass; chỉ chọn DB test và mở Login.
  Hướng dẫn G04 đổi sang đăng nhập thungan/admin/kho bằng mật khẩu hiện có.
- Xóa output build cũ riêng của tiện ích tại runs/build (đã xác minh đường dẫn).
  Giữ lịch sử requirements/plan/test/bug và các probe cũ làm bằng chứng quá khứ,
  không dùng chúng để chạy lại bypass với source mới.
- Không sửa BLL/auth/quyền, account/password, SQL/migration hoặc dữ liệu development.

## Kiểm chứng đã chạy

Windows/.NET10, source sau gỡ; không chạy integration ghi DB trong đợt này.
Bằng chứng tại [runs/removal-20261024](runs/removal-20261024/).

| Kiểm tra | Actual | Kết quả |
|---|---|---|
| Build Debug --no-restore |0warning/0error | PASS |
| Build Release --no-restore |0warning/0error | PASS |
| Publish Release --no-restore |Xuất publish thành công | PASS |
| AuthRulesTests có sẵn |14/14; chuẩn hóa username, hash/verify và ma trận quyền | PASS |
| Quét src/tools C#/PS/JSON |0 tên helper/hook/cờ/nhãn bypass | PASS |
| Kiểm DLL Debug/Release/publish |Không còn type/flag bypass; positive controls LoginForm/AuthRepository còn | PASS |
| GUI đăng nhập đúng/sai, logout, thu hồi phiên và quyền sau gỡ |Chờ thao tác trực tiếp của người dùng; không suy PASS từ unit/build | NOT RUN |
| Hình thức |Theo phân công người dùng | SKIPPED |

Đã gỡ source không đồng nghĩa đã hoàn tất regression GUI hoặc chốt G08/T32.
T32 phải kiểm lại bản bàn giao cuối, kể cả môi trường còn cờ test cũ.
G04 giữ32PASS/0FAIL/24BLOCKED đến khi đủ bằng chứng các ca GUI riêng.

## Kiểm lại theo yêu cầu người dùng

Đã chạy [BLL/SQL regression](runs/auth-regression-20261024/results.json) trên
DB G04 test schema016 bằng runtime login thực tế của app. Không sửa account
người dùng: tạo ba fixture NVR01–NVR03 với mật khẩu ngẫu nhiên chỉ trong bộ nhớ,
kiểm xong xóa cả account/employee. Không ghi password/hash vào evidence.

**14/14 PASS**,0fixture employee còn lại, transaction0:0:
đăng nhập ADMIN/CASH/STORE; sai password; username không có; CASH được bán nhưng
không tài chính; STORE không bán; ADMIN được cả hai; khóa tài khoản; nhân viên
nghỉ; đổi role/security stamp; đổi password; Dispose phiên khi logout và đăng nhập
lại ID khác; hủy login trả không có phiên. Các ca thu hồi đều chặn phiên cũ.
Phần Dispose là hành vi lớp phiên, chưa chứng minh handler nút Đăng xuất qua GUI.

[Source audit](runs/auth-regression-20261024/source-audit.json):203file sản phẩm,
0file đổi trong lượt chạy. [Accessibility chỉ đọc](runs/auth-regression-20261024/gui-readonly.txt)
trên Login thường và process Debug mang các cờ bypass cũ đều có ô nhập, nút
Đăng nhập, trạng thái yêu cầu đăng nhập, không có nút bypass/không tự vào phiên.
Process thử cờ cũ đã dọn, giữ cửa sổ manual ban đầu.

GUI submit tài khoản/mật khẩu không tự động hóa theo quy định Computer Use:
guidance.md, mục Non-negotiable Windows Automation Safety:
“Do not automate user authentication dialogs.” Đã nhờ người dùng thao tác
sai password → đúng password → Đăng xuất; đang chờ actual, không ghi PASS phần này.
Không phải lỗi engine/quyền công cụ hoặc yêu cầu duyệt lại đợt test.

Người dùng sau đó xác nhận đã đăng nhập thungan. Agent đọc WinForms thấy
Nhân viên · CASH và màn Bán hàng; SQL xác nhận account thungan là CASH.
Đăng nhập đúng qua GUI có bằng chứng. Chưa có actual đăng nhập sai/logout qua GUI.
Phát hiện menu CASH chưa đúng tổ chức UX, ghi riêng
[T07-NAV-20261024](../g02-truy-cap-ung-dung-cau-hinh-nen/t07-khung-giao-dien-dieu-huong/bug-report.md),
chưa sửa trong lượt test và không kết luận đây là lỗi nâng quyền thành ADMIN.
