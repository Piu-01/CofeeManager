# Báo cáo sau đợt test — 09/11/2026

**Cập nhật sau duyệt sửa DQ-B01:** đã sửa một dòng helper, **57/57 subcase và142/142 unit PASS**, Debug/Release và Debug đường F5 mặc định0warning/0error. Publish mới tắt đúng tiện ích DEV và giống DLL Release. Source/harness giữ nguyên trong lượt,1DBtest tạo/1dọn/0còn,credential fixture đã xóa. [Biên bản regression](bug-regression-results.md). Không còn bug đang chờ duyệt; native chuột/phím/modal vẫn BLOCKED/chưa đủ, tính năng chưa nghiệm thu toàn bộ. Các dòng18/19 và chờ duyệt bên dưới là lịch sử trước debug.

## Lịch sử đợt nghiệm thu trước sửa

**Đang làm: 18/19 subcase harness PASS, 1 FAIL; 142 unit PASS. Gate Release/publish PASS; native còn BLOCKED.** Người dùng đã duyệt đợt test bằng “duyệt'”. Chưa nghiệm thu tính năng hoặc chốt G08.

Đã kiểm lưu/thay/xóa credential DPAPI, tách target, file bị khóa; xác thực ba role và quyền; sai mật khẩu, khóa tài khoản, nhân viên nghỉ, sai role; đăng nhập thường, thu hồi phiên, chặn gửi lặp và hủy khi SQL chờ. Hộp cấu hình đã được kiểm bằng control API. Phát hiện **DQ-B01**: file DPAPI hợp lệ nhưng sai Version phát InvalidDataException thay vì thông báo Việt dự kiến. Source giữ nguyên, chưa sửa. Xem [báo cáo lỗi và hướng sửa](bug-report.md).

Runtime Release/publish không bật DEV dù có hai cờ; publish thành công và DLL giống nguyên byte với Release. Công cụ đọc được Login và owned modal ở125%, nhưng click báo geometry unavailable, Tab không đổi focus. Chưa chứng nhận đầy đủ thao tác chuột/phím, login/logout đủ role hoặc màn đích với module production. Windows user thứ hai và các biên cấu hình bổ sung còn NOT RUN.

[Kết quả chi tiết, lịch sử harness và lệnh gọi quá rộng](test-results.md), [19 record của harness](runs/acceptance_20261109_204755_debbbe06/probe-results-v3.json), [142 unit TRX](runs/acceptance_20261109_204755_debbbe06/unit/unit-only.trx), [gate publish](runs/acceptance_20261109_204755_debbbe06/release-publish-gate.json).

source-audit.json xác nhận SourceUnchanged=true. resource-audit.json ghi1 DB test tạo/1 dọn/0 còn,0 process fixture và không còn credential fixture. Không thay development, không commit/push. **Chờ duyệt sửa DQ-B01 kèm regression theo phạm vi trong bug-report.md.** Blocker native giữ đúng giới hạn thật, không coi là bug mới.

## Lịch sử bàn giao triển khai trước test

09/11/2026. Requirements và plan đã được người dùng duyệt. **Đã triển khai, chưa nghiệm thu; chờ duyệt đợt test chức năng.** Không đổi trạng thái325ID/T01–T32.

## Đã làm

- Login có vùng DEV với ADMIN/CASH/STORE và cấu hình credential local. Role chưa lưu mở cấu hình chọn sẵn; lưu thành công trở lại Login, bấm role để đăng nhập.
- Mỗi nút xác thực bằng AuthService hiện có, kiểm role thật, dùng chung pending/cancel/generation. Logout tạo Login mới với dependency dev; không giữ credential trong session.
- File credential DPAPI CurrentUser theo target, kiểm version/schema/role, ghi file tạm rồi move; lỗi lưu/đọc có thông báo không chứa secret. Không serialize LoginRequest vào file; dùng DTO private chỉ cho payload được mã hóa.
- Hộp cấu hình modal native có owner LoginForm qua entry point nội bộ trong OrbitDialogHost. Host nghiệp vụ đang đòi MainForm nên entry point Login dùng ShowDialog(owner), không thay stack các hộp nghiệp vụ.
- Factory chỉ bật Debug, hai cờ opt-in và đúng localhost/CoffeeManager_Development. Profile F5 đã bật cờ. Release factory trả null; chưa kiểm runtime/package nên chưa ghi PASS DQ01.
- Cập nhật UX ngoại lệ DEV, checklist gỡ ở hồ sơ tiện ích cũ và T32. Không thay SQL/DAL/BLL/mật khẩu hoặc dữ liệu development, không mở app/test, không commit/push.

## Build thực tế

Môi trường Windows, SDK10.0.401, solution CafeManager.slnx, source working tree có thay đổi tồn tại trước yêu cầu (không coi HEAD là toàn source).

| Lượt | Kết quả |
|---|---|
| Build Debug mặc định | FAILED exit1,0warning/0error ở bước MSBuild project-reference; giữ build-debug.log, không suy là lỗi C# |
| Debug tuần tự output mặc định | FAILED: MSB3027/MSB3021, exe Debug đang được process khác sử dụng;10warning/2error copyfile. Không dừng process người dùng |
| Debug output riêng | **SUCCESS,0warning/0error**, build-debug-isolated.log |
| Release output riêng | **SUCCESS,0warning/0error**, build-release-isolated.log |

Lệnh thành công (thay CONFIG bằng Debug/Release):

`dotnet build CafeManager.slnx -c CONFIG --no-restore -m:1 -nr:false -p:UseSharedCompilation=false -p:BaseOutputPath=D:/Workspace/Nam_3/HK1/CoffeeManager/artifacts/dev-role-quick-login/bin/ -v minimal`

Đầu ra riêng: `artifacts/dev-role-quick-login/bin/Debug/net10.0-windows/` và `Release/net10.0-windows/`. Tham số output chỉ dùng cho lệnh build này, không sửa csproj. File exe mặc định đang mở không được coi là bản mới; đóng app rồi F5/build lại để dùng bản mới.

`git diff --check` trên5file source/profile đã sửa: exit0, chỉ cảnh báo chuyển LF→CRLF ở OrbitDialogHost. Không chạy test từ lệnh build (project Tests chỉ compile).

## Phần chưa chạy và bước tiếp

[Test-plan](test-plan.md) bao gồm DQ01–DQ06: gate Debug/Release/publish, lưu/thay/xóa và lỗi DPAPI/I/O, xác thực đủ ba role/quyền, native modal/input, click lặp/cancel/logout/callback, regression login thường. **Toàn bộ chức năng NOT RUN**, chờ người dùng duyệt đợt test riêng theo post-change-test-workflow. Publish/runtime chưa kiểm. Hình thức SKIPPED theo phân công; DPI125% chức năng chưa chạy,100/150%NOT RUN.

Hướng dẫn dùng và checklist gỡ tại [README](README.md). Trạng thái vẫn Đang làm cho tới khi đủ bằng chứng kiểm chứng; không chốt G08 hoặc nghiệm thu thay các blocker cũ.
