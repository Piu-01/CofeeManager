# G01 manual — runtime Windows principal và UI chậm

**Ngày:** 09/10/2026. **Run directory:** G01_Manual_20261008_225305_cebd98ad
(DB chuẩn bị08/10, runtime chạy09/10); source hash kế thừa manifest B07,
runtime-source-stability.json count=0. Windows/.NET10.0.401/SQL17.0.1000.7,
DPI120=125%. Không sửa source/migration trong lượt này.

## Môi trường và thao tác

Người dùng tạo local Windows account CafeManagerApp. Login Windows map user
CafeManagerApp_Runtime, role CafeManagerApp trên DB test
CoffeeManager_Test_T05_20261008_225305_cebd98ad. User tự nhập mật khẩu tại máy;
không đưa password vào source/log/chat. Hai script bootstrap ở config/local/
được git check-ignore xác nhận ignored, Windows auth trong environment process.
RunAs terminal không dùng được với input của người dùng; chuyển hộp thoại
Get-Credential/Start-Process -Credential, không yêu cầu đổi mật khẩu.

Process Win32.GetOwner xác nhận PID30036 owner LAPTOP-79UNRIRC\CafeManagerApp.
Session SQL65: login_name/original_login_name cùng account, host_process_id30036.
UIAutomation bấm nút tải/Form thật; filter ZZZZZ để thử rỗng, blank để tải10 bàn.
Session admin test riêng BEGIN TRAN/SELECT BAN WITH(TABLOCKX,HOLDLOCK) giữ lock;
bấm tải trên app, đối chiếu request SQL blocked, resize, hủy, release/rollback
lock và reload; lặp lock rồi đóng Form. Không giao dịch ghi nghiệp vụ.

| Ca/subcase | Expected | Actual | Status |
|---|---|---|---|
| T03-K08 | App/migration identity tách biệt, không credentials vào Git/log | Windows process/session app riêng; sysadmin0/db_owner0/app role1,10 bàn; scripts ignored/no passwords | PASS |
| T05-K07-load | Query thật trả danh sách | 10 bàn dưới account app, không fallback admin | PASS subcase |
| T05-K07-empty | Filter không khớp có empty state, khác lỗi | ZZZZZ không có bàn, blank tải lại10 bàn | PASS subcase |
| T05-K07-slow | Query chậm không freeze, khóa gửi lặp | SQL blocked1, load disabled/cancel enabled; resize83ms | PASS subcase |
| T05-K07-cancel | Request dừng, UI ghi đã hủy, giữ dữ liệu cũ | SQL blocked0, load enabled/cancel disabled,10 bàn còn; status vẫn Đang tải… cả sau unlock | **FAIL** |
| T05-K07-reload | Tải lại sau hủy được | 10 bàn tải lại | PASS subcase |
| T05-K07-close | Đóng khi query chờ không giữ process/request | Process thoát khi lock còn giữ, requests0 sau đóng; lock rollback | PASS subcase |

**T05-K07 tổng: FAIL do B08**, không dùng harness Status=PASS để tick ca:
harness chỉ assert process/buttons/SQL, không assert text cancellation. Ảnh
runtime-canceled.png và JSON Cancel/AfterUnlockText chứng minh UI vẫn Đang tải….
ExitCode của process object trả null, không báo exit code0 từ dữ liệu thiếu.

## Bằng chứng

Trong runs nhóm manual: runtime-process-owner.json, runtime-sql-session.txt,
app-principal-check.txt, runtime-load.json, runtime-slow-ui.json, runtime-harness.txt,
runtime-canceled.png/runtime-loading-locked.png/runtime-empty.png, post-close-sql.txt.
Ảnh runtime-tables-1280x720.png bị PrintWindow black; không dùng làm chứng cứ
hình thức. Kết quả tải10 bàn từ UIAutomation/session SQL và ảnh canceled có10 bàn.
Capture không thay kiểm chứng DPI100/150 hoặc keyboard Tab/focus, vẫn NOT RUN.
Lần harness đầu thiếu ServerProcessId dẫn đến SQL diagnostic sai cú pháp;
finally rollback lock; giữ slow-cancel-attempt1.json. Không phải bug procedure.

B08 báo tại bug-report.md, chờ duyệt bổ sung. App đã đóng có chủ đích trong
ca close; không báo là crash. G01 chưa nghiệm thu.
