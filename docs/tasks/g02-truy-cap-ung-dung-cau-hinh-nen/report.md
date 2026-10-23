# G02 — Kết quả nghiệm thu và bàn giao kỹ thuật

**Điều tra bổ sung 18/10/2026:** người dùng chưa chạy được luồng F5 với DB/tài
khoản sử dụng được; phát hiện tác động DB test/tài khoản Windows chưa được bàn
giao rõ. Xem [báo cáo điều tra](../../bugs/project-environment-audit-20261018/report.md).
Kết quả PASS bên dưới là lịch sử kiểm chứng của bản/fixture ghi kèm, không thay
bằng chứng chạy development hiện tại. Lượt điều tra chỉ đọc, chưa sửa hay dọn dữ liệu.

## Kết luận nghiệm thu cuối — 17/10/2026

**G02 đủ điều kiện chốt kỹ thuật: 27/27 ID gốc PASS** (T06:10/10,
T07:7/7,T08:10/10). Đã đối chiếu mọi nhánh bắt buộc của ba plan, không cộng
số test xUnit/harness thành sốID. Bảng đối chiếu:
[acceptance-matrix.md](acceptance-matrix.md).

- Windows thật R7: **8/8recordPASS**,setup/runtimeidentity tách biệt,
  ORIGINAL_LOGIN đúng,khôngsysadmin;runtime setup/rawcredential bịSQL229.
- Setupclean tạo1ADMIN/1NVSDTNULL,Completed;successpopupOK đóngprocess exit0.
  Restart exe,khóa/nghỉadmin vẫn khóa bootstrap,không đổi hash. Runtimeexe
  đăngnhập được vàvàoADMINcatalogs. Ảnh đãreview,không chỉstatusJSON.
- B01–B07 đãsửa/regressionPASS;484/484automated,83/83SQLusernamebổsung,
 15/15UIboundary;postDesignerSave23/23CRUD và36/36UI/DPI.
- PKCSpackage/runtime10.0.13actual-loadPASS,auditkhôngliệtkêvulnerable;
  DALsauruntimepatch20/20PASS. Deploycầnruntimevá theoREADME.
- `runs/G02_FINAL_ACCEPTANCE_20261017/`:99filesource/migration/test/tool
  khôngđổi trongregression; deployedWinFormsDLLkhớpSHA256workingtree.
  Đốichiếulạisaudọn:0Windowsaccounttạm,0DBuser,0SQLlogin tạm.

**G02 đã bàn giao Git:** source/evidence commit
`1ab09af368f320a598a577d22542e41563e5e432` đã push thành công `origin/main`
theo yêu cầu “push đi”. Xem [git-handoff.md](git-handoff.md).
Không còn blocker nghiệm thu; không triển khaiG03 tự động.
Cácbáocáodưới là **lịch sử**, giữnguyênFAIL/BLOCKED vàđiểmchờduyệt cũ;
khôngdùngchúngthaykếtluậnmới.

## Lịch sử các lượt kiểm chứng

## Kết quả B07 và PKCS — cập nhật cuối17/10/2026

- **B07 đã duyệt, sửa và regressionPASS:**484/484test tựđộng,83/83SQL bổsung
  và15/15UI. ToànASCII/Unicodeboundary, bootstrap/login/legacypreflight trên
  CI-CS đều đúngcontract; không rewritechecksumDB cũ. RawFAIL giữ nguyên.
- **SEC-PKCS-01 đã xử lý:**package và DesktopRuntime10.0.13. Actual-load
  WinForms probe xác nhậnPKCS10.0.13/runtime10.0.13, không chỉ auditNuGet.
  DAL regression sau runtime patch20/20PASS. Deploycầnruntimevá nhưREADME.
- **G02 còn Đang làm:** lượtWindowsidentity thật R5 timeout ở chờUAC trước
  khi chạy script; chưa có kết quả setup-popup/exerestart. Đã hỏi người dùng
  xác nhậnYes/Có, chưa nhận câu trả lời. Không thay bằng impersonation.

Handoff `runs/G02_B07_HANDOFF_20261017/`:99filesource khôngđổi trongregression,
buildRelease thànhcông, harness/manifest. Chưa commit/push; không tick27IDgốc
khi cònWindowsgate.

