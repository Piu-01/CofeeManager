# G02 — Theo dõi blocker, cập nhật cuối 17/10/2026

## Kết luận cuối 17/10/2026 — không còn blocker nghiệm thu

WindowsroundR7 đãPASS8/8record:haiidentitythật tách quyền,setupOK đóngexe,
restart/lock/departure không mở lại bootstrap,runtimeexe loginđược. Cleanup
đãđốichiếulại0account/DBuser/SQLlogintạm. B07/PKCS đãđóngregression;
27/27IDgốc vàmọisubcase đãđốichiếutại [acceptance-matrix.md](acceptance-matrix.md).

**Chỉ còn bàn giao Git:** chưa commit/push,chờngườidùnggiaoriêng.
Khôngcòn yêu cầu bấmUAC hoặcduyệtbug đangchờ củaG02.

Cácmụcphíasau là **lịch sử blocker đã gỡ**, khônglàtrạngtháihiện tại.

### Windows roundR6 đã chạy qua UAC

Runtime identity/SQL principal thật và quyền tách biệt đãPASS, PKCSactual10.0.13.
Exe cũng hiển thị từ chối setupđúng (đãxemảnh). Driver khôngtìmđượcOK nên
roundBLOCKED trướcclean setup, cleanup đủhaiWindowsuser/haiSQLlogin. Đã sửa
driver thao tác native, đang chạylạiR7; không còn nóiR6chỉchờUAC.

## Kết luận cập nhật sauB07 — 17/10/2026

**B07 đã được duyệt/sửa/regressionPASS:**484/484automated,83/83SQL,
15/15UI. PKCSđãvápackage/runtime10.0.13 vàactual-loadWinFormsPASS,
DAL20/20PASS. Không cònchờduyệtB07 hoặcPKCS; cácghi chú bên dưới là lịch sử.

**Còn chặn:** Windowsidentitythật/setupsuccesspopup/restart exe. R5 timeout
khi chờUAC trước chạy script; chưacóactualkếtquả. Cần người dùng nhấnYes/Có
trênhộpthoạiWindows (quyền thực hiện đãđượcduyệt), sauđó chạy/lưuđủca và
cleanupSQLlogin/Windowsaccountstạm. Không đổigate thànhPASS từ484/83/15test.

## Cập nhật 17/10/2026

- Đã duyệt UAC/hai Windows identity tạm và vá/regression PKCS; không còn chờ
  quyền chuẩn bị môi trường. Tất cả tài khoản/login thử phải cleanup.
- Pin PKCS10.0.13, audit sạch; actual-load trước cài vẫn dùng Desktop10.0.12.
  Đã cài Desktop10.0.13; runner sau cài222/222PASS. Native/actual-load đang kiểm.
- **B07 mới, chờ duyệt:** SQL chặn username `_`/`-` trái contract. Ca UI
  length1/50/invalid/nativeLimit đã kiểm; valid mixedcharset FAIL. Hồ sơ T06,
  evidence `G02_FINAL_BOUNDARY_DIAG_20261017`; chưa sửa source/migration.
- Windows R3/R4 cleanup đủ; chỉ thay các gateWindows/setup-popup thànhPASS
  khi có actual lượt mới, không từ việc cài runtime/tạo tài khoản.

Đã chạy thêm các ca SQL/BLL/native trên source cố định sauB01/B02. Không còn
coi Designer/modal/DPI là chưa thử: đã có kết quả thực tế ở biên bản từng task.
**Chưa đủ điều kiện chốt27ID gốc.** Không đổi checklist T01–T32/325ca.

## 1. Đợt bug B03–B06 đã được duyệt và xử lý

**Cả4bugđãsửa/regressioncuốiPASS16/10/2026.** Bảng dưới giữ hiện tượng và
phạmvi trước sửa đểtruyvết; không phải danh sách chờduyệt lại.

| Bug | Task sở hữu | Actual trước sửa | Phạm vi đã sửa |
|---|---|---|---|
| B03 | T06 | SQLnhậnusername51, cắt50 rồiCompleted | BoundarySQLbootstrap/auth, giữcolumn/key50/ASCII vàatomicsetup; DBtest mới |
| B04 | T06, caDesignerT07 | LoginSaveDesigner thêmAbsolute20row, runtimebutton12px/font23px | Row/ColumnStyles tường minh, round-trip/runtime3DPI |
| B05 | T06, caDesignerT07 | BootstrapDesignerparseerror dòng38 | Initializer cú phápDesignersupported; giữfields/handlers/layout vàkhôngqueryDB |
| B06 | T08, caDesignerT07 | Editor/CatalogsDesignerparseerror dòng47 | Haiinitializer, giữbinding/theme/keyboard, round-trip7editor/3DPI |

Hồ sơ: [T06](t06-bootstrap-dang-nhap-phan-quyen/bug-report.md),
[T08](t08-danh-muc-nen/bug-report.md). B01/B02 đãsửa vàregressionPASS, không
gộp lại làbugchưa sửa. B03–B06 được sửa ngoài lượt nghiệm thu cũ, đóngmanifest
mới rồi chạyregression; khôngvá source tronglượt test.

