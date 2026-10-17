# T06 — Kết quả đợt nghiệm thu đã duyệt

## Kết luận cuối 17/10/2026 — T06 10/10 ID gốc PASS

Đối chiếu [ma trận G02](../acceptance-matrix.md). WindowsroundR7 đã gỡ cácgate
cuối, không cònNOTRUN/BLOCKED hiệnhành ởT06. B01–B07/PKCSregressionPASS.
Cácđoạn vàbảngphía dưới giữ **lịch sử** từnglượt, khôngxóaFAIL cũ.

| Ca cuối | Expected / actual | Kết quả |
|---|---|---|
| Windows.runtime | OWNERWindows=ORIGINAL_LOGIN=SUSER_SNAME;App1/Setup0/sysadmin0;setupcheck/bootstrap/rawcredential229 | PASS |
| Windows.setup | Windowsidentitykhác;Setup1/App0/sysadmin0;Completedbootstrap51401 | PASS |
| T06-K01.a.clean.exe | Exe--setup dướisetupidentity tạo1ADMIN/1NVNULL,Completed,masked;popupOK→process exit0;runtimeenvcốýinvalidkhôngđược dùng | PASS |
| T06-K02.c.exeRestart | Processmới thấyCompleted,Completekhóa,hashkhôngđổi | PASS |
| T06-K02.d.lock/departure | Mỗitrạngthái chạyexe--setupmới,Completed/Completekhóa,hashgiữ,exit0 |2/2PASS |
| T06-K02.e.runtime.exe | RuntimeWindows chạy--setupbịtừchốitrướcForm;Pendingkhôngđổi | PASS |
| Runtime.login.exe | NativeLogin→Main/catalogs bằngruntimeidentitythật,exit0 | PASS |
| Cleanup | SQLlogins/usersvàWindowsaccountstạmdọn;đốichiếulại0/0/0 | PASS |

Evidence `../runs/G02_WINDOWS_REAL_R7_20261017/`:8/8recordPASS,3cleanuprecordPASS,
ảnh đãreview. `G02_FINAL_ACCEPTANCE_20261017/integrity-and-cleanup.json` xác
nhận99filesourcekhôngđổi,deployedDLLkhớpworkingtreeSHA256,principalstạm0.
KhôngthaySQLimpersonation bằng“realidentity” suông:SQLvàownerprocess đãđọc
thực tế. RawR6driverOKBLOCKED giữ lại;R7tìmchildnativeOK rồiPostBM_CLICK.

Chốtnghiệmthukỹthuật,chưa commit/push; mãcommitG02sẽbổsungkhiđượcgiaoriêng.

## Windows thật R6 — 17/10/2026

UAC đã chạy. `../runs/G02_WINDOWS_REAL_R6_20261017/runtime-principal.json`
xác nhận runtime Windows identity thật trùng ORIGINAL_LOGIN/SUSER_SNAME,
roleApp1/Setup0/sysadmin0; setupcheck/bootstrap và rawcredential SELECT đều
SQL229. PKCS thực tế10.0.13. **PASS nhánh Windows runtime/SQL isolation**.

Ảnh `runtime-setup-denied.png` thể hiện exe từ chối quyền đúng, không vào setup.
Driver tìm nútOK bằngUIAutomation không thấy, nên round ghiBLOCKED trướcca
clean setup. Đây là lỗi thao tác driver, không kết luận bugứngdụng. Giữ raw,
driver bổ sung EnumChildWindows tìmOK rồiPostBM_CLICK, tiếp tục roundR7.
R6 cleanup haiSQLlogin và haiWindowsaccounts đềuPASS; không giữ credentials.

## Regression B07 đã duyệt — 17/10/2026

Giữ rawFAIL của `G02_FINAL_BOUNDARY_DIAG_20261017`, không đổi expected. Sau
duyệt sửa đúng4pattern SQL, đóngmanifest mới và kiểm trên DB mới:

