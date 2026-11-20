# Kết quả đợt test đã duyệt —09/11/2026

**Mới nhất sau sửa đã duyệt:** DQ-B01 PASS theo [biên bản regression](bug-regression-results.md):57/57 subcase,142unit, gate Release/publish PASS. Tất cả19 subcase cũ đã chạy lại PASS;33 ca file lỗi và5 ca UI component bổ sung PASS. Native và các giới hạn ghi trong biên bản vẫn chưa đủ; không chốt nghiệm thu tính năng. Kết quả18/19 bên dưới là lịch sử trước sửa, giữ nguyên.

## Lịch sử đợt trước debug

Run:`runs/acceptance_20261109_204755_debbbe06/`. **18/19 subcase harness PASS,1FAIL DQ-B01;142unitPASS. Chưa nghiệm thu toàn tính năng.** Bảng NOT RUN ở cuối là lịch sử trước đợt, không phải trạng thái hiện tại.

## Môi trường và source

Windows,SDK10.0.401,SQLServerlocalhost17.0.1000.7,schema001–027/demo. DB:`CoffeeManager_Test_G08_20261109_204755_debbbe06_demo`; install-outside-sandbox.json và OperationInstallMarker đối chiếu trước dùng/dọn. App thật DeviceDpi120=125%, không đổi Windows Scale. Runtime DAL dùng DQRuntime WITHOUT LOGIN/CafeManagerApp qua SessionInitializer, owner chỉ provision/đối chiếu; chưa thay kiểm Windows runtime identity thứ hai thật.

source-before.csv/source-after.csv/source-audit.json: **SourceUnchanged=true** cho src/database/tests/tools. DLL thực tế được kiểm trong probe trùng Debug build, SHA256 lưu dll-hashes.json. Release và publish WinFormsDLL giống nguyên byte (C3C54C…7FDDE1); Debug50A3CA…CEEDC9.

## Kết quả chức năng

| Ca/subcase | Input/thao tác, expected | Actual và trạng thái |
|---|---|---|
| DQ01.DebugGate | Factory Debug: đúng hai cờ/localhost/devDB bật; thiếu từng cờ/sai DB/server/connection rỗng-sai tắt | PASS probe-v3; không kết nối development |
| DQ01.ReleasePublishGate | Factory DLL publish có hai cờ/đúng target vẫn null, defaultLogin không nút dev | PASS release-publish-gate.json; DLL publish=Release |
| DQ02.Roundtrip.ADMIN/CASH/STORE | Save rồi instance mới Get lại credential ngẫu nhiên; ciphertext không chứa plaintext password | 3PASS |
| DQ02.ReplaceDeleteScope | Thay/xóa ADMIN giữ CASH; target khác đọc không thấy file | PASS |
| DQ02.CorruptAndVersion | Corrupt bytes rồi envelopeDPAPI hợp lệ Version2; expected thông báo lỗi Việt qua InvalidOperationException | FAIL:InvalidDataException ở Version2, DQ-B01; file fixture phục hồi trong finally |
| DQ02.LockedWritePreservesFile | Khóa file đọc để chặn replace; expected lỗi lưu/hash file cũ giữ nguyên/không tmp | PASS |
| DQ02 khác user/I/O quyền/oversize/schema-server mismatch | Kiểm identity Windows thứ hai và các biên bổ sung | NOT RUN; không có Windows identity fixture thứ hai được sử dụng; biên invalid-envelope còn thiếu đưa vào regressionB01 |
| DQ03.AuthAndPermission.ADMIN/CASH/STORE | BLLAuth thật từ credential lưu, identity đúng; đúng quyền chính; CASH/STORE bị chặn Finance; gọi QuickLoginAsync của form thật | 3PASS, mỗi lần một LoggedIn/session dispose; native đầy đủ màn đích chưa chứng nhận |
| DQ03.WrongPassword.ADMIN/CASH/STORE | Mật khẩu ngẫu nhiên sai bị từ chối | 3PASS |
| DQ03.LockedAndRetired | Operator khóa CASH/nghỉ STORE trên DBtest, khôi phục fixture trong finally | PASS:auth từ chối cả hai |
| DQ03.MismatchedRole | Map nút CASH vào account STORE | PASS:khôngLoggedIn, báo cập nhật role, nút mở lại |
| DQ04.ComponentDialogSave | Form thật, fill qua control API/PerformClick, xác thực CASH/lưu | PASS:statusĐãlưu,passwordclear,AcceptButtonnull,ShowInTaskbarfalse; không thay native |
| DQ04.Native | Quan sát Login và ownedmodal; click/Tab/selectall/Esc/X/Enter/resize | BLOCKED ở chuỗi input:geometry unavailable; Tab không đổi focus. AX đọc được hộp cấu hình,nhưng không chứng nhận thao tác đã tới đúngcontrol |
| DQ05.DoubleClickAndClosePending | SQLtest giữ transaction khóa AppSetupState; gửi CASH rồi STORE khi pending, đóng Login | PASS:pending tồn tại,tất cả5nút khóa,lần2không thay request,cancel không phátLoggedIn; rollbacktransactionfixture trongfinally |
| DQ05.Native khác/dialog-cancel/logout đổi role | Native clicklặp,close lúc lưu/logout3role/callback qua context | BLOCKED/NOT RUN phần native; không suy từ component |
| DQ06.RevokedSession | CASHlogin,operator khóaaccount, gọiDemandSales | PASS:Unauthorized và session.IsRevoked=true; fixturekhôi phục |
| DQ06.NormalLogin | Formthật nhập qua controlAPI rồi PerformClick login thường | PASS:mộtLoggedIn,passwordclear; không chứng nhận gõnative |
| DQ06.Unit | Chạy unit regression trongassembly, loại cácnamespaceintegrationcũ cần fixture riêng | 142/142PASS,unit/unit-only.trx |
| DQ06.Audit | Runtimeciphertext kiểm không chứa passwordfixture; package không filequicklogin.dpapi; source+DLLhash | PASS theo phạm vi; không tuyên bố auditvulnerability hoặc quét mọi secret của workspace |
| Hình thức / DPI100,150% | Theo phân công người dùng | SKIPPED / NOT RUN; không là blocker chức năng |