## Tiếp tục chốt — 17/10/2026

**Chưa hoàn tất.** Đã được duyệt UAC để kiểm hai Windows identity thật và vá
PKCS/runtime. Đã pin PKCS10.0.13, giữ SqlClient6.1.3; fresh audit không liệt kê
package vulnerable. G02 trước runtime patch222/222 và DAL20/20PASS. Sau cài
WindowsDesktop10.0.13, runner `20261017_155555_beee42d8` tiếp tục222/222PASS.

Actual-load probe trước cài cho thấy PKCS vẫn từ Desktop10.0.12 dù NuGet sạch;
không dùng audit thay actual-load. Installer10.0.13 được kiểm SHA512/chữ ký
Microsoft. Đang kiểm lại Windows/native sau cài. Các lượt R3/R4 đã cleanup đủ
SQL login và Windows account tạm; raw BLOCKED/FAIL giữ nguyên.

Ca username UI mới phát hiện **B07**: SQL chặn `_`/`-` trái contract. Các ca
invalid10, validlength1/50, native limit50, sessionfields PASS, riêng valid
mixedcharset FAIL. Đã ghi hồ sơ T06 và SELECT bằng chứng, **chờ duyệt bổ sung**,
chưa sửa migration/source trong lượt test. Không thể chốt27ID khi B07 còn mở.

Evidence: `runs/SEC_PKCS_20261016`, `SEC_PKCS_20261017`,
`G02_FINAL_BOUNDARY_DIAG_20261017`, `G02_WINDOWS_REAL_R3/R4_20261017`,
`G02_CLOSE_HANDOFF_20261017`. Chưa commit/push.

Trạng thái: **Đang làm — đợt nghiệm thu đã được duyệt, có FAIL và ca còn thiếu**.
Requirements, cả bộ plan và đợt test đã được người dùng duyệt. Kết quả từng
subcase nằm trong ba biên bản bên dưới; chưa nghiệm thu đủ27ID gốc.
Đợt debugB01/B02 đã được duyệt bằng “xác nhận”; cả2bug đãsửa vàregressionPASS.
Nghiệm thu bổ sung phát hiệnB03–B06; người dùng đãduyệt vàcả4bug **đãsửa,
regressioncuốiPASS ngày16/10/2026**. Vẫn còncaWindowsidentity/setup-popup.
Chưa commit/push G02. Chi tiết gate: [remaining-cases.md](remaining-cases.md).

## Kết quả sau đợt sửa B03–B06 — 16/10/2026

| Kiểm chứng bản cuối | Kết quả / evidence |
|---|---|
| Debug/Release workingtree | Buildthànhcông;Releasecuối0warning/0error ởloghandoff |
| xUnit/unit+SQLintegration |222/222PASS,exit0,SourceUnchangedtrue; `runs/20261016_211207_a26a8e4f/` |
| B03bootstrapSQL |17/17PASS:invalidoriginalinputkhôngtạodữliệu;valid1/50/CI/CSloginđúng |
| Authsupplement |28/28PASS:clean/demo/existing/legacy/rehash/revoke/collation |
| Designer |5surface mở đúngproject/save/close/reopen;ảnh đãxem, khôngcaption-only |
| Binary sauSave | Hashruntimekhớpclone;CRUD7kind/cancel/search/owner/order23/23PASS |
| UI sauSave tại100/125/150% |36/36PASS:gồm6viewport,21editor,keyboard,Login/Bootstrap;DPItrả125% |
| Integrity |98filesource/migration/test/tools khôngđổi tronglượt cuối; `runs/G02_B03_B06_HANDOFF_20261016/` |

B03giữfullUnicodeinput đếnvalidation trướcconvertVARCHAR50; B04RowStyles
tườngminh; B05/B06initializerDesignersupported. B06lượt1có2FAILgrid150%5px:
đãkếtthúclượt, giữraw rồi sửaColumnStylePercentmới thêm vềAutoSize nhưdefault
cũ; lượtcuốiPASS, không đổi expected. Không cậpnhậtchecksumDBtest cũ.