| Evidence | Ca / actual | Kết quả |
|---|---|---|
| `20261017_155948_e814a21e` |262test mới: auth/legacy-read mọi ASCII+6Unicode, CHECK128ASCII; toàn bộ222test cũ |484/484PASS |
| `G02_B07_SQL_SUPPLEMENT_20261017` |69invalid bootstrap dưới setup role, SQL51404,Pending/account0/NV0,transaction0:0 |69/69PASS |
| cùng lượt | `_`,`-`,`.`,`Ab._-19`,50`_`,50`-` ×2CI-CS:bootstrap commit đúng tên,login trim/case được |12/12PASS |
| cùng lượt |Legacy preflight accepted/trimmed symbols; confirmation không đổi hash, runtime login đúng,CI-CS |2/2PASS |
| `G02_B07_UI_REGRESSION_20261017` |10invalid UI trướcSQL;3validlength1/50/trim-case clearpassword;native51chỉnhận50;sessionaudit |15/15PASS |

Manifest/harness tại `../runs/G02_B07_HANDOFF_20261017/`; 99file không đổi trong
lượt regression. Source G02 chưa commit, baseline/seed/001 không đổi. B07 đóng
ở phạm vi kỹ thuật; T06 chưa Hoàn tất vì Windows setup/runtime identity và
successpopup/restart exe còn chờ UAC. LượtR5 timeout khi chờ hộp thoại cấp quyền,
không phải FAIL ứng dụng; không thay bằng impersonation hoặc đọc source.

PKCS actual-load sau patch đãPASS:probe UseWindowsForms lấy assembly từ
WindowsDesktop.App10.0.13, informational10.0.13/runtime10.0.13; xemSEC_PKCS.

## Bổ sung 17/10/2026 — username UI / credential context

Evidence `../runs/G02_FINAL_BOUNDARY_DIAG_20261017/results.json`:10invalid UI
(empty/space/51/inner/tab/newline/slash/Vietnamese/fullwidth/emoji) bị chặn
trước SQL, giữ input, không session;2valid UI length1/50 đăng nhập và clear
password; nativeEM_REPLACESEL51 chỉ nhận50; sessionfields probe PASS.
**FAIL** validtrim-case `Ab._-19`, UI báo lỗi username từ SQL. SELECT readonly
`sql-charset-investigation.json` xác nhận `_`/`-` bị patternSQL coi invalid;
không phải trim/password. Đã ghiB07, chờ duyệt, chưa vá trong lượt test.

Rà credential theo ngữ cảnh: request chỉ UI→BLL, ToString trả typename;
credential tạm chỉ DAL/BLL không bind UI; SQL nhận hash, không plaintext;
SessionContext giữ identity/stamp/token/revocation, không password/hash.
DataOperationLog chỉ allowlist metadata, không SQL/parameters/exception;
auth/setup repository không gọi logger hoặc serialize credential. Probe sinh
secret trong RAM, không CLI/log; đây là audit đường hiện có, không cam kết
memoryzeroing hoặc mọi possible secret từ patternscan.

Sau PKCS pin10.0.13: G02 `20261016_223718_97354082`222/222PASS, DAL20/20PASS.
Windows principal R3/R4 bị chặn do Desktop10.0.12 cấp PKCS cũ, cleanup đủ;
đã cài Desktop10.0.13, runner `20261017_155555_beee42d8`222/222PASS. Actual
Windows/native đang kiểm tiếp, chưa đổi gateWindows thànhPASS.

Môi trường: Windows/.NET SDK10.0.401, SQL Server localhost17.0; DB riêng có
G02TestMarker, baseline→001→002→003. Source G02 uncommitted, mốc nền G01
deabac108; checksum/manifest chi tiết trong evidence. Password fixture sinh
trong RAM, không ghi vào biên bản. Chưa chốt10ID gốc PASS.

## Evidence và thao tác

- `../runs/20261009_213818_cf2043cf/`: Test-G02 -Task T06 -AcceptanceApproved;
  **19/19 xUnit PASS**, exit0, SetupError=null, SourceUnchanged=true. DB chính
  `CoffeeManager_Test_G02_20261009_213819_752835c4`; DB race/rollback riêng
  liệt kê trong results.json. TRX/stdout/stderr đầy đủ.