Runnercuối222/222PASS,bootstrapboundary17/17 vàauthsupplement28/28PASS;
5surfaceDesigner round-trip, runtimebinaryclone sauSave đượcđốichiếuhash,
23/23CRUD và36/36UI/DPIcasePASS. B06lượt1FAILgiữraw; lượtcuối giữAutoSize
columnnhưdefaultcũ vàhếtgrid150%5px. Xemreport.md/handoff củađợtsửa.

Findingngoàiphạmvi: [PKCS audit](../../bugs/pkcs-dependency-audit/report.md),
SqlClient6.1.3→PKCS9.0.4 bịNU1902/NU1903; chờduyệtđánhgiá/nângdependency
riêng, khôngtựđổi thưviện/SDK hoặcdisableaudit trongđợtB03–B06.

## 2. Ca cần hỗ trợ môi trường hoặc thao tác native đáng tin cậy

| Nhánh | Trạng thái hiện tại | Cần gì để hoàn tất |
|---|---|---|
| Windows setup/runtime haiidentity/process thật | BLOCKED; DBuserWITHOUTLOGIN/EXECUTEASđãkiểm, token hiện tạiMedium/Admin deny-only | HaiWindowsidentity test đãprovision hoặcphiên cóquyền phùhợp đểprovision; chỉ cấprole ởDBtest riêng, passwordkhônggửichat/file/log |
| Setupclean thànhcông→nhấnOK→processđóng | BLOCKED; DB1ADMIN/1NVNULL/Completed, passwordclear; probe dừngởpopup vớiBusytrue | Thao tác/captureOK đúngmessagebox, xácnhận setupthoát; chưađủbằngchứnggọilàbugruntime |
| Bootstrap lần2 vàrestart exe thật | NativeFormreopenđãPASS, khôngthayactualexe dướiWindowssetupidentity | Chạy lạiapp--setup dướiidentityđúng, đối chiếuhash/statekhôngđổi |

Không tự tạoWindowsaccounts/tăngquyền hay dùngWindowsidentityowner/sysadmin
thayidentityruntime đểbáoPASS ca phân tách. Không cần quyềnAdmin cho các ca
SQL/nodeUI đã chạy qua testhost; blocker chỉ dành nhánhWindowsprincipal thật.

## 3. Đối chiếu cuối sau debug

- T06-K10: bảnB03 cóSQL51/1001/Unicode/null/space/controlbịchặn vàvalid1/50/
  CI-CSloginPASS. Raw51FAILtrướcsửa vẫn giữ; khôngđổi thànhPASS từMaxLength
  textbox. Khi chốtIDgốc đối chiếu đủUI/C#/SQL,khôngbỏcasechưacóactual.
- T06-K07: kết hợp ảnhmask/passwordclear vàràlog/session/credentialboundary;
  không xuấtplaintext/hash rađể làmproof. Scanpattern khôngthay auditngữcảnh.
- T07-K04.c: cả5surfaceđãmở/save/close/reopen, ảnh đãreview; Login/Bootstrap/
  editor/catalogs saufixđãPASS cácnhánhruntime/3DPI. Khônglấycaptionthayproof;
  hashbinary vàtest sauSave đãlưu.
- T07-K07.a:3nhánhNavigate/Logout/Close đãPASS cho luồngmodal thật ở
  `runs/G02_FINISH_LEAVE_CONFIRMED_20261016/`: parent bịkhóa trongdraft,
  X/Khônggiữnháp, X/Cóbỏ rồihànhđộngparentthànhcông; khôngtựhứa thêm
  mànnhápinline/POS chưa triển khai.
- T08-K02: directservice malformed/duplicate6kind vàSQL12case đãchạy;
  DMserverallocation/receipt cóca riêng. Cácsubcase tên/phone đãcóregression;
  sauDesignerfix cầngiữUIvalidation/keyboard/fields đúng.
- T08-K06.b: rerunmigration003checksumhiệntại trênDBdemomới đãPASS:
  AlreadyApplied/fullDBsnapshotgiữ, kểcảcustomname/support/history.
- Xem lại từngsubcase ba plan vàbằngchứng, không cộng sốtest xUnit/harness
  thành sốIDnghiệmthu. Case nàochưa cóactual thìgiữNOTRUN/BLOCKED.

## 4. Evidence và tính toàn vẹn

`runs/G02_FINISH_HANDOFF_20261016/` lưu harness/manifest nghiệmthu97file trước
sửa. ĐợtB03–B06 tại `runs/G02_B03_B06_HANDOFF_20261016/` có98file (thêmtest
boundary), source khôngđổi tronglượt cuối. FaultSQL chỉ WAITFOR sauCOMMIT trên DBtest có
marker, restorebody trongfinally; các scriptfixture được lưu. Designer dùng
bản saoTemp, khôngcopysource bịserializerđổi vào workingtree. DPI đãtrả125%.
CácrawharnessFAIL vẫngiữ, đối chiếu bằnglượt mới; không tính chúngthànhbugapp.
