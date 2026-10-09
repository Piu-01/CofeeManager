# G01-B08 — Regression sau duyệt09/10/2026

**Duyệt:** Người dùng “duyệt”, sau mở app xác nhận tiếp tục. Source sửa chỉ
StartupForm.cs: guard stale/disposed trước, cancellation hiện tại cập nhật
status/content qua helper ShowCanceledLoad, catch dùng cùng helper.
Không đổi Designer/service/DAL/SQL/rows/config/permission.

**Run:** G01_B08_20261009_111336; runtime11:18:37–11:18:48 +07:00 ngày09/10/2026.
SDK10.0.401, Windows DPI120=125%, SQLlocalhost17.0.1000.7,
DB CoffeeManager_Test_T05_20261008_225305_cebd98ad.
Process16932 owner LAPTOP-79UNRIRC\CafeManagerApp xác nhận bằng Win32.GetOwner.
Source chưa commit; manifest/stability nhóm count=0 thay đổi trong regression.

| Kiểm chứng | Expected | Actual | Status |
|---|---|---|---|
| Debug/Release build | Không lỗi/warning mới | 0 warning/0 error mỗi cấu hình | PASS |
| Hủy trước lần tải đầu | Đã hủy, không rows giả, request dừng | Text Đã hủy tải, blocked0; panel không dữ liệu | PASS |
| Hủy khi có10 bàn | Giữ dữ liệu cũ, status đã hủy, nút mở lại | 10 bàn còn, text đã hủy, load enabled/cancel disabled, blocked0 | PASS |
| Release SQL lock sau hủy | Không kết quả muộn ghi đè | Text đã hủy vẫn giữ sau rollback lock | PASS |
| Hủy lặp2 lượt | Không kẹt loading/cancellation | Cả2 lần text đã hủy, blocked0, giữ10 bàn | PASS |
| Reload | Query lại dưới app account | Trả10 bàn, status giờ tải mới | PASS |
| Resize khi request blocked | UI phản hồi | Resize22ms khi blocked1 | PASS |
| Close lúc blocked | Process thoát, không request SQL còn | ExitedWhileLocked=true, requests0 sau close | PASS |
| Filter rỗng | Không có bàn khác lỗi | ZZZZZ empty, blank tải10 bàn | PASS |

Harness nay assert **text cancellation** trước/sau unlock và hủy lặp, không chỉ
buttons/process như lượt lỗi trước. Không gửi request mới khi nút tải disabled;
guard stale được review giữ nguyên trước cập nhật hủy. Không ép điều kiện bất
khả dụng từ UI để giả chứng cứ request-version. Process.ExitCode null do observer
không trả mã, không báo0; thoát/no requests được kiểm trực tiếp.

## Bằng chứng/lệnh

`dotnet build CafeManager.slnx -c Debug/Release --no-restore`;
WinForms thật UIAutomation, sessionSQL riêng giữ BAN TABLOCKX/HOLDLOCK trong
transaction và rollback; Assert-CanceledText dùng mã Unicode để tránh encoding
PowerShell harness. Script source lưu runtime-harness.txt.
Trong `../runs/G01_B08_20261009_111336/` ở nhóm: build-debug/release.txt,
process-owner.json, b08-runtime-regression.json, source-manifest/stability.json,
canceled-before-first-load.png, runtime-canceled.png và các ảnh runtime.

**B08 PASS; T05-K07 PASS**, T05 hiện10/10 ca nguyên bản PASS. Giữ manual FAIL
và ảnh/json lượt trước tại manual-test-results.md/runs manual.
DPI100/150%, keyboard Tab/focus và nhánh runner test-failure B04 bổ sung vẫn
NOT RUN như báo cáo trước; không coi bộ45 mã PASS là đã chạy hết các kiểm chứng
bổ sung đó. Designer không sửa ở B08, kế thừa regression B07. Chưa commit.