- `../runs/G02_SUPPLEMENT_20261009/`: C# probe gọi BLL/DAL thật và SQL thật;
  từng actual trong supplemental.json, 9DB fixture ghi tại Databases.
- `../runs/G02_EXTRA_20261009/`: preflight CS username collision và DB-error
  probe; từng expected/actual trong extra-results.json.
- `../runs/G02_NATIVE_{100,125,150}_20261009/` và
  `../runs/G02_UI_EXTRA_20261010/`: Login/MainForm thật, principal DB runtime
  impersonation Pooling=false, native screenshot; không thay kiểm chứng Windows
  principal thực tế của người cài.
- Harness thao tác bổ sung được lưu tại `../runs/harness-snapshot/`. Các
  assertions ở source/tests/migrations/tools sản phẩm không sửa trong lượt.

## Subcase bắt buộc

PASS dưới đây chỉ dành cho subcase đã đủ chứng cứ được nêu, không tự tick rootID.

| Subcase | Expected | Actual / evidence | Kết quả |
|---|---|---|---|
| T06-K01.a.SQL | Clean:1NV/ADMIN, optionalSDTNULL, Completed atomic | Probe clean:1ADMIN/1NV NULL; bootstrap/rollback integration đối chiếu toàn bảng | PASS |
| T06-K01.a.UI | Nhập bootstrap clean bằng app/process setup thật | Chưa thực hiện setup UI bằng Windows identity riêng | NOT RUN |
| T06-K01.b | Chỉ bật demo admin đã chọn | Probe:admin bật, thungan/kho inactive/placeholder không đổi | PASS |
| T06-K01.c | Legacy verify→Completed, không đổi hash/lương | Probe valid:LegacyReview→Completed, hash không đổi; chỉ procedure state update | PASS |
| T06-K01.d.inactive | Không reset admin inactive | Probe reject, LegacyReview giữ | PASS |
| T06-K01.d.departed | Không reset admin đã nghỉ | Probe reject, LegacyReview giữ | PASS |
| T06-K01.d.malformed | Không tự sửa hash invalid | Probe reject, hash/state giữ | PASS |
| T06-K01.existing | Tạo account cho NV chưa account, không đổi hồ sơ | Probe NV002 snapshot hồ sơ trước/sau bằng nhau | PASS |
| T06-K02.a | Hai session chỉ1commit | xUnit concurrent:Task.WhenAll,1success,1ADMIN,Completed,transaction0 | PASS |
| T06-K02.b | Lỗi sau account rollback toàn bộ, retry được | xUnit trigger:toàn bảng trước/sau bằng nhau; bỏ trigger rồi retryCompleted | PASS |
| T06-K02.c | Lần2/restart exe chặn, hash không đổi | Lần2/connection mới bị chặn; restart exe với setup chưa thao tác | NOT RUN |
| T06-K02.d.account | Completed + khóa admin không mở lại | xUnit SQL51401 khi admin locked | PASS |
| T06-K02.d.employee | Completed + admin nghỉ không mở lại | Đã kiểm employee0 login/permission; chưa gọi bootstrap nhánh này | NOT RUN |
| T06-K02.e.runtime | Runtime không EXECUTE setup | Probe runtime SQL229 | PASS |
| T06-K02.e.timeout | Timeout sau commit đối chiếu, không gửi tạo lặp | Chưa inject mất response bootstrap | NOT RUN |
| T06-K03.a.correct | Mật khẩu đúng được login | Probe case khác và native3role login thành công | PASS |
| T06-K03.a.wrong | Mật khẩu sai bị chặn | Probe rejected | PASS |
| T06-K03.a.missing | Login lạ bị chặn | Probe rejected | PASS |
| T06-K03.b.placeholder | Placeholder thất bại an toàn | Unit + integration probe rejected | PASS |
| T06-K03.b.malformed | Hash malformed không crash/không login | Integration probe rejected | PASS |
| T06-K03.c | Password không trim | Password cóspace loginđúng; bỏspace rejected | PASS |
| T06-K04.a.account | HoatDong0 bị chặn | Probe riêngaccount0 rejected | PASS |
| T06-K04.a.employee | DangLamViec0 bị chặn | Probe riêngemployee0 rejected | PASS |
| T06-K04.b.account | Khóa giữa phiên revoke trước nghiệp vụ | Probe + xUnit next Demand rejected/revoked | PASS |
| T06-K04.b.employee | Nghỉ giữa phiên revoke | Probe next Demand rejected/revoked | PASS |
| T06-K05.a | OldV3 yêu cầu rehash | Probe Iteration10000→SuccessRehashNeeded | PASS |
| T06-K05.b | Hash DB nâng thật/stamp mới | Probe login:hash khác,verifySuccess,stamp khác; không emit hash | PASS |
| T06-K05.c | Password đổi trước CAS không bị overwrite | xUnit đọcold,owner đổipassword, runtimeCASnull; oldloginreject,newloginaccept | PASS |
| T06-K06.a | Full3role permissionmatrix | Probe loop Enum.GetValues:11permission×3role=33; actual text “12” trong probe là countlabel sai, loop source kiểm đủ11 | PASS |
| T06-K06.b | Rolechange revoke + directservice read/write | Demand revoke và freshloginCASH đãPASS; directwrite bằng objectrolecũ chưa thao tác | NOT RUN |
| T06-K07.a.login | Password che và session khôngcredential | NativeUseSystemPasswordChar=true; session không Password/Hash; screenshotmask | PASS |
| T06-K07.a.setup | Setupchepassword/khôngsecret | Chưa chạy bootstrapnative | NOT RUN |
| T06-K07.a.logs | Khôngsecret/hash tronglog | Scanartifact:0Identityhashpattern; probes khôngemitpassword; không chứng minh mọi possible secret | NOT RUN |
| T06-K08.a | Objectsession cũ khôngservice được | Probe Dispose→CatalogList rejected, không ObjectDisposedException | PASS |
| T06-K08.b.read | Logout readpending bỏ kết quả muộn | Native SQLlock→logout→unlock,oldMain disposed,không reopen | PASS |
| T06-K08.b.login | Đóng/logout trong loginpending không mởMainmuộn | Chưa native thao tác nhánhloginpending | NOT RUN |
| T06-K09.a | DBerror riêng,không success/saiPasswordgiả,giữinput | BLL throwsDataAccessException; probe đòiKindConnection nhưng endpoint có thểTimeout; chưa nativegiữinput | BLOCKED |
| T06-K10.a.length1 | Username1 hợp lệ | Probeaccepted | PASS |
| T06-K10.a.length50 | Username50 hợp lệ | Probeaccepted | PASS |
| T06-K10.a.length51 | Username51 bị chặn | ProbeArgumentException | PASS |
| T06-K10.a.charset/trim | ASCII/trim/case đồng nhấtUI/BLL/SQL | Unitinvalid/trim và SQLkey đãchạy; chưa đủ mọiUI/SQLboundary | NOT RUN |
| T06-K10.b.duplicate | Case/key trùng SQL chặn | CI/CS probe duplicate lowercase rejected | PASS |
| T06-K10.b.preflight | Legacy collision migrationrollback | Extra CSlegacy admin/ADMIN:Version2/DDLabsent sau reject | PASS |
| T06-K10.c.CI | CI target case-insensitivekey | ProbeloginMIXED.CASE/duplicate rejected | PASS |
| T06-K10.c.CS | CS target keyvẫncase-independent | ProbeloginMIXED.CASE/duplicate rejected | PASS |

