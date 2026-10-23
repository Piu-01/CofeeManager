# Regression B01 — quyền đọc marker test

23/10/2026, sau duyệt B01 của người dùng. **B01 PASS**; không cấpPASS toàn task hoặcQ01–Q10.

## Môi trường và thay đổi

Windows/.NET10, SQL Server localhost, DB `CoffeeManager_Test_G03_20261019_215457_29e552a9_demo`.
Marker `20261019_215457_29e552a9`, manifest cònCleaned=false. Đọc cấu hình runtimeDPAPI,
không đọc mật khẩu/hash tài khoản ứng dụng, không in chuỗi kết nối. Helper auth đọc identity thật.

Thực thi `tools/ui/Grant-TestLoginMarker.ps1`: kiểm target/manifest/marker, lấyUSER_NAME()
qua connection runtime; từ chốidbo/guest/public/sys/INFORMATION_SCHEMA. User thực làG03Gui.
Cấp **GRANT SELECT ON OBJECT::dbo.G03TestMarker TO [G03Gui]**, chỉ trong DB test này.
Không sửa SQL schema/procedure, role cấp rộng, login/password hoặc dữ liệu nghiệp vụ.
Không sửa source ứng dụng chofix này; thêm công cụ provision và hồ sơ. Công cụ dùng
identifierQUOTENAME, giá trị parameter, kiểm lại marker bằng owner trước GRANT.
Không dừng app người dùng. Không tự cấp quyền trong handler bấmTEST.

## Kết quả chạy

| ID | Thao tác / expected | Actual | Kết quả |
|---|---|---|---|
| B01-R01 | Manifest và marker thật phải khớp trướcGRANT | Khớp đúngDB/marker, chưaCleaned | PASS |
| B01-R02 | Chỉ runtime user của DBtest được đọc marker | G03Gui, HAS_PERMS_BY_NAME=1; SELECTRunId thành công | PASS |
| B01-R03 | Helper hiện có phải trả ADMIN thật và quyền BLL hợp lệ | ReadStateCompleted, Active/Employedtrue, RoleADMIN; PermissionService.DemandAsync(MaterialRead) thành công | PASS |
| B01-R04 | Marker sai phải bị từ chối trước tạo phiên | wrong-marker-regression→InvalidOperationException | PASS |
| B01-R05 | Handler nútTEST mở đúng mộtMainForm | LoginForm thật với input trống; PerformClick trên nút fixture; ApplicationSessionContext mở1MainForm, sessionADMIN | PASS |
| B01-R06 | Chỉ kiểm chức năng ở125%, không nghiệm thu thị giác | Form.DeviceDpi=120; PerMonitorV2; không chụp ảnh/kiểm hình thức | PASS chức năng |

R05 chạy STA+Application.Run với form/handler thật trong **process fixture do probe sở hữu**,
không click cửa sổ authentication bằng computer-use, không đăng nhập thay người dùng trên app đang mở.
MainForm được đóng và session/contextDispose sau khi xác nhận. Probe không commit kho/điểm/tiền.

## Lệnh và bằng chứng

- Provision: `tools/ui/Grant-TestLoginMarker.ps1`; `runs/marker-provision.json` ghi target/user/quyền,
  cùng kết quả runtime đọc marker sau cấp quyền.
- Build probe Debug output riêng `runs/permission-bin`:0warning/0error.
- Regression: `dotnet docs/tasks/temporary-test-login/runs/permission-bin/PermissionDiagnostic.dll docs/tasks/temporary-test-login/runs/b01-regression.json --regression`.
- JSON regression có guard, permission1, runtimeReadMarker=true, setup/identity hợp lệ,
  ActualHelperAndBll=true, WrongMarkerRejected=true, ActualLoginHandlerMainFormPASS/Dpi120/MainForms1.
- Bằng chứng trước sửa `runs/permission-diagnostic.json` giữ nguyên (permission0/SQL229).

## Giới hạn

Chưa chạy toàn bộQ01–Q10: thiếuaccount/role lỗi riêng, doubleclick/cancellation/logout,
đăng nhập thường sai password, Release inspection đầy đủ và toàn bộ luồngprofileF5.
Không ghiPASS các phần này từ build hoặc R05. Hình thứcSKIPPED theo yêu cầu người dùng.
Các ca cố tìnhtarget manifest sai/chưaCleaned trong scriptprovision chưa chạy thành một ma trận riêng.

G08 vẫn phải gỡ nút/helper/profile/launcher và **Grant-TestLoginMarker.ps1** trước PASS;
kiểm lại đăng nhập thường và bản bàn giao, giữ lịch sử test này. Không commit/push.
