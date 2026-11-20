# Quyền cài đặt và vận hành

| Identity | Quyền cần | Không dùng cho |
|---|---|---|
| Operator migration | CREATE DB/DDL và chạy migration có checksum | Runtime bán hàng |
| Windows setup riêng | SQL login/user và CafeManagerSetup | Phiên app thường |
| Windows runtime riêng | SQL login/user và CafeManagerApp | sysadmin, db_owner, DDL, BACKUP/RESTORE |

Script provision trong database/security không lưu password. provision-app-user.sql nhận @WindowsLogin và @DatabaseUser kiểu nvarchar(128); login Windows phải có sẵn, không sysadmin, user phải mới. Chạy bằng tool SQL có bind tham số, không chép @WindowsLogin thành literal chứa input tùy ý. provision-setup-user.sql cần operator thay đúng placeholder identity trước chạy; sau bootstrap ALTER ROLE CafeManagerSetup DROP MEMBER [identity đã chọn]. Không cấp quyền setup cho runtime để chạy Login.

Kiểm effective permissions bằng identity runtime thật, không chỉ nhìn membership: gọi public procedure đủ use case; thử DDL và DML bảng tiền/kho/điểm/lịch sử phải bị chặn theo contract. User WITHOUT LOGIN/EXECUTE AS trong probe chỉ kiểm quyền DB, không thay chứng cứ kết nối runtime thật. Credential SQL chung không tạo ranh giới chống người đã sở hữu credential; BLL kiểm quyền thao tác theo session, không tin role do client truyền.

App giữ hash qua PasswordService, không mật khẩu session/log. Config runtime local không commit hoặc đóng gói. Thiếu config/SQL/chứng chỉ phải hiện lỗi, không fallback ngầm. Mọi cờ development/test chỉ dùng đúng đường được chọn; sản phẩm không có bỏ qua Login.

## Mất quyền ADMIN

AppSetupState.Completed không mở lại khi ADMIN bị khóa/nghỉ việc. Nếu còn ADMIN được phép, dùng chức năng quản lý tài khoản để giải quyết. Nếu mất mọi ADMIN, dừng thao tác quản trị và liên hệ operator/chủ dữ liệu; kiểm backup và hồ sơ tài khoản để chốt phương án phục hồi có phê duyệt. Bản này **không cung cấp đường recovery/reset mật khẩu mới**, không UPDATE AppSetupState về Pending, không bật bypass hoặc dùng mật khẩu mặc định.

Phục hồi backup thử vào DB riêng theo backup-restore.md để kiểm dữ liệu/quyền trước quyết định. Không tự sửa hash hoặc quyền trong DB sử dụng để chạy qua bước nghiệm thu.