Các ca NOT RUN/BLOCKED không được suy PASS từ source. T06 tiếp tục Đang làm.

## Regression phụ thuộc sau debug B01/B02 — 11/10/2026

RunnerAll tại `../runs/20261011_171039_f2e46bea/` dùng3DBtest mới với003sửa,
19/19case tự độngT06 tiếp tụcPASS (196/196của cảrunner), SetupError=null,
exit0,sourcefixed=true. Kiểm auth/setup/race/rollback/CAS/session cùng schema
mới, không thay PASS cho các nhánhsetupnative/restart/timeout vẫnNOTRUN ởtrên.

## Bổ sung nghiệm thu 11–16/10/2026 — source sản phẩm không đổi

Manifest97file được đối chiếu tại `../runs/G02_FINISH_HANDOFF_20261016/`.
Các kết quả dưới thay đúng trạng thái nhánh tương ứng trước đó, không xóa
rawFAIL và không tự chốt10ID gốc khi còn FAIL/BLOCKED.

| Subcase | Expected / actual | Kết quả / evidence |
|---|---|---|
| T06-K02.d.account | Khóa admin sauCompleted không mở setup; gọi lại bị chặn,Completed giữ | PASS, FINISH_DATA |
| T06-K02.d.employee | Nghỉ admin sauCompleted không mở setup; gọi lại bị chặn,Completed giữ | PASS, FINISH_DATA |
| T06-K06.b.read/write | Đổi ADMIN→CASH, hai object phiên cũ read/write đều bị revoke; không tạoNCC73; login mới cóCASH | PASS, FINISH_DATA |
| T06-K08.b.loginPending | Đóng Login khi SQL bị session khác khóa; unlock không phát LoggedIn, không mởMain mới | PASS, FINISH_AUTH_CONFIRMED/FINISH_AUTH_UIA |
| T06-K09.a.loginDBError | Login SQLtimeout báo lỗiDB, không giả saiPassword/success; giữ input; ảnhmasked | PASS, FINISH_AUTH_CONFIRMED/FINISH_AUTH_UIA |
| T06-K02.e.timeoutAfterCommit | Chèn WAITFOR sauCOMMIT trên DBtest; commandtimeout1s; UIđọcStateCompleted và khóa gửi lại | PASS, FINISH_AUTH_CONFIRMED |
| T06-K07.a.setupMask | Bootstrap password/confirmation UseSystemPasswordChar=true, ảnhnativeche; saucommit clear | PASS nhánhmask, FINISH_AUTH_CONFIRMED |
| T06-K02.c.reopenForm | Mở lại BootstrapForm sauCompleted: nútCompletekhóa, hash không đổi | PASS choForm; chưa thay test restart exe bằngWindowssetupidentity |
| T06-K01.a.clean.native | DBcó1ADMIN/1NVSDTNULL/Completed; passwordđãclear, formBusytrue, probe không hoàn tất nhấnOK success | BLOCKED ởbước đóng popup; CLEAN_DIAGNOSTIC |
| T06-K10.a.SQL.length1 | Bindinputkhôngclamp, SQLlưuđúng1 | PASS, FINISH_DATA |
| T06-K10.a.SQL.length50 | Bindinputkhôngclamp, SQLlưuđúng50 | PASS, FINISH_DATA |
| T06-K10.a.SQL.length51 | Expectedreject; actualtạoadminusername50 vàCompleted | **FAIL G02-B03**, FINISH_DATA |
| T07-K04.c.Login/sourceT06 | SaveDesigner→buildclone→runtime: Button12px/font23px,8rowAbsolute20 | **FAIL G02-B04**, DESIGNER_SOLUTION + DESIGNER_AFTER_SAVE |
| T07-K04.c.Bootstrap/sourceT06 | Đúngsolution nhưngDesignerbáo khôngparseđược dòng38 | **FAIL G02-B05**, DESIGNER_SOLUTION |

