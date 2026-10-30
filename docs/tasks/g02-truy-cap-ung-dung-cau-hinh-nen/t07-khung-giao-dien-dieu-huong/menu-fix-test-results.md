# T07-NAV-20261024 — Sửa menu theo ma trận quyền

Người dùng xác nhận hướng sửa; duyệt debug bao gồm regression tương ứng.
Chỉ sửa MainForm.cs và SessionHomeControl.cs; không đổi AuthService/BLL/SQL,
ba role ADMIN/CASH/STORE, account hoặc mật khẩu sử dụng hiện tại.

## Hành vi sau sửa

| Role | Menu nghiệp vụ | Màn hình mở đầu |
|---|---|---|
| CASH | Bán hàng, Lịch sử hóa đơn, Giao hàng, Khách hàng, Tồn phục vụ bán | POS |
| STORE | Nguyên liệu / Tồn, Menu / Công thức | Trang phiên kho |
| ADMIN | Toàn bộ8module hiện có và Danh mục nền | Trang phiên quản lý |

Trang phiên, Thông tin hệ thống và Đăng xuất vẫn có theo phiên. Không thêm module
tương lai hoặc dashboard giả. CASH chọn bàn/món/ưu đãi trong POS; không còn ba
lối quản trị riêng. Nhãn không gian/breadcrumb/role rõ Thu ngân/Kho/Quản lý.

## Môi trường và bằng chứng

Windows, .NET10, DeviceDpi120=125%, SQL Server localhost,
CoffeeManager_Test_G04_20261024_152759_12204643_manual, schema016, source chưa commit.
Fixture mật khẩu ngẫu nhiên trong bộ nhớ; AuthService đăng nhập ba account test
NVM01–NVM03 bằng runtime SQL thực tế. Kiểm component MainForm thật; CASH dùng
SalesControl/BLL/DAL thật và đọc dữ liệu SQL. Nội dung các module khác dùng stub
vì đợt này kiểm menu/startup, không kiểm lại CRUD của chúng.

Artifacts: [menu-results.json](../runs/menu-role-fix-20261024/menu-results.json),
[source-audit](../runs/menu-role-fix-20261024/source-audit.json),
[scope-audit](../runs/menu-role-fix-20261024/change-scope-audit.json).
203file source cố định trong lượt kiểm; so với lượt auth chỉ2file WinForms thay đổi.

| Phạm vi / ID tham chiếu | Expected / actual | Kết quả |
|---|---|---|
| T07-K01,3role | CASH vào SalesControl, ADMIN/STORE vào SessionHomeControl đúng vai trò | PASS3kiểm tra |
| T07-K02,3role | Đúng danh sách/thứ tự menu và Catalogs chỉADMIN | PASS6kiểm tra |
| T07-K01,nhãn3role | Caption vai trò đúng, CASH breadcrumb Thu ngân / Bán hàng | PASS4kiểm tra |
| T07-K02,lối POS | CASH có Chọn bàn,F3Khách/Ưuđãi,F4Đơn mở bật/hiện; SQL tảiPOS thành công | PASS2kiểm tra |
| Fixture/safety | Account admin/thungan/kho giữ role/stamp;0employee fixture còn lại;transaction0:0 | PASS3kiểm tra |
| Build Debug/Release | Cả hai0warning/0error; output riêng tránh khóa DLL app đang mở | PASS |
| Navigation nháp/cache,handler bàn/khách/ưu đãi/CRUD/checkout | Không thay code này; chưa thao tác lại trong đợt menu | NOT RUN |
| Hình thức | Người dùng tự kiểm, không chụp ảnh/hover/spacing | SKIPPED |
| DPI100/150% | Không đổi WindowsScale | NOT RUN |

Tổng component18/18PASS. Lần chạy đầu chưa vào test vì build vào output mặc định
bị process cũ18436 khóa DLL; giữ `attempt1-file-lock.log` và source-audit tương ứng.
Lượt sau chỉ đổi output của harness, source sản phẩm/expected không đổi; không
đóng cửa sổ người dùng. Không coi lỗi khóa file là lỗi sản phẩm hoặc xóa lịch sử.

Đã dọn3account/employee fixture, giữ DB G04 cho phiên manual đang diễn ra.
Đã mở app Debug mới PID13836 ở Login, không bypass. Launcher manual chọn bản mới
mặc định; yêu cầu người dùng đăng nhập thường. Cửa sổ cũ vẫn giữ ngữ cảnh hiện tại.
Không commit/push; không nâng24ca GUI G04 thànhPASS từ kết quả component này.
