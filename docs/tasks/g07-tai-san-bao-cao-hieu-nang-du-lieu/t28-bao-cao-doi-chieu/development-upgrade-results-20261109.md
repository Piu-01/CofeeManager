# G07-DEV01 — Đã nâng development và kiểm lại Tổng quan

09/11/2026. Người dùng đã duyệt nâng021→024→027 kèm backup, đối chiếu dữ liệu và kiểm đọc. **Lỗi thiếu procedure ở Tổng quan đã được xử lý trên DB development và kiểm chức năng native PASS theo phạm vi này.** Không nghiệm thu thay G06/G07/G08 hoặc325ID còn thiếu.

## Backup và nâng schema

Đã đóng app development bằng Alt+F4 bình thường, xác nhận cửa sổ biến mất; không kill process hoặc Visual Studio. Kiểm đúng localhost/CoffeeManager_Development, marker CoffeeManager.Development.v1/SchemaReady1 và schema021.

| Chặng | Lệnh và bằng chứng | Kết quả |
|---|---|---|
|021→024|`tools/database/Update-DevelopmentG06.ps1 -Apply`; [manifest](../../g06-nhan-su-cong-luong/runs/development024_20261109_214928_9d110509/manifest.json)|Succeeded;022–024 Applied; hash trước/sau81E066…2A76E3 khớp |
|024→027|`tools/database/Update-DevelopmentG07.ps1 -Apply`; [manifest](../runs/developmentG07_20261109_214946_4da7406b/manifest.json)|Succeeded;025–027 Applied; hash trước/sau8C4716…F87A13 khớp |
|Backup trước từng chặng|COPY_ONLY/CHECKSUM; sau đó chạy RESTORE VERIFYONLY WITH CHECKSUM trên đúng từng file|Cả2PASS, giữ backup |

Script kiểm checksum migration đã áp dụng trước mỗi chặng. Hai snapshot có phạm vi khác nhau (G07 thêm tài sản), chỉ so trước/sau trong cùng chặng; không suy hash khác giữa hai script là dữ liệu bị đổi. Không seed lại, không reset tài khoản/mật khẩu, không ghi fixture hoặc giao dịch thử vào development.

Hai file backup được giữ trong thư mục backup SQL Server:

- `CoffeeManager_Development_before_G06_024_20261109_214928_9d110509.bak`.
- `CoffeeManager_Development_before_G07_20261109_214946_4da7406b.bak`.

Đường dẫn đầy đủ và kết quả VERIFYONLY trong [backup-verification.json](../runs/development-handoff_20261109_214910_f23efd01/backup-verification.json). Không tự xóa hoặc restore đè backup.

## Kiểm đọc sau nâng

Windows,SDK10.0.401,SQLServerlocalhost17.0.1000.7,schema027. Probe tham chiếu DLL Debug thực của app; đọc connection/credential ADMIN đã lưu DPAPI trong bộ nhớ, không in/lưu plaintext hoặc hash mật khẩu. Trước AuthService, kiểm PasswordService.Verify trả Success để không kích hoạt rehash trong lượt kiểm đọc. Không tạo SessionContext giả, không impersonate owner khi gọi BLL.

**16/16 kiểm đọc PASS** qua runtime thật: sysadmin0,db_owner0,CafeManagerApp1. [Raw kết quả](../runs/development-handoff_20261109_214910_f23efd01/read-check-results.json):

| Nhóm | Kết quả |
|---|---|
|Runtime identity|Đúng DB và quyền hạn chế như trên |
|Dashboard + SQL đối chiếu độc lập|Ngày09/11/2026: doanh thu đã trả0₫,đơn mở1,công nợ5.500.000₫; BLL khớp SELECT độc lập |
|ReportOptions|52 lựa chọn đọc được |
|9 loại report|SalesDay,SalesChannel,Products,Payments,Inventory,Debts,CashFlow,Labor,Assets đọc được cho ngày hiện tại; không chứng nhận mọi kỳ/bộ lọc |
|EmployeeContext/List|Context schema27;10 nhân viên đọc được |
|AssetContext/List|Đúng DB;10 tài sản đọc được |

Kiểm procedure ban đầu dùng danh sách có tên giả định sp_AssetContext nên đếm3; source AssetRepository thực tế dùng sp_HRContext. AssetContext/List đã được gọi qua BLL thật và PASS, không tạo thêm procedure chỉ để khớp danh sách kiểm.

Probe chuẩn bị ban đầu chưa compile do gọi ConnectionFactory.CreateConnection nội bộ. Đã sửa riêng harness dùng SqlConnection với cùng runtime connection trước khi bắt đầu kiểm; source trước sửa harness giữ ở probe-source-before-compile-correction.txt. Build probe sau sửa0warning/0error. Không sửa source/migration hoặc expected sản phẩm trong lượt kiểm.

## WinForms thật

Đã mở đúng exe Debug development và dùng nút ADMIN hiện có. Click theo AX ban đầu báo geometry unavailable; Tab không cập nhật focus qua AX. Sau đó chụp duy nhất để định vị nút (không lưu/so ảnh hoặc nghiệm thu hình thức), click tọa độ theo screenshot mới. Đọc lại AX trên MainForm production xác nhận:

- Role ADMIN và Tổng quan quản lý hiện đúng.
- “Khách đã trả hôm nay (09/11/2026):0₫ / Đơn mở:1 / Công nợ nhà cung cấp hiện tại:5.500.000₫” khớp BLL/SQL.
- Có thời điểm đọc21:55 theo SQL Server; không còn “Chưa cập nhật tổng quan”/lỗi dữ liệu chưa xác định.

[Bằng chứng native](../runs/development-handoff_20261109_214910_f23efd01/native-dashboard.json). **Để app mở tại Tổng quan cho người dùng.** Cửa sổ chạy exe với cấu hình tương đương profile development, không gắn lại debugger Visual Studio. Không kiểm hover/spacing/hình thức; DPI100/150%NOT RUN. Native các màn khác ngoài Home chưa nghiệm thu từ lượt này.

## Bảo toàn dữ liệu và kết luận

[read-data-audit.json](../runs/development-handoff_20261109_214910_f23efd01/read-data-audit.json) xác nhận hash nguồn gốc sau kiểm BLL/native bằng trước kiểm và bằng AfterHash của nâng G07. Phạm vi gồm hồ sơ/account/công/lương/thu chi/nợ/trả nợ/tài sản/tồn/điểm/hóa đơn theo snapshot script, không tuyên bố mọi byte metadata DB bất biến sau migration.

[source-audit.json](../runs/development-handoff_20261109_214910_f23efd01/source-audit.json):SourceUnchanged=true cho src/database/tests/tools. Không sửa code/theme, không commit/push. [Manifest tổng](../runs/development-handoff_20261109_214910_f23efd01/manifest.json). **G07-DEV01 đóng theo phạm vi đồng bộ DB và kiểm Home; các kết quả/giới hạn nhóm trước giữ nguyên.**