Freshrestoreclone báoNU1902/NU1903 ởPKCS9.0.4 transitive từSqlClient6.1.3;
khácvớibuildcachedworkingtree0warning. Finding đãđốichiếuadvisory13/10,
[hồsơriêng](../../bugs/pkcs-dependency-audit/report.md), **chưa tựnângpackage**.
Chưa xácnhậnđườngkhai thác trongapp. Khôngbáodependency“sạch” từbuild/testPASS.

G02vẫnĐang làm vìWindowssetup/runtimeidentitythật,setup-exerestart vànhấnOK/
thoátpopupclean chưađủproof; khôngdùngimpersonation/Formreopen đểthaythế.
Chưa commit/push. Chi tiếttừngtask vàremaining-cases.md vẫngiữlịch sửFAIL.

## Lịch sử nghiệm thu bổ sung trước sửa — 11–16/10/2026

- Đã kiểm trực tiếp7tab:CRUD/tìm literals, mở lạiprocess, validation,
  xóaunused/history;7editor Hủy/X×Không/Có không ghi nháp.
- Đã kiểm đủ6viewport100/125/150%, keyboard/mnemonic và21editor theoDPI;
 3lượtconfirmed mỗi10/10PASS. DPI đãtrả125%Recommended.
- Đã kiểm100navigation rồi1click=recheck+List, callbacklogout1lần,
  weakrefsMain/session/cachedcontrols đượcGC, SQLDMV0→0; rolechange/lock đóngphiên.
- SQL/BLLbổsung59PASS/1FAILB03; selector/Validate3role×2purpose×5ID và
  lookup18case đãchạy. Các lượtcodevalidation12SQL/13service vàclean-default9case
  ghiJSONriêng. Demo003rerun mới giữfullhistory/configuration vàAlreadyApplied.
- SQLcommit rồiresponse-timeout đãtáihiện ởBootstrap vàDMeditor:Bootstrap
  đối chiếuCompleted/khóaretry; editorUnknown giữdraft/sendlock, receiptGet
  xácnhận1row. Không chỉinjectstate giả.
- Designer **đã thao tác được**, không cònblockermởnhầmproject: phát hiện
  LoginSavehỏnglayout, Bootstrap/editor/catalogsparseerror. Clone sauLoginSave
  build/chạythật xácnhậnbutton12px/font23px. Khôngghiđèsourceworking.

| Bug đã xử lý ở đợt sau | Actual trước sửa | Hồ sơ |
|---|---|---|
| B03 | SQLusername51bịcắt50 vàsetupCompleted | [T06](t06-bootstrap-dang-nhap-phan-quyen/bug-report.md) |
| B04 | LoginDesignerSave làmclippingruntime | [T06](t06-bootstrap-dang-nhap-phan-quyen/bug-report.md) |
| B05 | BootstrapDesignerkhôngparseinitializer | [T06](t06-bootstrap-dang-nhap-phan-quyen/bug-report.md) |
| B06 | Editor/CatalogsDesignerkhôngparseinitializer | [T08](t08-danh-muc-nen/bug-report.md) |

Ởlượtnghiệmthunày khôngsửaB03–B06; đợtdebugđượcduyệtchạy sauđó ghiởtrên.
CònBLOCKED nhánh2Windowsidentities thật/processexe
setuprestart (tokenMedium/Admin deny-only) và nhấnOK/thoátpopupsetupclean.
ĐãcóDBimpersonationtest vànativeFormreopen nhưngkhôngdùngthaycácnhánh đó.
Các biênbản task giữ từngactual/expected/PASS/FAIL/BLOCKED/NOTRUN;
khôngnghiệmthu27/27từcounttest. Source97filekhôngđổi trongđợtbổsung.

## Kết quả hiện tại — cập nhật 11/10/2026

### Sau debug B01/B02 — mốc trước đợt nghiệm thu bổ sung

| Kiểm chứng sau sửa | Kết quả | Evidence |
|---|---|---|
| Build Debug/Release toànsolution |0warning/0error cả2configuration | Tool output; integrity.json ghi mốc |
| RunnerAll trên3DBtest mới |196/196PASS, exit0, sourceunchanged=true | `runs/20261011_171039_f2e46bea/` |
| B02 boundary SQL Create/Update |79case mớiPASS; tên/phone vượtgiới hạn không còntruncate | CùngTRX, CatalogBoundaryRegressionTests |
| B02 phoneSQL riêng |10/10PASS:NULL/rỗng/trùng/16 vàvalid15/0đầu, Create/Update | `runs/G02_B02_PHONE_CONFIRMED_20261011/` |
| B01 native |5/5PASS:10click +directhandlers, cancel/retry, timeout/retry, đốichiếuunknownstate, logoutpending | `runs/G02_B01_REGRESSION_20261011/` |
| Integrity source/schema |97file khôngđổi sauSQL/native;001→002→003checksum mới | `runs/G02_B01_B02_REGRESSION_20261011/` |

