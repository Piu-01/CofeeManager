# Biên bản điều tra chỉ đọc — 18/10/2026

**Bổ sung sau khi người dùng mở F5:** AUD-R10 đã xác định process3928 và module
UI thực sự nạp từ Temp/opencode/G02B03B06DesignerRound2; không có OrbitContentHost.
Nguyên nhân giao diện cũ đã xác minh; hàng BLOCKED bên dưới giữ làm lịch sử.

Không là lượt test nghiệm thu hoặc debug. Không chạy test tạo fixture, SQLwrite,
build, login hoặc thay đổi cấu hình. Source và schema giữ nguyên.

| Mã | Thao tác / expected | Actual | Trạng thái |
|---|---|---|---|
| AUD-R01 | Đối chiếu kế hoạch G01–G08/T01–T32 với source | T06auth/T24 account management; thiếu lối bàn giao development rõ ràng | Đã đối chiếu |
| AUD-R02 | Kiểm biến kết nối, không xuất secret | Process/User/Machine của môi trường audit đều thiếu; ảnh trùng nhánh thiếu config | Đã xác minh phạm vi audit |
| AUD-R03 | Kiểm mask trong source | UseSystemPasswordChar=true; toggle khi checkbox tick | Đã xác minh source; runtime NOT RUN |
| AUD-R04 | SELECT metadata testDB |112 DB/4480 MiB,93 G02 marker/39exactRunId,0 lỗi đọc | Đã xác minh; CSV/JSON cạnh hồ sơ |
| AUD-R05 | Read Windows user/group | CafeManagerApp enabled,Users; không Administrators | Đã xác minh |
| AUD-R06 | Read SQLprincipal/mapping | Windowslogin sysadmin0,1 mappingT05,0CMG02 mapping trong112 DB | Đã xác minh |
| AUD-R07 | Truy log tạo account | Không lấy được event4720phù hợp | Không xác định actor |
| AUD-R08 | Read các task/service trùng tên dự án |0matching task/0matching service | Đã kiểm theo tên; không là toàn máy |
| AUD-R09 | Read runtimes/config filenames |Có10.0.13;2script localG01; không xuất nội dung secret | Đã kiểm kê |
| AUD-R10 | So executable screenshot với process | Chưa thấy processapp, chỉdevenv; DLLchuẩn cóOrbitContentHost | BLOCKED đường dẫn runtime của ảnh |
| AUD-R11 | DọnDB/account hoặc đổi config |Không thực hiện | NOT RUN / chưa được giao thao tác khắc phục |

Trong sandbox, Get-LocalUser/CIM/SQL ban đầu không đọc được; đã dùng quyền thực
thi ngoài sandbox cho **các lệnh chỉ đọc**. Không chạy script Complete-G02 Windows
hay Test-G02. Không tự động hóa nhập vào hộp đăng nhập hoặc cài đặt bảo mật Windows.

### Regression sau cleanup được người dùng duyệt 18/10/2026

PASS: 112 DB allowlist đã xóa, còn0 DB test; SQL Windows login cũ còn0;
Windows user CafeManagerApp không còn sau UAC. DB development và DB QuanLyCafe_ERP
vẫn ONLINE. DevelopmentSetup --verify: DPAPI config và login/permission/session
cả ba vai trò PASS. Không chạy GUI/full test suite, không sửa source trong lượt kiểm tra.
Giới hạn: QLBANHANG RESTORING không đọc được dependency nội bộ, giữ nguyên.
