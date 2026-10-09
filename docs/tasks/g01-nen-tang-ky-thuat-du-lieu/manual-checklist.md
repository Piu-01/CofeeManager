# G01 — Phối hợp nghiệm thu ba ca manual

**Duyệt:** Người dùng xác nhận “duyệt”; chọn tự tạo tài khoản Windows riêng và
tự kiểm chứng Designer. Chưa có bằng chứng PASS cho ba ca này.

## Môi trường đã chuẩn bị

- Run: `G01_Manual_20261008_225305_cebd98ad`.
- DB mới riêng: `CoffeeManager_Test_T05_20261008_225305_cebd98ad`.
- Schema → seed → migration 001 → fixtures T05 đã chạy; view có 10 bàn.
- Manifest và migration/environment JSON: `runs/G01_Manual_20261008_225305_cebd98ad/`.
- Login Windows hiện có `LAPTOP-79UNRIRC\Lenovo` là sysadmin; chưa có login app riêng.
- Không ghi dữ liệu thử vào DB dùng thật. Source app/migration không sửa.

## T03-K08 — Chờ tên tài khoản Windows riêng

**Cập nhật sau người dùng tạo account:** Đã xác nhận local user CafeManagerApp
enabled; tạo SQL Windows login LAPTOP-79UNRIRC\CafeManagerApp, ánh xạ user
CafeManagerApp_Runtime vào CafeManagerApp trên DB manual. SQL impersonation
đối chiếu: sysadmin=0, db_owner=0, app role=1, đọc10 bàn; procedure mã thiếu trả
51109, UPDATE HOADON bị DENY229. Bằng chứng app-principal-check.txt trong runs manual.
Lần probe đầu sqlcmd thiếu -I gây lỗi compile1934; chạy lại -I cho kết quả trên.
Không sửa schema/procedure hoặc quyền để né lỗi.

Đã chuẩn bị script local (ignored) config/local/Run-ManualApp.ps1, không password;
guard đúng Windows identity, đặt connection bằng Windows auth riêng cho process,
kiểm SQL identity trước khi mở app. Đã mở RunAs; chờ người dùng nhập mật khẩu
trực tiếp tại máy. Actual Windows app process/slow-query chưa kiểm chứng nên
hai ca vẫn BLOCKED. Script lưu metadata vào LOCALAPPDATA/CoffeeManager/manual
của account app; không thu/lưu mật khẩu.

1. Người dùng tạo Windows account tiêu chuẩn dành cho app.
2. Gửi tên `MÁY\tài_khoản`; không gửi mật khẩu trong chat hoặc Git.
3. Sau khi có tên, cấu hình SQL Windows login không sysadmin, dùng template
   `database/security/provision-app-user.sql` ánh xạ user vào CafeManagerApp
   trên DB test trên; xác nhận không db_owner/quyền server quản trị.
4. Chạy app bằng identity riêng, đặt CAFEMANAGER_APP_CONNECTION trong process
   của account đó; SQL chứng minh identity/role/DB và kiểm cấu hình/log.

**Expected:** App chỉ đọc view/execute contract theo role; migration identity
tách biệt; không credential vào source/log. **Actual:** chưa có account, BLOCKED.

## T04-K05 — Người dùng kiểm chứng Designer

1. Mở Visual Studio cửa sổ riêng → Open a project or solution → `CafeManager.slnx`.
2. Solution Explorer → CafeManager.WinForms → Forms → StartupForm.cs.
3. Chuột phải View Designer (Shift+F7). Đợi Designer tải xong.
4. Ctrl+S, đóng tab Designer và mở lại View Designer.
5. Gửi ảnh Designer sau mở lại và xác nhận có/không lỗi. Nếu VS đổi file,
   giữ diff để đối chiếu; không sửa file theo cảm tính hoặc xóa resx.

**Expected:** Designer hiển thị Form/control, không lỗi load/serialization,
không mở SQL từ constructor Designer. **Actual:** chờ bằng chứng, BLOCKED.

**Bằng chứng đã nhận:** ảnh Designer hiển thị đúng Form tại DPI 125%, không màn
lỗi. Source hiện có không đổi so manifest; mới có csproj.user local. Chờ xác nhận
đã Ctrl+S, đóng tab và mở lại thành công để chốt ca. Ảnh SSMS vẫn dùng login
Lenovo sysadmin, không chứng minh app principal riêng đã tạo.

**Cập nhật:** Người dùng xác nhận Ctrl+S/đóng/mở lại thành công: T04-K05 PASS.
Ảnh SSMS Personalization Account không phải Windows Settings; hướng dẫn người
dùng Win+I → Accounts → Other users, hoặc Win+R → ms-settings:otherusers.

**Sau sửa B07:** StartupForm thay layout/styles, cần lặp save/close/reopen với
source mới. K05 hiện BLOCKED regression; xác nhận cũ giữ cho source trước sửa.
Ảnh native125% của B07 ở runs/G01_B07_20261008_232240; DPI100/150 và account
app/slow-query còn cần phối hợp.

**Mới nhất:** Đã thao tác VS18 qua DTE lưu/đóng/mở lại Designer B07, xác nhận
native screenshot/UIAutomation, source không đổi. K05 PASS. Account app và
slow-query vẫn chưa có bằng chứng; chưa có local user CafeManagerApp trên máy.

## T05-K07 — Tiếp tục sau khi có app identity riêng

- Khởi động app thật qua Program với connection riêng, tải thành công 10 bàn;
  filter không khớp trả trạng thái rỗng khác trạng thái lỗi.
- Session SQL test riêng chạy `database/tests/t05/slow-ui.sql` (BAN lock 20 giây).
- App bấm tải: hiện loading, khóa gửi lặp, resize/di chuyển/control vẫn phản hồi.
- Hủy: không cập nhật kết quả muộn; nút tải khả dụng sau cancellation.
- Lặp lock/tải rồi đóng Form khi đang chờ: process thoát, không exception/control
  disposed; session lock rollback và không để transaction mở.
- Thử tải lại sau hủy; ghi thao tác/bằng chứng, giữ input/dữ liệu khi lỗi.
- Kiểm 1280×720/1366×768, DPI 100/125/150%, Tab/focus; ảnh thực tế cho DPI.

**Actual:** chưa chạy các thao tác này; không dùng test xUnit hoặc account admin
thay cho nghiệm thu runtime identity/WinForms. Trạng thái BLOCKED.