Tên evidence ở trên là thư mục `../runs/G02_<tên>_20261011/` hoặc20261016:
`G02_FINISH_DATA_20261011`, `G02_FINISH_AUTH_CONFIRMED_20261011`,
`G02_FINISH_AUTH_UIA_20261016`, `G02_FINISH_CLEAN_DIAGNOSTIC_20261016`,
`G02_DESIGNER_SOLUTION_20261011`, `G02_DESIGNER_AFTER_SAVE_20261011`.
JSON ghi từngId/expected/actual/status; C#harness vàSQLfaultfixture được lưu.

### Giới hạn cần giữ

- Setup/runtime hiện đã kiểm bằng2userDB riêng WITHOUTLOGIN/EXECUTEAS, chưa
  thay được2Windowsprocessidentity thật. Token hiện tại Medium, nhómAdmin
  deny-only; chưa tạo localaccount hay tự nâng quyền. CaWindowsprincipal cònBLOCKED.
- Nativeclean đãcommit là bằng chứng SQL/UIinput, **không** chứng minh nhấnOK/
  thoátsetup thành công. Chưa rõ automation popup hay luồngUI; không gán thêm
  bug runtime ngoàiB03–B05 khi chưa đủ chứng cứ.
- LượtAuthFault đầu cófalsefailure“reopenallowed” vìharnesschưađợiShown/readState;
  lượtCONFIRMEDđãđợiStateCompleted. GiữrawFAIL, không đổi expected.
