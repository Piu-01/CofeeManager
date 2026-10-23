# Nút Bỏ qua đăng nhập — TEST (tạm thời)

Chỉ dành cho bản Debug và DB test local có marker. Bấm nút trên Login để vào ADMIN, không nhập tài khoản/mật khẩu. Không có nút này khi chạy Release hoặc profile F5 development thường. Đăng nhập thường giữ nguyên AuthService.

## Mở bản test

### Trong Visual Studio (F5)

Đã sửa lỗi quyền marker trên DB test hiện tại ngày23/10/2026. Nếu chuẩn bị lại chính
fixture này và runtime chưa có quyền đọc marker, chạy một lần bằng Windows identity
có quyền quản trị DB local:

```powershell
.\tools\ui\Grant-TestLoginMarker.ps1
```

Công cụ chỉ nhận đúngDB/marker đã định nghĩa của fixture hiện tại, xác minh trước cấp
quyềnSELECT một bảng marker cho user runtime. Không chạyGRANT từ nút đăng nhập,
không cấpdb_owner/db_datareader hoặc đổi password. Fixture khác cần cập nhật cấu hình
provision được duyệt; công cụ không tự mở rộng sang DB khác.

Chọn cấu hình **Debug**, mở danh sách profile ở nút Start và chọn **CoffeeManager.Test**,
rồi bấm **F5**. Trên Login bấm **Bỏ qua đăng nhập — TEST**. Profile đọc manifest test
hiện có; thiếu/sai manifest sẽ dừng cấu hình, không rơi về DB development.

`CoffeeManager.Development` vẫn giữ nguyên để dùng đăng nhập thường. Không cần chạy
PowerShell khi đã chọn profile Test. Release không hỗ trợ profile Test.

### Bằng launcher PowerShell

Tại gốc CoffeeManager:

```powershell
dotnet build src/CafeManager.WinForms/CafeManager.WinForms.csproj -c Debug -o docs/tasks/temporary-test-login/runs/build/Debug
.\tools\ui\Start-TestLogin.ps1
```

Launcher mặc định dùng manifest `docs/tasks/g03-du-lieu-phuc-vu-ban-hang/runs/manual-g03-20261019/manifest.json`, chọn rõ DB `CoffeeManager_Test_G03_20261019_215457_29e552a9_demo`. Không ghi/sửa manifest hoặc DB. Đọc profile kết nối local đã mã hóa bằng DPAPI, chỉ thay catalog cho process con; không đọc mật khẩu tài khoản ứng dụng.

Launcher không tự vào app. Trên Login bấm **Bỏ qua đăng nhập — TEST**. Nếu marker/DB/admin không hợp lệ, giữ Login và báo lỗi; không tự đổi DB hoặc sửa account. Chế độ setup và đăng nhập bình thường vẫn dùng quy trình cũ.

Muốn dùng bản Debug khác vừa build hoặc manifest test khác còn hiệu lực:

```powershell
.\tools\ui\Start-TestLogin.ps1 -ManifestPath '<manifest DB test>' -ExecutablePath '<Debug exe vừa build>'
```

Các biến `CAFEMANAGER_APP_CONNECTION`, `CAFEMANAGER_TEST_LOGIN`, `CAFEMANAGER_TEST_DATABASE`, `CAFEMANAGER_TEST_MARKER` chỉ được đặt cho process con. Không cần đặt biến môi trường toàn máy; không chép connection string có secret vào chat/source. Marker được kiểm bằng chính kết nối runtime trước mỗi lần tạo phiên test.

## Phạm vi và trạng thái

- Chưa chạy kiểm chức năng trong pha triển khai; xem test-plan.md để duyệt đợt test. Build Debug/Release sạch không có nghĩa nút đã được nghiệm thu.
- Nút bỏ qua password cho test, không chứng minh đăng nhập thường hoặc xác thực đã PASS. BLL/SQL vẫn kiểm account/role/security stamp hiện tại.
- Hỗ trợ code ở DPI100/125/150%, ưu tiên125%; agent chỉ kiểm chức năng/layout kỹ thuật ở125%, người dùng tự kiểm hình thức. Không tự đổi Scale hoặc ép Program.cs chạy DPI cố định.

## Gỡ ở cuối G08 / T32 — bắt buộc trước PASS G08

Theo yêu cầu người dùng ngày22/10/2026, checklist này thực hiện ở cuối G08, trước chốt
PASS. Chỉ ẩn nút hoặc dùng `#if DEBUG` không đủ: phải gỡ đường bypass khỏi source và
kiểm lại Debug/Release/publish. Chưa gỡ hoặc thiếu bằng chứng kiểm lại thì **G08 chưa PASS**.
Điều kiện đã ghi trong kế hoạch dự án và AGENTS.md; các mục dưới đây hiện vẫn chưa thực hiện.

- [ ] Xóa `Forms/LoginForm.TemporaryTestLogin.cs` và `Configuration/TemporaryTestLogin.cs` trong WinForms.
- [ ] Xóa `DataAccess/Repositories/TemporaryTestTarget.cs`.
- [ ] Gỡ hook `#if DEBUG` của Program và property/configure call trong ApplicationSessionContext.
- [ ] Gỡ partial method SetTemporaryTestLoginBusy cùng các lời gọi trong LoginForm. Giữ RunLoginAsync và luồng đăng nhập thường nếu còn dùng; không xóa AuthService/LoggedIn.
- [ ] Xóa launcher `tools/ui/Start-TestLogin.ps1`, công cụ `tools/ui/Grant-TestLoginMarker.ps1`, profile `CoffeeManager.Test` trong launchSettings.json và nhánh đọc manifest trong Program; gỡ hướng dẫn/flags test khỏi tài liệu vận hành nếu có; giữ hồ sơ lịch sử test theo quy định dự án.
- [ ] Build Debug/Release và kiểm publish cuối, xác nhận không còn nút/helper bypass kể cả khi có flags test; regression đăng nhập thường/sai mật khẩu/logout/loading/hủy/thu hồi phiên/quyền.
- [ ] Lưu bằng chứng đã gỡ ở hồ sơ T32 và báo cáo G08 trước PASS. Hiện chưa gỡ vì dự án còn đang triển khai.