**B01/B02 đã xử lý** trongphạm vi đượcduyệt. B01 khóa/guardread trongloading,
giữTải đốichiếuunknownDelete; B02 nhậnparameterđủrộng trướcvalidation,giữcột
lưu vàBLLcontract. Không sửachecksum củaDBtest đãáp dụng003cũ. RawFAIL
trướcsửa vàrawharnesserror vẫn giữ. Ngắtresponse sauwrite thật/Designer/
modal/keyboard vàcácsubcase cònthiếu không được thayPASS từregression này.

### Lịch sử nghiệm thu trước sửa

| Phần kiểm tra | Kết quả thực chạy | Evidence |
|---|---|---|
| T06 xUnit/unit + SQL integration |19/19PASS, exit0 | `runs/20261009_213818_cf2043cf/` |
| T08 xUnit/unit + SQL integration |98/98PASS, exit0 | `runs/20261009_214858_45d2ab1f/` |
| T06 bổ sung | Clean/demo/legacy, DB rehash, CI/CS và revoke đã có actual riêng | `runs/G02_SUPPLEMENT_20261009/` |
| SQL bổ sung |20/20PASS: invalid boundaries/NULL/FK/actor/stamp, valid boundaries, selector3role×2purpose | `runs/G02_REMAINING_SQL_CONFIRMED_20261011/` |
| Native UI |3role start/menu,100navigation và10login/logout đã chạy; loading gửi lặpFAIL | `runs/G02_NATIVE_*`, `runs/G02_UI_EXTRA_20261010/` |
| Designer/modal/keyboard và các nhánh khác | Có BLOCKED/NOT RUN; chưa đủ nghiệm thu | Biên bản từngtask |

Không cộng số test thành số ID nghiệm thu. Hai lỗi ứng dụng đã được xác nhận:

- **G02-B01:** Tìm vẫn enabled trong loading, thao tác lặp tạo thêm read/recheck.
- **G02-B02:** SQL Supplier nhận tên101/SDT16 rồi cắt100/15 và lưu, thay vì reject.

Hồ sơ: [bug sở hữu T08](t08-danh-muc-nen/bug-report.md),
[liên kết T07](t07-khung-giao-dien-dieu-huong/bug-report.md).
CácFAIL ởtrên làlịch sửtrướcsửa; kết quả sau đợt debug đãduyệt ghiởđầu báo cáo.

Biên bản đầy đủ:
[T06](t06-bootstrap-dang-nhap-phan-quyen/test-results.md),
[T07](t07-khung-giao-dien-dieu-huong/test-results.md),
[T08](t08-danh-muc-nen/test-results.md).
Ca còn thiếu gồm setup Windows identity/UI và restart/timeout; native
Designer save-close-reopen, modal/keyboard và navigation GC/event/SQL resource;
CRUD native7tab, concurrent editor và mất response sau commit. Giữ riêng
PASS/FAIL/BLOCKED/NOT RUN cho từng subcase, không coi ảnh hoặc đọc source là test.

Manifest đợt nghiệm thu trướcsửa `runs/acceptance-integrity-20261011.json`:96file;
chỉ `.csproj.user` metadata local IDE đổi, product source/migrations/tests không
đổi. Các raw harness FAIL vẫn lưu; lỗi đo transaction/rowversion, readiness và
modal không tự gán thành bug ứng dụng. DPI desktop đã trả125% Recommended.

## Đầu ra đã viết