- Designer chỉ sửa bản saoTemp, không ghiđè sourceproject. Sourceworking vẫn
  fixed97file. Các bướcusernamecharset/UIboundary vàexeWindowsrestart còn ở
  danh sách cònthiếu, khôngsuyPASS từ đọc source hoặc trườngMaxLength.

## Đợt debug B03–B06 đã duyệt — regression cuối16/10/2026

Source được sửa ngoài lượt nghiệm thu trước, sau đó đóngmanifest mới.
Runner cuối `../runs/20261016_211207_a26a8e4f/`: **222/222PASS**, gồm45caseT06
(19cũ+26boundarymới) và177T08, exit0/SetupErrornull/SourceUnchangedtrue.
3DBtest mới cómarker;002sửa không được áp lênDB giữchecksum cũ.

| Nhánh | Expected / actual | Kết quả |
|---|---|---|
| B03.AuthRead/LegacyRead×12invalid | FullinputNVARCHAR(MAX):empty/space/outerspace/inner/control/slash/Vietnamese/fullwidth/emoji/51/1001 đều51404,transaction0:0 |24/24PASS |
| B03.AuthRead/LegacyRead.valid | Canonicalkeyg02.admin vẫnđọcđúng1account |2/2PASS |
| B03.Bootstrap13invalid | NULL/empty/space/outerspace/inner/control/slash/Unicode/51/1001 đều51404; Pending/NV/account/CV khôngđổi,transaction0:0 |13/13PASS |
| B03.Valid1/50/default/CI/CS | Lưuđúnglength,Completed;BLLloginlowercase+outerspace thànhcông |4/4PASS |
| Authsupplement | Clean/demo/existing/legacyinvalid/inactive/departed/valid, DBrehash/revoke vàCI-CS nhưtrước trên002mới |28/28PASS |
| B04.Loginbinary sauSave×3DPI |8rowexplicit,7AutoSize; buttonHeight≥font+padding,labelsđủbounds,masktrue;0constructorfactoryopens |3/3PASS |
| B05.Bootstrapbinary sauSave×3DPI |9rowexplicit;2modeđúngenable/disable,candidate,name/passwordmask;buttonđủheight;khôngconstructorquery |3/3PASS |
| DesignerLogin/Bootstrap | Mở/save/close/reopen đúngproject,surface thật khôngparseerror | PASS, ảnh đãxem |

Evidence: `../runs/G02_B03_BOOTSTRAP_20261016/results.json`,
`G02_B03_AUTH_SUPPLEMENT_20261016/supplemental.json`,
`G02_B03_B06_DESIGNER_FINAL_CONFIRMED_20261016/`,
`G02_B03_B06_FINAL_LAYOUT_{96,120,144}_20261016/`.
Toànbộ98filesource/migration/test/tools khôngđổi từđầurunnercuối đếnsaunative;
binarytest khớpSHA256clone sauSave ở `G02_B03_B06_HANDOFF_20261016/integrity.json`.

B03/B04/B05 đã xử lý vàregressionPASS. Không chốtT06Hoàn tất: Windowsidentity
thật/setup-exerestart vàsuccesspopupclean cònBLOCKED nhưđãghi, khôngdùng
DBimpersonation/Formreopen thaythế. CảnhbáoNuGetPKCS đượcghiriêng, chưa nângpackage.