Mỗi subcase PASS/FAIL và Type/Sql của harness nằm trong probe-results-v3.json. Có19record, không cộng unit thành sốcaDQ hoặc325ID. DQ01 ReleasePublish là record riêng, ngoài19record.

## Lệnh và lịch sử lượt

- `dotnet test … --no-build --no-restore -p:BaseOutputPath=…/artifacts/dev-role-quick-login/bin/ --filter "FullyQualifiedName!~CafeManager.Tests.G02&FullyQualifiedName!~CafeManager.Tests.Integration" --logger "trx;LogFileName=unit-only.trx"`:142PASS. Không chạy lại toàn integrationT05/G02 vì nằm ngoài phạm vi và cần runner/marker riêng.
- Lệnh unit đầu trong sandbox ABORTED vìtesthostIPC90s. Lệnh không filter ngoài sandbox do agent gọi quá rộng:142PASS/461FAIL từguardthiếufixtureintegration;giữunit.trx/unit-outside-sandbox.trx, không quy461lỗi đó thành bug mới sản phẩm,không chỉnh expected.
- Lượt Probe đầu nạp SqlClientfacade qua referencefile→PlatformNotSupportedException ởgate/SQL;storage5PASS/1FAIL,giữprobe-results.json. ProbeV2 thêmPackageReferenceSqlClient6.1.3 đểruntimeassetđúng,storagebugvẫnFAIL;bootstrapfixture dùngdq.adminkhôngkhớpseedadmin→DALrefuse. ProbeV3 chỉsửausernamefixtuređúngadmin và thêmSQLnumberdiagnostic,expected/sourcesảnphẩmkhôngđổi;đãchạy lại19record. Rawresults/sourcecủamỗilượtđượcgiữriêng.
- NativeOnly làlượtfixtuređộc lập sauProbeV3,đặtpasswordngẫunhiênmới chỉchoaccountDBtest,khôngkhôiphụclỗi sảnphẩm. Factoryproductionkhôngnớiguard:helperinstanceDBtestqua reflectionnhưplan. Contextthật nhưngmodulefactoriesmặcđịnh,khôngchứngnhậncácmoduleproduction.
- Nativeđầu hếtthời hạn12phúttựđóng,clickADMINoutcomeunknown. LượtNativeOnlykhởichạyHiddenkhôngcócửasổtargetable;đãdừng bằngstopfile vàmởlạiquaComputerUse. AXđọcLogin/ownedmodal;clickgeometryunavailable,Tabfocuskhôngđổi;khôngclaimnativebaRolePASS. NativeJSONL/tooltrace và native-fixture.json lưugiớihạn;khôngkiểmảnh/hover/hìnhthức.

## Dọn tài nguyên

resource-audit.json:1DBtạo/1dọn/0còn;0processfixture,credentialfixtureDPAPIkhôngcòn. Markertrướcdropkhớpinstallmanifest. KhôngtạoSQLlogin/Windowsaccount,khôngđụngdevelopment. Sourcegiữnguyên,khôngcommit/push.

## Lịch sử trước khi bắt đầu đợt

09/11/2026 — chưa bắt đầu đợt test chức năng. Build được phép trong plan đã chạy; chi tiết lệnh/môi trường/log tại report.md. Chưa tạo DB/fixture hoặc phiên native; chưa có commit/schema test mới.

| Mã | Expected | Actual | Trạng thái |
|---|---|---|---|
| BUILD-DEBUG | Solution compile | Output riêng,0warning/0error,build-debug-isolated.log | PASS (build) |
| BUILD-RELEASE | Solution compile | Output riêng,0warning/0error,build-release-isolated.log | PASS (build) |
| DQ01 | Đúng gate Debug/Release/publish | Chưa kiểm runtime | NOT RUN |
| DQ02a/b | DPAPI lưu/thay/xóa, scope, lỗi I/O | Chưa chạy fixture | NOT RUN |
| DQ03 | Ba role xác thực thật và ca từ chối | Chưa chạy SQL/native | NOT RUN |
| DQ04 | Native modal/input/layout125% | Chưa thao tác app | NOT RUN |
| DQ05 | Pending/cancel/logout/callback đúng | Chưa gây trì hoãn hoặc thao tác | NOT RUN |
| DQ06 | Login thường/phiên/quyền và audit secret | Chưa chạy regression/package | NOT RUN |
| Hình thức | Người dùng tự kiểm | Theo phân công AGENTS | SKIPPED |
| DPI100/150% | Không tự đổi Windows Scale | Chưa chạy | NOT RUN |

Không suy PASS chức năng từ build. Lịch sử hai lượt build thất bại giữ trong report và log riêng, không ghi đè.