| Task | Đầu ra | Kiểm tra kỹ thuật |
|---|---|---|
| T06 | Migration002; setup clean/demo/legacy; PasswordHasher, CAS rehash, session/quyền; Login/Bootstrap | Build; SQL compile; setup→login→Demand trên DB test riêng |
| T07 | MainForm, ApplicationSessionContext, AppTheme, lazy navigation, lifecycle/nháp và trạng thái | Build; smoke mở/đóng Login khi thiếu cấu hình ở lượt T07 |
| T08 | Migration003; DTO/request7bảng, DAL/BLL/lookup/payment selector, CatalogsControl/editor | SQL compile; dependency smoke tạo/đọc/sửaDM và selector PT001/002 |

Hồ sơ contract: [auth](../../database/auth-contract.md),
[catalog](../../database/catalog-contract.md),
[migrations](../../database/migrations.md). Baseline/seed/001 giữ nguyên.

Evidence kỹ thuật cuối tại `runs/technical_20261009_212048_828af430/`:
Debug/Release 0 warning/0 error, migrations001→002→003 và dependency-smoke.
DB `CoffeeManager_Test_G02_20261009_212048_828af430` có marker, không seed;
password tạo trong RAM, không log. Smoke dùng Windows identity người triển khai,
không chứng minh principal hạn chế, concurrency/rollback/race hay native UI.
Capture manifest ban đầu có lỗi regex sau khi các lệnh kỹ thuật đã thành công;
đã lưu lại manifest riêng, ghi CaptureNote trong technical.json.

Sau khi chốt hai chi tiết UI (màn bắt đầu ADMIN và căn phải cột số), đã build
lại source cuối: Debug/Release 0 warning/0 error, log và manifest tại
`runs/build_20261009_212611_777cf7c4/`. SQL/BLL của dependency smoke không đổi.

## Bộ công cụ nghiệm thu

- `tools/database/Test-G02.ps1`: tách setupError/exitCode/TRX/stdout/stderr,
  snapshot source trước/sau; DB test riêng, setup/runtime WITHOUT LOGIN và
  Pooling=false khi impersonate.
- xUnit đã viết cho auth rules, race/rollback/restart-lock, CAS password-change,
  session revocation; từng7kind CRUD/search/revision/FK-history, mã Chi được
  bảo vệ, phương thức/receipt và service-call trái quyền CASH/STORE.
- Fixtures lịch sử và order paid/open là dữ liệu DB test. Unit/integration
  đã chạy trong hai runner ghi trên; không triển khai POS/kho/lương tương lai.
- Native collector `tools/ui/Verify-G02.ps1` đã kiểm cú pháp, chưa chạy;
  không thay thao tác Designer/keyboard/DPI hoặc counts instance/event/SQL.
- [manual-test-guide.md](manual-test-guide.md) chỉ rõ nhánh cần fixture/thao tác
  bổ sung; bộ xUnit không thay mọi subcase bắt buộc.

## Phạm vi đợt test đã được duyệt

| Task | ID gốc | Phạm vi bắt buộc |
|---|---|---|
| T06 | K01–K10 (10) | Bootstrap clean/demo/legacy, concurrent/rollback/restart; auth/rehash race/username/collation; quyền/session/logout/DB errors/secret |
| T07 | K01–K07 (7) |3role, một Main/phiên;100navigation/10login-logout và tài nguyên;2viewport×3DPI; Designer save-close-reopen, keyboard/loading/late async/nháp/hủy-lưu |
| T08 | K01–K10 (10) | CRUD/search/validation/biên/FK của từng7bảng;3role×4thao tác và lookup-fields; migration/rerun/preflight; protected rows/payment selector; stale/unknown/receipt/snapshot |

Mỗi subcase có expected/actual/status/evidence riêng theo ba plan đã duyệt;
test lần lượt T06→T07→T08, giữ source/migration cố định. Biên bản tại task,
báo cáo chung kể cả FAIL/BLOCKED. Bug ghi task sở hữu, gom đợt để duyệt debug/
regression. Không đánh dấu Hoàn tất từ build/smoke hoặc số test tự động.

Bổ sung 18/10/2026: đã provision DB development cố định, profile F5 và ba account. Xem [kết quả bàn giao](development-handoff-results.md); không thay thế nghiệm thu G02/UI hoặc xử lý 112 DB test/Windows user cũ.

18/10/2026 — bàn giao Login thường, bỏ chọn account; thông tin mock có tại database/demo/README.md và accounts.json. Xem development-login-ui-results.md, mục phương án cuối. Ba role service PASS; GUI bản cuối NOT RUN.
