# Đợt kiểm chức năng nút TEST

22/10/2026 — plan triển khai đã duyệt; code và build xong, **chờ duyệt một đợt test chức năng**. Không tự chạy từ duyệt plan. Không kiểm hình thức, ảnh hoặc ma trận DPI100/150%. Chỉ chạy chức năng ở125%; ghi DeviceDpi thực, không suy diễn từ độ phân giải.

## Môi trường và source

- Debug `runs/build/Debug/CafeManager.WinForms.exe`; Release ở thư mục tương ứng. Snapshot nguồn sau bổ sung profile F5: `runs/source-profile-implemented.csv`; snapshot trước bổ sung vẫn giữ tại `runs/source-implemented.csv`. Không sửa source/migration trong lượt test; ghi bug và tiếp tục ca độc lập.
- DB test được chọn explicit bằng manifest G03 còn Cleaned=false; helper kiểm marker bằng runtime connection. Không đọc accounts.dpapi, hash/password ứng dụng; không đổi account/password để ép testPASS.
- Có thể dùng harness WinForms trong process sở hữu để kích handler bằng PerformClick/reflection và kiểm cancellation/busy; ghi rõ khác với thao tác app chính. Không dùng tiện ích để vượt hạn chế công cụ.
- Không thay đổi DB sử dụng thật. Ca identity lỗi chỉ dùng fixture/test riêng được xác định trước; không sửa tài khoản admin/demo có sẵn đang được người dùng sử dụng. Không đủ fixture thì ghi NOT RUN/BLOCKED và nêu rõ phần chưa chạy.

## Ma trận cụ thể

| ID | Thao tác | Expected |
|---|---|---|
| Q01a | Debug không flags; thiếu database/marker; flag khác1 | Login thường, không nút TEST, không tự tạo phiên |
| Q01b | Release set toàn bộ flags như Debug | Không type/helper test trong DLL UI/DAL, không nút hoặc delegate bypass |
| Q02a | Constructor target sai server, DB development, catalog không khớp | Từ chối cấu hình trước khi đọc identity |
| Q02b | DB test đúng nhưng marker sai, thiếu table, runtime thiếu quyền đọc | Không phiên/MainForm; lỗi giữ Login, không fallback sang owner/DB khác |
| Q02c | Launcher thiếu exe, manifest Cleaned/server/database/marker sai | Dừng trước mở app; không thay env toàn máy hoặc profileF5 |
| Q03 | Mở Debug bằng launcher, username/password trống; bấmTEST | Marker đúng, ReadState.Completed/AdminAccountId hợp lệ; identity ADMIN thật, một MainForm, PermissionService kiểm thao tác được |
| Q04 | Setup/identity không hợp lệ trong fixture riêng | Không phiên khi setup chưaCompleted/khôngAdminAccountId/account mất/khóa/nghỉ/sai role. Không gán role/stamp giả; ghi riêng ca chưa có fixture |
| Q05 | Nút thường: để trống input; nhập account test với password sai giả lập | Validation hoặc lỗi xác thực cũ; không đi nhánhTEST. Không đọc password thật từ app/DPAPI |
| Q06a | Delegate test đang chờ qua harness; bấm TEST2lần và nút thường | Chỉ một request pending, cả hai nút khóa; kết thúc mở trạng thái đúng |
| Q06b | Đóng form khi pending; delegate về session sau hủy | Không LoggedIn/MainForm muộn; session trả muộn đượcDispose; không exception ở control đãDispose |
| Q06c | Delegate test lỗi hoặc guard marker fail | Không phiên; Login còn dùng được, status không có connection string |
| Q07 | TEST→MainForm→Logout→Login→TEST | Một MainForm mỗi phiên; phiên trướcDispose/revoked, lần sau đọc DB/identity lại |
| Q08 | Source audit và metadata Debug/Release | Không hash/password hard-code, không gọi ReadCredentialAsync/LoginAsync trong helper bypass; Release không có type test; quyền/login thường không bị đổi |
| Q09 | DeviceDpi125% (=120) trên form thực/harness trước thao tác | Nút/tab input có thể thao tác, không khóa do layout; không kiểm màu/font/spacing/hover bằng ảnh |
| Q10 | Visual Studio Debug →CoffeeManager.Test→F5; sai/thiếu/đã dọn manifest; Release với profile Test | Debug đúng manifest có nút; lỗi không fallback Development, Release không cho dùng profile Test; Development vẫn nguyên |

## Bằng chứng/bàn giao

Lưu test-results.md với input/expected/actual và log JSON/TRX khi phù hợp; ghi explicit các ca in-process, app thật, fixture và NOT RUN. Quyền kiểm BLL không được gọi PASS chỉ từ việc thấy MainForm. Không thử commit tiền/kho/điểm cho tiện ích đăng nhập này. Source audit trước/sau, log không chứa secret. Tổng hợp bug để duyệt một lần nếu phát hiện; không sửa trong đợt test.
